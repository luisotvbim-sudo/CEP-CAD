using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PluginConceito.Application.Presentation;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class StampSelectionViewModel : ObservableObject
    {
        private string _selectedBlock;
        private string _selectedAttribute;

        public StampSelectionViewModel(IEnumerable<string> blockNames)
        {
            BlockNames = new ObservableCollection<string>(
                (blockNames ?? Enumerable.Empty<string>()).Where(value => value != null));
            Attributes = new ObservableCollection<string>();
        }

        public ObservableCollection<string> BlockNames { get; }

        public ObservableCollection<string> Attributes { get; }

        public string SelectedBlock
        {
            get { return _selectedBlock; }
            set { SetProperty(ref _selectedBlock, value); }
        }

        public string SelectedAttribute
        {
            get { return _selectedAttribute; }
            set { SetProperty(ref _selectedAttribute, value); }
        }

        public void SetAttributes(IEnumerable<string> attributes)
        {
            string current = SelectedAttribute;
            Attributes.Clear();

            foreach (string attribute in
                (attributes ?? Enumerable.Empty<string>()).Where(value => value != null))
            {
                Attributes.Add(attribute);
            }

            SelectedAttribute = !string.IsNullOrWhiteSpace(current) &&
                Attributes.Contains(current)
                ? current
                : Attributes.FirstOrDefault();
        }
    }
}
