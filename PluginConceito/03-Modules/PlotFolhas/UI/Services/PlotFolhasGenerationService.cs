using System;
using System.Collections.Generic;
using System.Linq;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotFolhasGenerationService
    {
        private readonly PlotExecutionService _executionService;
        private readonly OutputFolderService _outputFolders;

        public PlotFolhasGenerationService(
            PlotExecutionService executionService,
            OutputFolderService outputFolders)
        {
            _executionService = executionService ?? throw new ArgumentNullException(nameof(executionService));
            _outputFolders = outputFolders ??
                throw new ArgumentNullException(nameof(outputFolders));
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

            string requestedFolder = useAutomaticEmissionFolder
                ? automaticEmissionBaseFolder
                : outputFolder;
            if (string.IsNullOrWhiteSpace(requestedFolder))
            {
                return PlotFolhasGenerationPreparation.Fail(
                    "Escolha uma pasta de saída.");
            }

            string resolvedOutputFolder;
            try
            {
                resolvedOutputFolder = _outputFolders.Prepare(
                    outputFolder,
                    useAutomaticEmissionFolder,
                    automaticEmissionBaseFolder);
            }
            catch (Exception exception)
            {
                return PlotFolhasGenerationPreparation.Fail(
                    "Não foi possível criar/acessar a pasta: " + exception.Message);
            }

            return PlotFolhasGenerationPreparation.Success(
                plan,
                resolvedOutputFolder,
                _outputFolders.FindExistingFiles(
                    plan,
                    resolvedOutputFolder));
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
            return _outputFolders.TryOpen(outputFolder);
        }
    }
}
