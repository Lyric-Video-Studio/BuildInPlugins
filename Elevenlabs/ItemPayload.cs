using PluginBase;
using System.ComponentModel;

namespace ElevenLabsPlugin
{
    public class ElevenLabsItemPayload
    {
        private string pollingId;
        public string PollingId { get => pollingId; set => pollingId = value; }

        private string prompt = "";

        [Description("Use for defining the music")]
        public string Prompt { get => prompt; set => prompt = value; }

        /*private string lyrics = "";

        //[Description("Use for defining the lyrics or text to synthesize")]
        [EditorColumnSpan(2)]
        public string Lyrics { get => lyrics; set => lyrics = value; }*/
    }
}