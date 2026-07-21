using System;
using System.Windows;
using System.Windows.Threading;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwcadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotFolhasDocumentTracker
    {
        private PlotFolhasWindow _window;
        private Document _document;

        public void Attach(PlotFolhasWindow window, Document document)
        {
            Detach();
            _window = window;
            _document = document;
            Subscribe();
        }

        public void Detach()
        {
            Unsubscribe();
            _window = null;
            _document = null;
        }

        private void Subscribe()
        {
            DocumentCollection manager = ZwcadApplication.DocumentManager;
            if (manager == null) return;
            manager.DocumentActivated += OnDocumentActivated;
            manager.DocumentBecameCurrent += OnDocumentBecameCurrent;
            manager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;
        }

        private void Unsubscribe()
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
            if (_document != null && ReferenceEquals(e.Document, _document))
            {
                Close("Documento fechado; janela encerrada.");
            }
        }

        private void CloseWindowIfDocumentChanged(Document currentDocument)
        {
            if (_document != null && currentDocument != null && !ReferenceEquals(currentDocument, _document))
            {
                Close("Documento trocado; janela encerrada.");
            }
        }

        private void Close(string statusMessage)
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
            }
        }
    }
}
