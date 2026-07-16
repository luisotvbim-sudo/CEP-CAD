using System;
using System.IO;
using System.Linq;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class NamingStandardParser
    {
        private static readonly char[] PreferredSeparators = { '-', '_', '.' };

        public ParsedName Parse(string fileName)
        {
            string baseName = Path.GetFileNameWithoutExtension(fileName ?? string.Empty).Trim();
            char separator = DetectSeparator(baseName);
            if (separator == '\0') return new ParsedName("-", new[] { baseName });

            return new ParsedName(separator.ToString(), baseName.Split(new[] { separator }, StringSplitOptions.None));
        }

        private static char DetectSeparator(string value)
        {
            var bestCandidate = PreferredSeparators
                .Select(separator => new { Separator = separator, Count = value.Count(character => character == separator) })
                .OrderByDescending(candidate => candidate.Count)
                .ThenBy(candidate => Array.IndexOf(PreferredSeparators, candidate.Separator))
                .First();
            return bestCandidate.Count > 0 ? bestCandidate.Separator : '\0';
        }
    }
}
