using PluginBase;
using System.ComponentModel;

namespace LumaAiDreamMachinePlugin
{
    public class ItemPayload
    {
        private string pathToImage = "";
        private string prompt = "";

        [Description("Image source must be exactly one of these dimensions: 1024x576, 576x1024, 768x768")]
        [EditorWidth(500)]
        [EditorColumnSpan(5)]
        public string PathToImage { get => pathToImage; set => pathToImage = value; }

        public string Prompt { get => prompt; set => prompt = value; }

        private string pollingId;

        [Description("This id is used internally to poll results and is not meant to be edited manually, if you're not sure what you are doing.  " +
            "You can manually remove this, if video generation failed and you would like to discard the results. PollingId will be preserved and you need to manually clear it go generate this again (polling id might be needed elsewhere too)")]
        public string PollingId { get => pollingId; set => pollingId = value; }
    }
}