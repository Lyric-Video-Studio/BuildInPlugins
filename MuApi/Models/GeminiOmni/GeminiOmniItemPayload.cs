using MuApiPlugin.Models.Seedance2;
using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin.Models.GeminiOmni
{
    public class GeminiOmniItemPayload
    {
        [Description("Item-level prompt suffix for Gemini Omni video generation.")]
        [EditorWidth(360)]
        public string Prompt { get; set; }

        [PropertyComboOptions(["4", "6", "8", "10"])]
        public int Duration { get; set; } = 8;

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

        public ImageReferenceContainer ImageReferences { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {
            if (!GeminiOmniTrackPayload.IsGeminiOmniVideoModel(model))
            {
                return false;
            }

            if (model == GeminiOmniTrackPayload.ModelT2V &&
                (propertyName == nameof(ImageReferences) || ImageReferenceContainer.IsImageRefName(propertyName)))
            {
                return false;
            }

            return true;
        }
    }
}
