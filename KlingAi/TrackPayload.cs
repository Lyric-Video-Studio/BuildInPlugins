using PluginBase;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace KlingAiPlugin
{
    public class TrackPayload
    {
        private KlingVideoRequest imgToVidPayload = new KlingVideoRequest();

        [Description("Video settings")]
        public KlingVideoRequest Settings { get => imgToVidPayload; set => imgToVidPayload = value; }
    }
}