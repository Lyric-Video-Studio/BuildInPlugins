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

        [CustomName("Voice profile 1")]
        public string AudioId1 { get; set; } = GeminiOmniProfileOptions.None;

        [CustomName("Voice profile 2")]
        public string AudioId2 { get; set; } = GeminiOmniProfileOptions.None;

        [CustomName("Voice profile 3")]
        public string AudioId3 { get; set; } = GeminiOmniProfileOptions.None;

        [CustomName("Character 1")]
        public string CharacterId1 { get; set; } = GeminiOmniProfileOptions.None;

        [CustomName("Character 2")]
        public string CharacterId2 { get; set; } = GeminiOmniProfileOptions.None;

        [CustomName("Character 3")]
        public string CharacterId3 { get; set; } = GeminiOmniProfileOptions.None;

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

        public string[] GetEffectiveAudioIds(GeminiOmniItemPayload itemPayload)
        {
            return
            [
                GetEffectiveId(AudioId1, itemPayload?.AudioId1),
                GetEffectiveId(AudioId2, itemPayload?.AudioId2),
                GetEffectiveId(AudioId3, itemPayload?.AudioId3)
            ];
        }

        public string[] GetEffectiveCharacterIds(GeminiOmniItemPayload itemPayload)
        {
            return
            [
                GetEffectiveId(CharacterId1, itemPayload?.CharacterId1),
                GetEffectiveId(CharacterId2, itemPayload?.CharacterId2),
                GetEffectiveId(CharacterId3, itemPayload?.CharacterId3)
            ];
        }

        private static string GetEffectiveId(string trackValue, string itemValue)
        {
            return IsExplicitIdSelection(itemValue) ? itemValue : trackValue;
        }

        private static bool IsExplicitIdSelection(string value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && !string.Equals(value, GeminiOmniProfileOptions.None, StringComparison.OrdinalIgnoreCase);
        }
    }
}
