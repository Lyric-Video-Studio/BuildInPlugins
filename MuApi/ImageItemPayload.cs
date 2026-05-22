using MuApiPlugin.Models.GptImage2;
using MuApiPlugin.Models.GeminiOmni;
using MuApiPlugin.Models.MidjourneyV8;
using PluginBase;

namespace MuApiPlugin
{
    public class ImageItemPayload : IPayloadPropertyVisibility, IApiPollingPayload
    {
        public ImageItemPayload()
        {
        }

        public ImageItemPayload(string text)
        {
            GptImage2.Prompt = text;
            MidjourneyV8.Prompt = text;
            GeminiOmniCharacter.Descriptions = text;
        }

        public ImageItemPayload(string text, bool isImageSource)
        {
            if (isImageSource)
            {
                GptImage2.ImageReferences.ImageSources.Add(new ImageReferenceItem() { ImageFile = text });
                MidjourneyV8.ImageReferences.ImageSources.Add(new ImageReferenceItem() { ImageFile = text });
                GeminiOmniCharacter.ImageReferences.ImageSources.Add(new ImageReferenceItem() { ImageFile = text });
            }
            else
            {
                GptImage2.Prompt = text;
                MidjourneyV8.Prompt = text;
                GeminiOmniCharacter.Descriptions = text;
            }
        }

        [HideAllChildren]
        [ParentName("")]
        public GeminiOmniCharacterItemPayload GeminiOmniCharacter { get; set; } = new();

        [HideAllChildren]
        [ParentName("")]
        public GptImage2ItemPayload GptImage2 { get; set; } = new();

        [HideAllChildren]
        [ParentName("")]
        public MidjourneyV8ItemPayload MidjourneyV8 { get; set; } = new();
        public string PollingId { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is ImageTrackPayload tp)
            {
                switch (propertyName)
                {
                    case nameof(GeminiOmniCharacter):
                        return ImageTrackPayload.IsGeminiOmniCharacter(tp);
                    case nameof(GptImage2):
                        return ImageTrackPayload.IsGptImage2(tp);
                    case nameof(MidjourneyV8):
                        return ImageTrackPayload.IsMidjourneyV8(tp);
                    default:
                        break;
                }

                if (ImageTrackPayload.IsGeminiOmniCharacter(tp))
                {
                    return GeminiOmniCharacter.ShouldPropertyBeVisible(propertyName, tp.Model);
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
