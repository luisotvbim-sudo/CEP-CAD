using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotFolhasViewModel : INotifyPropertyChanged
    {
        private string _searchText;
        private bool _showOnlyIssues;
        private string _outputFolder;
        private readonly string _automaticEmissionBaseFolder;
        private bool _outputFolderChosenByUser;
        private string _deviceName;
        private string _ctbName;
        private bool _overwriteExisting;
        private string _namingSeparator = "-";
        private string _statusMessage;
        private bool _isBusy;
        private FolhaInfo _selectedSheet;

        public PlotFolhasViewModel(
            IEnumerable<FolhaInfo> sheets,
            IEnumerable<string> devices,
            IEnumerable<string> plotStyles,
            string defaultOutputFolder,
            bool useAutomaticEmissionFolder,
            string defaultDevice,
            string defaultPlotStyle,
            string namingSeparator,
            IReadOnlyList<string> namingParts)
        {
            Sheets = new ObservableCollection<FolhaInfo>(sheets ?? Enumerable.Empty<FolhaInfo>());
            Devices = new ObservableCollection<string>((devices ?? Enumerable.Empty<string>()).Where(value => value != null));
            PlotStyles = new ObservableCollection<string>((plotStyles ?? Enumerable.Empty<string>()).Where(value => value != null));
            NamingParts = new ObservableCollection<NamingPartViewModel>();

            _outputFolder = defaultOutputFolder ?? string.Empty;
            _automaticEmissionBaseFolder = useAutomaticEmissionFolder ? _outputFolder : null;
            DeviceName = SelectValue(Devices, defaultDevice);
            CtbName = SelectValue(PlotStyles, defaultPlotStyle);
            StatusMessage = "Revise as folhas e configure os arquivos de saída.";

            LoadNamingStructure(namingSeparator, namingParts);
            SheetsView = CollectionViewSource.GetDefaultView(Sheets);
            SheetsView.Filter = FilterSheet;

            foreach (FolhaInfo sheet in Sheets)
            {
                sheet.PropertyChanged += OnSheetPropertyChanged;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<FolhaInfo> Sheets { get; }

        public ICollectionView SheetsView { get; }

        public ObservableCollection<string> Devices { get; }

        public ObservableCollection<string> PlotStyles { get; }

        public ObservableCollection<NamingPartViewModel> NamingParts { get; }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (!SetField(ref _searchText, value, nameof(SearchText))) return;
                SheetsView.Refresh();
                RaisePropertyChanged(nameof(VisibleSheetCount));
            }
        }

        public bool ShowOnlyIssues
        {
            get { return _showOnlyIssues; }
            set
            {
                if (!SetField(ref _showOnlyIssues, value, nameof(ShowOnlyIssues))) return;
                SheetsView.Refresh();
                RaisePropertyChanged(nameof(VisibleSheetCount));
            }
        }

        public string OutputFolder
        {
            get { return _outputFolder; }
            set
            {
                if (!SetField(ref _outputFolder, value, nameof(OutputFolder))) return;
                _outputFolderChosenByUser = true;
                RaisePropertyChanged(nameof(UseAutomaticEmissionFolder));
            }
        }

        public bool UseAutomaticEmissionFolder
        {
            get { return !_outputFolderChosenByUser && !string.IsNullOrWhiteSpace(_automaticEmissionBaseFolder); }
        }

        public string AutomaticEmissionBaseFolder { get { return _automaticEmissionBaseFolder; } }

        public string DeviceName
        {
            get { return _deviceName; }
            set { SetField(ref _deviceName, value, nameof(DeviceName)); }
        }

        public string CtbName
        {
            get { return _ctbName; }
            set { SetField(ref _ctbName, value, nameof(CtbName)); }
        }

        public bool OverwriteExisting
        {
            get { return _overwriteExisting; }
            set { SetField(ref _overwriteExisting, value, nameof(OverwriteExisting)); }
        }

        public string NamingSeparator
        {
            get { return _namingSeparator; }
            set
            {
                string normalized = string.IsNullOrEmpty(value) ? string.Empty : value.Substring(0, 1);
                SetField(ref _namingSeparator, normalized, nameof(NamingSeparator));
            }
        }

        public string StatusMessage
        {
            get { return _statusMessage; }
            set { SetField(ref _statusMessage, value ?? string.Empty, nameof(StatusMessage)); }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            set { SetField(ref _isBusy, value, nameof(IsBusy)); }
        }

        public FolhaInfo SelectedSheet
        {
            get { return _selectedSheet; }
            set { SetField(ref _selectedSheet, value, nameof(SelectedSheet)); }
        }

        public int TotalSheetCount { get { return Sheets.Count; } }

        public int VisibleSheetCount { get { return SheetsView == null ? Sheets.Count : SheetsView.Cast<object>().Count(); } }

        public int PdfCount { get { return Sheets.Count(sheet => sheet.Plotar); } }

        public int DwgCount { get { return Sheets.Count(sheet => sheet.GerarDwg); } }

        public int IssueCount { get { return Sheets.Count(sheet => !sheet.Valida || sheet.Avisos.Count > 0); } }

        public IReadOnlyList<string> GetNamingParts()
        {
            return NamingParts.Select(part => part.Value ?? string.Empty).ToList();
        }

        public void ChooseOutputFolder(string outputFolder)
        {
            _outputFolderChosenByUser = true;
            SetField(ref _outputFolder, outputFolder ?? string.Empty, nameof(OutputFolder));
            RaisePropertyChanged(nameof(UseAutomaticEmissionFolder));
        }

        public void SetResolvedOutputFolder(string outputFolder)
        {
            SetField(ref _outputFolder, outputFolder ?? string.Empty, nameof(OutputFolder));
        }

        public void AddNamingPart()
        {
            if (NamingParts.Count >= NamingHeader.MaximumParts)
            {
                StatusMessage = "A estrutura aceita no máximo " + NamingHeader.MaximumParts + " campos.";
                return;
            }

            NamingParts.Add(new NamingPartViewModel(NamingParts.Count + 1, string.Empty));
        }

        public void RemoveNamingPart()
        {
            const int minimumParts = 4;
            if (NamingParts.Count <= minimumParts)
            {
                StatusMessage = "A estrutura deve ter pelo menos " + minimumParts + " campos.";
                return;
            }

            NamingParts.RemoveAt(NamingParts.Count - 1);
        }

        public void SetAllPdf(bool value)
        {
            foreach (FolhaInfo sheet in Sheets) sheet.Plotar = value;
            RefreshSummary();
        }

        public void SetAllDwg(bool value)
        {
            foreach (FolhaInfo sheet in Sheets) sheet.GerarDwg = value;
            RefreshSummary();
        }

        public void Refresh()
        {
            SheetsView.Refresh();
            RefreshSummary();
        }

        private void LoadNamingStructure(string separator, IReadOnlyList<string> parts)
        {
            IReadOnlyList<string> values = parts ?? new List<string>();
            NamingSeparator = string.IsNullOrEmpty(separator) ? "-" : separator;

            int count = Math.Max(4, values.Count);
            count = Math.Min(NamingHeader.MaximumParts, count);
            for (int index = 0; index < count; index++)
            {
                string value = index < values.Count ? values[index] : string.Empty;
                NamingParts.Add(new NamingPartViewModel(index + 1, value));
            }
        }

        private bool FilterSheet(object value)
        {
            FolhaInfo sheet = value as FolhaInfo;
            if (sheet == null) return false;
            if (ShowOnlyIssues && sheet.Valida && sheet.Avisos.Count == 0) return false;
            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            string term = SearchText.Trim();
            return Contains(sheet.NomeArquivo, term) ||
                   Contains(sheet.Formato, term) ||
                   Contains(sheet.Status, term) ||
                   sheet.Sequencia.ToString().IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool Contains(string value, string term)
        {
            return (value ?? string.Empty).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string SelectValue(IEnumerable<string> values, string preferred)
        {
            List<string> available = values.ToList();
            string match = available.FirstOrDefault(value => string.Equals(value, preferred, StringComparison.OrdinalIgnoreCase));
            return match ?? available.FirstOrDefault();
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

        private void RefreshSummary()
        {
            RaisePropertyChanged(nameof(PdfCount));
            RaisePropertyChanged(nameof(DwgCount));
            RaisePropertyChanged(nameof(IssueCount));
            RaisePropertyChanged(nameof(VisibleSheetCount));
        }

        private bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            RaisePropertyChanged(propertyName);
            return true;
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    internal sealed class NamingPartViewModel : INotifyPropertyChanged
    {
        private string _value;

        public NamingPartViewModel(int position, string value)
        {
            Position = position;
            _value = value ?? string.Empty;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public int Position { get; }

        public string Value
        {
            get { return _value; }
            set
            {
                string normalized = value ?? string.Empty;
                if (normalized.Length > 6) normalized = normalized.Substring(0, 6);
                if (string.Equals(_value, normalized, StringComparison.Ordinal)) return;
                _value = normalized;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }
    }
}
