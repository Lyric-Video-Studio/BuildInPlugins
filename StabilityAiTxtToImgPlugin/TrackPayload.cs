using System.ComponentModel;

namespace StabilityAiTxtToImgPlugin
{
    public class TrackPayload
    {
        private Request txt2ImgPayload = new Request();

        [Description("Image settings")]
        public Request Settings { get => txt2ImgPayload; set => txt2ImgPayload = value; }
    }
}
