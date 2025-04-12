using PluginBase;
using System.ComponentModel;

namespace MinimaxPlugin
{
    public class ItemPayload
    {
        [IgnoreDynamicEdit]
        public bool IsVideo { get; set; } = true;

        private string prompt = "";
        private string imagePath;

        //public KeyFrames KeyFrames { get; set; } = new KeyFrames();

        public string Prompt { get => prompt; set => prompt = value; }

        private string pollingId;

        [Description("PollingId (generation id) is filled when you make new request. Unlike in other plugins, this is not cleared when generation is completed, because ths id is needed if you want to extend the video. " +
            "If you need to create new variation, clear this id, otherwise the same result will be fetched from the server")]
        public string PollingId { get => pollingId; set => pollingId = value; }

        [Description("First frame for video")]
        [EnableFileDrop]
        public string ImagePath { get => imagePath; set => imagePath = value; }
    }
}