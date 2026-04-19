using PluginBase;
using System.ComponentModel;

namespace GooglePlugin
{
    public class GoogleAudioItemPayload
    {
        [Description("Text or multi-speaker script to synthesize")]
        [EditorColumnSpan(2)]
        public string Prompt { get; set; } = "## Transcript: ";
    }
}
