using PluginBase;
using System.ComponentModel;

namespace SoniloPlugin;

public class AudioItemPayload : IPayloadPropertyVisibility
{
    [Description("Sonilo async task id. Leave it intact to resume polling; clear it to force a new generation.")]
    public string TaskId { get; set; } = "";

    [EnableTextEditExpand]
    [Description("Prompt for text-to-music/text-to-SFX, or optional guidance for video-to-music/video-to-SFX.")]
    public string Prompt { get; set; } = "";

    [EnableTextEditExpand]
    [Description("Optional music guidance for combined video-to-sound. Leave empty to let Sonilo infer the music from the video.")]
    public string MusicPrompt { get; set; } = "";

    [EnableTextEditExpand]
    [Description("Optional SFX guidance for combined video-to-sound. Leave empty to let Sonilo infer effects from the video.")]
    public string SfxPrompt { get; set; } = "";

    [EnableFileDrop]
    [Description("Local video file or public URL. Local files are uploaded directly to Sonilo.")]
    public string VideoSource { get; set; } = "";

    [EnableTextEditExpand]
    [Description("Optional Sonilo segments JSON. Music segments use start/prompt/label; SFX segments use contiguous start/end/prompt ranges.")]
    public string SegmentsJson { get; set; } = "";

    public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
    {
        var mode = (trackPayload as AudioTrackPayload)?.Mode ?? SoniloModes.AudioTextToMusic;
        return propertyName switch
        {
            nameof(TaskId) => true,
            nameof(Prompt) => mode is not SoniloModes.AudioVideoToSound,
            nameof(MusicPrompt) => mode is SoniloModes.AudioVideoToSound,
            nameof(SfxPrompt) => mode is SoniloModes.AudioVideoToSound,
            nameof(VideoSource) => mode is SoniloModes.AudioVideoToMusic or SoniloModes.AudioVideoToSfx or SoniloModes.AudioVideoToSound,
            nameof(SegmentsJson) => mode is SoniloModes.AudioVideoToMusic or SoniloModes.AudioVideoToSfx or SoniloModes.AudioVideoToSound,
            _ => true
        };
    }
}
