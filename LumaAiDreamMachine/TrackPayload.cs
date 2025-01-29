using PluginBase;
using System.ComponentModel;

namespace LumaAiDreamMachinePlugin
{
    public class TrackPayload
    {
        [IgnoreDynamicEdit]
        public bool IsVideo { get; set; } = true;

        private Request imgToVidPayload = new Request();

        [Description("Video settings")]
        public Request Settings { get => imgToVidPayload; set => imgToVidPayload = value; }
    }
}