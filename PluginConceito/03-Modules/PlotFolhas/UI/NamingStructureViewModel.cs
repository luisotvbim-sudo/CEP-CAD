using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PluginConceito.Application.Presentation;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class NamingStructureViewModel : ObservableObject
    {
        private const int MinimumParts = 4;
        private const int MaximumParts = 10;
        private string _separator = "-";

        public NamingStructureViewModel(
            string separator,
            IReadOnlyList<string> parts)
        {
            Parts = new ObservableCollection<NamingPartViewModel>();
            Load(separator, parts);
        }

        public ObservableCollection<NamingPartViewModel> Parts { get; }

        public string Separator
        {
            get { return _separator; }
            set
            {
                string normalized = string.IsNullOrEmpty(value)
                    ? string.Empty
                    : value.Substring(0, 1);
                SetProperty(ref _separator, normalized);
            }
        }

        public IReadOnlyList<string> GetValues()
        {
            return Parts.Select(part => part.Value ?? string.Empty).ToList();
        }

        public IReadOnlyList<bool> GetSequentialFlags()
        {
            return Parts.Select(part => part.IsSequential).ToList();
        }

        public string TryAddPart()
        {
            if (Parts.Count >= MaximumParts)
            {
                return "A estrutura aceita no máximo " +
                    MaximumParts + " campos.";
            }

            Parts.Add(new NamingPartViewModel(string.Empty));
            return null;
        }

        public string TryRemovePart()
        {
            if (Parts.Count <= MinimumParts)
            {
                return "A estrutura deve ter pelo menos " + MinimumParts + " campos.";
            }

            Parts.RemoveAt(Parts.Count - 1);
            return null;
        }

        private void Load(string separator, IReadOnlyList<string> parts)
        {
            IReadOnlyList<string> values = parts ?? new List<string>();
            Separator = string.IsNullOrEmpty(separator) ? "-" : separator;

            int count = Math.Max(MinimumParts, values.Count);
            count = Math.Min(MaximumParts, count);
            for (int index = 0; index < count; index++)
            {
                string value = index < values.Count ? values[index] : string.Empty;
                Parts.Add(new NamingPartViewModel(value));
            }
        }
    }
}
