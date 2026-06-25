using PluginBase;

namespace GooglePlugin
{
    public class ImageItemPayload
    {
        public string Prompt { get; set; }

        [EnableFileDrop]
        public string ImageSource { get; set; }

        [EnableFileDrop]
        public string ImageSource2 { get; set; }

        [EnableFileDrop]
        public string ImageSource3 { get; set; }

        [EnableFileDrop]
        public string ImageSource4 { get; set; }
    }
}

