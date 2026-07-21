using System;
using System.Windows;
using PluginConceito.Application.Contracts;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotFolhasGenerationRunner
    {
        private readonly PlotFolhasGenerationService _generationService;
        private readonly SeloBlockService _seloService;
        private readonly ITelemetry _telemetry;

        public PlotFolhasGenerationRunner(
            PlotFolhasGenerationService generationService,
            SeloBlockService seloService,
            ITelemetry telemetry)
        {
            _generationService = generationService ??
                throw new ArgumentNullException(nameof(generationService));
            _seloService = seloService ?? throw new ArgumentNullException(nameof(seloService));
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        }

        public void Run(
            PlotFolhasWindow window,
            PlotFolhasGenerationPreparation preparation,
            bool overwrite)
        {
            bool completed = false;
            try
            {
                window.SetBusy(true);
                ReportStart(window, preparation);
                FillSeloIfConfigured(window, preparation);

                PlotExecutionResult result = _generationService.Execute(
                    preparation,
                    preparation.OutputFolder,
                    window.DeviceName,
                    window.CtbName,
                    overwrite,
                    message => ReportStatus(window, message));

                _telemetry.TrackEvent("CNT_PLOT_FOLHAS.Plot.Success");
                ReportSuccess(window, result);
                completed = true;
            }
            catch (Exception exception)
            {
                ReportFailure(window, exception);
            }
            finally
            {
                window.SetBusy(false);
            }

            if (completed) OpenOutputFolder(window, preparation.OutputFolder);
        }

        private void FillSeloIfConfigured(
            PlotFolhasWindow window,
            PlotFolhasGenerationPreparation preparation)
        {
            if (string.IsNullOrWhiteSpace(window.SelectedStampBlock) ||
                string.IsNullOrWhiteSpace(window.SelectedStampAttribute))
            {
                return;
            }

            _seloService.FillSeloAttributes(
                preparation.Plan.SelectedSheets,
                window.SelectedStampBlock,
                window.SelectedStampAttribute);
        }

        private void OpenOutputFolder(PlotFolhasWindow window, string outputFolder)
        {
            window.ProcessPendingUiMessages();
            string error = _generationService.TryOpenOutputFolder(outputFolder);
            if (!string.IsNullOrWhiteSpace(error)) ReportStatus(window, error);
        }

        private void ReportFailure(PlotFolhasWindow window, Exception exception)
        {
            _telemetry.TrackException("CNT_PLOT_FOLHAS.Plot", exception);
            ReportStatus(window, "Erro na plotagem: " + exception.Message);
            MessageBox.Show(
                window,
                exception.Message,
                "Erro na plotagem",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private static void ReportStart(
            PlotFolhasWindow window,
            PlotFolhasGenerationPreparation preparation)
        {
            ReportStatus(window, string.Format(
                "Gerando {0} PDF(s) e {1} DWG(s)...",
                preparation.Plan.PdfSheets.Count,
                preparation.Plan.DwgSheets.Count));
        }

        private static void ReportSuccess(
            PlotFolhasWindow window,
            PlotExecutionResult result)
        {
            ReportStatus(window, string.Format(
                "Concluído: {0} PDF(s) e {1} DWG(s) gerado(s).",
                result.PdfCount,
                result.DwgCount));
        }

        private static void ReportStatus(PlotFolhasWindow window, string message)
        {
            window.SetStatusMessage(message);
            window.ProcessPendingUiMessages();
        }
    }
}
