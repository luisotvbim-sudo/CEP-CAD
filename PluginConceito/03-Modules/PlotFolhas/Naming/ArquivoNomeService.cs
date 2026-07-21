using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class ArquivoNomeService
    {
        private static readonly HashSet<char> InvalidCharacters =
            new HashSet<char>(Path.GetInvalidFileNameChars());

        public string SanitizePart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var result = new StringBuilder();
            bool lastWasSeparator = false;

            foreach (char character in value.Trim())
            {
                bool separator = char.IsWhiteSpace(character) || character == '-' || character == '_';
                if (InvalidCharacters.Contains(character))
                {
                    continue;
                }

                if (separator)
                {
                    if (!lastWasSeparator && result.Length > 0)
                    {
                        result.Append('_');
                    }

                    lastWasSeparator = true;
                    continue;
                }

                result.Append(character);
                lastWasSeparator = false;
            }

            return result.ToString().Trim('_', '.');
        }

        public string BuildAutomaticName(string baseName)
        {
            return NormalizeManualName(baseName);
        }

        public string BuildStructuredName(string separator, IEnumerable<string> parts)
        {
            string safeSeparator = SanitizeSeparator(separator);
            string value = string.Join(safeSeparator, (parts ?? Enumerable.Empty<string>())
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(SanitizePart));
            return AddPdfExtension(value);
        }

        private static string SanitizeSeparator(string separator)
        {
            if (string.IsNullOrEmpty(separator))
            {
                return string.Empty;
            }

            char character = separator[0];
            return InvalidCharacters.Contains(character) || char.IsWhiteSpace(character)
                ? string.Empty
                : character.ToString();
        }

        private static string AddPdfExtension(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            const string extension = ".pdf";
            string fileName = value.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
                ? value.Substring(0, value.Length - extension.Length)
                : value;
            return fileName + extension;
        }

        public string NormalizeManualName(string value)
        {
            string withoutExtension = Path.GetFileNameWithoutExtension(value ?? string.Empty);
            string safeName = SanitizePart(withoutExtension);
            return string.IsNullOrWhiteSpace(safeName) ? string.Empty : safeName + ".pdf";
        }

        public string NormalizeInlineName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string fileName = Path.GetFileName(value.Trim());
            if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                fileName = fileName.Substring(0, fileName.Length - 4);
            }

            var result = new StringBuilder();
            foreach (char character in fileName)
            {
                if (!InvalidCharacters.Contains(character))
                {
                    result.Append(char.IsWhiteSpace(character) ? '_' : character);
                }
            }

            return AddPdfExtension(result.ToString().Trim('_', '.'));
        }

        public void ValidateNames(IEnumerable<FolhaInfo> sheets)
        {
            List<FolhaInfo> list = sheets.ToList();
            foreach (FolhaInfo sheet in list)
            {
                sheet.ErroNome = ValidateSingleName(sheet.NomeArquivo);
            }

            var duplicates = list
                .Where(sheet => !string.IsNullOrWhiteSpace(sheet.NomeArquivo))
                .GroupBy(sheet => sheet.NomeArquivo, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1);

            foreach (var group in duplicates)
            {
                foreach (FolhaInfo sheet in group)
                {
                    sheet.ErroNome = "nome de arquivo duplicado";
                }
            }
        }

        private static string ValidateSingleName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "nome de arquivo vazio";
            }

            if (!string.Equals(Path.GetExtension(fileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return "a extensão deve ser .pdf";
            }

            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return "nome contém caracteres inválidos";
            }

            if (fileName.Length > 180)
            {
                return "nome de arquivo muito longo";
            }

            return null;
        }
    }
}
