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

        public enum ScaleDimension
        {
            Width,
            Height
        }

        private int width;
        private int height;
        private int xOffset;
        private int yOffset;
        private bool scale;
        private bool lockAspectRatio;
        private int selectedScaleDimension;
        private double sourceAspectRatio;
        private bool synchronizingAspectRatio;

        [Description("Output width in pixels")]
        public int Width
        {
            get { return width; }
            set
            {
                if (width == value) return;
                width = value;
                RaisePropertyChanged(nameof(Width));

                if (!synchronizingAspectRatio && Scale && LockAspectRatio && SelectedScaleDimension == (int)ScaleDimension.Width)
                {
                    SynchronizeAspectRatio();
                }
            }
        }

        [Description("Output height in pixels")]
        public int Height
        {
            get { return height; }
            set
            {
                if (height == value) return;
                height = value;
                RaisePropertyChanged(nameof(Height));

                if (!synchronizingAspectRatio && Scale && LockAspectRatio && SelectedScaleDimension == (int)ScaleDimension.Height)
                {
                    SynchronizeAspectRatio();
                }
            }
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
            set
            {
                if (scale == value) return;
                scale = value;
                RaisePropertyChanged(nameof(Scale));
                RaisePropertyChanged(nameof(IsCrop));
                RaiseDimensionEditingPropertiesChanged();

                if (scale && LockAspectRatio)
                {
                    SynchronizeAspectRatio();
                }
            }
        }

        public bool IsCrop => !Scale;

        [Description("Keep the source image aspect ratio while scaling")]
        public bool LockAspectRatio
        {
            get => lockAspectRatio;
            set
            {
                if (lockAspectRatio == value) return;
                lockAspectRatio = value;
                RaisePropertyChanged(nameof(LockAspectRatio));
                RaiseDimensionEditingPropertiesChanged();

                if (lockAspectRatio && Scale)
                {
                    SynchronizeAspectRatio();
                }
            }
        }

        [Description("Dimension used to calculate the other dimension when the aspect ratio is locked: 0 = width, 1 = height")]
        public int SelectedScaleDimension
        {
            get => selectedScaleDimension;
            set
            {
                var normalizedValue = value == (int)ScaleDimension.Height
                    ? (int)ScaleDimension.Height
                    : (int)ScaleDimension.Width;

                if (selectedScaleDimension == normalizedValue) return;
                selectedScaleDimension = normalizedValue;
                RaisePropertyChanged(nameof(SelectedScaleDimension));
                RaiseDimensionEditingPropertiesChanged();

                if (Scale && LockAspectRatio)
                {
                    SynchronizeAspectRatio();
                }
            }
        }

        [JsonIgnore]
        public string[] ScaleDimensionNames => [Localizations.AppResources.Width, Localizations.AppResources.Height];

        [JsonIgnore]
        public bool CanEditWidth => !Scale || !LockAspectRatio || SelectedScaleDimension == (int)ScaleDimension.Width;

        [JsonIgnore]
        public bool CanEditHeight => !Scale || !LockAspectRatio || SelectedScaleDimension == (int)ScaleDimension.Height;

        public event PropertyChangedEventHandler? PropertyChanged;

        [IgnoreDynamicEdit]
        public int SelectedRotation { get; set; }

        [JsonIgnore]
        public string[] RotateNames => ["None", "Left", "Right"]; //Localizations.AppResources.RotateNone, Localizations.AppResources.RotateLeft, Localizations.AppResources.RotateRigth];

        public string SelectedItemName => SelectedRotation >= 0 && SelectedRotation < RotateNames.Length ? RotateNames[SelectedRotation] : "";

        internal void SetSourceDimensions(double sourceWidth, double sourceHeight)
        {
            sourceAspectRatio = sourceWidth > 0 && sourceHeight > 0
                ? sourceWidth / sourceHeight
                : 0;

            if (Scale && LockAspectRatio)
            {
                SynchronizeAspectRatio();
            }
        }

        private void SynchronizeAspectRatio()
        {
            if (sourceAspectRatio <= 0 || synchronizingAspectRatio)
            {
                return;
            }

            synchronizingAspectRatio = true;
            try
            {
                if (SelectedScaleDimension == (int)ScaleDimension.Height)
                {
                    if (Height > 0)
                    {
                        Width = Math.Max(1, (int)Math.Round(Height * sourceAspectRatio));
                    }
                }
                else if (Width > 0)
                {
                    Height = Math.Max(1, (int)Math.Round(Width / sourceAspectRatio));
                }
            }
            finally
            {
                synchronizingAspectRatio = false;
            }
        }

        private void RaiseDimensionEditingPropertiesChanged()
        {
            RaisePropertyChanged(nameof(CanEditWidth));
            RaisePropertyChanged(nameof(CanEditHeight));
        }

        protected void RaisePropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
