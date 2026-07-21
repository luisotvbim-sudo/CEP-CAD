using System;
using PluginConceito.Application.Contracts;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotFolhasZoomWorkflow
    {
        private readonly SheetZoomService _zoomService;
        private readonly ITelemetry _telemetry;

        public PlotFolhasZoomWorkflow(
            SheetZoomService zoomService,
            ITelemetry telemetry)
        {
            _zoomService = zoomService ?? throw new ArgumentNullException(nameof(zoomService));
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        }

        public void Run(PlotFolhasWindow window)
        {
            if (window == null) return;
            if (window.SelectedSheet == null)
            {
                window.SetStatusMessage("Selecione uma folha para dar zoom.");
                return;
            }

            try
            {
                window.SetBusy(true);
                _zoomService.ZoomTo(window.SelectedSheet);
                window.SetStatusMessage(
                    "Zoom aplicado na folha " + window.SelectedSheet.Sequencia + ".");
            }
            catch (Exception exception)
            {
                _telemetry.TrackException("CNT_PLOT_FOLHAS.Zoom", exception);
                window.SetStatusMessage("Erro no zoom: " + exception.Message);
            }
            finally
            {
                window.SetBusy(false);
            }
        }
    }
}
