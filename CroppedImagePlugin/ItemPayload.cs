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
            set 
            {
                if (source != value)
                {
                    sourceBitmap = null;
                }
                source = value; 
                RaisePropertyChanged(nameof(Source)); 
                RaisePropertyChanged(nameof(SourceBitmap)); 
            }
        }

        private Bitmap sourceBitmap;

        [JsonIgnore]
        public Bitmap SourceBitmap 
        {
            get
            {
                if(sourceBitmap != null)
                {
                    return sourceBitmap;
                }
                sourceBitmap = File.Exists(Source) ? new Bitmap(Source) : null;
                return sourceBitmap;
            }            
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}