using MuApiPlugin.Models.GptImage2;
using MuApiPlugin.Models.MidjourneyV8;
using PluginBase;

namespace MuApiPlugin
{
    public class ImageItemPayload : IPayloadPropertyVisibility
    {
        public ImageItemPayload()
        {
        }

        public ImageItemPayload(string text)
        {
            GptImage2.Prompt = text;
            MidjourneyV8.Prompt = text;
        }

        [HideAllChildren]
        [ParentName("")]
        public GptImage2ItemPayload GptImage2 { get; set; } = new();

        [HideAllChildren]
        [ParentName("")]
        public MidjourneyV8ItemPayload MidjourneyV8 { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is ImageTrackPayload tp)
            {
                switch (propertyName)
                {
                    case nameof(GptImage2):
                        return ImageTrackPayload.IsGptImage2(tp);
                    case nameof(MidjourneyV8):
                        return ImageTrackPayload.IsMidjourneyV8(tp);
                    default:
                        break;
                }

                if (ImageTrackPayload.IsGptImage2(tp))
                {
                    return GptImage2.ShouldPropertyBeVisible(propertyName, tp.Model);
                }

                if (ImageTrackPayload.IsMidjourneyV8(tp))
                {
                    return MidjourneyV8.ShouldPropertyBeVisible(propertyName, tp.Model);
                }
            }

            return true;
        }
    }
}
