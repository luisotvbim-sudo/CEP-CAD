using System.Windows;

namespace PluginConceito.Modules.InsertNotes
{
    internal sealed partial class InsertNotesWindow
    {
        private readonly InsertNotesViewModel _viewModel;

        public InsertNotesWindow(InsertNotesViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;
        }

        private void OnBuscarNotasClick(object sender, RoutedEventArgs e)
        {
            string error = _viewModel.Validate();

            if (error != null)
            {
                MessageBox.Show(error, "Validacao", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("Campos validados com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
