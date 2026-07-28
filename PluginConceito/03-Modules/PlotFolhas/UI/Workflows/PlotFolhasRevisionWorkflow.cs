using System;
using System.Windows;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotFolhasRevisionWorkflow
    {
        private const string DialogTitle = "Subir revisão";

        private readonly SheetRevisionService _revisionService;

        public PlotFolhasRevisionWorkflow(
            SheetRevisionService revisionService)
        {
            _revisionService = revisionService ??
                throw new ArgumentNullException(nameof(revisionService));
        }

        public void Toggle(PlotFolhasWindow window)
        {
            if (window == null || window.EditedSheet == null)
            {
                return;
            }

            SheetRevisionResult result = _revisionService.Toggle(
                window.EditedSheet,
                window.Sheets,
                window.NamingStructure);
            window.RefreshSheets();
            window.SetStatusMessage(result.Message);

            if (result.HasWarning)
            {
                MessageBox.Show(
                    window,
                    result.WarningMessage,
                    DialogTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }
}
