using PluginBase;

namespace GooglePlugin
{
    public class VideoItemPayload
    {
        public string Prompt { get; set; }

        [PropertyComboOptions(["4", "6", "8"])]
        public string Duration { get; set; } = "8";

        [EnableFileDrop]
        public string ImageSource { get; set; }
    }
}