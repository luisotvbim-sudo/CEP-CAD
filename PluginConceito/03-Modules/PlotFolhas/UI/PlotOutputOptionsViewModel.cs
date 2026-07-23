using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PluginConceito.Application.Presentation;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotOutputOptionsViewModel : ObservableObject
    {
        private string _outputFolder;
        private readonly string _automaticEmissionBaseFolder;
        private bool _outputFolderChosenByUser;
        private string _deviceName;
        private string _ctbName;
        private bool _overwriteExisting;

        public PlotOutputOptionsViewModel(
            IEnumerable<string> devices,
            IEnumerable<string> plotStyles,
            string defaultOutputFolder,
            bool useAutomaticEmissionFolder,
            string defaultDevice,
            string defaultPlotStyle)
        {
            Devices = CreateOptions(devices);
            PlotStyles = CreateOptions(plotStyles);
            _outputFolder = defaultOutputFolder ?? string.Empty;
            _automaticEmissionBaseFolder = useAutomaticEmissionFolder
                ? _outputFolder
                : null;
            DeviceName = SelectValue(Devices, defaultDevice);
            CtbName = SelectValue(PlotStyles, defaultPlotStyle);
        }

        public ObservableCollection<string> Devices { get; }

        public ObservableCollection<string> PlotStyles { get; }

        public string OutputFolder
        {
            get { return _outputFolder; }
            set
            {
                if (!SetProperty(ref _outputFolder, value)) return;
                _outputFolderChosenByUser = true;
                OnPropertyChanged(nameof(UseAutomaticEmissionFolder));
            }
        }

        public bool UseAutomaticEmissionFolder =>
            !_outputFolderChosenByUser &&
            !string.IsNullOrWhiteSpace(_automaticEmissionBaseFolder);

        public string AutomaticEmissionBaseFolder => _automaticEmissionBaseFolder;

        public string DeviceName
        {
            get { return _deviceName; }
            set { SetProperty(ref _deviceName, value); }
        }

        public string CtbName
        {
            get { return _ctbName; }
            set { SetProperty(ref _ctbName, value); }
        }

        public bool OverwriteExisting
        {
            get { return _overwriteExisting; }
            set { SetProperty(ref _overwriteExisting, value); }
        }

        public void ChooseOutputFolder(string outputFolder)
        {
            _outputFolderChosenByUser = true;
            SetProperty(
                ref _outputFolder,
                outputFolder ?? string.Empty);
            OnPropertyChanged(nameof(UseAutomaticEmissionFolder));
        }

        public void SetResolvedOutputFolder(string outputFolder)
        {
            SetProperty(
                ref _outputFolder,
                outputFolder ?? string.Empty);
        }

        private static ObservableCollection<string> CreateOptions(
            IEnumerable<string> values)
        {
            return new ObservableCollection<string>(
                (values ?? Enumerable.Empty<string>()).Where(value => value != null));
        }

        private static string SelectValue(
            IEnumerable<string> values,
            string preferred)
        {
            List<string> available = values.ToList();
            string match = available.FirstOrDefault(value => string.Equals(
                value,
                preferred,
                StringComparison.OrdinalIgnoreCase));
            return match ?? available.FirstOrDefault();
        }
    }
}
