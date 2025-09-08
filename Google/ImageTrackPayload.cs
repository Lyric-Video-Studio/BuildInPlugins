using PluginBase;

namespace GooglePlugin
{
    public class ImageTrackPayload
    {
        public string Model { get; set; } = "gemini-2.5-flash-image-preview";
        public string Prompt { get; set; }

        [EnableFileDrop]
        public string ImageSource { get; set; }
    }
}