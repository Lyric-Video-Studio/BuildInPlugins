using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin.Models.GeminiOmni
{
    public class GeminiOmniAudioItemPayload
    {
        [Description("Name for the saved Gemini Omni voice profile.")]
        [EditorWidth(320)]
        public string Name { get; set; }

        [Description("Describe the voice timbre, style, and emotion.")]
        [EditorWidth(360)]
        public string VoiceDescription { get; set; }

        [Description("Short sample line for the voice. MuApi allows up to 120 characters.")]
        [EditorWidth(360)]
        public string ExampleDialogue { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {
            return GeminiOmniAudioTrackPayload.IsGeminiOmniAudioModel(model);
        }
    }
}
