using System.ComponentModel;

namespace LumaAiDreamMachinePlugin
{
    public class ItemPayload
    {
        private string prompt = "";

        public KeyFrames KeyFrames { get; set; } = new KeyFrames();

        public string Prompt { get => prompt; set => prompt = value; }

        private string pollingId;

        [Description("This id is used internally to poll results and is not meant to be edited manually, if you're not sure what you are doing.  " +
            "You can manually remove this, if video generation failed and you would like to discard the results. PollingId will be preserved and you need to manually clear it go generate this again (polling id might be needed elsewhere too)")]
        public string PollingId { get => pollingId; set => pollingId = value; }
    }
}