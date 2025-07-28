using PluginBase;
using System.ComponentModel;

namespace RunwayMlPlugin
{
    public class TrackPayload
    {
        private Request request = new Request();

        [IgnorePropertyName]
        public Request Request { get => request; set => request = value; }

        [Description("Used with Act2. Image or video required. If video is selected, it will be used, not image")]
        public string ReferenceImage { get; set; }

        [Description("Used with Act2. Image or video required. If video is selected, it will be used, not image")]
        public string ReferenceVideo { get; set; }
    }
}