using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin.Models.Seedance2
{
    public class Seedance2TrackPayload
    {
        public const string ModelT2V = "seedance-v2.0-t2v";
        public const string ModelI2V = "seedance-v2.0-i2v";
        public const string ModelT2V480p = "seedance-2.0-t2v-480p";
        public const string ModelI2V480p = "seedance-2.0-i2v-480p";
        public const string ModelOmniRef = "seedance-2.0-omni-reference";
        public const string ModelMiniT2V = "seedance-2-mini-text-to-video";
        public const string ModelMiniI2V = "seedance-2-mini-image-to-video";
        public const string ModelMiniOmniRef = "seedance-2-mini-omni-reference";

        [Description("Track-level prompt prefix. Reference uploaded media in your prompt with @image1, @image2, @audio1 etc")]
        [EditorWidth(360)]
        public string Prompt { get; set; }

        [PropertyComboOptions(["16:9", "9:16", "4:3", "3:4"])]
        public string AspectRatio { get; set; } = "16:9";

        [PropertyComboOptions(["basic", "high"])]
        public string Quality { get; set; } = "high";

        [PropertyComboOptions(["480p", "720p"])]
        public string Resolution { get; set; } = "720p";

        [CustomName("Generate audio")]
        public bool GenerateAudio { get; set; }

        [CustomName("High bitrate")]
        public bool HighBitrate { get; set; }

        public ImageReferenceContainer ImageReferences { get; set; } = new();

        public AudioReferenceContainer AudioReferences { get; set; } = new();

        public VideoReferenceContainer VideoReferences { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {
            if (IsImageToVideoModel(model) && (AudioReferenceContainer.IsAudioRefName(propertyName) || VideoReferenceContainer.IsVideoRefName(propertyName)))
            {
                return false;
            }

            if (IsTextToVideoModel(model) &&
                (AudioReferenceContainer.IsAudioRefName(propertyName) || ImageReferenceContainer.IsImageRefName(propertyName) || VideoReferenceContainer.IsVideoRefName(propertyName)))
            {
                return false;
            }

            if (IsMiniModel(model) && propertyName == nameof(Quality))
            {
                return false;
            }

            if (!IsMiniModel(model) && propertyName is nameof(Resolution) or nameof(GenerateAudio) or nameof(HighBitrate))
            {
                return false;
            }

            return true;
        }

        public static bool IsTextToVideoModel(string model)
        {
            return model is ModelT2V or ModelT2V480p or ModelMiniT2V;
        }

        public static bool IsImageToVideoModel(string model)
        {
            return model is ModelI2V or ModelI2V480p or ModelMiniI2V;
        }

        public static bool IsMiniModel(string model)
        {
            return model is ModelMiniT2V or ModelMiniI2V or ModelMiniOmniRef;
        }
    }
}
