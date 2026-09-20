using PluginBase;
using System.ComponentModel;

namespace GooglePlugin
{
    public class GoogleAudioTrackPayload : IPayloadPropertyVisibility
    {
        public const string ModelTts = "gemini-3.1-flash-tts-preview";
        public const string ModelLyriaClip = "lyria-3-clip-preview";
        public const string ModelLyria35 = "lyria-3.5";

        // Keep the old constant name as a source-compatibility alias because the plugin
        // already exposes it through GetSupportedModels().
        public const string ModelLyriaPro = ModelLyria35;
        public const string LegacyModelLyriaProPreview = "lyria-3-pro-preview";

        [PropertyComboOptions([ModelTts, ModelLyriaClip, ModelLyria35])]
        public string Model { get; set; } = "gemini-3.1-flash-tts-preview";

        [Description("Shared instructions for the scene, tone, pronunciation or accent")]
        public string Prompt { get; set; } = "## Scene: <insert scene here>. ## Sample Context: <insert context here>. ## Transcript: ";

        public float Temperature { get; set; } = 1.0f;

        [PropertyComboOptions(["mp3", "wav"])]
        [Description("Current Lyria 3 Clip and Lyria 3.5 API output is MP3. WAV is retained only for legacy Lyria 3 Pro preview payloads.")]
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

            if (propertyName == nameof(MusicFormat))
            {
                if (Model is ModelLyriaClip or ModelLyria35)
                {
                    MusicFormat = "mp3";
                    return false;
                }

                if (!IsLyriaModel(Model))
                {
                    return false;
                }
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
            return model is ModelLyriaClip or ModelLyria35 or LegacyModelLyriaProPreview;
        }

        public static bool IsLyriaPro(string model)
        {
            // Do not treat Lyria 3.5 as the legacy Pro model here. The current Google.GenAI
            // GenerateContentConfig.ResponseMimeType property only supports text MIME types,
            // so using it for Lyria 3.5 WAV requests throws before generation starts.
            return model == LegacyModelLyriaProPreview;
        }
    }
}
