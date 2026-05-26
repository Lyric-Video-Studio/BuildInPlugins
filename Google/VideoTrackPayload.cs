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

        [EnableFileDrop]
        public string ImageSource2 { get; set; }

        [EnableFileDrop]
        public string ImageSource3 { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (propertyName is nameof(ImageSource2) or nameof(ImageSource3) && trackPayload is VideoTrackPayload tp)
            {
                return tp.Model == "veo-3.1-fast-generate-preview";
            }
            return true;
        }
    }
}