using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PluginConceito.Modules.InsertNotes
{
    internal sealed class InsertNotesViewModel : INotifyPropertyChanged
    {
        private bool _isUpdating;
        private bool _hasLevel1;
        private bool _hasLevel2;

        public ObservableCollection<DisciplineViewModel> Level0 { get; } = new ObservableCollection<DisciplineViewModel>();
        public ObservableCollection<DisciplineViewModel> Level1 { get; } = new ObservableCollection<DisciplineViewModel>();
        public ObservableCollection<DisciplineViewModel> Level2 { get; } = new ObservableCollection<DisciplineViewModel>();

        public bool HasLevel1
        {
            get { return _hasLevel1; }
            private set { _hasLevel1 = value; OnPropertyChanged(); }
        }

        public bool HasLevel2
        {
            get { return _hasLevel2; }
            private set { _hasLevel2 = value; OnPropertyChanged(); }
        }

        public InsertNotesViewModel()
        {
            List<DisciplineViewModel> tree = BuildTree();

            foreach (DisciplineViewModel item in tree)
            {
                Level0.Add(item);
                WireUp(item);
            }
        }

        private void WireUp(DisciplineViewModel item)
        {
            item.PropertyChanged += OnDisciplinePropertyChanged;

            foreach (DisciplineViewModel child in item.Children)
            {
                WireUp(child);
            }
        }

        private void OnDisciplinePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(DisciplineViewModel.IsChecked))
            {
                return;
            }

            if (_isUpdating)
            {
                return;
            }

            var discipline = (DisciplineViewModel)sender;

            if (Level0.Contains(discipline))
            {
                if (discipline.IsChecked)
                {
                    _isUpdating = true;

                    foreach (DisciplineViewModel sibling in Level0)
                    {
                        if (sibling != discipline)
                        {
                            sibling.IsChecked = false;
                        }
                    }

                    _isUpdating = false;

                    Level1.Clear();
                    Level2.Clear();
                    HasLevel1 = false;
                    HasLevel2 = false;

                    foreach (DisciplineViewModel child in discipline.Children)
                    {
                        Level1.Add(child);
                    }

                    HasLevel1 = Level1.Count > 0;
                }
                else
                {
                    Level1.Clear();
                    Level2.Clear();
                    HasLevel1 = false;
                    HasLevel2 = false;
                }
            }
            else if (Level1.Contains(discipline))
            {
                if (discipline.IsChecked)
                {
                    _isUpdating = true;

                    foreach (DisciplineViewModel sibling in Level1)
                    {
                        if (sibling != discipline)
                        {
                            sibling.IsChecked = false;
                        }
                    }

                    _isUpdating = false;

                    Level2.Clear();
                    HasLevel2 = false;

                    foreach (DisciplineViewModel child in discipline.Children)
                    {
                        Level2.Add(child);
                    }

                    HasLevel2 = Level2.Count > 0;
                }
                else
                {
                    Level2.Clear();
                    HasLevel2 = false;
                }
            }
            else if (Level2.Contains(discipline))
            {
                if (discipline.IsChecked)
                {
                    _isUpdating = true;

                    foreach (DisciplineViewModel sibling in Level2)
                    {
                        if (sibling != discipline)
                        {
                            sibling.IsChecked = false;
                        }
                    }

                    _isUpdating = false;
                }
            }
        }

        private static List<DisciplineViewModel> BuildTree()
        {
            var telecom = new DisciplineViewModel("TELECOM");
            telecom.Children.Add(new DisciplineViewModel("CABEAMENTO ESTRUTURADO"));
            telecom.Children.Add(new DisciplineViewModel("CFTV"));
            telecom.Children.Add(new DisciplineViewModel("TELEFONIA"));
            telecom.Children.Add(new DisciplineViewModel("INTERFONIA"));
            telecom.Children.Add(new DisciplineViewModel("CATV"));

            var eletrica = new DisciplineViewModel("ELÉTRICA E AFINS");
            eletrica.Children.Add(telecom);
            eletrica.Children.Add(new DisciplineViewModel("SPDA"));
            eletrica.Children.Add(new DisciplineViewModel("ENTRADA DE ENERGIA"));

            return new List<DisciplineViewModel>
            {
                eletrica,
                new DisciplineViewModel("HIDRAULICA"),
                new DisciplineViewModel("PPCI"),
                new DisciplineViewModel("GÁS"),
                new DisciplineViewModel("MECANICA"),
                new DisciplineViewModel("INFRAESTRUTURA"),
                new DisciplineViewModel("ESTRUTURA")
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
