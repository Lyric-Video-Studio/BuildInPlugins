using MuApiPlugin.Models.GptImage2;
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
        }

        [HideAllChildren]
        [ParentName("")]
        public GptImage2ItemPayload GptImage2 { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is ImageTrackPayload tp)
            {
                switch (propertyName)
                {
                    case nameof(GptImage2):
                        return ImageTrackPayload.IsGptImage2(tp);
                    default:
                        break;
                }

                if (ImageTrackPayload.IsGptImage2(tp))
                {
                    return GptImage2.ShouldPropertyBeVisible(propertyName, tp.Model);
                }
            }

            return true;
        }
    }
}
