using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using PluginConceito.Application.Presentation;

namespace PluginConceito.Modules.InsertNotes
{
    internal sealed class InsertNotesViewModel : ObservableObject
    {
        private readonly InsertNotesCatalog _catalog;
        private readonly List<NoteViewModel> _allNotes =
            new List<NoteViewModel>();

        private bool _isUpdatingSelection;
        private DisciplineViewModel _selectedDiscipline;
        private string _searchText;

        public InsertNotesViewModel()
            : this(new InsertNotesCatalog())
        {
        }

        internal InsertNotesViewModel(InsertNotesCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

            AddDisciplines(_catalog.GetDisciplines());
            AddExclusiveItems(
                Stages,
                _catalog.GetStages(),
                OnExclusiveItemChanged);
            AddExclusiveItems(
                Importance,
                _catalog.GetImportanceLevels(),
                OnImportanceChanged);
        }

        public ObservableCollection<DisciplineViewModel> Level0 { get; } =
            new ObservableCollection<DisciplineViewModel>();

        public ObservableCollection<DisciplineViewModel> Level1 { get; } =
            new ObservableCollection<DisciplineViewModel>();

        public ObservableCollection<DisciplineViewModel> Level2 { get; } =
            new ObservableCollection<DisciplineViewModel>();

        public ObservableCollection<DisciplineViewModel> Stages { get; } =
            new ObservableCollection<DisciplineViewModel>();

        public ObservableCollection<DisciplineViewModel> Importance { get; } =
            new ObservableCollection<DisciplineViewModel>();

        public ObservableCollection<NoteViewModel> Notes { get; } =
            new ObservableCollection<NoteViewModel>();

        public bool HasLevel1
        {
            get { return Level1.Count > 0; }
        }

        public bool HasLevel2
        {
            get { return Level2.Count > 0; }
        }

        public bool HasNotes
        {
            get { return _allNotes.Count > 0; }
        }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (!SetProperty(ref _searchText, value))
                {
                    return;
                }

