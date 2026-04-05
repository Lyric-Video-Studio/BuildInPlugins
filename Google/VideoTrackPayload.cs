using PluginBase;

namespace GooglePlugin
{
    public class VideoTrackPayload : IPayloadPropertyVisibility
    {
        [PropertyComboOptions(["veo-3.1-fast-generate-preview", "veo-3.1-generate-preview", "veo-3.1-lite-generate-preview"])]
        public string Model { get; set; } = "veo-3.1-fast-generate-preview";
        public string Prompt { get; set; }

        [PropertyComboOptions(["720p", "1080p", "4k"])]
        public string Resolution { get; set; } = "1080p";

        [PropertyComboOptions(["16:9", "9:16"])]
        public string AspectRatio { get; set; } = "16:9";

        [EnableFileDrop]
        public string ImageSource { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {           

            return true;
        }
    }
}