using PluginBase;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace KlingAiPlugin
{
    public class TrackPayloadLipsync
    {
        private KlingLipsyncRequest imgToVidPayload = new KlingLipsyncRequest();

        [Description("Video settings")]
        [IgnorePropertyName]
        public KlingLipsyncRequest Settings { get => imgToVidPayload; set => imgToVidPayload = value; }        
    }
}