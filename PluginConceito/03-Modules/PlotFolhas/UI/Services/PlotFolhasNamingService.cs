using System;
using System.Collections.Generic;
using System.Linq;
using PluginConceito.Application.Contracts;
using ZwSoft.ZwCAD.ApplicationServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotFolhasNamingService
    {
        private readonly IZwcadContext _zwcad;
        private readonly ArquivoNomeService _nameService;
        private readonly FolhaNomenclaturaService _nomenclatureService;

        public PlotFolhasNamingService(
            IZwcadContext zwcad,
            ArquivoNomeService nameService,
            FolhaNomenclaturaService nomenclatureService)
        {
            _zwcad = zwcad ?? throw new ArgumentNullException(nameof(zwcad));
            _nameService = nameService ?? throw new ArgumentNullException(nameof(nameService));
            _nomenclatureService = nomenclatureService ?? throw new ArgumentNullException(nameof(nomenclatureService));
        }

        public void ApplyStructure(
            IReadOnlyList<FolhaInfo> sheets,
            NamingStructureDefinition structure)
        {
            if (structure == null)
            {
                throw new ArgumentNullException(nameof(structure));
            }

            for (int sheetIndex = 0; sheetIndex < sheets.Count; sheetIndex++)
            {
                sheets[sheetIndex].ResetRevision(false);
                List<string> sheetParts = new List<string>();
                foreach (NamingFieldDefinition field in structure.Fields)
                {
                    sheetParts.Add(field.IsSequential
                        ? GetSequentialValue(field.Value, sheetIndex)
                        : field.Value);
                }

                sheets[sheetIndex].NomeArquivo =
                    _nameService.BuildStructuredName(
                        structure.Separator,
                        sheetParts);
            }

            _nameService.ValidateNames(sheets);
        }

        private static string GetSequentialValue(string baseValue, int offset)
        {
            if (string.IsNullOrEmpty(baseValue)) return baseValue;

            int digitStart = FindDigitStart(baseValue);
            string prefix = digitStart > 0 ? baseValue.Substring(0, digitStart) : string.Empty;
            string numberPart = baseValue.Substring(digitStart);

            if (!long.TryParse(numberPart, out long number)) return baseValue;

            long next = number + offset;
            return prefix + next.ToString("D" + numberPart.Length);
        }

        private static int FindDigitStart(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsDigit(value[i])) return i;
            }

            return value.Length;
        }

        public void NormalizeEditedName(FolhaInfo editedSheet, IReadOnlyList<FolhaInfo> allSheets)
        {
            if (editedSheet == null) return;
            editedSheet.NomeArquivo = _nameService.NormalizeInlineName(editedSheet.NomeArquivo);
            _nameService.ValidateNames(allSheets);
        }

        public PlotFolhasNameValidation NormalizeAndValidate(IReadOnlyList<FolhaInfo> sheets)
        {
            foreach (FolhaInfo sheet in sheets)
            {
                sheet.NomeArquivo = _nameService.NormalizeInlineName(sheet.NomeArquivo);
            }

            _nameService.ValidateNames(sheets);
            return new PlotFolhasNameValidation(sheets.Where(sheet => !sheet.Valida).ToList());
        }

        public int Save(IReadOnlyList<FolhaInfo> sheets)
        {
            Document document = _zwcad.ActiveDocument;
            if (document == null) throw new InvalidOperationException("Não existe desenho ativo.");
            return _nomenclatureService.SaveNames(document, sheets);
        }
    }

    internal sealed class PlotFolhasNameValidation
    {
        public PlotFolhasNameValidation(IReadOnlyList<FolhaInfo> invalidSheets)
        {
            InvalidSheets = invalidSheets;
        }

        public IReadOnlyList<FolhaInfo> InvalidSheets { get; }
        public bool IsValid { get { return InvalidSheets.Count == 0; } }
    }
}
