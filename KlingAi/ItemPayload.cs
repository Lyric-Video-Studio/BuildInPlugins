using PluginBase;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace KlingAiPlugin
{
    public class ItemPayload
    {
        private string prompt = "";
        public string Prompt { get => prompt; set => prompt = value; }

        [EnableFileDrop]
        public string StartFramePath { get; set; }

        [EnableFileDrop]
        public string EndFramePath { get; set; }

        private string pollingId;

        [Description("PollingId (generation id) is filled when you make new request. Unlike in other plugins, this is not cleared when generation is completed, because ths id is needed if you want to extend the video. " +
            "If you need to create new variation, clear this id, otherwise the same result will be fetched from the server")]
        public string PollingId { get => pollingId; set => pollingId = value; }
    }
}