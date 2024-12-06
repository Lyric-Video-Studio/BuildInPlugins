using Avalonia.Media.Imaging;
using PluginBase;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace CroppedImagePlugin
{
    public class ItemPayload : INotifyPropertyChanged
    {
        private string source;

        [EditorWidth(400)]
        public string Source
        {
            get { return source; }
            set { source = value; RaisePropertyChanged(nameof(Source)); RaisePropertyChanged(nameof(SourceBitmap)); }
        }

        [JsonIgnore]
        public Bitmap SourceBitmap => File.Exists(Source) ? new Bitmap(Source) : null;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}