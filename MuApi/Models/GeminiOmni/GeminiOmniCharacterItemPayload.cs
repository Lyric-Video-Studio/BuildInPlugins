using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin.Models.GeminiOmni
{
    public class GeminiOmniCharacterItemPayload
    {
        [Description("Describe the character's appearance, identity, style, and personality.")]
        [EditorWidth(360)]
        public string Descriptions { get; set; }

        [Description("Optional display name for the saved character profile.")]
        [EditorWidth(320)]
        public string CharacterName { get; set; }

        public ImageReferenceContainer ImageReferences { get; set; } = new();

        [CustomName("Voice profile 1")]
        public string AudioId1 { get; set; } = GeminiOmniProfileOptions.None;

        [CustomName("Voice profile 2")]
        public string AudioId2 { get; set; } = GeminiOmniProfileOptions.None;

        [CustomName("Voice profile 3")]
        public string AudioId3 { get; set; } = GeminiOmniProfileOptions.None;

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {
            return GeminiOmniCharacterTrackPayload.IsGeminiOmniCharacterModel(model);
        }
    }
}
