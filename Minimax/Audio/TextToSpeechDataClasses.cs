using PluginBase;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MinimaxPlugin.Audio
{
    public class MusicRequest
    {
        public const string MusicModel = "music-2.6";
        public const string MusicCover = "music-cover";
        public const string MusicModelFree = "music-2.6-free";
        public const string MusicCoverFree = "music-cover-free";

        [JsonPropertyName("model")]
        [IgnoreDynamicEdit]
        public string Model { get; set; } = MusicModelFree;

        [JsonPropertyName("lyrics")]
        [Description("Song lyrics, using \n to separate lines. Supports structure tags: [Intro], [Verse], [Pre Chorus], [Chorus], [Interlude], [Bridge], [Outro], [Post Chorus], [Transition], [Break], [Hook], [Build Up], [Inst], [Solo]")]
        public string Lyrics { get; set; } = "";

        [JsonPropertyName("prompt")]
        [IgnoreDynamicEdit]
        public string Prompt { get; set; } = "";

        [Description("Whether to automatically generate lyrics based on the prompt description. Only supported on music-2.6 / music-2.6-free")]
        [JsonPropertyName("lyrics_optimizer")]
        public bool LyricsOptimizer { get; set; }

        [Description("Whether to generate instrumental music (no vocals). Only supported on music-2.6 / music-2.6-free.")]
        [JsonPropertyName("is_instrumental")]
        public bool IsInstrumental { get; set; }

        [JsonPropertyName("audio_base64")]
        public string Audio { get; set; }

        /// <summary>
        /// Audio setting for the output audio file.
        /// </summary>
        [JsonPropertyName("audio_setting")]
        [IgnoreDynamicEdit]
        public AudioSetting AudioSetting { get; set; } = new AudioSetting();

        /// <summary>
        /// This parameter controls the format of the output content.
        /// </summary>
        [JsonPropertyName("output_format")]
        [IgnoreDynamicEdit]
        public string OutputFormat { get; set; } = "hex";

        public static bool IsMusicModel(string model)
        {
            return model is MusicModel or MusicCover or MusicModelFree or MusicCoverFree;
        }
    }

    /// <summary>
    /// Represents the request payload for the Text-to-Speech API.
    /// </summary>
    public class T2ARequest : IPayloadPropertyVisibility
    {
        /// <summary>
        /// Desired model. Includes: speech-02-hd, speech-01-turbo, speech-01-hd, speech-01-turbo.
        /// </summary>
        [JsonPropertyName("model")]
        [PropertyComboOptions(["speech-2.8-hd", "speech-2.8-turbo", "speech-2.6-hd", "speech-2.6-turbo", MusicRequest.MusicModel, MusicRequest.MusicCover, MusicRequest.MusicModelFree, MusicRequest.MusicCoverFree])]
        public string Model { get; set; } = "speech-02-hd";

        /// <summary>
        /// Text to be synthesized. Character limit < 5000 chars.
        /// </summary>
        [JsonPropertyName("text")]
        [IgnoreDynamicEdit]
        [Description("The text to be converted into speech. Must be less than 10,000 characters.\r\n\r\n    For texts over 3,000 characters, streaming output is recommended.\r\n\r\n    Paragraph breaks should be marked with newline characters.\r\n\r\n    Pause control: You can customize speech pauses by adding markers in the form <#x#>, where x is the pause duration in seconds. Valid range: [0.01, 99.99], up to two decimal places. Pause markers must be placed between speakable text segments and cannot be used consecutively.\r\n\r\n    Interjection tags: Only supported when using speech-2.8-hd or speech-2.8-turbo models. Supported interjections: (laughs), (chuckle), (coughs), (clear-throat), (groans), (breath), (pant), (inhale), (exhale), (gasps), (sniffs), (sighs), (snorts), (burps), (lip-smacking), (humming), (hissing), (emm), (sneezes).\r\n")]
        public string Text { get; set; }

        /// <summary>
        /// Boolean value indicating whether the generated audio will be a stream. Defaults to non-streaming output.
        /// </summary>
        [JsonPropertyName("stream")]
        [IgnoreDynamicEdit]
        public bool Stream { get; set; }

        /// <summary>
        /// Voice setting for the speech synthesis.
        /// </summary>
        [JsonPropertyName("voice_setting")]
        public VoiceSetting VoiceSetting { get; set; } = new VoiceSetting();

        /// <summary>
        /// Audio setting for the output audio file.
        /// </summary>
        [JsonPropertyName("audio_setting")]
        public AudioSetting AudioSetting { get; set; } = new AudioSetting();

        /// <summary>
        /// Weighted voice mixing. Either voice_id or timber_weights is required.
        /// </summary>
        [JsonPropertyName("timber_weights")]
        [IgnoreDynamicEdit]
        public List<TimberWeight> TimberWeights { get; set; } = new List<TimberWeight>();

        /// <summary>
        /// Enhance the ability to recognize specified languages and dialects.
        /// </summary>
        [JsonPropertyName("language_boost")]
        public string LanguageBoost { get; set; } = "auto";

        /// <summary>
        /// The parameter controls whether the subtitle service is enabled.
        /// </summary>
        [JsonPropertyName("subtitle_enable")]
        [IgnoreDynamicEdit]
        public bool SubtitleEnable { get; set; }

        /// <summary>
        /// This parameter controls the format of the output content.
        /// </summary>
        [JsonPropertyName("output_format")]
        [IgnoreDynamicEdit]
        public string OutputFormat { get; set; } = "hex";

        public System.Reactive.Subjects.Subject<bool> UpdateVoices { get; } = new System.Reactive.Subjects.Subject<bool>();

        [CustomAction("Refresh voices")]
        public void RefreshVoces()
        {
            UpdateVoices.OnNext(true);
        }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is T2ARequest tp)
            {
                if (propertyName == nameof(VoiceSetting.VoiceId) ||
                    propertyName == nameof(VoiceSetting.Speed) ||
                    propertyName == nameof(VoiceSetting.Volume) ||
                    propertyName == nameof(VoiceSetting.Pitch) ||
                    propertyName == nameof(VoiceSetting.Emotion) || 
                    propertyName == nameof(T2ARequest.LanguageBoost) || 
                    propertyName == nameof(T2ARequest.RefreshVoces))
                {
                    return !MusicRequest.IsMusicModel(tp.Model);
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Represents the voice setting.
    /// </summary>
    public class VoiceSetting
    {
        /// <summary>
        /// The ID of the voice to use.
        /// </summary>
        [JsonPropertyName("voice_id")]
        public string VoiceId { get; set; }

        /// <summary>
        /// Speed of the speech.
        /// </summary>
        [JsonPropertyName("speed")]
        [Range(0.5, 2)]
        public float Speed { get; set; } = 1.0f;

        /// <summary>
        /// Volume of the speech.
        /// </summary>
        [JsonPropertyName("vol")]
        [Range(0, 10)]
        public float Volume { get; set; } = 1.0f;

        /// <summary>
        /// Pitch of the speech.
        /// </summary>
        [JsonPropertyName("pitch")]
        [Range(-12, 12)]
        public float Pitch { get; set; } = 0;

        /// <summary>
        /// Emotion of the speech
        /// </summary>
        [JsonPropertyName("emotion")]
        [PropertyComboOptions(["happy", "sad", "angry", "fearful", "disgusted", "surprised", "calm", "calm", "fluent", "whisper"])]
        public string Emotion { get; set; } = "happy";
    }

    /// <summary>
    /// Represents the audio setting.
    /// </summary>
    public class AudioSetting
    {
        /// <summary>
        /// Sample rate of the audio.
        /// </summary>
        [JsonPropertyName("sample_rate")]
        [PropertyComboOptions(["44100", "32000", "24000", "22050", "16000", "8000"])]
        public int SampleRate { get; set; } = 44100;

        /// <summary>
        /// Bitrate of the audio.
        /// </summary>
        [JsonPropertyName("bitrate")]
        [PropertyComboOptions(["256000", "128000", "64000", "32000"])]
        public int Bitrate { get; set; } = 256000;

        /// <summary>
        /// Format of the audio (e.g., "mp3").
        /// </summary>
        [JsonPropertyName("format")]
        [PropertyComboOptions(["mp3", "wav", "pcm"])]
        public string Format { get; set; } = "mp3";
    }

    /// <summary>
    /// Represents the timber weights for voice mixing.
    /// </summary>
    public class TimberWeight
    {
        /// <summary>
        /// The ID of the voice to be mixed.
        /// </summary>
        [JsonPropertyName("voice_id")]
        public string VoiceId { get; set; }

        /// <summary>
        /// The weight of the voice in the mix.
        /// </summary>
        [JsonPropertyName("weight")]
        public int Weight { get; set; }
    }

    /// <summary>
    /// Represents the overall response from the T2A API.
    /// </summary>
    public class T2AResponse
    {
        /// <summary>
        /// The main data of the response.
        /// </summary>
        [JsonPropertyName("data")]
        public ResponseData Data { get; set; }

        /// <summary>
        /// Additional information about the request.
        /// </summary>
        [JsonPropertyName("extra_info")]
        public ExtraInfo ExtraInfo { get; set; }

        /// <summary>
        /// The ID of the current conversation.
        /// </summary>
        [JsonPropertyName("trace_id")]
        public string TraceId { get; set; }

        /// <summary>
        /// Contains error codes and status messages.
        /// </summary>
        [JsonPropertyName("base_resp")]
        public BaseResponse BaseResp { get; set; }
    }

    /// <summary>
    /// Represents the data part of the response.
    /// </summary>
    public class ResponseData
    {
        /// <summary>
        /// The generated audio content in hexadecimal format.
        /// </summary>
        [JsonPropertyName("audio")]
        public string Audio { get; set; }

        /// <summary>
        /// The status of the audio generation.
        /// </summary>
        [JsonPropertyName("status")]
        public int Status { get; set; }

        /// <summary>
        /// The download link for the synthesized subtitles.
        /// </summary>
        [JsonPropertyName("subtitle_file")]
        public string SubtitleFile { get; set; }
    }

    /// <summary>
    /// Represents extra information provided in the response.
    /// </summary>
    public class ExtraInfo
    {
        /// <summary>
        /// Length of the audio in milliseconds.
        /// </summary>
        [JsonPropertyName("audio_length")]
        public int AudioLength { get; set; }

        /// <summary>
        /// Sample rate of the generated audio.
        /// </summary>
        [JsonPropertyName("audio_sample_rate")]
        public int AudioSampleRate { get; set; }

        /// <summary>
        /// Size of the audio file in bytes.
        /// </summary>
        [JsonPropertyName("audio_size")]
        public int AudioSize { get; set; }

        /// <summary>
        /// Bitrate of the generated audio.
        /// </summary>
        [JsonPropertyName("audio_bitrate")]
        public int AudioBitrate { get; set; }

        /// <summary>
        /// The number of words in the text.
        /// </summary>
        [JsonPropertyName("word_count")]
        public int WordCount { get; set; }

        /// <summary>
        /// The ratio of invisible characters.
        /// </summary>
        [JsonPropertyName("invisible_character_ratio")]
        public int InvisibleCharacterRatio { get; set; }

        /// <summary>
        /// The format of the audio.
        /// </summary>
        [JsonPropertyName("audio_format")]
        public string AudioFormat { get; set; }

        /// <summary>
        /// The number of characters used for billing.
        /// </summary>
        [JsonPropertyName("usage_characters")]
        public int UsageCharacters { get; set; }
    }

    /// <summary>
    /// Represents the base response containing status information.
    /// </summary>
    public class BaseResponse
    {
        /// <summary>
        /// The status code of the response. 0 indicates success.
        /// </summary>
        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }

        /// <summary>
        /// The status message of the response.
        /// </summary>
        [JsonPropertyName("status_msg")]
        public string StatusMsg { get; set; }
    }

    /// <summary>
    /// Represents the root object containing various lists of voice and music generation information.
    /// </summary>
    public class VoiceListResponse
    {
        [JsonPropertyName("voice_slots")]
        public List<VoiceSlot> VoiceSlots { get; set; }

        [JsonPropertyName("system_voice")]
        public List<SystemVoice> SystemVoice { get; set; }

        [JsonPropertyName("voice_cloning")]
        public List<VoiceCloning> VoiceCloning { get; set; }

        [JsonPropertyName("voice_generation")]
        public List<VoiceGeneration> VoiceGeneration { get; set; }

        [JsonPropertyName("music_generation")]
        public List<MusicGeneration> MusicGeneration { get; set; }
    }

    /// <summary>
    /// Represents a voice slot with its ID, name, and description.
    /// </summary>
    public class VoiceSlot
    {
        [JsonPropertyName("voice_id")]
        public string VoiceId { get; set; }

        [JsonPropertyName("voice_name")]
        public string VoiceName { get; set; }

        [JsonPropertyName("description")]
        public List<string> Description { get; set; }
    }

    /// <summary>
    /// Represents a system voice with its ID, name, and description.
    /// </summary>
    public class SystemVoice
    {
        [JsonPropertyName("voice_id")]
        public string VoiceId { get; set; }

        [JsonPropertyName("voice_name")]
        public string VoiceName { get; set; }

        [JsonPropertyName("description")]
        public List<string> Description { get; set; }
    }

    /// <summary>
    /// Represents a cloned voice with its ID, description, and creation time.
    /// </summary>
    public class VoiceCloning
    {
        [JsonPropertyName("voice_id")]
        public string VoiceId { get; set; }

        [JsonPropertyName("description")]
        public List<string> Description { get; set; }

        [JsonPropertyName("created_time")]
        public string CreatedTime { get; set; }
    }

    /// <summary>
    /// Represents a generated voice with its ID, description, and creation time.
    /// </summary>
    public class VoiceGeneration
    {
        [JsonPropertyName("voice_id")]
        public string VoiceId { get; set; }

        [JsonPropertyName("description")]
        public List<string> Description { get; set; }

        [JsonPropertyName("created_time")]
        public string CreatedTime { get; set; }
    }

    /// <summary>
    /// Represents a music generation entry with voice, instrumental, and creation time information.
    /// </summary>
    public class MusicGeneration
    {
        [JsonPropertyName("voice_id")]
        public string VoiceId { get; set; }

        [JsonPropertyName("instrumental_id")]
        public string InstrumentalId { get; set; }

        [JsonPropertyName("created_time")]
        public string CreatedTime { get; set; }
    }
}