using System;
using System.Collections.Generic;
using System.IO;
using PluginConceito.Application.Contracts;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class DwgExportService
    {
        private readonly IZwcadContext _zwcad;
        private readonly DwgLayoutIsolator _layoutIsolator;
        private readonly ViewportModelIsolator _modelIsolator;

        public DwgExportService(IZwcadContext zwcad, FolhaFormatCatalog formats)
        {
            _zwcad = zwcad ?? throw new ArgumentNullException(nameof(zwcad));
            if (formats == null) throw new ArgumentNullException(nameof(formats));

            _layoutIsolator = new DwgLayoutIsolator(formats);
            _modelIsolator = new ViewportModelIsolator();
        }

        public int ExportSheets(
            IReadOnlyList<FolhaInfo> sheets,
            string outputFolder,
            bool overwriteExisting,
            Action<string> progress)
        {
            if (sheets == null || sheets.Count == 0) return 0;

            Document document = _zwcad.ActiveDocument;
            if (document == null) throw new InvalidOperationException("Não existe desenho ativo.");
            if (string.IsNullOrWhiteSpace(outputFolder))
                throw new ArgumentException("Pasta de saída obrigatória.", nameof(outputFolder));

            Directory.CreateDirectory(outputFolder);
            int exported = 0;

            for (int index = 0; index < sheets.Count; index++)
            {
                FolhaInfo sheet = sheets[index];
                string outputPath = Path.Combine(
                    outputFolder,
                    Path.ChangeExtension(sheet.NomeArquivo, ".dwg"));

                EnsureOutputDoesNotReplaceSource(document.Name, outputPath);
                EnsureOutputCanBeCreated(outputPath, overwriteExisting);

                Report(progress, string.Format(
                    "DWG folha {0}/{1}: preparando {2}",
                    index + 1,
                    sheets.Count,
                    outputPath));

                ExportSingleSheet(document, outputPath, sheet, overwriteExisting, progress);
                exported++;
            }

            Report(progress, "DWG concluído: " + exported + " arquivo(s).");
            return exported;
        }

        private void ExportSingleSheet(
            Document document,
            string outputPath,
            FolhaInfo sheet,
            bool overwriteExisting,
            Action<string> progress)
        {
            string temporaryPath = CreateTemporaryOutputPath(outputPath);

            try
            {
                using (Database database = CloneActiveDatabase(document))
                {
                    Report(progress, "DWG folha " + sheet.Sequencia + ": isolando Layout.");
                    DwgLayoutIsolationResult layout = _layoutIsolator.Isolate(database, sheet);

                    Report(progress, string.Format(
                        "DWG folha {0}: Layout isolado; mantidos={1}, apagados={2}, viewports={3}.",
                        sheet.Sequencia,
                        layout.EntitiesKept,
                        layout.EntitiesErased,
                        layout.ModelViewportsKept));

                    Report(progress, "DWG folha " + sheet.Sequencia + ": isolando Model.");
                    ModelIsolationResult model = _modelIsolator.Isolate(database, sheet.LayoutName);

                    if (model.Outcome == ModelIsolationOutcome.ModelClearedWithoutViewport)
                    {
                        Report(progress, string.Format(
                            "DWG folha {0}: Model esvaziado; nenhuma viewport de Model pertence à folha, apagados={1}.",
                            sheet.Sequencia,
                            model.EntitiesErased));
                    }
                    else if (model.Outcome == ModelIsolationOutcome.ModelPreservedWithoutMatches)
                    {
                        Report(progress, string.Format(
                            "DWG folha {0}: Model preservado integralmente; as regiões não encontraram elementos, mantidos={1}.",
                            sheet.Sequencia,
                            model.EntitiesKept));
                    }
                    else
                    {
                        Report(progress, string.Format(
                            "DWG folha {0}: Model isolado; regiões={1}, mantidos={2}, apagados={3}, sem limites={4}.",
                            sheet.Sequencia,
                            model.ViewportsConsidered,
                            model.EntitiesKept,
                            model.EntitiesErased,
                            model.EntitiesKeptWithoutExtents));
                    }

                    _layoutIsolator.PrepareOpeningView(database, sheet);
                    Report(progress, "DWG folha " + sheet.Sequencia + ": vista inicial centralizada no Layout.");

                    database.SaveAs(temporaryPath, DwgVersion.Current);
                }

                if (!File.Exists(temporaryPath))
                    throw new IOException("O arquivo DWG temporário não foi gerado: " + temporaryPath);

                PublishOutput(temporaryPath, outputPath, overwriteExisting);
            }
            finally
            {
                TryDeleteTemporaryFile(temporaryPath);
            }

            if (!File.Exists(outputPath))
                throw new IOException("DWG não foi gerado: " + outputPath);

            Report(progress, "DWG folha " + sheet.Sequencia + ": arquivo gerado.");
        }

        private static Database CloneActiveDatabase(Document document)
        {
            using (DocumentLock documentLock = document.LockDocument())
            {
                Database clone = document.Database.Wblock();
                if (clone == null)
                    throw new InvalidOperationException("O ZWCAD não conseguiu criar a cópia do desenho ativo.");

                return clone;
            }
        }

        private static string CreateTemporaryOutputPath(string outputPath)
        {
            string folder = Path.GetDirectoryName(outputPath);
            string name = Path.GetFileNameWithoutExtension(outputPath);
            return Path.Combine(folder, "." + name + "." + Guid.NewGuid().ToString("N") + ".tmp.dwg");
        }

        private static void PublishOutput(
            string temporaryPath,
            string outputPath,
            bool overwriteExisting)
        {
            if (!File.Exists(outputPath))
            {
                File.Move(temporaryPath, outputPath);
                return;
            }

            if (!overwriteExisting)
                throw new IOException("Arquivo já existe: " + outputPath);

            File.Replace(temporaryPath, outputPath, null, true);
        }

        private static void EnsureOutputCanBeCreated(string outputPath, bool overwriteExisting)
        {
            if (File.Exists(outputPath) && !overwriteExisting)
                throw new IOException("Arquivo já existe: " + outputPath);
        }

        private static void EnsureOutputDoesNotReplaceSource(string sourcePath, string outputPath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !Path.IsPathRooted(sourcePath)) return;

            if (string.Equals(
                Path.GetFullPath(sourcePath),
                Path.GetFullPath(outputPath),
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "O DWG de saída não pode sobrescrever o desenho aberto: " + outputPath);
            }
        }

        private static void TryDeleteTemporaryFile(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // O arquivo final já foi publicado ou a limpeza poderá ser feita pelo usuário.
            }
        }

        private void Report(Action<string> progress, string message)
        {
            _zwcad.WriteMessage(message);
            progress?.Invoke(message);
        }
    }
}
