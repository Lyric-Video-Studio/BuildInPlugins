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

        [Description("Id of your existing Kling video. Use 'Copy content id' of your existiing video to get the id easy. You can't use both video_id and pat")]
        public string InputVideoId { get; set; }

        [Description("Path to your input video. Video files support .mp4/.mov, file size does not exceed 100MB, video length does not exceed 10s and is not shorter than 2s, only 720p and 1080p are supported. Note that using Google Drive may not work. You can't use both video_id and path")]
        public string InputVideoPath { get; set; }

        private string pollingId;

        [Description("PollingId (generation id) is filled when you make new request. Unlike in other plugins, this is not cleared when generation is completed, because ths id is needed if you want to extend the video. " +
            "If you need to create new variation, clear this id, otherwise the same result will be fetched from the server")]
        public string PollingId { get => pollingId; set => pollingId = value; }
    }
}