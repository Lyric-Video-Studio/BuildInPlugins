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
        [Description("NOTE!!! DropBox content delivery does not work with Alibaba, use Google drive instead")]
        [IgnoreDynamicEdit] // TODO: Backend is not working
        public string FirstFrame { get; set; }

        [EnableFileDrop]
        [Description("NOTE!!! DropBox content delivery does not work with Alibaba, use Google drive instead")]
        [IgnoreDynamicEdit]  // TODO: Backend is not working
        public string LastFrame { get; set; }
    }
}