using PluginBase;
using System.ComponentModel;

namespace FalAiPlugin
{
    internal class AudioItemPayload : IPayloadPropertyVisibility
    {
        public string PollingId { get; set; }
        public string Prompt { get; set; }

        [EnableFileDrop]
        [Description("Video file or public URL used as the sync source for Mirelo SFX")]
        public string VideoSource { get; set; }

        [Description("Optional seed. Leave 0 for random.")]
        public int Seed { get; set; }

        [Description("Duration of the generated audio in seconds (1–600 for SFX 2.0, limited to video length; 1–10 for legacy SFX 1.5).")]
        public float Duration { get; set; } = 10;

        [Description("Start position in the source video, in seconds (legacy SFX 1.5 only).")]
        public float StartOffset { get; set; }

        [Description("Sounds to exclude from the soundtrack (SFX 2.0).")]
        public string NegativePrompt { get; set; }

        [Description("Keep the source speech in the generated soundtrack (SFX 2.0). Disable for sound effects only.")]
        public bool PreserveSpeech { get; set; } = true;

        [Description("Return the separately extracted speech as the alternative audio track when generating a single sample (SFX 2.0).")]
        public bool ReturnSpeech { get; set; }

        [Description("Number of soundtracks to generate. Each sample is billed separately (SFX 2.0). LVS supports up to two returned tracks.")]
        [PropertyComboOptions(["1", "2"])]
        public int NumSamples { get; set; } = 1;

        [Description("Generated audio format (SFX 2.0).")]
        [PropertyComboOptions(["wav", "mp3", "aac", "flac"])]
        public string OutputFormat { get; set; } = "wav";

        [Description("MP3 or AAC output bitrate in kbps. 0 selects the service default; not used for WAV or FLAC (SFX 2.0).")]
        public int AudioBitrateKbps { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is AudioTrackPayload tp)
            {
                if (tp.Model == AudioTrackPayload.LegacyVideoToAudioSfxModel)
                {
                    return propertyName is nameof(PollingId) or nameof(Prompt) or nameof(VideoSource)
                        or nameof(Seed) or nameof(Duration) or nameof(StartOffset);
                }

                if (tp.Model == AudioTrackPayload.VideoToAudioSfxModel)
                {
                    if (propertyName == nameof(AudioBitrateKbps))
                    {
                        return OutputFormat is "mp3" or "aac";
                    }

                    return propertyName is nameof(PollingId) or nameof(Prompt) or nameof(VideoSource)
                        or nameof(Seed) or nameof(Duration) or nameof(NegativePrompt)
                        or nameof(PreserveSpeech) or nameof(ReturnSpeech) or nameof(NumSamples)
                        or nameof(OutputFormat);
                }
            }

            return propertyName is not (nameof(VideoSource) or nameof(Seed) or nameof(Duration)
                or nameof(StartOffset) or nameof(NegativePrompt) or nameof(PreserveSpeech)
                or nameof(ReturnSpeech) or nameof(NumSamples) or nameof(OutputFormat)
                or nameof(AudioBitrateKbps));
        }
    }
}
