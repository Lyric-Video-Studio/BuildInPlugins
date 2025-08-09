using PluginBase;
using System.ComponentModel;

namespace MistralTxtToImgPlugin
{
    public class TrackPayload
    {
        public string Prompt { get; set; } = "Create image of";
    }
}