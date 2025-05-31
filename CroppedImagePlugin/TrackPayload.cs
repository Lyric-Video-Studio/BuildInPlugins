using System.ComponentModel;
using System.Text.Json.Serialization;
using PluginBase;

namespace CroppedImagePlugin
{
    public class TrackPayload : INotifyPropertyChanged
    {
        public enum RotateFinalOutput
        {
            None,
            Left,
            Right
        }

        private int width;
        private int height;
        private int xOffset;
        private int yOffset;
        private bool scale;

        [Description("When scaling, set width or height to 0 to scale keeping the aspect ratio")]
        public int Width
        {
            get { return width; }
            set { if (width == value) return; width = value; RaisePropertyChanged(nameof(Width)); }
        }

        [Description("When scaling, set width or height to 0 to scale keeping the aspect ratio")]
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

        public event PropertyChangedEventHandler? PropertyChanged;

        [IgnoreDynamicEdit]
        public int SelectedRotation { get; set; }

        [JsonIgnore]
        public string[] RotateNames => ["None", "Left", "Right"]; //Localizations.AppResources.RotateNone, Localizations.AppResources.RotateLeft, Localizations.AppResources.RotateRigth];

        public string SelectedItemName => SelectedRotation >= 0 && SelectedRotation < RotateNames.Length ? RotateNames[SelectedRotation] : "";

        protected void RaisePropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}