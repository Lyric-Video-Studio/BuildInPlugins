using PluginBase;
using System.ComponentModel;

namespace KlingAiPlugin
{
    public class ImageTrackPayload
    {
        private KlingImageRequest imgPayload = new KlingImageRequest();

        [Description("Image settings")]
        [IgnorePropertyName]
        public KlingImageRequest Settings { get => imgPayload; set => imgPayload = value; }

        [EnableFileDrop]
        public string ImageReference { get; set; }
    }
}