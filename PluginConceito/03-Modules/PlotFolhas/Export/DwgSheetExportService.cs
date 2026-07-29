using System;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class DwgSheetExportService
    {
        private readonly DwgDatabaseCloner _databaseCloner;
        private readonly DwgLayoutIsolator _layoutIsolator;
        private readonly ViewportModelIsolator _modelIsolator;
        private readonly ModelSpaceSheetIsolator _modelSpaceSheetIsolator;
        private readonly DwgModelOpeningViewService _modelOpeningViewService;

        public DwgSheetExportService(FolhaFormatCatalog formats)
        {
            _databaseCloner = new DwgDatabaseCloner();
            _layoutIsolator = new DwgLayoutIsolator(formats);
            _modelIsolator = new ViewportModelIsolator();
            _modelSpaceSheetIsolator = new ModelSpaceSheetIsolator(formats);
            _modelOpeningViewService = new DwgModelOpeningViewService();
        }

        public void Export(
            Document document,
            FolhaInfo sheet,
            string outputPath,
            bool overwriteExisting,
            Action<string> report)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Arquivo de saída obrigatório.", nameof(outputPath));

            report = report ?? delegate { };

            ObjectId baseViewportId;
            using (var output = new DwgOutputFile(document.Name, outputPath, overwriteExisting))
            using (Database database = _databaseCloner.Clone(
                document,
                sheet.IsModelSpace ? null : sheet.LayoutName,
                out baseViewportId))
            {
                if (sheet.IsModelSpace)
                {
                    ExportModelSpace(database, sheet, report);
                }
                else
                {
                    ExportLayout(database, sheet, baseViewportId, report);
                }

                database.SaveAs(output.TemporaryPath, DwgVersion.Current);
                output.Publish();
                output.VerifyPublished();
            }

            report("DWG folha " + sheet.Sequencia + ": arquivo gerado.");
        }

        private void ExportLayout(
            Database database,
            FolhaInfo sheet,
            ObjectId baseViewportId,
            Action<string> report)
        {
            DwgLayoutIsolationResult layout =
                _layoutIsolator.Isolate(
                    database,
                    sheet,
                    baseViewportId);
            ReportLayout(sheet, layout, report);

            ModelIsolationResult model = _modelIsolator.Isolate(
                database,
                sheet.LayoutName,
                baseViewportId);
            ReportViewportModel(sheet, model, report);

            _layoutIsolator.PrepareOpeningView(
                database,
                sheet,
                baseViewportId);
            report(
                "DWG folha " + sheet.Sequencia +
                ": vista inicial centralizada no Layout.");
        }

        private void ExportModelSpace(
            Database database,
            FolhaInfo sheet,
            Action<string> report)
        {
            ModelSpaceSheetIsolationResult result =
                _modelSpaceSheetIsolator.Isolate(database, sheet);
            report(string.Format(
                "DWG folha {0}: Model limpo por região; mantidos={1}, apagados={2}.",
                sheet.Sequencia,
                result.EntitiesKept,
                result.EntitiesErased));

            _modelOpeningViewService.Prepare(database, sheet);
            report(
                "DWG folha " + sheet.Sequencia +
                ": vista inicial centralizada no Model.");
        }

        private static void ReportLayout(
            FolhaInfo sheet,
            DwgLayoutIsolationResult result,
            Action<string> report)
        {
            report(string.Format(
                "DWG folha {0}: Layout isolado; mantidos={1}, apagados={2}, viewports={3}.",
                sheet.Sequencia,
                result.EntitiesKept,
                result.EntitiesErased,
                result.ModelViewportsKept));
        }

        private static void ReportViewportModel(
            FolhaInfo sheet,
            ModelIsolationResult result,
            Action<string> report)
        {
            if (result.ViewportsConsidered == 0)
            {
                report(string.Format(
                    "DWG folha {0}: Model esvaziado; a folha não possui viewport de Model ativa, apagados={1}.",
                    sheet.Sequencia,
                    result.EntitiesErased));
                return;
            }

            report(string.Format(
                "DWG folha {0}: Model isolado; viewports={1}, mantidos={2}, apagados={3}, invisíveis por layer/viewport={4}, curvas cortadas={5}, trechos criados={6}, falhas de corte={7}, blocos/Xrefs mantidos={8}.",
                sheet.Sequencia,
                result.ViewportsConsidered,
                result.EntitiesKept,
                result.EntitiesErased,
                result.EntitiesErasedByVisibility,
                result.CurvesSplit,
                result.CurvePiecesCreated,
                result.CurvesNotSplit,
                result.BlockReferencesKept));
        }
    }
}
