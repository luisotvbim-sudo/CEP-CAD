using System;
using System.Collections.Generic;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class FolhaFormatCatalog
    {
        // Nomes de bloco aceitos: somente o padrao exato CEP-*.
        // As dimensoes sao armazenadas como lado menor x lado maior.
        private static readonly IReadOnlyDictionary<string, FolhaFormat> Formats =
            new Dictionary<string, FolhaFormat>(StringComparer.OrdinalIgnoreCase)
            {
                { "CEP-A4", new FolhaFormat("A4", 210.0, 297.0) },
                { "CEP-A3", new FolhaFormat("A3", 297.0, 420.0) },
                { "CEP-A2", new FolhaFormat("A2", 420.0, 594.0) },
                { "CEP-A1", new FolhaFormat("A1", 594.0, 841.0) },
                { "CEP-A0", new FolhaFormat("A0", 841.0, 1189.0) },

                { "CEP-A1E", new FolhaFormat("A1E", 594.0, 1189.0) },
                { "CEP-A0E", new FolhaFormat("A0E", 841.0, 1408.0) }
            };

        public bool TryParse(string blockName, out FolhaFormat format)
        {
            format = null;
            if (string.IsNullOrWhiteSpace(blockName))
            {
                return false;
            }

            return Formats.TryGetValue(blockName.Trim(), out format);
        }

        public bool DimensionsMatch(FolhaFormat format, double width, double height, out string message)
        {
            double actualShort = Math.Min(Math.Abs(width), Math.Abs(height));
            double actualLong = Math.Max(Math.Abs(width), Math.Abs(height));
            double shortTolerance = Math.Max(2.0, format.ShortSide * 0.002);
            double longTolerance = Math.Max(2.0, format.LongSide * 0.002);

            bool shortSideMatches = Math.Abs(actualShort - format.ShortSide) <= shortTolerance;
            bool longSideMatches = Math.Abs(actualLong - format.LongSide) <= longTolerance;

            bool matches = shortSideMatches && longSideMatches;
            message = matches ? null : BuildDimensionMessage(format, actualShort, actualLong);
            return matches;
        }

        private static string BuildDimensionMessage(FolhaFormat format, double actualShort, double actualLong)
        {
            return string.Format(
                "{0} deveria medir {1:0.##} x {2:0.##} mm, mas mede {3:0.##} x {4:0.##} mm",
                format.Name,
                format.ShortSide,
                format.LongSide,
                actualShort,
                actualLong);
        }
    }

    internal sealed class FolhaFormat
    {
        public FolhaFormat(string name, double shortSide, double longSide)
        {
            Name = name;
            ShortSide = shortSide;
            LongSide = longSide;
        }

        public string Name { get; }

        public double ShortSide { get; }

        public double LongSide { get; }
    }
}