                FilterNotes();
            }
        }

        public string Validate()
        {
            if (!Level0.Any(item => item.IsChecked))
            {
                return "Selecione uma disciplina.";
            }

            if (!Stages.Any(item => item.IsChecked))
            {
                return "Selecione uma etapa de projeto.";
            }

            return null;
        }

        private void AddDisciplines(
            IEnumerable<DisciplineDefinition> disciplines)
        {
            foreach (DisciplineDefinition definition in disciplines)
            {
                DisciplineViewModel discipline = CreateDisciplineViewModel(
                    definition);
                Level0.Add(discipline);
                SubscribeToDisciplineTree(discipline);
            }
        }

        private static void AddExclusiveItems(
            ICollection<DisciplineViewModel> target,
            IEnumerable<string> itemNames,
            PropertyChangedEventHandler changeHandler)
        {
            foreach (string itemName in itemNames)
            {
                var item = new DisciplineViewModel(itemName);
                target.Add(item);
                item.PropertyChanged += changeHandler;
            }
        }

        private static DisciplineViewModel CreateDisciplineViewModel(
            DisciplineDefinition definition)
        {
            var viewModel = new DisciplineViewModel(definition.Name);
            foreach (DisciplineDefinition child in definition.Children)
            {
                viewModel.Children.Add(CreateDisciplineViewModel(child));
            }

            return viewModel;
        }

        private void SubscribeToDisciplineTree(DisciplineViewModel discipline)
        {
            discipline.PropertyChanged += OnDisciplineChanged;

            foreach (DisciplineViewModel child in discipline.Children)
            {
                SubscribeToDisciplineTree(child);
            }
        }

        private void OnDisciplineChanged(
            object sender,
            PropertyChangedEventArgs eventArgs)
        {
            if (!IsSelectionChange(eventArgs) || _isUpdatingSelection)
            {
                return;
            }

            var discipline = (DisciplineViewModel)sender;
            if (Level0.Contains(discipline))
            {
                HandleLevel0Change(discipline);
            }
            else if (Level1.Contains(discipline))
            {
                HandleLevel1Change(discipline);
            }
            else if (Level2.Contains(discipline))
            {
                HandleLevel2Change(discipline);
            }
        }

        private void HandleLevel0Change(DisciplineViewModel discipline)
        {
            ClearLevel(Level2, nameof(HasLevel2));

            if (!discipline.IsChecked)
            {
                ClearLevel(Level1, nameof(HasLevel1));
                _selectedDiscipline = null;
                ClearNotes();
                return;
            }

            SelectExclusively(Level0, discipline);
            ReplaceLevel(Level1, discipline.Children, nameof(HasLevel1));
            _selectedDiscipline = discipline;
            RefreshNotes();
        }

        private void HandleLevel1Change(DisciplineViewModel discipline)
        {
            ClearLevel(Level2, nameof(HasLevel2));

            if (!discipline.IsChecked)
            {
                _selectedDiscipline = FindSelected(Level0);
                RefreshNotes();
                return;
            }

            SelectExclusively(Level1, discipline);
            ReplaceLevel(Level2, discipline.Children, nameof(HasLevel2));
            _selectedDiscipline = discipline;
            RefreshNotes();
        }

        private void HandleLevel2Change(DisciplineViewModel discipline)
        {
            if (discipline.IsChecked)
            {
                SelectExclusively(Level2, discipline);
                _selectedDiscipline = discipline;
            }
            else
            {
                _selectedDiscipline = FindSelected(Level1);
            }

            RefreshNotes();
        }

        private void OnExclusiveItemChanged(
            object sender,
            PropertyChangedEventArgs eventArgs)
        {
            if (!IsSelectionChange(eventArgs) || _isUpdatingSelection)
            {
                return;
            }

            var selectedItem = (DisciplineViewModel)sender;
            if (selectedItem.IsChecked)
            {
                SelectExclusively(Stages, selectedItem);
            }
        }

        private void OnImportanceChanged(
            object sender,
            PropertyChangedEventArgs eventArgs)
        {
            if (!IsSelectionChange(eventArgs) || _isUpdatingSelection)
            {
                return;
            }

            var selectedItem = (DisciplineViewModel)sender;
            if (selectedItem.IsChecked)
            {
                SelectExclusively(Importance, selectedItem);
            }

            RefreshNotes();
        }

        private void SelectExclusively(
            IEnumerable<DisciplineViewModel> items,
            DisciplineViewModel selectedItem)
        {
            _isUpdatingSelection = true;
            try
            {
                foreach (DisciplineViewModel item in items)
                {
                    if (!ReferenceEquals(item, selectedItem))
                    {
                        item.IsChecked = false;
                    }
                }
            }
            finally
            {
                _isUpdatingSelection = false;
            }
        }

        private void ReplaceLevel(
            ObservableCollection<DisciplineViewModel> level,
            IEnumerable<DisciplineViewModel> items,
            string visibilityProperty)
        {
            ClearSelection(level);
            level.Clear();
            foreach (DisciplineViewModel item in items)
            {
                level.Add(item);
            }

            OnPropertyChanged(visibilityProperty);
        }

        private void ClearLevel(
            ObservableCollection<DisciplineViewModel> level,
            string visibilityProperty)
        {
            if (level.Count == 0)
            {
                return;
            }

            ClearSelection(level);
            level.Clear();
            OnPropertyChanged(visibilityProperty);
        }

        private void ClearSelection(
            IEnumerable<DisciplineViewModel> items)
        {
            bool wasUpdating = _isUpdatingSelection;
            _isUpdatingSelection = true;
            try
            {
                foreach (DisciplineViewModel item in items)
                {
                    item.IsChecked = false;
                }
            }
            finally
            {
                _isUpdatingSelection = wasUpdating;
            }
        }

        private void RefreshNotes()
        {
            bool hasImportance = Importance.Any(item => item.IsChecked);
            if (_selectedDiscipline == null || !hasImportance)
            {
                ClearNotes();
                return;
            }

            LoadNotes(_selectedDiscipline.Name);
        }

        private void LoadNotes(string disciplineName)
        {
            _allNotes.Clear();
            foreach (string noteName in _catalog.GetNotes(disciplineName))
            {
                _allNotes.Add(new NoteViewModel(noteName));
            }

            ResetSearch();
            FilterNotes();
            OnPropertyChanged(nameof(HasNotes));
        }

        private void ClearNotes()
        {
            Notes.Clear();
            _allNotes.Clear();
            ResetSearch();
            OnPropertyChanged(nameof(HasNotes));
        }

        private void ResetSearch()
        {
            if (_searchText == null)
            {
                return;
            }

            _searchText = null;
            OnPropertyChanged(nameof(SearchText));
        }

        private void FilterNotes()
        {
            Notes.Clear();
            string[] terms = GetSearchTerms(_searchText);

            foreach (NoteViewModel note in _allNotes)
            {
                if (terms.Length == 0 || terms.Any(term =>
                    note.Name.IndexOf(
                        term,
                        StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    Notes.Add(note);
                }
            }
        }

        private static string[] GetSearchTerms(string searchText)
        {
            return (searchText ?? string.Empty)
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static DisciplineViewModel FindSelected(
            IEnumerable<DisciplineViewModel> items)
        {
            return items.FirstOrDefault(item => item.IsChecked);
        }

        private static bool IsSelectionChange(
            PropertyChangedEventArgs eventArgs)
        {
            return eventArgs.PropertyName ==
                nameof(DisciplineViewModel.IsChecked);
        }
    }
}
