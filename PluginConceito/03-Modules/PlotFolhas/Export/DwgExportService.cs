using System;
using System.Collections.Generic;
using System.IO;
using PluginConceito.Application.Contracts;
using ZwSoft.ZwCAD.ApplicationServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class DwgExportService
    {
        private readonly IZwcadContext _zwcad;
        private readonly DwgSheetExportService _sheetExporter;

        public DwgExportService(IZwcadContext zwcad, FolhaFormatCatalog formats)
        {
            _zwcad = zwcad ?? throw new ArgumentNullException(nameof(zwcad));
            if (formats == null) throw new ArgumentNullException(nameof(formats));

            _sheetExporter = new DwgSheetExportService(formats);
        }

        public int ExportSheets(
            IReadOnlyList<FolhaInfo> sheets,
            string outputFolder,
            bool overwriteExisting,
            Action<string> progress)
        {
            if (sheets == null || sheets.Count == 0) return 0;
            if (string.IsNullOrWhiteSpace(outputFolder))
                throw new ArgumentException("Pasta de saída obrigatória.", nameof(outputFolder));

            Document document = _zwcad.ActiveDocument;
            if (document == null) throw new InvalidOperationException("Não existe desenho ativo.");

            Directory.CreateDirectory(outputFolder);

            for (int index = 0; index < sheets.Count; index++)
            {
                FolhaInfo sheet = sheets[index];
                string outputPath = Path.Combine(
                    outputFolder,
                    Path.ChangeExtension(sheet.NomeArquivo, ".dwg"));

                Report(progress, string.Format(
                    "DWG folha {0}/{1}: preparando {2}",
                    index + 1,
                    sheets.Count,
                    outputPath));

                _sheetExporter.Export(
                    document,
                    sheet,
                    outputPath,
                    overwriteExisting,
                    message => Report(progress, message));
            }

            Report(progress, "DWG concluído: " + sheets.Count + " arquivo(s).");
            return sheets.Count;
        }

        private void Report(Action<string> progress, string message)
        {
            _zwcad.WriteMessage(message);
            progress?.Invoke(message);
        }
    }
}
