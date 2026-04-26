using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin.Models.ViduQ2Turbo
{
    public class ViduQ2TurboTrackPayload
    {
        public const string ModelT2V = "vidu-q2-turbo-text-to-video";
        public const string ModelI2V = "vidu-q2-turbo-image-to-video";
        public const string ModelStartEnd = "vidu-q2-turbo-start-end-video";

        [Description("Track-level prompt prefix for Vidu Q2 Turbo video generation.")]
        [EditorWidth(360)]
        public string Prompt { get; set; }

        [PropertyComboOptions(["360p", "540p", "720p", "1080p"])]
        public string Resolution { get; set; } = "720p";

        [PropertyComboOptions(["16:9", "9:16", "1:1", "4:3", "3:4"])]
        public string AspectRatio { get; set; } = "16:9";

        [CustomName("Background music")]
        public bool Bgm { get; set; }

        [PropertyComboOptions(["small", "medium", "large"])]
        public string MovementAmplitude { get; set; } = "medium";

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {
            if (!IsViduQ2TurboModel(model))
            {
                return false;
            }

            if (model == ModelStartEnd && propertyName == nameof(AspectRatio))
            {
                return false;
            }

            return true;
        }

        public static bool IsViduQ2TurboModel(string model)
        {
            return model is ModelT2V or ModelI2V or ModelStartEnd;
        }
    }
}
