using System.Collections.Generic;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotFolhasGenerationPreparation
    {
        private PlotFolhasGenerationPreparation(
            PlotOutputPlan plan,
            string outputFolder,
            IReadOnlyList<string> existingFiles,
            string errorMessage,
            bool showWarningDialog)
        {
            Plan = plan;
            OutputFolder = outputFolder;
            ExistingFiles = existingFiles ?? new List<string>();
            ErrorMessage = errorMessage;
            ShowWarningDialog = showWarningDialog;
        }

        public PlotOutputPlan Plan { get; }
        public string OutputFolder { get; }
        public IReadOnlyList<string> ExistingFiles { get; }
        public string ErrorMessage { get; }
        public bool ShowWarningDialog { get; }
        public bool IsValid { get { return Plan != null && string.IsNullOrWhiteSpace(ErrorMessage); } }

        public static PlotFolhasGenerationPreparation Success(
            PlotOutputPlan plan,
            string outputFolder,
            IReadOnlyList<string> existingFiles)
        {
            return new PlotFolhasGenerationPreparation(plan, outputFolder, existingFiles, null, false);
        }

        public static PlotFolhasGenerationPreparation Fail(string message, bool showWarningDialog = false)
        {
            return new PlotFolhasGenerationPreparation(null, null, null, message, showWarningDialog);
        }
    }
}
