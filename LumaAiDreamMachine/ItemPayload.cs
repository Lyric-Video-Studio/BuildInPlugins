using PluginBase;
using System.ComponentModel;

namespace LumaAiDreamMachinePlugin
{
    public class ItemPayload : IPayloadPropertyVisibility
    {
        [IgnoreDynamicEdit]
        public bool IsVideo { get; set; } = true;

        private string prompt = "";

        public KeyFrames KeyFrames { get; set; } = new KeyFrames();

        public string Prompt { get => prompt; set => prompt = value; }

        [Description("Used for modify video")]
        public string VideoFile { get; set; }

        [Description("Optional, but recommended for modify video. You can copy frame path from video item with right click context menu")]
        [EnableFileDrop]
        public string FirstFrame { get; set; }

        private string pollingId;

        [Description("PollingId (generation id) is filled when you make new request. Unlike in other plugins, this is not cleared when generation is completed, " +
            "because this id is needed if you want to extend the video. " +
            "If you need to create new variation, clear this id, otherwise the same result will be fetched from the server")]
        public string PollingId { get => pollingId; set => pollingId = value; }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (itemPayload is ItemPayload ip)
            {
                if (propertyName == nameof(ItemPayload.KeyFrames.frame0.url) || propertyName == nameof(ItemPayload.KeyFrames.frame0.id))
                {
                    return string.IsNullOrEmpty(ip.VideoFile);
                }

                if (propertyName == nameof(ItemPayload.FirstFrame))
                {
                    return !string.IsNullOrEmpty(ip.VideoFile);
                }
            }
            return true;
        }
    }
}