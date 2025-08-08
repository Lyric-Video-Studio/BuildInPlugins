using PluginBase;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WanPlugin
{
    public class ItemPayload
    {
        private string pollingId;

        [Description("PollingId (generation id) is filled when you make new request.")]
        public string PollingId { get => pollingId; set => pollingId = value; }

        public string Prompt { get; set; }
        public string NegativePrompt { get; set; }

        public int Seed { get; set; } = 0;

        [EnableFileDrop]
        public string FirstFrame { get; set; }

        [EnableFileDrop]
        [IgnoreDynamicEdit] // Nvm, this was not yet possible
        public string LastFrame { get; set; }
    }
}