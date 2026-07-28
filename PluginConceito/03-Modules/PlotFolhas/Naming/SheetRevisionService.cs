using System;
using System.Collections.Generic;
using System.Linq;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class SheetRevisionService
    {
        private readonly RevisionNameService _revisionNameService;
        private readonly ArquivoNomeService _fileNameService;

        public SheetRevisionService(
            RevisionNameService revisionNameService,
            ArquivoNomeService fileNameService)
        {
            _revisionNameService = revisionNameService ??
                throw new ArgumentNullException(nameof(revisionNameService));
            _fileNameService = fileNameService ??
                throw new ArgumentNullException(nameof(fileNameService));
        }

        public SheetRevisionResult Toggle(
            FolhaInfo sheet,
            IReadOnlyList<FolhaInfo> allSheets,
            NamingStructureDefinition structure)
        {
            if (sheet == null)
            {
                return new SheetRevisionResult(
                    "Folha não encontrada para atualizar a revisão.",
                    null);
            }

            if (!sheet.SubirRevisao)
            {
                sheet.CancelRevision();
                Validate(allSheets);
                return new SheetRevisionResult(
                    "Revisão restaurada na folha " + sheet.Sequencia + ".",
                    null);
            }

            if (sheet.HasRevisionSnapshot)
            {
                return BuildAlreadyProcessedResult(sheet);
            }

            sheet.BeginRevision();
            RevisionNameResult result = _revisionNameService.Increment(
                sheet.NomeArquivo,
                structure?.Separator,
                structure?.RevisionTarget);

            if (!result.IsSuccess)
            {
                sheet.FailRevision(result.Error, result.FailureKind);
                Validate(allSheets);
                return new SheetRevisionResult(
                    "Folha " + sheet.Sequencia + ": " + result.Error + ".",
                    result.RequiresIdentificationWarning
                        ? result.Error
                        : null);
            }

            string originalName = sheet.OriginalNameBeforeRevision;
            sheet.CompleteRevision(result.FileName);
            Validate(allSheets);
            return new SheetRevisionResult(
                "Revisão incrementada na folha " + sheet.Sequencia +
                ": " + originalName + " → " + result.FileName + ".",
                null);
        }

        public void Reset(
            IEnumerable<FolhaInfo> sheets,
            bool restoreOriginalNames)
        {
            foreach (FolhaInfo sheet in
                sheets ?? Enumerable.Empty<FolhaInfo>())
            {
                sheet?.ResetRevision(restoreOriginalNames);
            }
        }

        private SheetRevisionResult BuildAlreadyProcessedResult(
            FolhaInfo sheet)
        {
            if (string.IsNullOrWhiteSpace(sheet.ErroRevisao))
            {
                return new SheetRevisionResult(
                    "A revisão da folha " + sheet.Sequencia +
                    " já foi incrementada.",
                    null);
            }

            return new SheetRevisionResult(
                "A revisão da folha " + sheet.Sequencia +
                " não foi alterada: " + sheet.ErroRevisao + ".",
                sheet.RevisionFailureKind ==
                    RevisionNameFailureKind.Identification
                    ? sheet.ErroRevisao
                    : null);
        }

        private void Validate(IEnumerable<FolhaInfo> sheets)
        {
            _fileNameService.ValidateNames(
                sheets ?? Enumerable.Empty<FolhaInfo>());
        }
    }
}
