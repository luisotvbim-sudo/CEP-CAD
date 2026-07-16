using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class ParsedName
    {
        public ParsedName(string separator, IEnumerable<string> parts)
        {
            Separator = string.IsNullOrEmpty(separator) ? "-" : separator;
            Parts = new ReadOnlyCollection<string>((parts ?? Enumerable.Empty<string>())
                .Select(part => part ?? string.Empty)
                .ToList());
        }

        public string Separator { get; private set; }

        public IReadOnlyList<string> Parts { get; private set; }

        public bool HasParts
        {
            get { return Parts.Count > 0; }
        }
    }
}
