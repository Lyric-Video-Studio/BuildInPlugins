using PluginBase;

namespace GooglePlugin
{
    public class ImageItemPayload
    {
        public string Prompt { get; set; }

        [EnableFileDrop]
        public string ImageSource { get; set; }
    }
}