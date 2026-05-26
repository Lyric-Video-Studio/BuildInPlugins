using PluginBase;

namespace GooglePlugin
{
    public class VideoItemPayload: IPayloadPropertyVisibility
    {
        public string Prompt { get; set; }

        [PropertyComboOptions(["4", "6", "8"])]
        public string Duration { get; set; } = "8";

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