using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PluginConceito.Modules.InsertNotes
{
    internal sealed class NoteViewModel : INotifyPropertyChanged
    {
        private bool _isChecked;

        public string Name { get; }

        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked == value)
                {
                    return;
                }

                _isChecked = value;
                OnPropertyChanged();
            }
        }

        public NoteViewModel(string name)
        {
            Name = name;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
