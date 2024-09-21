using System.ComponentModel;

namespace LumaAiDreamMachinePlugin
{
    public class TrackPayload
    {
        private Request imgToVidPayload = new Request();

        [Description("Video settings")]
        public Request Settings { get => imgToVidPayload; set => imgToVidPayload = value; }
    }
}