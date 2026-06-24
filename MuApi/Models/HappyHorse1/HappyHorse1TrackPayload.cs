using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin.Models.HappyHorse1
{
    public class HappyHorse1TrackPayload
    {
        public const string LegacyModelT2V1080p = "happy-horse-1-text-to-video-1080p";
        public const string LegacyModelI2V1080p = "happy-horse-1-image-to-video-1080p";
        public const string LegacyModelT2V720p = "happy-horse-1-text-to-video-720p";
        public const string LegacyModelI2V720p = "happy-horse-1-image-to-video-720p";
        public const string ModelI2V1080p = "happy-horse-1.1-image-to-video-1080p";
        public const string ModelReferenceToVideo1080p = "happy-horse-1.1-reference-to-video-1080p";

        [Description("Track-level prompt prefix for Happy Horse video generation.")]
        [EditorWidth(360)]
        public string Prompt { get; set; }

        [PropertyComboOptions(["16:9", "9:16", "4:3", "3:4"])]
        public string AspectRatio { get; set; } = "16:9";

        [Description("Same seed and same prompt can give similar results. Leave zero for random behavior.")]
        public int Seed { get; set; } = 0;

        public ImageReferenceContainer ImageReferences { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {
            if (!IsHappyHorseModel(model))
            {
                return false;
            }

            if (IsTextToVideoModel(model) && (propertyName == nameof(ImageReferences) || ImageReferenceContainer.IsImageRefName(propertyName)))
            {
                return false;
            }

            if (!SupportsSeed(model) && propertyName == nameof(Seed))
            {
                return false;
            }

            return true;
        }

        public static bool IsHappyHorseModel(string model)
        {
            return IsTextToVideoModel(model)
                || IsImageToVideoModel(model)
                || IsReferenceToVideoModel(model);
        }

        public static bool IsTextToVideoModel(string model)
        {
            return model is LegacyModelT2V1080p or LegacyModelT2V720p;
        }

        public static bool IsImageToVideoModel(string model)
        {
            return model is LegacyModelI2V1080p or LegacyModelI2V720p or ModelI2V1080p;
        }

        public static bool IsReferenceToVideoModel(string model)
        {
            return model == ModelReferenceToVideo1080p;
        }

        public static bool SupportsSeed(string model)
        {
            return IsReferenceToVideoModel(model);
        }
    }
}
