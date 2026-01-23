using Mscc.GenerativeAI.Types;
using PluginBase;

namespace GooglePlugin
{
    public class VideoTrackPayload : IPayloadPropertyVisibility
    {
        public string Model { get; set; } = "veo-3.1-fast-generate-preview";
        public string Prompt { get; set; }
        public string NegativePrompt { get; set; }
        public ImageAspectRatio Size { get; set; } = ImageAspectRatio.Ratio16x9;

        public string Resolution { get; set; } = "1080p";

        [EnableFileDrop]
        public string ImageSource { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            

            return true;
        }
    }
}