using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PluginConceito.Modules.InsertNotes
{
    internal sealed class DisciplineViewModel : INotifyPropertyChanged
    {
        private bool _isChecked;
        private bool _isVisible;

        public string Name { get; }

        public int Level { get; }

        public List<DisciplineViewModel> Children { get; } = new List<DisciplineViewModel>();

        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked == value)
                {
                    return;
                }

                _isChecked = value;
                OnPropertyChanged();

                foreach (DisciplineViewModel child in Children)
                {
                    child.IsVisible = value;
                }
            }
        }

        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                if (_isVisible == value)
                {
                    return;
                }

                _isVisible = value;
                OnPropertyChanged();

                if (!value)
                {
                    _isChecked = false;
                    OnPropertyChanged(nameof(IsChecked));

                    foreach (DisciplineViewModel child in Children)
                    {
                        child.IsVisible = false;
                    }
                }
            }
        }

        public Thickness IndentMargin
        {
            get { return new Thickness(Level * 24, 2, 0, 2); }
        }

        public DisciplineViewModel(string name, int level)
        {
            Name = name;
            Level = level;
            _isVisible = level == 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
