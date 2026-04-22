using PluginBase;
using System.ComponentModel;

namespace GooglePlugin
{
    public class GoogleAudioTrackPayload : IPayloadPropertyVisibility
    {
        public const string ModelTts = "gemini-3.1-flash-tts-preview";
        public const string ModelLyriaClip = "lyria-3-clip-preview";
        public const string ModelLyriaPro = "lyria-3-pro-preview";

        [PropertyComboOptions([ModelTts, ModelLyriaClip, ModelLyriaPro])]
        public string Model { get; set; } = "gemini-3.1-flash-tts-preview";

        [Description("Shared instructions for the scene, tone, pronunciation or accent")]
        public string Prompt { get; set; } = "## Scene: <insert scene here>. ## Sample Context <insert context here>.";

        public float Temperature { get; set; } = 1.0f;

        [PropertyComboOptions(["mp3", "wav"])]
        [Description("Lyria 3 Clip always returns MP3. Lyria 3 Pro can also return WAV.")]
        public string MusicFormat { get; set; } = "mp3";

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
            if (IsLyriaModel(Model))
            {
                if (propertyName is nameof(Temperature) or nameof(MultiSpeaker) or nameof(Speaker1Name) or nameof(Speaker1Voice) or nameof(Speaker2Name) or nameof(Speaker2Voice))
                {
                    return false;
                }
            }

            if (!IsLyriaModel(Model) && propertyName == nameof(MusicFormat))
            {
                return false;
            }

            if (!IsLyriaPro(Model) && propertyName == nameof(MusicFormat) && MusicFormat == "wav")
            {
                MusicFormat = "mp3";
            }

            if (!MultiSpeaker &&
                (propertyName == nameof(Speaker2Name) || propertyName == nameof(Speaker2Voice)))
            {
                return false;
            }

            return true;
        }

        public static bool IsLyriaModel(string model)
        {
            return model is ModelLyriaClip or ModelLyriaPro;
        }

        public static bool IsLyriaPro(string model)
        {
            return model == ModelLyriaPro;
        }
    }
}
