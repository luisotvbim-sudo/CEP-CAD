using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PluginConceito.Application.Contracts;
using ZwSoft.ZwCAD.ApplicationServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotFolhasSessionService
    {
        private readonly IZwcadContext _zwcad;
        private readonly FolhaScanner _scanner;
        private readonly ArquivoNomeService _nameService;
        private readonly FolhaNomenclaturaService _nomenclatureService;
        private readonly PlotService _plotService;

        public PlotFolhasSessionService(
            IZwcadContext zwcad,
            FolhaScanner scanner,
            ArquivoNomeService nameService,
            FolhaNomenclaturaService nomenclatureService,
            PlotService plotService)
        {
            _zwcad = zwcad ?? throw new ArgumentNullException(nameof(zwcad));
            _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
            _nameService = nameService ?? throw new ArgumentNullException(nameof(nameService));
            _nomenclatureService = nomenclatureService ?? throw new ArgumentNullException(nameof(nomenclatureService));
            _plotService = plotService ?? throw new ArgumentNullException(nameof(plotService));
        }

        public PlotFolhasSession Create()
        {
            IReadOnlyList<FolhaInfo> sheets = _scanner.ScanActiveLayout();
            Document document = _zwcad.ActiveDocument;
            if (sheets.Count == 0)
            {
                return PlotFolhasSession.Empty(document);
            }

            string baseName = GetDefaultBaseName(document);
            _nomenclatureService.LoadSavedNames(document, sheets);
            foreach (FolhaInfo sheet in sheets)
            {
                sheet.NomeArquivo = string.IsNullOrWhiteSpace(sheet.NomeArquivo)
                    ? _nameService.BuildAutomaticName(baseName, sheet)
                    : _nameService.NormalizeManualName(sheet.NomeArquivo);
            }
            _nameService.ValidateNames(sheets);

            IReadOnlyList<string> devices = _plotService.GetPlotDevices();
            IReadOnlyList<string> styles = _plotService.GetPlotStyleSheets();
            ParsedName parsedName = new NamingStandardParser().Parse(sheets.First().NomeArquivo);
            bool useAutomaticEmissionFolder;
            string outputFolder = GetDefaultOutputFolder(document, out useAutomaticEmissionFolder);
            return new PlotFolhasSession(
                document,
                sheets,
                devices,
                styles,
                outputFolder,
                useAutomaticEmissionFolder,
                _plotService.GetDefaultPlotDevice(devices),
                _plotService.GetDefaultPlotStyle(styles),
                parsedName.Separator,
                parsedName.Parts);
        }

        private static string GetDefaultBaseName(Document document)
        {
            if (document == null || string.IsNullOrWhiteSpace(document.Name)) return "Projeto";
            string fileName = Path.GetFileNameWithoutExtension(document.Name);
            return string.IsNullOrWhiteSpace(fileName) ? "Projeto" : fileName;
        }

        private static string GetDefaultOutputFolder(Document document, out bool useAutomaticEmissionFolder)
        {
            useAutomaticEmissionFolder = false;
            if (document != null && !string.IsNullOrWhiteSpace(document.Name))
            {
                string folder = Path.GetDirectoryName(document.Name);
                if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
                {
                    useAutomaticEmissionFolder = true;
                    return folder;
                }
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
    }

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
            IReadOnlyList<string> namingParts)
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
        public bool HasSheets { get { return Sheets.Count > 0; } }

        public static PlotFolhasSession Empty(Document document)
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
                new List<string>());
        }
    }
}
