using System.Collections.Generic;

namespace PluginConceito.Modules.InsertNotes
{
    internal sealed class DisciplineViewModel : SelectableItemViewModel
    {
        public List<DisciplineViewModel> Children { get; } = new List<DisciplineViewModel>();

        public DisciplineViewModel(string name)
            : base(name)
        {
        }
    }
}
