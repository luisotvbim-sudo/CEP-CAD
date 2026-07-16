using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotFolhasGenerationService
    {
        private readonly PlotExecutionService _executionService;

        public PlotFolhasGenerationService(PlotExecutionService executionService)
        {
            _executionService = executionService ?? throw new ArgumentNullException(nameof(executionService));
        }

        public PlotFolhasGenerationPreparation Prepare(
            IReadOnlyList<FolhaInfo> sheets,
            string outputFolder,
            string deviceName,
            bool useAutomaticEmissionFolder,
            string automaticEmissionBaseFolder)
        {
            PlotOutputPlan plan = PlotOutputPlan.Create(sheets);
            if (plan.SelectedSheets.Count == 0)
            {
                return PlotFolhasGenerationPreparation.Fail("Marque PDF ou DWG em pelo menos uma folha.");
            }

            if (plan.SelectedSheets.Any(sheet => !sheet.Valida))
            {
                return PlotFolhasGenerationPreparation.Fail(
                    "Existem folhas selecionadas com erro. Corrija antes de gerar os arquivos.",
                    true);
            }

            if (plan.HasPdfOutput && string.IsNullOrWhiteSpace(deviceName))
            {
                return PlotFolhasGenerationPreparation.Fail("Escolha uma impressora/plotter PDF.");
            }

            string resolvedOutputFolder = useAutomaticEmissionFolder
                ? automaticEmissionBaseFolder
                : outputFolder;
            if (string.IsNullOrWhiteSpace(resolvedOutputFolder))
            {
                return PlotFolhasGenerationPreparation.Fail("Escolha uma pasta de saída.");
            }

            try
            {
                resolvedOutputFolder = useAutomaticEmissionFolder
                    ? CreateNextEmissionFolder(resolvedOutputFolder)
                    : Directory.CreateDirectory(resolvedOutputFolder).FullName;
            }
            catch (Exception exception)
            {
                return PlotFolhasGenerationPreparation.Fail(
                    "Não foi possível criar/acessar a pasta: " + exception.Message);
            }

            return PlotFolhasGenerationPreparation.Success(
                plan,
                resolvedOutputFolder,
                plan.FindExistingFiles(resolvedOutputFolder));
        }

        public PlotExecutionResult Execute(
            PlotFolhasGenerationPreparation preparation,
            string outputFolder,
            string deviceName,
            string ctbName,
            bool overwriteExisting,
            Action<string> progress)
        {
            if (preparation == null || !preparation.IsValid)
            {
                throw new InvalidOperationException("A geração não foi preparada corretamente.");
            }

            return _executionService.Execute(
                preparation.Plan,
                outputFolder,
                deviceName,
                ctbName,
                overwriteExisting,
                progress);
        }

        public string TryOpenOutputFolder(string outputFolder)
        {
            if (string.IsNullOrWhiteSpace(outputFolder) || !Directory.Exists(outputFolder)) return null;

            try
            {
                Process process = Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "/e,\"" + outputFolder + "\"",
                    WorkingDirectory = outputFolder,
                    UseShellExecute = true
                });
                if (process == null)
                {
                    return "Arquivos gerados, mas o Windows Explorer não foi iniciado.";
                }
                return null;
            }
            catch (Exception exception)
            {
                return "Arquivos gerados, mas não foi possível abrir a pasta: " + exception.Message;
            }
        }

        private static string CreateNextEmissionFolder(string baseFolder)
        {
            Directory.CreateDirectory(baseFolder);
            for (int number = 1; number < int.MaxValue; number++)
            {
                string folderName = "Emissão " + number.ToString("00", CultureInfo.InvariantCulture);
                string candidate = Path.Combine(baseFolder, folderName);
                if (Directory.Exists(candidate) || File.Exists(candidate)) continue;

                Directory.CreateDirectory(candidate);
                return candidate;
            }

            throw new IOException("Não foi possível determinar o próximo número de emissão.");
        }
    }

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
