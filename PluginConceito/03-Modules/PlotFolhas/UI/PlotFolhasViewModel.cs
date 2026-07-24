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
            IEnumerable<string> stampBlockNames,
            SheetSpaceKind sourceSpace,
            string sourceLayoutName)
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
            SourceSpace = sourceSpace;
            SourceLayoutName = sourceLayoutName ?? string.Empty;
            StatusMessage = IsModelSpace
                ? "Sessão no Model: revise as folhas e configure as saídas."
                : "Sessão no Layout " + SourceLayoutName +
                    ": revise as folhas e configure as saídas.";
        }

        public PlotSheetCollectionViewModel SheetCollection { get; }

        public PlotOutputOptionsViewModel Output { get; }

        public NamingStructureViewModel NamingStructure { get; }

        public StampSelectionViewModel StampSelection { get; }

        public SheetSpaceKind SourceSpace { get; }

        public string SourceLayoutName { get; }

        public bool IsModelSpace
        {
            get { return SourceSpace == SheetSpaceKind.Model; }
        }

        public string SourceSubtitle
        {
            get
            {
                return IsModelSpace
                    ? "Revise, nomeie e gere as folhas encontradas no Model."
                    : "Revise, nomeie e gere as folhas encontradas no Layout " +
                        SourceLayoutName + ".";
            }
        }

        public string SourceDetail
        {
            get
            {
                return IsModelSpace
                    ? "Model"
                    : "Layout: " + SourceLayoutName;
            }
        }

        public string GenerateButtonText
        {
            get
            {
                return IsModelSpace
                    ? "Gerar arquivos do Model"
                    : "Gerar arquivos do Layout";
            }
        }

        public string ZoomToolTip
        {
            get
            {
                return IsModelSpace
                    ? "Aproximar esta folha no Model"
                    : "Aproximar esta folha no Layout";
            }
        }

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
