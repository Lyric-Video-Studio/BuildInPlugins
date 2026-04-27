using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin.Models.HappyHorse1
{
    public class HappyHorse1TrackPayload
    {
        public const string ModelT2V1080p = "happy-horse-1-text-to-video-1080p";
        public const string ModelI2V1080p = "happy-horse-1-image-to-video-1080p";
        public const string ModelT2V720p = "happy-horse-1-text-to-video-720p";
        public const string ModelI2V720pp = "happy-horse-1-image-to-video-720p";

        [Description("Track-level prompt prefix for Happy Horse 1 video generation.")]
        [EditorWidth(360)]
        public string Prompt { get; set; }

        [PropertyComboOptions(["16:9", "9:16", "4:3", "3:4"])]
        public string AspectRatio { get; set; } = "16:9";

        public ImageReferenceContainer ImageReferences { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {
            if (!IsHappyHorseModel(model))
            {
                return false;
            }

            if (model is ModelT2V1080p or ModelT2V720p && (propertyName == nameof(ImageReferences) || ImageReferenceContainer.IsImageRefName(propertyName)))
            {
                return false;
            }

            return true;
        }

        public static bool IsHappyHorseModel(string model)
        {
            return model is ModelT2V1080p or ModelI2V1080p or ModelT2V720p or ModelI2V720pp;
        }
    }
}
