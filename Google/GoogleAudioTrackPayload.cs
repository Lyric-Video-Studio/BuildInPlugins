using PluginBase;
using System.ComponentModel;

namespace GooglePlugin
{
    public class GoogleAudioTrackPayload : IPayloadPropertyVisibility
    {
        [PropertyComboOptions(["gemini-3.1-flash-tts-preview"])]
        public string Model { get; set; } = "gemini-3.1-flash-tts-preview";

        [Description("Shared instructions for the scene, tone, pronunciation or accent")]
        public string Prompt { get; set; } = "## Scene: <insert scene here>. ## Sample Context <insert context here>.";

        public float Temperature { get; set; } = 1.0f;

        [Description("Use multi-speaker synthesis. Script should then contain lines like Speaker 1: ...")]
        [TriggerReload]
        public bool MultiSpeaker { get; set; } = true;

        public string Speaker1Name { get; set; } = "Speaker 1";

        [PropertyComboOptions(["Zephyr", "Puck", "Charon", "Kore", "Fenrir", "Leda", "Orus", "Aoede"])]
        public string Speaker1Voice { get; set; } = "Zephyr";

        public string Speaker2Name { get; set; } = "Speaker 2";

        [PropertyComboOptions(["Puck", "Zephyr", "Charon", "Kore", "Fenrir", "Leda", "Orus", "Aoede"])]
        public string Speaker2Voice { get; set; } = "Puck";

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (!MultiSpeaker &&
                (propertyName == nameof(Speaker2Name) || propertyName == nameof(Speaker2Voice)))
            {
                return false;
            }

            return true;
        }
    }
}
