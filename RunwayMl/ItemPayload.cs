using PluginBase;
using System.ComponentModel;

namespace RunwayMlPlugin
{
    public class ItemPayload
    {
        private string pollingId;

        [Description("PollingId (generation id) is filled when you make new request. Unlike in other plugins, this is not cleared when generation is completed, because ths id is needed if you want to extend the video. " +
            "If you need to create new variation, clear this id, otherwise the same result will be fetched from the server")]
        public string PollingId { get => pollingId; set => pollingId = value; }

        public string Prompt { get; set; }

        public int Seed { get; set; } = 0;

        [EnableFileDrop]
        public string ImageSource { get => imageSource; set => imageSource = value; }

        private string imageSource;

        [EnableFileDrop]
        public string VideoSource { get; set; }
    }
}