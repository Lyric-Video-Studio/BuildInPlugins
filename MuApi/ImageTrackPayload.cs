using MuApiPlugin.Models.GptImage2;
using MuApiPlugin.Models.MidjourneyV8;
using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin
{
    public class ImageTrackPayload : IPayloadPropertyVisibility
    {
        public event EventHandler ModelChanged;
        private string model = GptImage2TrackPayload.ModelTxtToImg;

        [PropertyComboOptions([GptImage2TrackPayload.ModelTxtToImg, GptImage2TrackPayload.ModelImgToImg, MidjourneyV8TrackPayload.ModelTxtToImg])]
        public string Model
        {
            get => model;
            set
            {
                var notifi = IPayloadPropertyVisibility.UserInitiatedSet && model != value;
                model = value;

                if (notifi)
                {
                    ModelChanged?.Invoke(this, null);
                }
            }
        }

        [HideAllChildren]
        [ParentName("")]
        public GptImage2TrackPayload GptImage2 { get; set; } = new();

        [HideAllChildren]
        [ParentName("")]
        public MidjourneyV8TrackPayload MidjourneyV8 { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is ImageTrackPayload tp)
            {
                switch (propertyName)
                {
                    case nameof(GptImage2):
                        return IsGptImage2(tp);
                    case nameof(MidjourneyV8):
                        return IsMidjourneyV8(tp);
                    default:
                        break;
                }

                if (IsGptImage2(tp))
                {
                    return tp.GptImage2.ShouldPropertyBeVisible(propertyName, model);
                }

                if (IsMidjourneyV8(tp))
                {
                    return tp.MidjourneyV8.ShouldPropertyBeVisible(propertyName, model);
                }
            }

            return true;
        }

        public static bool IsGptImage2(ImageTrackPayload tp)
        {
            return tp.Model is GptImage2TrackPayload.ModelTxtToImg or GptImage2TrackPayload.ModelImgToImg;
        }

        public static bool IsMidjourneyV8(ImageTrackPayload tp)
        {
            return tp.Model == MidjourneyV8TrackPayload.ModelTxtToImg;
        }

        [CustomAction("Model info", true)]
        public void ModelInfo()
        {
            IUriLauncher.Launcher.LaunchUrl($"https://muapi.ai/playground/{Model}");
        }
    }
}
