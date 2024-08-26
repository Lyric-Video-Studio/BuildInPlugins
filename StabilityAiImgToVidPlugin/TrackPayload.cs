using System.ComponentModel;

namespace StabilityAiImgToVidPlugin
{
    public class TrackPayload
    {
        private Request ImgToVidPayload = new Request();

        [Description("Video settings")]
        public Request Settings { get => ImgToVidPayload; set => ImgToVidPayload = value; }
    }
}
