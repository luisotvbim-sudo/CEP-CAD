using System;
using System.Collections.Generic;
using PluginConceito.Application.Contracts;
using ZwcadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotFolhasHandler
    {
        private readonly ITelemetry _telemetry;
        private readonly PlotFolhasSessionService _sessionService;
        private readonly PlotFolhasNamingWorkflow _namingWorkflow;
        private readonly PlotFolhasRevisionWorkflow _revisionWorkflow;
        private readonly PlotFolhasGenerationWorkflow _generationWorkflow;
        private readonly PlotFolhasZoomWorkflow _zoomWorkflow;
        private readonly PlotFolhasDocumentTracker _documentTracker;

        private PlotFolhasWindow _window;

        public PlotFolhasHandler(
            ITelemetry telemetry,
            PlotFolhasSessionService sessionService,
            PlotFolhasNamingWorkflow namingWorkflow,
            PlotFolhasRevisionWorkflow revisionWorkflow,
            PlotFolhasGenerationWorkflow generationWorkflow,
            PlotFolhasZoomWorkflow zoomWorkflow,
            PlotFolhasDocumentTracker documentTracker)
        {
            _telemetry = telemetry ??
                throw new ArgumentNullException(nameof(telemetry));
            _sessionService = sessionService ??
                throw new ArgumentNullException(nameof(sessionService));
            _namingWorkflow = namingWorkflow ??
                throw new ArgumentNullException(nameof(namingWorkflow));
            _revisionWorkflow = revisionWorkflow ??
                throw new ArgumentNullException(nameof(revisionWorkflow));
            _generationWorkflow = generationWorkflow ??
                throw new ArgumentNullException(nameof(generationWorkflow));
            _zoomWorkflow = zoomWorkflow ??
                throw new ArgumentNullException(nameof(zoomWorkflow));
            _documentTracker = documentTracker ??
                throw new ArgumentNullException(nameof(documentTracker));
        }

        public void Execute()
        {
            TryShowSession(
                "CNT_PLOT_FOLHAS.Execute",
                "Falha ao mapear folhas: ",
                true);
        }

        private void TryShowSession(
            string telemetryOperation,
            string errorPrefix,
            bool trackWindowOpened)
        {
            try
            {
                PlotFolhasSession session = _sessionService.Create();
                if (!session.HasSheets)
                {
                    CloseCurrentWindow();
                    ZwcadApplication.ShowAlertDialog(
                        BuildNoSheetsMessage(session));
                    return;
                }

                ShowWindow(session);
                if (trackWindowOpened)
                    _telemetry.TrackEvent("CNT_PLOT_FOLHAS.WindowOpened");
            }
            catch (Exception exception)
            {
                _telemetry.TrackException(telemetryOperation, exception);
                ZwcadApplication.ShowAlertDialog(
                    errorPrefix + GetInnermostMessage(exception));
            }
        }

        private static string GetInnermostMessage(Exception exception)
        {
            Exception current = exception;
            while (current.InnerException != null)
            {
                current = current.InnerException;
            }

            return current.Message;
        }

        private void ShowWindow(PlotFolhasSession session)
        {
            CloseCurrentWindow();

            IReadOnlyList<string> blockNames = _namingWorkflow.GetStampBlockNames();
            _window = new PlotFolhasWindow(
                session.Sheets,
                session.Devices,
                session.PlotStyles,
                session.OutputFolder,
                session.UseAutomaticEmissionFolder,
                session.DefaultDevice,
                session.DefaultPlotStyle,
                session.NamingSeparator,
                session.NamingParts,
                blockNames,
                session.SourceSpace,
                session.SourceLayoutName);

            AttachWindowEvents(_window);
            _documentTracker.Attach(_window, session.Document);
            ZwcadApplication.ShowModelessWindow(_window);
        }

        private static string BuildNoSheetsMessage(
            PlotFolhasSession session)
        {
            string source = session.SourceSpace == SheetSpaceKind.Model
                ? "Model"
                : "layout " + session.SourceLayoutName;
            return "Nenhuma folha foi encontrada no " + source + ".\n\n" +
                "Use blocos com nomes exatos CEP-A4, CEP-A3, CEP-A2, CEP-A1, " +
                "CEP-A0, CEP-A1E ou CEP-A0E.";
        }

        private void CloseCurrentWindow()
        {
            _window?.Close();
            _documentTracker.Detach();
        }

        private void AttachWindowEvents(PlotFolhasWindow window)
        {
            window.ApplyStructuredNameRequested += OnApplyStructuredNameRequested;
            window.FileNameEdited += OnFileNameEdited;
            window.RevisionChangeRequested += OnRevisionChangeRequested;
            window.ZoomRequested += OnZoomRequested;
            window.SaveNamesRequested += OnSaveNamesRequested;
            window.PlotRequested += OnPlotRequested;
            window.StampBlockChanged += OnStampBlockChanged;
            window.LoadNamesFromStampRequested += OnLoadNamesFromStampRequested;
            window.RefreshRequested += OnRefreshRequested;
            window.Closed += OnWindowClosed;
        }

        private void DetachWindowEvents(PlotFolhasWindow window)
        {
            if (window == null)
            {
                return;
            }

            window.ApplyStructuredNameRequested -= OnApplyStructuredNameRequested;
            window.FileNameEdited -= OnFileNameEdited;
            window.RevisionChangeRequested -= OnRevisionChangeRequested;
            window.ZoomRequested -= OnZoomRequested;
            window.SaveNamesRequested -= OnSaveNamesRequested;
            window.PlotRequested -= OnPlotRequested;
            window.StampBlockChanged -= OnStampBlockChanged;
            window.LoadNamesFromStampRequested -= OnLoadNamesFromStampRequested;
            window.RefreshRequested -= OnRefreshRequested;
            window.Closed -= OnWindowClosed;
        }

        private void OnApplyStructuredNameRequested(object sender, EventArgs e)
        {
            ExecuteSafely(
                "CNT_PLOT_FOLHAS.ApplyStructure",
                "Falha ao aplicar a estrutura: ",
                () => _namingWorkflow.ApplyStructure(_window));
        }

        private void OnFileNameEdited(object sender, EventArgs e)
        {
            ExecuteSafely(
                "CNT_PLOT_FOLHAS.NormalizeName",
                "Falha ao atualizar o nome: ",
                () => _namingWorkflow.NormalizeEditedName(_window));
        }

        private void OnRevisionChangeRequested(object sender, EventArgs e)
        {
            ExecuteSafely(
                "CNT_PLOT_FOLHAS.ToggleRevision",
                "Falha ao atualizar a revisão: ",
                () => _revisionWorkflow.Toggle(_window));
        }

        private void OnSaveNamesRequested(object sender, EventArgs e)
        {
            ExecuteSafely(
                "CNT_PLOT_FOLHAS.SaveNames",
                "Falha ao salvar a nomenclatura: ",
                () => _namingWorkflow.Save(_window));
        }

        private void OnZoomRequested(object sender, EventArgs e)
        {
            ExecuteSafely(
                "CNT_PLOT_FOLHAS.Zoom",
                "Falha ao aproximar a folha: ",
                () => _zoomWorkflow.Run(_window));
        }

        private void OnPlotRequested(object sender, EventArgs e)
        {
            ExecuteSafely(
                "CNT_PLOT_FOLHAS.Plot",
                "Falha ao gerar os arquivos: ",
                () => _generationWorkflow.Run(_window));
        }

        private void OnStampBlockChanged(object sender, EventArgs e)
        {
            ExecuteSafely(
                "CNT_PLOT_FOLHAS.LoadStampAttributes",
                "Falha ao carregar os atributos do selo: ",
                () => _namingWorkflow.LoadStampAttributes(_window));
        }

        private void OnLoadNamesFromStampRequested(object sender, EventArgs e)
        {
            ExecuteSafely(
                "CNT_PLOT_FOLHAS.LoadNamesFromStamp",
                "Falha ao carregar os nomes do selo: ",
                () => _namingWorkflow.LoadNamesFromStamp(_window));
        }

        private void OnRefreshRequested(object sender, EventArgs e)
        {
            TryShowSession(
                "CNT_PLOT_FOLHAS.Refresh",
                "Falha ao atualizar folhas: ",
                false);
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            var closedWindow = sender as PlotFolhasWindow;
            DetachWindowEvents(closedWindow);
            _documentTracker.Detach();
            if (ReferenceEquals(closedWindow, _window)) _window = null;
        }

        private void ExecuteSafely(
            string telemetryOperation,
            string errorPrefix,
            Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception exception)
            {
                _telemetry.TrackException(
                    telemetryOperation,
                    exception);
                ZwcadApplication.ShowAlertDialog(
                    errorPrefix + GetInnermostMessage(exception));
            }
        }
    }
}
