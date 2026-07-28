using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        public NamingStructureDefinition CreateDefinition()
        {
            return new NamingStructureDefinition(
                Separator,
                Parts.Select(part => new NamingFieldDefinition(
                    part.Value,
                    part.IsSequential,
                    part.IsRevision)));
        }

        public string TryAddPart()
        {
            if (Parts.Count >= MaximumParts)
            {
                return "A estrutura aceita no máximo " +
                    MaximumParts + " campos.";
            }

            AddPart(new NamingPartViewModel(string.Empty));
            return null;
        }

        public string TryRemovePart()
        {
            if (Parts.Count <= MinimumParts)
            {
                return "A estrutura deve ter pelo menos " + MinimumParts + " campos.";
            }

            NamingPartViewModel removed = Parts[Parts.Count - 1];
            removed.PropertyChanged -= OnPartPropertyChanged;
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
                AddPart(new NamingPartViewModel(value));
            }
        }

        private void AddPart(NamingPartViewModel part)
        {
            part.PropertyChanged += OnPartPropertyChanged;
            Parts.Add(part);
        }

        private void OnPartPropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(NamingPartViewModel.IsRevision))
            {
                return;
            }

            var selectedPart = sender as NamingPartViewModel;
            if (selectedPart == null || !selectedPart.IsRevision)
            {
                return;
            }

            foreach (NamingPartViewModel part in Parts)
            {
                if (!ReferenceEquals(part, selectedPart))
                {
                    part.IsRevision = false;
                }
            }
        }
    }
}
