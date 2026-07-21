using System;
using System.Windows;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotFolhasGenerationWorkflow
    {
        private readonly PlotFolhasGenerationService _generationService;
        private readonly PlotFolhasNamingService _namingService;
        private readonly PlotFolhasGenerationRunner _runner;

        public PlotFolhasGenerationWorkflow(
            PlotFolhasGenerationService generationService,
            PlotFolhasNamingService namingService,
            PlotFolhasGenerationRunner runner)
        {
            _generationService = generationService ??
                throw new ArgumentNullException(nameof(generationService));
            _namingService = namingService ??
                throw new ArgumentNullException(nameof(namingService));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        public void Run(PlotFolhasWindow window)
        {
            if (window == null) return;

            PlotFolhasGenerationPreparation preparation = Prepare(window);
            if (!preparation.IsValid)
            {
                ReportInvalidPreparation(window, preparation);
                return;
            }

            window.SetResolvedOutputFolder(preparation.OutputFolder);
            bool overwrite;
            if (!TryResolveOverwrite(window, preparation, out overwrite)) return;

            _runner.Run(window, preparation, overwrite);
        }

        private PlotFolhasGenerationPreparation Prepare(PlotFolhasWindow window)
        {
            window.CommitChanges();
            _namingService.NormalizeAndValidate(window.Sheets);

            PlotFolhasGenerationPreparation preparation = _generationService.Prepare(
                window.Sheets,
                window.OutputFolder,
                window.DeviceName,
                window.UseAutomaticEmissionFolder,
                window.AutomaticEmissionBaseFolder);
            window.RefreshSheets();
            return preparation;
        }

        private static bool TryResolveOverwrite(
            PlotFolhasWindow window,
            PlotFolhasGenerationPreparation preparation,
            out bool overwrite)
        {
            overwrite = window.OverwriteExisting;
            if (preparation.ExistingFiles.Count == 0 || overwrite) return true;

            MessageBoxResult answer = MessageBox.Show(
                window,
                preparation.ExistingFiles.Count +
                    " arquivo(s) já existem na pasta de saída.\n\nDeseja sobrescrever?",
                "Plotar folhas",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (answer == MessageBoxResult.Yes)
            {
                overwrite = true;
                return true;
            }

            window.SetStatusMessage(
                "Plotagem cancelada para evitar sobrescrever arquivos.");
            return false;
        }

        private static void ReportInvalidPreparation(
            PlotFolhasWindow window,
            PlotFolhasGenerationPreparation preparation)
        {
            window.SetStatusMessage(preparation.ErrorMessage);
            if (!preparation.ShowWarningDialog) return;

            MessageBox.Show(
                window,
                preparation.ErrorMessage,
                "Plotar folhas",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
