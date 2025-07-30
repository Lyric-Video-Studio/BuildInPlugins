using PluginBase;
using System.ComponentModel;

namespace LumaAiDreamMachinePlugin
{
    public class TrackPayload : IPayloadPropertyVisibility
    {
        [IgnoreDynamicEdit]
        public bool IsVideo { get; set; } = true;

        private Request imgToVidPayload = new Request();

        [Description("Video settings")]
        [IgnorePropertyName]
        public Request Settings { get => imgToVidPayload; set => imgToVidPayload = value; }

        public string VideoEditMode { get; set; } = "flex_1";

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (itemPayload is ItemPayload ip)
            {
                if (propertyName == nameof(Request.loop) || propertyName == nameof(Request.aspect_ratio) ||
                    propertyName == nameof(Request.resolution) || propertyName == nameof(Request.duration))
                {
                    return string.IsNullOrEmpty(ip.VideoFile);
                }
                if (propertyName == nameof(TrackPayload.VideoEditMode))
                {
                    return !string.IsNullOrEmpty(ip.VideoFile);
                }
            }
            return true;
        }
    }
}