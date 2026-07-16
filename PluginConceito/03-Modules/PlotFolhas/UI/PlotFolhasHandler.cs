using System;
using System.Windows;
using System.Windows.Threading;
using PluginConceito.Application.Contracts;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwcadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotFolhasHandler
    {
        private readonly IModuleContext _context;
        private readonly PlotFolhasSessionService _sessionService;
        private readonly PlotFolhasNamingService _namingService;
        private readonly PlotFolhasGenerationService _generationService;
        private readonly SheetZoomService _zoomService;

        private PlotFolhasWindow _window;
        private Document _windowDocument;

        public PlotFolhasHandler(IModuleContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            var formatCatalog = new FolhaFormatCatalog();
            var fileNameService = new ArquivoNomeService();
            var nomenclatureService = new FolhaNomenclaturaService();
            var plotService = new PlotService(context.Zwcad);
            var scanner = new FolhaScanner(context.Zwcad, formatCatalog);
            var dwgExportService = new DwgExportService(context.Zwcad, formatCatalog);
            var executionService = new PlotExecutionService(
                context.Zwcad,
                nomenclatureService,
                plotService,
                dwgExportService);

            _sessionService = new PlotFolhasSessionService(
                context.Zwcad,
                scanner,
                fileNameService,
                nomenclatureService,
                plotService);
            _namingService = new PlotFolhasNamingService(
                context.Zwcad,
                fileNameService,
                nomenclatureService);
            _generationService = new PlotFolhasGenerationService(executionService);
            _zoomService = new SheetZoomService(context.Zwcad);
        }

        public void Execute()
        {
            try
            {
                PlotFolhasSession session = _sessionService.Create();
                if (!session.HasSheets)
                {
                    ZwcadApplication.ShowAlertDialog(
                        "Nenhuma folha foi encontrada no layout atual.\n\n" +
                        "Use blocos com nomes exatos CEP-A4, CEP-A3, CEP-A2, CEP-A1, CEP-A0, " +
                        "CEP-A1E ou CEP-A0E.");
                    return;
                }

                ShowWindow(session);
                _context.Telemetry.TrackEvent("CNT_PLOT_FOLHAS.WindowOpened");
            }
            catch (Exception exception)
            {
                _context.Telemetry.TrackException("CNT_PLOT_FOLHAS.Execute", exception);
                ZwcadApplication.ShowAlertDialog("Falha ao mapear folhas: " + exception.Message);
            }
        }

        private void ShowWindow(PlotFolhasSession session)
        {
            _window?.Close();
            UnsubscribeDocumentLifeCycle();

            _window = new PlotFolhasWindow(
                session.Sheets,
                session.Devices,
                session.PlotStyles,
                session.OutputFolder,
                session.UseAutomaticEmissionFolder,
                session.DefaultDevice,
                session.DefaultPlotStyle,
                session.NamingSeparator,
                session.NamingParts);

            _window.ApplyStructuredNameRequested += OnApplyStructuredNameRequested;
            _window.FileNameEdited += OnFileNameEdited;
            _window.ZoomRequested += OnZoomRequested;
            _window.SaveNamesRequested += OnSaveNamesRequested;
            _window.PlotRequested += OnPlotRequested;
            _window.Closed += OnWindowClosed;

            _windowDocument = session.Document;
            SubscribeDocumentLifeCycle();
            ZwcadApplication.ShowModelessWindow(_window);
        }

        private void OnApplyStructuredNameRequested(object sender, EventArgs e)
        {
            PlotFolhasWindow window = _window;
            if (window == null) return;

            _namingService.ApplyStructure(window.Sheets, window.NamingSeparator, window.NamingParts);
            window.RefreshSheets();
            window.SetStatusMessage("Estrutura aplicada em todas as folhas.");
        }

        private void OnFileNameEdited(object sender, EventArgs e)
        {
            PlotFolhasWindow window = _window;
            if (window == null || window.EditedSheet == null) return;

            _namingService.NormalizeEditedName(window.EditedSheet, window.Sheets);
            window.RefreshSheets();
            window.SetStatusMessage("Nome atualizado na folha " + window.EditedSheet.Sequencia + ".");
        }

        private void OnSaveNamesRequested(object sender, EventArgs e)
        {
            PlotFolhasWindow window = _window;
            if (window == null) return;

            window.CommitChanges();
            PlotFolhasNameValidation validation = _namingService.NormalizeAndValidate(window.Sheets);
            window.RefreshSheets();
            if (!validation.IsValid)
            {
                MessageBox.Show(
                    window,
                    "Existem folhas com erro de nome. Corrija antes de salvar a nomenclatura.",
                    "Salvar nomenclatura",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                window.SetBusy(true);
                int saved = _namingService.Save(window.Sheets);
                window.SetStatusMessage("Nomenclatura salva em " + saved + " folha(s).");
            }
            catch (Exception exception)
            {
                _context.Telemetry.TrackException("CNT_PLOT_FOLHAS.SaveNames", exception);
                window.SetStatusMessage("Erro ao salvar nomenclatura: " + exception.Message);
                MessageBox.Show(window, exception.Message, "Erro ao salvar nomenclatura", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                window.SetBusy(false);
            }
        }

        private void OnZoomRequested(object sender, EventArgs e)
        {
            PlotFolhasWindow window = _window;
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
                window.SetStatusMessage("Zoom aplicado na folha " + window.SelectedSheet.Sequencia + ".");
            }
            catch (Exception exception)
            {
                _context.Telemetry.TrackException("CNT_PLOT_FOLHAS.Zoom", exception);
                window.SetStatusMessage("Erro no zoom: " + exception.Message);
            }
            finally
            {
                window.SetBusy(false);
            }
        }

        private void OnPlotRequested(object sender, EventArgs e)
        {
            PlotFolhasWindow window = _window;
            if (window == null) return;

            window.CommitChanges();
            _namingService.NormalizeAndValidate(window.Sheets);
            PlotFolhasGenerationPreparation preparation = _generationService.Prepare(
                window.Sheets,
                window.OutputFolder,
                window.DeviceName,
                window.UseAutomaticEmissionFolder,
                window.AutomaticEmissionBaseFolder);
            window.RefreshSheets();

            if (!preparation.IsValid)
            {
                window.SetStatusMessage(preparation.ErrorMessage);
                if (preparation.ShowWarningDialog)
                {
                    MessageBox.Show(window, preparation.ErrorMessage, "Plotar folhas", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            window.SetResolvedOutputFolder(preparation.OutputFolder);

            bool overwrite = window.OverwriteExisting;
            if (preparation.ExistingFiles.Count > 0 && !overwrite)
            {
                MessageBoxResult answer = MessageBox.Show(
                    window,
                    preparation.ExistingFiles.Count + " arquivo(s) já existem na pasta de saída.\n\nDeseja sobrescrever?",
                    "Plotar folhas",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (answer != MessageBoxResult.Yes)
                {
                    window.SetStatusMessage("Plotagem cancelada para evitar sobrescrever arquivos.");
                    return;
                }
                overwrite = true;
            }

            ExecuteGeneration(window, preparation, overwrite);
        }

        private void ExecuteGeneration(
            PlotFolhasWindow window,
            PlotFolhasGenerationPreparation preparation,
            bool overwrite)
        {
            bool generationCompleted = false;
            try
            {
                window.SetBusy(true);
                ReportStatus(window, string.Format(
                    "Gerando {0} PDF(s) e {1} DWG(s)...",
                    preparation.Plan.PdfSheets.Count,
                    preparation.Plan.DwgSheets.Count));

                PlotExecutionResult result = _generationService.Execute(
                    preparation,
                    preparation.OutputFolder,
                    window.DeviceName,
                    window.CtbName,
                    overwrite,
                    message => ReportStatus(window, message));

                _context.Telemetry.TrackEvent("CNT_PLOT_FOLHAS.Plot.Success");
                ReportStatus(window, string.Format(
                    "Concluído: {0} PDF(s) e {1} DWG(s) gerado(s).",
                    result.PdfCount,
                    result.DwgCount));
                generationCompleted = true;
            }
            catch (Exception exception)
            {
                _context.Telemetry.TrackException("CNT_PLOT_FOLHAS.Plot", exception);
                ReportStatus(window, "Erro na plotagem: " + exception.Message);
                MessageBox.Show(window, exception.Message, "Erro na plotagem", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                window.SetBusy(false);
            }

            if (!generationCompleted) return;

            window.ProcessPendingUiMessages();
            string openFolderError = _generationService.TryOpenOutputFolder(preparation.OutputFolder);
            if (!string.IsNullOrWhiteSpace(openFolderError))
            {
                ReportStatus(window, openFolderError);
            }
        }

        private static void ReportStatus(PlotFolhasWindow window, string message)
        {
            window.SetStatusMessage(message);
            window.ProcessPendingUiMessages();
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            UnsubscribeDocumentLifeCycle();
            _window = null;
            _windowDocument = null;
        }

        private void SubscribeDocumentLifeCycle()
        {
            DocumentCollection manager = ZwcadApplication.DocumentManager;
            if (manager == null) return;
            manager.DocumentActivated += OnDocumentActivated;
            manager.DocumentBecameCurrent += OnDocumentBecameCurrent;
            manager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;
        }

        private void UnsubscribeDocumentLifeCycle()
        {
            DocumentCollection manager = ZwcadApplication.DocumentManager;
            if (manager == null) return;
            manager.DocumentActivated -= OnDocumentActivated;
            manager.DocumentBecameCurrent -= OnDocumentBecameCurrent;
            manager.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;
        }

        private void OnDocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            CloseWindowIfDocumentChanged(e.Document);
        }

        private void OnDocumentBecameCurrent(object sender, DocumentCollectionEventArgs e)
        {
            CloseWindowIfDocumentChanged(e.Document);
        }

        private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
        {
            if (_windowDocument != null && ReferenceEquals(e.Document, _windowDocument))
            {
                CloseWindowBecauseDocumentChanged("Documento fechado; janela encerrada.");
            }
        }

        private void CloseWindowIfDocumentChanged(Document currentDocument)
        {
            if (_windowDocument != null && currentDocument != null && !ReferenceEquals(currentDocument, _windowDocument))
            {
                CloseWindowBecauseDocumentChanged("Documento trocado; janela encerrada.");
            }
        }

        private void CloseWindowBecauseDocumentChanged(string statusMessage)
        {
            PlotFolhasWindow window = _window;
            if (window == null) return;
            try
            {
                window.SetStatusMessage(statusMessage);
                window.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(window.Close));
            }
            catch
            {
                // O ZWCAD pode estar destruindo a janela ao mesmo tempo.
            }
        }
    }
}
