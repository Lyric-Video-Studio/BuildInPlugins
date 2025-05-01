using Bfl;
using PluginBase;
using System.ComponentModel;

namespace BflTxtToImgPlugin
{
    public class TrackPayload
    {
        private FluxPro11Inputs txt2ImgPayload = new FluxPro11Inputs();

        [Description("Image settings")]
        [IgnorePropertyName]
        public FluxPro11Inputs Settings { get => txt2ImgPayload; set => txt2ImgPayload = value; }
    }
}