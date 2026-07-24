using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PluginConceito.Application.Contracts;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotFolhasSessionService
    {
        private readonly IZwcadContext _zwcad;
        private readonly FolhaScanner _scanner;
        private readonly ArquivoNomeService _nameService;
        private readonly FolhaNomenclaturaService _nomenclatureService;
        private readonly PlotService _plotService;
        private readonly NamingStandardParser _namingParser;

        public PlotFolhasSessionService(
            IZwcadContext zwcad,
            FolhaScanner scanner,
            ArquivoNomeService nameService,
            FolhaNomenclaturaService nomenclatureService,
            PlotService plotService,
            NamingStandardParser namingParser)
        {
            _zwcad = zwcad ?? throw new ArgumentNullException(nameof(zwcad));
            _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
            _nameService = nameService ?? throw new ArgumentNullException(nameof(nameService));
            _nomenclatureService = nomenclatureService ?? throw new ArgumentNullException(nameof(nomenclatureService));
            _plotService = plotService ?? throw new ArgumentNullException(nameof(plotService));
            _namingParser = namingParser ?? throw new ArgumentNullException(nameof(namingParser));
        }

        public PlotFolhasSession Create()
        {
            Document document = _zwcad.ActiveDocument;
            string sourceLayoutName = LayoutManager.Current.CurrentLayout;
            SheetSpaceKind sourceSpace = GetSourceSpace(document, sourceLayoutName);
            IReadOnlyList<FolhaInfo> sheets = _scanner.ScanActiveSpace();
            if (sheets.Count == 0)
            {
                return PlotFolhasSession.Empty(
                    document,
                    sourceSpace,
                    sourceLayoutName);
            }

            string baseName = GetDefaultBaseName(document);
            _nomenclatureService.LoadSavedNames(document, sheets);
            foreach (FolhaInfo sheet in sheets)
            {
                sheet.NomeArquivo = string.IsNullOrWhiteSpace(sheet.NomeArquivo)
                    ? _nameService.BuildAutomaticName(baseName)
                    : _nameService.NormalizeManualName(sheet.NomeArquivo);
            }
            _nameService.ValidateNames(sheets);

            IReadOnlyList<string> devices = _plotService.GetPlotDevices();
            IReadOnlyList<string> styles = _plotService.GetPlotStyleSheets();
            ParsedName parsedName = _namingParser.Parse(sheets.First().NomeArquivo);
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
                parsedName.Parts,
                sourceSpace,
                sourceLayoutName);
        }

        private static SheetSpaceKind GetSourceSpace(
            Document document,
            string layoutName)
        {
            if (document == null)
            {
                return string.Equals(
                    layoutName,
                    "Model",
                    StringComparison.OrdinalIgnoreCase)
                    ? SheetSpaceKind.Model
                    : SheetSpaceKind.Layout;
            }

            using (Transaction transaction =
                document.Database.TransactionManager.StartTransaction())
            {
                ObjectId layoutId = LayoutManager.Current.GetLayoutId(
                    layoutName);
                var layout = (Layout)transaction.GetObject(
                    layoutId,
                    OpenMode.ForRead);
                SheetSpaceKind result = layout.ModelType
                    ? SheetSpaceKind.Model
                    : SheetSpaceKind.Layout;
                transaction.Commit();
                return result;
            }
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
}
