using System;
using System.ComponentModel;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class NamingHeader : INotifyPropertyChanged
    {
        public const int MaximumParts = 10;
        private readonly string[] _parts = new string[MaximumParts];
        private string _separator = "-";
        private int _visiblePartCount = 4;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Separator
        {
            get { return _separator; }
            set { SetValue(ref _separator, value, nameof(Separator)); }
        }

        public int VisiblePartCount
        {
            get { return _visiblePartCount; }
            set { SetValue(ref _visiblePartCount, Math.Max(1, Math.Min(MaximumParts, value)), nameof(VisiblePartCount)); }
        }

        public string GetPart(int index) { return _parts[index] ?? string.Empty; }

        public void SetPart(int index, string value)
        {
            if (string.Equals(_parts[index], value, StringComparison.Ordinal)) return;
            _parts[index] = value;
            RaisePropertyChanged("Part" + index);
        }

        private void SetValue<T>(ref T field, T value, string propertyName)
        {
            if (Equals(field, value)) return;
            field = value;
            RaisePropertyChanged(propertyName);
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
