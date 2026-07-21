using System.Collections.Generic;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotFolhasViewModel : ObservableViewModel
    {
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
            IReadOnlyList<string> namingParts,
            IEnumerable<string> stampBlockNames)
        {
            SheetCollection = new PlotSheetCollectionViewModel(sheets);
            Output = new PlotOutputOptionsViewModel(
                devices,
                plotStyles,
                defaultOutputFolder,
                useAutomaticEmissionFolder,
                defaultDevice,
                defaultPlotStyle);
            NamingStructure = new NamingStructureViewModel(
                namingSeparator,
                namingParts);
            StampSelection = new StampSelectionViewModel(stampBlockNames);
            StatusMessage = "Revise as folhas e configure os arquivos de saída.";
        }

        public PlotSheetCollectionViewModel SheetCollection { get; }

        public PlotOutputOptionsViewModel Output { get; }

        public NamingStructureViewModel NamingStructure { get; }

        public StampSelectionViewModel StampSelection { get; }

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

        public void AddNamingPart()
        {
            string error = NamingStructure.TryAddPart();
            if (!string.IsNullOrWhiteSpace(error)) StatusMessage = error;
        }

        public void RemoveNamingPart()
        {
            string error = NamingStructure.TryRemovePart();
            if (!string.IsNullOrWhiteSpace(error)) StatusMessage = error;
        }
    }
}
