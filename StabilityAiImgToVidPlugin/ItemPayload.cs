using PluginBase;
using System.ComponentModel;

namespace StabilityAiImgToVidPlugin
{
    public class ItemPayload
    {
        private string pathToImage = "";

        [Description("Image source must be exactly one of these dimensions: 1024x576, 576x1024, 768x768")]
        [EditorWidth(500)]
        [EditorColumnSpan(5)]
        [EnableFileDrop]
        public string PathToImage { get => pathToImage; set => pathToImage = value; }

        private string pollingId;

        [Description("This id is used internally to poll results and is not meant to be edited manually, if you're not sure what you are doing. This will be cleared on successfull video generation. " +
            "You can manually remove this, if video generation failed and you would like to discard the results")]
        public string PollingId { get => pollingId; set => pollingId = value; }
    }
}