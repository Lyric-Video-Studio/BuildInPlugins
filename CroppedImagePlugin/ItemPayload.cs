using PluginBase;
using System.ComponentModel;

namespace CroppedImagePlugin
{
    public class ItemPayload : INotifyPropertyChanged
    {
        private string source;

        [EditorWidth(400)]
        public string Source
        {
            get { return source; }
            set { source = value; RaisePropertyChanged(nameof(Source)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}