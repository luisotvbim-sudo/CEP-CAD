using System.Collections.Generic;
using ZwSoft.ZwCAD.ApplicationServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotFolhasSession
    {
        public PlotFolhasSession(
            Document document,
            IReadOnlyList<FolhaInfo> sheets,
            IReadOnlyList<string> devices,
            IReadOnlyList<string> plotStyles,
            string outputFolder,
            bool useAutomaticEmissionFolder,
            string defaultDevice,
            string defaultPlotStyle,
            string namingSeparator,
            IReadOnlyList<string> namingParts,
            SheetSpaceKind sourceSpace,
            string sourceLayoutName)
        {
            Document = document;
            Sheets = sheets;
            Devices = devices;
            PlotStyles = plotStyles;
            OutputFolder = outputFolder;
            UseAutomaticEmissionFolder = useAutomaticEmissionFolder;
            DefaultDevice = defaultDevice;
            DefaultPlotStyle = defaultPlotStyle;
            NamingSeparator = namingSeparator;
            NamingParts = namingParts;
            SourceSpace = sourceSpace;
            SourceLayoutName = sourceLayoutName;
        }

        public Document Document { get; }
        public IReadOnlyList<FolhaInfo> Sheets { get; }
        public IReadOnlyList<string> Devices { get; }
        public IReadOnlyList<string> PlotStyles { get; }
        public string OutputFolder { get; }
        public bool UseAutomaticEmissionFolder { get; }
        public string DefaultDevice { get; }
        public string DefaultPlotStyle { get; }
        public string NamingSeparator { get; }
        public IReadOnlyList<string> NamingParts { get; }
        public SheetSpaceKind SourceSpace { get; }
        public string SourceLayoutName { get; }
        public bool HasSheets { get { return Sheets.Count > 0; } }

        public static PlotFolhasSession Empty(
            Document document,
            SheetSpaceKind sourceSpace,
            string sourceLayoutName)
        {
            return new PlotFolhasSession(
                document,
                new List<FolhaInfo>(),
                new List<string>(),
                new List<string>(),
                string.Empty,
                false,
                null,
                null,
                "-",
                new List<string>(),
                sourceSpace,
                sourceLayoutName);
        }
    }
}
