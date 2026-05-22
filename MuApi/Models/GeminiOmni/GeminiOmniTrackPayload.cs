using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin.Models.GeminiOmni
{
    public class GeminiOmniTrackPayload
    {
        public const string ModelT2V = "gemini-omni-text-to-video";
        public const string ModelI2V = "gemini-omni-image-to-video";

        [Description("Track-level prompt prefix for Gemini Omni video generation.")]
        [EditorWidth(360)]
        public string Prompt { get; set; }

        [PropertyComboOptions(["720p", "1080p", "4k"])]
        public string Resolution { get; set; } = "1080p";

        [PropertyComboOptions(["16:9", "9:16"])]
        public string AspectRatio { get; set; } = "16:9";

        [Description("Same seed and same prompt can give similar results. Leave zero for random behavior.")]
        public int Seed { get; set; } = 0;

        public ImageReferenceContainer ImageReferences { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {
            if (!IsGeminiOmniVideoModel(model))
            {
                return false;
            }

            if (model == ModelT2V && (propertyName == nameof(ImageReferences) || ImageReferenceContainer.IsImageRefName(propertyName)))
            {
                return false;
            }

            return true;
        }

        public static bool IsGeminiOmniVideoModel(string model)
        {
            return model is ModelT2V or ModelI2V;
        }
    }
}
