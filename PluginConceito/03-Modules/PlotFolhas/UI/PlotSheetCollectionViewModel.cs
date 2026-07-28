using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Threading;
using PluginConceito.Application.Presentation;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotSheetCollectionViewModel : ObservableObject
    {
        private readonly PlotSheetFilter _filter;
        private readonly DeferredUiAction _deferredFilterRefresh;
        private bool _bulkSelectionUpdate;

        public PlotSheetCollectionViewModel(IEnumerable<FolhaInfo> sheets)
        {
            _filter = new PlotSheetFilter();
            _deferredFilterRefresh = new DeferredUiAction(
                Dispatcher.CurrentDispatcher,
                DispatcherPriority.Background);
            Items = new ObservableCollection<FolhaInfo>(
                sheets ?? Enumerable.Empty<FolhaInfo>());
            View = CollectionViewSource.GetDefaultView(Items);
            View.Filter = _filter.Matches;

            foreach (FolhaInfo sheet in Items)
                sheet.PropertyChanged += OnSheetPropertyChanged;
        }

        public ObservableCollection<FolhaInfo> Items { get; }

        public ICollectionView View { get; }

        public string SearchText
        {
            get { return _filter.SearchText; }
            set
            {
                if (string.Equals(
                    _filter.SearchText,
                    value,
                    StringComparison.Ordinal))
                {
                    return;
                }

                _filter.SearchText = value;
                OnPropertyChanged(nameof(SearchText));
                RefreshFilter();
            }
        }

        public bool ShowOnlyIssues
        {
            get { return _filter.ShowOnlyIssues; }
            set
            {
                if (_filter.ShowOnlyIssues == value) return;
                _filter.ShowOnlyIssues = value;
                OnPropertyChanged(nameof(ShowOnlyIssues));
                QueueFilterRefresh();
            }
        }

        public bool ShowOnlyRevisionUpdates
        {
            get { return _filter.ShowOnlyRevisionUpdates; }
            set
            {
                if (_filter.ShowOnlyRevisionUpdates == value) return;
                _filter.ShowOnlyRevisionUpdates = value;
                OnPropertyChanged(nameof(ShowOnlyRevisionUpdates));
                QueueFilterRefresh();
            }
        }

        public bool ShowOnlySelectedOutputs
        {
            get { return _filter.ShowOnlySelectedOutputs; }
            set
            {
                if (_filter.ShowOnlySelectedOutputs == value) return;
                _filter.ShowOnlySelectedOutputs = value;
                OnPropertyChanged(nameof(ShowOnlySelectedOutputs));
                QueueFilterRefresh();
            }
        }

        public bool AllPdfSelected
        {
            get { return Items.Count > 0 && Items.All(sheet => sheet.Plotar); }
            set { SetAllPdf(value); }
        }

        public bool AllDwgSelected
        {
            get { return Items.Count > 0 && Items.All(sheet => sheet.GerarDwg); }
            set { SetAllDwg(value); }
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
            SetBulkSelection(sheet => sheet.Plotar = value);
        }

        public void SetAllDwg(bool value)
        {
            SetBulkSelection(sheet => sheet.GerarDwg = value);
        }

        private void SetBulkSelection(Action<FolhaInfo> update)
        {
            _bulkSelectionUpdate = true;
            try
            {
                foreach (FolhaInfo sheet in Items) update(sheet);
            }
            finally
            {
                _bulkSelectionUpdate = false;
            }

            RefreshSummary();
            if (ShowOnlySelectedOutputs) QueueFilterRefresh();
        }

        public void Refresh()
        {
            View.Refresh();
            RefreshSummary();
        }

        private void OnSheetPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_bulkSelectionUpdate &&
                (e.PropertyName == nameof(FolhaInfo.Plotar) ||
                e.PropertyName == nameof(FolhaInfo.GerarDwg)))
            {
                return;
            }

            if (e.PropertyName == nameof(FolhaInfo.Plotar) ||
                e.PropertyName == nameof(FolhaInfo.GerarDwg) ||
                e.PropertyName == nameof(FolhaInfo.SubirRevisao) ||
                e.PropertyName == nameof(FolhaInfo.Status) ||
                e.PropertyName == nameof(FolhaInfo.Valida))
            {
                RefreshSummary();
            }

            if (_filter.IsAffectedBy(e))
            {
                QueueFilterRefresh();
            }
        }

        private void QueueFilterRefresh()
        {
            _deferredFilterRefresh.Schedule(RefreshFilter);
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
            OnPropertyChanged(nameof(AllPdfSelected));
            OnPropertyChanged(nameof(AllDwgSelected));
        }

    }
}
