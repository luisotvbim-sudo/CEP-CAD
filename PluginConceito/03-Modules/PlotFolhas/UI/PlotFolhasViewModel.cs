using System.Collections.Generic;
using PluginConceito.Application.Presentation;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotFolhasViewModel : ObservableObject
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
            set { SetProperty(ref _statusMessage, value ?? string.Empty); }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            set { SetProperty(ref _isBusy, value); }
        }

        public FolhaInfo SelectedSheet
        {
            get { return _selectedSheet; }
            set { SetProperty(ref _selectedSheet, value); }
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
