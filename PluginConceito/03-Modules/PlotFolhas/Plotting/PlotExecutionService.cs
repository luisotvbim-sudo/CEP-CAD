using System;
using PluginConceito.Application.Contracts;
using ZwSoft.ZwCAD.ApplicationServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotExecutionService
    {
        private readonly IZwcadContext _zwcad;
        private readonly FolhaNomenclaturaService _nomenclaturaService;
        private readonly PlotService _plotService;
        private readonly DwgExportService _dwgExportService;

        public PlotExecutionService(
            IZwcadContext zwcad,
            FolhaNomenclaturaService nomenclaturaService,
            PlotService plotService,
            DwgExportService dwgExportService)
        {
            _zwcad = zwcad ?? throw new ArgumentNullException(nameof(zwcad));
            _nomenclaturaService = nomenclaturaService ?? throw new ArgumentNullException(nameof(nomenclaturaService));
            _plotService = plotService ?? throw new ArgumentNullException(nameof(plotService));
            _dwgExportService = dwgExportService ?? throw new ArgumentNullException(nameof(dwgExportService));
        }

        public PlotExecutionResult Execute(
            PlotOutputPlan plan,
            string outputFolder,
            string deviceName,
            string ctbName,
            bool overwriteExisting,
            Action<string> progress)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            Document document = _zwcad.ActiveDocument;
            if (document == null) throw new InvalidOperationException("Nao existe desenho ativo.");

            int savedNames = _nomenclaturaService.SaveNames(document, plan.SelectedSheets);
            Report(progress, "Nomenclatura salva em " + savedNames + " folha(s).");

            int pdfCount = plan.HasPdfOutput
                ? _plotService.PlotSheets(plan.PdfSheets, outputFolder, deviceName, ctbName, overwriteExisting, progress)
                : 0;
            int dwgCount = plan.DwgSheets.Count > 0
                ? _dwgExportService.ExportSheets(plan.DwgSheets, outputFolder, overwriteExisting, progress)
                : 0;

            return new PlotExecutionResult(pdfCount, dwgCount);
        }

        private static void Report(Action<string> progress, string message)
        {
            if (progress != null) progress(message);
        }
    }

    internal sealed class PlotExecutionResult
    {
        public PlotExecutionResult(int pdfCount, int dwgCount)
        {
            PdfCount = pdfCount;
            DwgCount = dwgCount;
        }

        public int PdfCount { get; private set; }

        public int DwgCount { get; private set; }
    }
}
