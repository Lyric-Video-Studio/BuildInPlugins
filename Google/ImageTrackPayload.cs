using PluginBase;

namespace GooglePlugin
{
    public class ImageTrackPayload : IPayloadPropertyVisibility
    {
        [PropertyComboOptions(["gemini-3.1-flash-lite-image", "gemini-3.1-flash-image-preview", "gemini-3-pro-image-preview", "gemini-2.5-flash-image", "models/imagen-4.0-generate-001"])]
        public string Model { get; set; } = "gemini-3.1-flash-lite-image";
        public string Prompt { get; set; }

        [PropertyComboOptions(["1K", "2K", "4K"])]
        public string Size { get; set; } = "1K";

        [EnableFileDrop]
        public string ImageSource { get; set; }

        [EnableFileDrop]
        public string ImageSource2 { get; set; }

        [EnableFileDrop]
        public string ImageSource3 { get; set; }

        [EnableFileDrop]
        public string ImageSource4 { get; set; }

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

