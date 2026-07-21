namespace PluginConceito.Modules.InsertNotes
{
    internal sealed partial class InsertNotesWindow
    {
        public InsertNotesWindow(InsertNotesViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
