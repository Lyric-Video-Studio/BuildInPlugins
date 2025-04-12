using PluginBase;
using System.ComponentModel;

namespace MinimaxPlugin
{
    public class ImageTrackPayload
    {
        [IgnoreDynamicEdit]
        public bool IsVideo { get; set; } = false;

        /*private ImageRequest txtToImgPayload = new ImageRequest();

        [Description("Image settings")]
        public ImageRequest Settings { get => txtToImgPayload; set => txtToImgPayload = value; }*/
    }
}