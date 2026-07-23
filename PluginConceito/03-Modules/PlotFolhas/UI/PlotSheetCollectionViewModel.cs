using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using PluginConceito.Application.Presentation;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotSheetCollectionViewModel : ObservableObject
    {
        private string _searchText;
        private bool _showOnlyIssues;

        public PlotSheetCollectionViewModel(IEnumerable<FolhaInfo> sheets)
        {
            Items = new ObservableCollection<FolhaInfo>(
                sheets ?? Enumerable.Empty<FolhaInfo>());
            View = CollectionViewSource.GetDefaultView(Items);
            View.Filter = Filter;

            foreach (FolhaInfo sheet in Items)
                sheet.PropertyChanged += OnSheetPropertyChanged;
        }

        public ObservableCollection<FolhaInfo> Items { get; }

        public ICollectionView View { get; }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (!SetProperty(ref _searchText, value)) return;
                RefreshFilter();
            }
        }

        public bool ShowOnlyIssues
        {
            get { return _showOnlyIssues; }
            set
            {
                if (!SetProperty(ref _showOnlyIssues, value)) return;
                RefreshFilter();
            }
        }

        public int TotalCount => Items.Count;

        public int VisibleCount => View == null
            ? Items.Count
            : View.Cast<object>().Count();

        public int PdfCount => Items.Count(sheet => sheet.Plotar);

        public int DwgCount => Items.Count(sheet => sheet.GerarDwg);

        public int IssueCount => Items.Count(
            sheet => !sheet.Valida || sheet.Avisos.Count > 0);

        public void SetAllPdf(bool value)
        {
            foreach (FolhaInfo sheet in Items) sheet.Plotar = value;
            RefreshSummary();
        }

        public void SetAllDwg(bool value)
        {
            foreach (FolhaInfo sheet in Items) sheet.GerarDwg = value;
            RefreshSummary();
        }

        public void Refresh()
        {
            View.Refresh();
            RefreshSummary();
        }

        private bool Filter(object value)
        {
            var sheet = value as FolhaInfo;
            if (sheet == null) return false;
            if (ShowOnlyIssues && sheet.Valida && sheet.Avisos.Count == 0) return false;
            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            string term = SearchText.Trim();
            return Contains(sheet.NomeArquivo, term) ||
                Contains(sheet.Formato, term) ||
                Contains(sheet.Status, term) ||
                sheet.Sequencia.ToString().IndexOf(
                    term,
                    StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void OnSheetPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FolhaInfo.Plotar) ||
                e.PropertyName == nameof(FolhaInfo.GerarDwg) ||
                e.PropertyName == nameof(FolhaInfo.Status) ||
                e.PropertyName == nameof(FolhaInfo.Valida))
            {
                RefreshSummary();
            }
        }

        private void RefreshFilter()
        {
            View.Refresh();
            OnPropertyChanged(nameof(VisibleCount));
        }

        private void RefreshSummary()
        {
            OnPropertyChanged(nameof(PdfCount));
            OnPropertyChanged(nameof(DwgCount));
            OnPropertyChanged(nameof(IssueCount));
            OnPropertyChanged(nameof(VisibleCount));
        }

        private static bool Contains(string value, string term)
        {
            return (value ?? string.Empty).IndexOf(
                term,
                StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
