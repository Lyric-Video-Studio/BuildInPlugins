using PluginBase;
using System.ComponentModel;

namespace SoniloPlugin;

public class VideoTrackPayload : IPayloadPropertyVisibility
{
    [TriggerReload]
    [Description("Generated soundtrack to mux into the source video.")]
    public string Mode { get; set; } = SoniloModes.VideoSound;

    [Description("Keep isolated dialogue/narration from the source video in the new soundtrack.")]
    public bool PreserveSpeech { get; set; } = false;

    [Description("Combined mode only: keep the whole original audio track instead of only isolated speech. Supersedes Preserve speech.")]
    public bool KeepOriginalSound { get; set; } = false;

    [Description("Duck generated music under the retained speech/original audio.")]
    public bool Ducking { get; set; } = false;

    public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
    {
        var mode = (trackPayload as VideoTrackPayload)?.Mode ?? Mode;
        return propertyName switch
        {
            nameof(PreserveSpeech) => mode is SoniloModes.VideoMusic or SoniloModes.VideoSound,
            nameof(KeepOriginalSound) => mode is SoniloModes.VideoSound,
            nameof(Ducking) => mode is SoniloModes.VideoMusic or SoniloModes.VideoSound,
            _ => true
        };
    }
}
