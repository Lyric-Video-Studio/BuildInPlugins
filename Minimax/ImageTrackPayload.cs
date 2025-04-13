using PluginBase;
using System.ComponentModel;
using System.Globalization;

namespace MinimaxPlugin
{
    public class ImageTrackPayload
    {
        private ImageRequest txtToImgPayload = new ImageRequest();

        [Description("Image settings")]
        public ImageRequest Settings { get => txtToImgPayload; set => txtToImgPayload = value; }

        public string CharacterReference { get; set; }
    }
}