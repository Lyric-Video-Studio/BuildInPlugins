using PluginBase;

namespace GooglePlugin
{
    public class ImageTrackPayload : IPayloadPropertyVisibility
    {
        public string Model { get; set; } = "gemini-2.5-flash-image-preview";
        public string Prompt { get; set; }
        public string Size { get; set; } = "1K";

        [EnableFileDrop]
        public string ImageSource { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (propertyName == nameof(Size))
            {
                return Model == "gemini-3-pro-image-preview";
            }

            return true;
        }
    }
}