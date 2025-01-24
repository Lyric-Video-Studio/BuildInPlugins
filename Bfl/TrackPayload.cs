using Bfl;
using System.ComponentModel;

namespace BflTxtToImgPlugin
{
    public class TrackPayload
    {
        private FluxPro11Inputs txt2ImgPayload = new FluxPro11Inputs();

        [Description("Image settings")]
        public FluxPro11Inputs Settings { get => txt2ImgPayload; set => txt2ImgPayload = value; }
    }
}