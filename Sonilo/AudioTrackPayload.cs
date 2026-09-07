using PluginBase;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SoniloPlugin;

public class AudioTrackPayload : IPayloadPropertyVisibility
{
    [TriggerReload]
    [Description("Choose whether Sonilo generates from text alone or follows an existing video.")]
    public string Mode { get; set; } = SoniloModes.AudioTextToMusic;

    [Range(1, 360)]
    [Description("Requested output duration in seconds. Text-to-music supports 5-360 seconds; text-to-SFX supports 1-180 seconds.")]
    public int DurationSeconds { get; set; } = 30;

    [Description("Output format. WAV and MP3 are exposed because both are supported by Sonilo and explicitly supported by LVS.")]
    public string OutputFormat { get; set; } = "wav";

    [Description("Keep isolated dialogue/narration from the source video in the generated music/sound mix.")]
    public bool PreserveSpeech { get; set; } = false;

    [Description("Duck generated music under speech so dialogue stays intelligible.")]
    public bool Ducking { get; set; } = false;

    public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
    {
        var mode = (trackPayload as AudioTrackPayload)?.Mode ?? Mode;
        return propertyName switch
        {
            nameof(DurationSeconds) => mode is SoniloModes.AudioTextToMusic or SoniloModes.AudioTextToSfx,
            nameof(PreserveSpeech) => mode is SoniloModes.AudioVideoToMusic or SoniloModes.AudioVideoToSound,
            nameof(Ducking) => mode is SoniloModes.AudioVideoToMusic or SoniloModes.AudioVideoToSound,
            _ => true
        };
    }
}
