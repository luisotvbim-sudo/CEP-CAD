using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed partial class PlotFolhasWindow : Window
    {
        private readonly PlotFolhasViewModel _viewModel;

        public PlotFolhasWindow(
            IEnumerable<FolhaInfo> sheets,
            IEnumerable<string> devices,
            IEnumerable<string> plotStyles,
            string defaultOutputFolder,
            bool useAutomaticEmissionFolder,
            string defaultDevice,
            string defaultPlotStyle,
            string namingSeparator,
            IReadOnlyList<string> namingParts,
            IEnumerable<string> stampBlockNames,
            SheetSpaceKind sourceSpace,
            string sourceLayoutName)
        {
            InitializeComponent();
            _viewModel = new PlotFolhasViewModel(
                sheets,
                devices,
                plotStyles,
                defaultOutputFolder,
                useAutomaticEmissionFolder,
                defaultDevice,
                defaultPlotStyle,
                namingSeparator,
                namingParts,
                stampBlockNames,
                sourceSpace,
                sourceLayoutName);
            DataContext = _viewModel;
        }

        public event EventHandler ApplyStructuredNameRequested;
        public event EventHandler FileNameEdited;
        public event EventHandler ZoomRequested;
        public event EventHandler SaveNamesRequested;
        public event EventHandler PlotRequested;
        public event EventHandler StampBlockChanged;
        public event EventHandler LoadNamesFromStampRequested;
        public event EventHandler RefreshRequested;

        public string NamingSeparator { get { return _viewModel.NamingStructure.Separator; } }
        public IReadOnlyList<string> NamingParts { get { return _viewModel.NamingStructure.GetValues(); } }
        public IReadOnlyList<bool> NamingPartSequential { get { return _viewModel.NamingStructure.GetSequentialFlags(); } }
        public string OutputFolder { get { return _viewModel.Output.OutputFolder; } }
        public bool UseAutomaticEmissionFolder { get { return _viewModel.Output.UseAutomaticEmissionFolder; } }
        public string AutomaticEmissionBaseFolder { get { return _viewModel.Output.AutomaticEmissionBaseFolder; } }
        public string DeviceName { get { return _viewModel.Output.DeviceName; } }
        public string CtbName { get { return _viewModel.Output.CtbName; } }
        public bool OverwriteExisting { get { return _viewModel.Output.OverwriteExisting; } }
        public FolhaInfo SelectedSheet { get { return _viewModel.SelectedSheet; } }
        public FolhaInfo EditedSheet { get; private set; }
        public IReadOnlyList<FolhaInfo> Sheets { get { return _viewModel.SheetCollection.Items.ToList(); } }
        public string SelectedStampBlock { get { return _viewModel.StampSelection.SelectedBlock; } }
        public string SelectedStampAttribute { get { return _viewModel.StampSelection.SelectedAttribute; } }

        public void CommitChanges()
        {
            SheetsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            SheetsGrid.CommitEdit(DataGridEditingUnit.Row, true);
            Keyboard.ClearFocus();
        }

        public void RefreshSheets()
        {
            _viewModel.SheetCollection.Refresh();
            SheetsGrid.Items.Refresh();
        }

        public void SetBusy(bool busy)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action<bool>(SetBusy), busy);
                return;
            }

            _viewModel.IsBusy = busy;
            Mouse.OverrideCursor = busy ? Cursors.Wait : null;
        }

        public void SetStatusMessage(string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action<string>(SetStatusMessage), message);
                return;
            }

            _viewModel.StatusMessage = message;
        }

        public void SetResolvedOutputFolder(string outputFolder)
        {
            _viewModel.Output.SetResolvedOutputFolder(outputFolder);
        }

        public void SetStampAttributes(IEnumerable<string> attributes)
        {
            _viewModel.StampSelection.SetAttributes(attributes);
        }

        public void ProcessPendingUiMessages()
        {
            if (!Dispatcher.CheckAccess()) return;
            Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));
        }

        private void ApplyStructuredNameClick(object sender, RoutedEventArgs e)
        {
            Raise(ApplyStructuredNameRequested);
        }

        private void AddNamingPartClick(object sender, RoutedEventArgs e)
        {
            _viewModel.AddNamingPart();
        }

        private void RemoveNamingPartClick(object sender, RoutedEventArgs e)
        {
            _viewModel.RemoveNamingPart();
        }

        private void SelectAllPdfClick(object sender, RoutedEventArgs e) { _viewModel.SheetCollection.SetAllPdf(true); }
        private void ClearAllPdfClick(object sender, RoutedEventArgs e) { _viewModel.SheetCollection.SetAllPdf(false); }
        private void SelectAllDwgClick(object sender, RoutedEventArgs e) { _viewModel.SheetCollection.SetAllDwg(true); }
        private void ClearAllDwgClick(object sender, RoutedEventArgs e) { _viewModel.SheetCollection.SetAllDwg(false); }

        private void ZoomSheetClick(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            FolhaInfo sheet = button?.Tag as FolhaInfo;
            if (sheet != null) _viewModel.SelectedSheet = sheet;
            Raise(ZoomRequested);
        }

        private void SaveNamesClick(object sender, RoutedEventArgs e)
        {
            Raise(SaveNamesRequested);
        }

        private void PlotClick(object sender, RoutedEventArgs e)
        {
            Raise(PlotRequested);
        }

        private void BrowseFolderClick(object sender, RoutedEventArgs e)
        {
            using (var dialog = new WinForms.FolderBrowserDialog())
            {
                dialog.Description = "Escolha a pasta de saída dos arquivos";
                dialog.SelectedPath = OutputFolder;
                dialog.ShowNewFolderButton = true;
                if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                {
                    _viewModel.Output.ChooseOutputFolder(dialog.SelectedPath);
                }
            }
        }

        private void OnStampBlockSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Raise(StampBlockChanged);
        }

        private void LoadNamesFromStampClick(object sender, RoutedEventArgs e)
        {
            Raise(LoadNamesFromStampRequested);
        }

        private void RefreshClick(object sender, RoutedEventArgs e)
        {
            Raise(RefreshRequested);
        }

        private void OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column == null || !string.Equals(Convert.ToString(e.Column.Header), "Nome do arquivo", StringComparison.Ordinal))
            {
                return;
            }

            EditedSheet = e.Row?.Item as FolhaInfo;
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => Raise(FileNameEdited)));
        }

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            while (source != null)
            {
                if (source is TextBox || source is ComboBox || source is ComboBoxItem ||
                    source is CheckBox || source is Button || source is DataGridCell)
                    return;
                source = VisualTreeHelper.GetParent(source);
            }

            if (e.OriginalSource is UIElement element && element.Focusable) return;
            Keyboard.ClearFocus();
        }

        private void Raise(EventHandler handler)
        {
            handler?.Invoke(this, EventArgs.Empty);
        }
    }
}
