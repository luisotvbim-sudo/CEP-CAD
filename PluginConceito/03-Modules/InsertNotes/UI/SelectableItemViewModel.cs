using System;
using PluginConceito.Application.Presentation;

namespace PluginConceito.Modules.InsertNotes
{
    internal abstract class SelectableItemViewModel : ObservableObject
    {
        private bool _isChecked;

        protected SelectableItemViewModel(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "O nome do item é obrigatório.",
                    nameof(name));
            }

            Name = name;
        }

        public string Name { get; }

        public bool IsChecked
        {
            get { return _isChecked; }
            set { SetProperty(ref _isChecked, value); }
        }
    }
}
