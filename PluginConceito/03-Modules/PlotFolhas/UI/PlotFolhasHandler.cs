using System;
using System.Collections.Generic;
using PluginConceito.Application.Contracts;
using ZwcadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotFolhasHandler
    {
        private readonly IModuleContext _context;
        private readonly PlotFolhasSessionService _sessionService;
        private readonly PlotFolhasNamingWorkflow _namingWorkflow;
        private readonly PlotFolhasGenerationWorkflow _generationWorkflow;
        private readonly PlotFolhasZoomWorkflow _zoomWorkflow;
        private readonly PlotFolhasDocumentTracker _documentTracker;

        private PlotFolhasWindow _window;

        public PlotFolhasHandler(
            IModuleContext context,
            PlotFolhasSessionService sessionService,
            PlotFolhasNamingWorkflow namingWorkflow,
            PlotFolhasGenerationWorkflow generationWorkflow,
            PlotFolhasZoomWorkflow zoomWorkflow,
            PlotFolhasDocumentTracker documentTracker)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _sessionService = sessionService ??
                throw new ArgumentNullException(nameof(sessionService));
            _namingWorkflow = namingWorkflow ??
                throw new ArgumentNullException(nameof(namingWorkflow));
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
                    _context.Telemetry.TrackEvent("CNT_PLOT_FOLHAS.WindowOpened");
            }
            catch (Exception exception)
            {
                _context.Telemetry.TrackException(telemetryOperation, exception);
                ZwcadApplication.ShowAlertDialog(errorPrefix + exception.Message);
            }
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
            window.ZoomRequested += OnZoomRequested;
            window.SaveNamesRequested += OnSaveNamesRequested;
            window.PlotRequested += OnPlotRequested;
            window.StampBlockChanged += OnStampBlockChanged;
            window.LoadNamesFromStampRequested += OnLoadNamesFromStampRequested;
            window.RefreshRequested += OnRefreshRequested;
            window.Closed += OnWindowClosed;
        }

        private void OnApplyStructuredNameRequested(object sender, EventArgs e)
        {
            _namingWorkflow.ApplyStructure(_window);
        }

        private void OnFileNameEdited(object sender, EventArgs e)
        {
            _namingWorkflow.NormalizeEditedName(_window);
        }

        private void OnSaveNamesRequested(object sender, EventArgs e)
        {
            _namingWorkflow.Save(_window);
        }

        private void OnZoomRequested(object sender, EventArgs e)
        {
            _zoomWorkflow.Run(_window);
        }

        private void OnPlotRequested(object sender, EventArgs e)
        {
            _generationWorkflow.Run(_window);
        }

        private void OnStampBlockChanged(object sender, EventArgs e)
        {
            _namingWorkflow.LoadStampAttributes(_window);
        }

        private void OnLoadNamesFromStampRequested(object sender, EventArgs e)
        {
            _namingWorkflow.LoadNamesFromStamp(_window);
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
            _documentTracker.Detach();
            if (ReferenceEquals(sender, _window)) _window = null;
        }
    }
}
