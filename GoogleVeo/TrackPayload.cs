using PluginBase;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace GoogleVeoPlugin
{
    public class TrackPayload
    {
        private Veo2GenerationRequest imgToVidPayload = new Veo2GenerationRequest();

        [Description("Video settings")]
        public Veo2GenerationRequest Settings { get => imgToVidPayload; set => imgToVidPayload = value; }
    }
}