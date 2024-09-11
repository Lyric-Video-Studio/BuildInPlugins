using System.ComponentModel;

namespace CroppedImagePlugin
{
    internal class TrackPayload : INotifyPropertyChanged
    {
        private int width;
        private int height;
        private int xOffset;
        private int yOffset;
        private bool scale;

        public int Width
        {
            get { return width; }
            set { if (width == value) return; width = value; RaisePropertyChanged(nameof(Width)); }
        }

        public int Height
        {
            get { return height; }
            set { if (height == value) return; height = value; RaisePropertyChanged(nameof(Height)); }
        }

        public int XOffset
        {
            get { return xOffset; }
            set { if (xOffset == value) return; xOffset = value; RaisePropertyChanged(nameof(XOffset)); }
        }

        public int YOffset
        {
            get { return yOffset; }
            set { if (yOffset == value) return; yOffset = value; RaisePropertyChanged(nameof(YOffset)); }
        }

        [Description("Scale image instead of cropping. x & y offsets & center does not effect, only width and heigth")]
        public bool Scale
        {
            get => scale;
            set { scale = value; RaisePropertyChanged(nameof(Scale)); RaisePropertyChanged(nameof(IsCrop)); }
        }

        public bool IsCrop => !Scale;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}