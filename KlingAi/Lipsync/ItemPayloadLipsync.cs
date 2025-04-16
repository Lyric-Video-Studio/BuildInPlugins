using PluginBase;
using System.ComponentModel;

namespace KlingAiPlugin
{
    public class ItemPayloadLipsync
    {
        [Description("Text or audio file is needed")]
        public string Text { get; set; }

        [EnableFileDrop]
        public string AudioFile { get; set; }

        [Description("Id of your existing Kling video. Use 'Copy content id' of your existiing video to get the id easy")]
        public string InputVideoId { get; set; }

        private string pollingId;

        [Description("PollingId (generation id) is filled when you make new request. Unlike in other plugins, this is not cleared when generation is completed, because ths id is needed if you want to extend the video. " +
            "If you need to create new variation, clear this id, otherwise the same result will be fetched from the server")]
        public string PollingId { get => pollingId; set => pollingId = value; }
    }
}