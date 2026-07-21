using System;
using System.IO;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class DwgOutputFile : IDisposable
    {
        private readonly string _outputPath;
        private readonly bool _overwriteExisting;
        private bool _published;

        public DwgOutputFile(
            string sourcePath,
            string outputPath,
            bool overwriteExisting)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Arquivo de saída obrigatório.", nameof(outputPath));

            EnsureDoesNotReplaceSource(sourcePath, outputPath);
            if (File.Exists(outputPath) && !overwriteExisting)
                throw new IOException("Arquivo já existe: " + outputPath);

            _outputPath = outputPath;
            _overwriteExisting = overwriteExisting;
            TemporaryPath = CreateTemporaryPath(outputPath);
        }

        public string TemporaryPath { get; }

        public void Publish()
        {
            if (!File.Exists(TemporaryPath))
                throw new IOException("O arquivo DWG temporário não foi gerado: " + TemporaryPath);

            if (!File.Exists(_outputPath))
            {
                File.Move(TemporaryPath, _outputPath);
            }
            else if (_overwriteExisting)
            {
                File.Replace(TemporaryPath, _outputPath, null, true);
            }
            else
            {
                throw new IOException("Arquivo já existe: " + _outputPath);
            }

            _published = true;
        }

        public void VerifyPublished()
        {
            if (!_published || !File.Exists(_outputPath))
                throw new IOException("DWG não foi gerado: " + _outputPath);
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(TemporaryPath)) File.Delete(TemporaryPath);
            }
            catch
            {
                // Uma falha de limpeza não invalida um arquivo final já publicado.
            }
        }

        private static string CreateTemporaryPath(string outputPath)
        {
            string folder = Path.GetDirectoryName(outputPath);
            string name = Path.GetFileNameWithoutExtension(outputPath);
            return Path.Combine(folder, "." + name + "." + Guid.NewGuid().ToString("N") + ".tmp.dwg");
        }

        private static void EnsureDoesNotReplaceSource(string sourcePath, string outputPath)
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
    }
}
