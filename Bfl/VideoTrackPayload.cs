using System.ComponentModel;
using PluginBase;

namespace BflTxtToImgPlugin
{
    public class VideoTrackPayload : IPayloadPropertyVisibility
    {
        public const string ModeTextToVideo = "Text to Video";
        public const string ModeImageToVideo = "Image to Video";
        public const string ModeVideoContinuation = "Video Continuation";

        [TriggerReload]
        [PropertyComboOptions([ModeTextToVideo, ModeImageToVideo, ModeVideoContinuation])]
        public string Mode { get; set; } = ModeTextToVideo;

        public string Prompt { get; set; }

        [PropertyComboOptions(["auto", "21:9", "2:1", "16:9", "4:3", "1:1", "3:4", "9:16"])]
        public string AspectRatio { get; set; } = "auto";

        [PropertyComboOptions(["auto", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20"])]
        public string Duration { get; set; } = "auto";

        [PropertyComboOptions(["hd", "fhd"])]
        public string Resolution { get; set; } = "hd";

        public bool GenerateAudio { get; set; } = true;
        public int SafetyTolerance { get; set; } = 4;

        [Description("If true, the video will be generated in draft mode, costing less and faster")]
        public bool Draft { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload) => true;
    }
}
