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

        public DwgSheetExportService(FolhaFormatCatalog formats)
        {
            _databaseCloner = new DwgDatabaseCloner();
            _layoutIsolator = new DwgLayoutIsolator(formats);
            _modelIsolator = new ViewportModelIsolator();
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

            using (var output = new DwgOutputFile(document.Name, outputPath, overwriteExisting))
            using (Database database = _databaseCloner.Clone(document))
            {
                DwgLayoutIsolationResult layout = _layoutIsolator.Isolate(database, sheet);
                ReportLayout(sheet, layout, report);

                ModelIsolationResult model = _modelIsolator.Isolate(
                    database,
                    sheet.LayoutName,
                    report);
                ReportModel(sheet, model, report);

                _layoutIsolator.PrepareOpeningView(database, sheet);
                report("DWG folha " + sheet.Sequencia + ": vista inicial centralizada no Layout.");

                database.SaveAs(output.TemporaryPath, DwgVersion.Current);
                output.Publish();
                output.VerifyPublished();
            }

            report("DWG folha " + sheet.Sequencia + ": arquivo gerado.");
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

        private static void ReportModel(
            FolhaInfo sheet,
            ModelIsolationResult result,
            Action<string> report)
        {
            if (result.ViewportsConsidered == 0)
            {
                report(string.Format(
                    "DWG folha {0}: Model esvaziado; nenhuma viewport de Model válida, apagados={1}.",
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
