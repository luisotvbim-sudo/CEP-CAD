using System;
using PluginConceito.Application.Presentation;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class NamingPartViewModel : ObservableObject
    {
        private string _value;
        private bool _isSequential;
        private bool _isRevision;

        public NamingPartViewModel(string value)
        {
            _value = value ?? string.Empty;
        }

        public string Value
        {
            get { return _value; }
            set
            {
                string normalized = value ?? string.Empty;
                if (normalized.Length > 6) normalized = normalized.Substring(0, 6);
                SetProperty(ref _value, normalized);
            }
        }

        public bool IsSequential
        {
            get { return _isSequential; }
            set
            {
                if (!SetProperty(ref _isSequential, value))
                {
                    return;
                }

                if (value && _isRevision)
                {
                    _isRevision = false;
                    OnPropertyChanged(nameof(IsRevision));
                }
            }
        }

        public bool IsRevision
        {
            get { return _isRevision; }
            set
            {
                if (!SetProperty(ref _isRevision, value))
                {
                    return;
                }

                if (value && _isSequential)
                {
                    _isSequential = false;
                    OnPropertyChanged(nameof(IsSequential));
                }
            }
        }
    }
}
