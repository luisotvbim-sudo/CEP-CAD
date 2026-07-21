using System;
using System.ComponentModel;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class NamingPartViewModel : INotifyPropertyChanged
    {
        private string _value;
        private bool _isSequential;

        public NamingPartViewModel(string value)
        {
            _value = value ?? string.Empty;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Value
        {
            get { return _value; }
            set
            {
                string normalized = value ?? string.Empty;
                if (normalized.Length > 6) normalized = normalized.Substring(0, 6);
                if (string.Equals(_value, normalized, StringComparison.Ordinal)) return;
                _value = normalized;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        public bool IsSequential
        {
            get { return _isSequential; }
            set
            {
                if (_isSequential == value) return;
                _isSequential = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSequential)));
            }
        }
    }
}
