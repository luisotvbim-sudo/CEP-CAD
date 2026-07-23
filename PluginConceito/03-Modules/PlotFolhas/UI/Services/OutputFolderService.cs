using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class OutputFolderService
    {
        public IReadOnlyList<string> FindExistingFiles(
            PlotOutputPlan plan,
            string outputFolder)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var existingFiles = new List<string>();
            foreach (FolhaInfo sheet in plan.SelectedSheets)
            {
                AddIfExisting(
                    existingFiles,
                    outputFolder,
                    sheet.NomeArquivo,
                    sheet.Plotar);
                AddIfExisting(
                    existingFiles,
                    outputFolder,
                    Path.ChangeExtension(sheet.NomeArquivo, ".dwg"),
                    sheet.GerarDwg);
            }

            return existingFiles;
        }

        public string Prepare(
            string outputFolder,
            bool useAutomaticEmissionFolder,
            string automaticEmissionBaseFolder)
        {
            string folder = useAutomaticEmissionFolder
                ? automaticEmissionBaseFolder
                : outputFolder;
            if (string.IsNullOrWhiteSpace(folder))
            {
                throw new ArgumentException(
                    "Escolha uma pasta de saída.",
                    nameof(outputFolder));
            }

            return useAutomaticEmissionFolder
                ? CreateNextEmissionFolder(folder)
                : Directory.CreateDirectory(folder).FullName;
        }

        public string TryOpen(string outputFolder)
        {
            if (string.IsNullOrWhiteSpace(outputFolder) ||
                !Directory.Exists(outputFolder))
            {
                return null;
            }

            try
            {
                Process process = Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "/e,\"" + outputFolder + "\"",
                    WorkingDirectory = outputFolder,
                    UseShellExecute = true
                });
                return process == null
                    ? "Arquivos gerados, mas o Windows Explorer não foi iniciado."
                    : null;
            }
            catch (Exception exception)
            {
                return "Arquivos gerados, mas não foi possível abrir a pasta: " +
                    exception.Message;
            }
        }

        private static string CreateNextEmissionFolder(string baseFolder)
        {
            Directory.CreateDirectory(baseFolder);

            for (int number = 1; number < int.MaxValue; number++)
            {
                string folderName = "Emissão " +
                    number.ToString("00", CultureInfo.InvariantCulture);
                string candidate = Path.Combine(baseFolder, folderName);
                if (Directory.Exists(candidate) || File.Exists(candidate))
                {
                    continue;
                }

                Directory.CreateDirectory(candidate);
                return candidate;
            }

            throw new IOException(
                "Não foi possível determinar o próximo número de emissão.");
        }

        private static void AddIfExisting(
            ICollection<string> existingFiles,
            string outputFolder,
            string fileName,
            bool selected)
        {
            if (!selected)
            {
                return;
            }

            string path = Path.Combine(outputFolder, fileName);
            if (File.Exists(path))
            {
                existingFiles.Add(path);
            }
        }
    }
}
