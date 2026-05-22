using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin.Models.GeminiOmni
{
    public class GeminiOmniAudioTrackPayload
    {
        public const string ModelAudioProfile = "gemini-omni-audio";

        public static readonly string[] PresetVoiceIds =
        [
            "achernar", "achird", "algenib", "algieba", "alnilam", "aoede", "autonoe", "callirrhoe", "charon", "despina",
            "enceladus", "erinome", "fenrir", "gacrux", "iapetus", "kore", "laomedeia", "leda", "orus", "puck",
            "pulcherrima", "rasalgethi", "sadachbia", "sadaltager", "schedar", "sulafat", "umbriel", "vindemiatrix", "zephyr", "zubenelgenubi"
        ];

        [Description("Preset Gemini Omni base voice used to create the reusable profile.")]
        [CustomName("Base preset voice")]
        public string PresetVoiceId { get; set; } = "achernar";

        [CustomAction("Preview Google voices", true)]
        public void PreviewGoogleVoices()
        {
            IUriLauncher.Launcher.LaunchUrl("https://aistudio.google.com/generate-speech");
        }

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {
            return IsGeminiOmniAudioModel(model);
        }

        public static bool IsGeminiOmniAudioModel(string model)
        {
            return model == ModelAudioProfile;
        }
    }
}
