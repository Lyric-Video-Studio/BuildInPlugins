using PluginBase;

namespace GooglePlugin
{
    public class VideoItemPayload
    {
        public string Prompt { get; set; }
        public string NegativePrompt { get; set; }

        [EnableFileDrop]
        public string ImageSource { get; set; }
    }
}