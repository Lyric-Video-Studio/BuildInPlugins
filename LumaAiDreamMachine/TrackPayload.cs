using System.ComponentModel;

namespace LumaAiDreamMachinePlugin
{
    public class TrackPayload
    {
        private string uploadUrl;
        private Request ImgToVidPayload = new Request();

        [Description("Video settings")]
        public Request Settings { get => ImgToVidPayload; set => ImgToVidPayload = value; }

        [Description("Path to public / accessible folder, where you image is uploaded, for img2vid")]
        public string UploadUrl { get => uploadUrl; set => uploadUrl = value; }
    }
}