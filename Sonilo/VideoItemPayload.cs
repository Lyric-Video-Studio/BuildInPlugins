using PluginBase;
using System.ComponentModel;

namespace SoniloPlugin;

public class VideoItemPayload : IPayloadPropertyVisibility
{
    [Description("Sonilo async task id. Leave it intact to resume polling; clear it to force a new generation.")]
    public string TaskId { get; set; } = "";

    [EnableFileDrop]
    [Description("Local video file or public URL. Local files are uploaded directly to Sonilo.")]
    public string VideoSource { get; set; } = "";

    [EnableTextEditExpand]
    [Description("Optional guidance for music-only or SFX-only generation. Leave empty to infer from the video.")]
    public string Prompt { get; set; } = "";

    [EnableTextEditExpand]
    [Description("Optional music guidance in combined mode. Leave empty to infer from the video.")]
    public string MusicPrompt { get; set; } = "";

    [EnableTextEditExpand]
    [Description("Optional SFX guidance in combined mode. Leave empty to infer from the video.")]
    public string SfxPrompt { get; set; } = "";

    [EnableTextEditExpand]
    [Description("Optional Sonilo segments JSON. Music segments use start/prompt/label; SFX segments use contiguous start/end/prompt ranges.")]
    public string SegmentsJson { get; set; } = "";

    public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
    {
        var mode = (trackPayload as VideoTrackPayload)?.Mode ?? SoniloModes.VideoSound;
        return propertyName switch
        {
            nameof(TaskId) => true,
            nameof(VideoSource) => true,
            nameof(Prompt) => mode is SoniloModes.VideoMusic or SoniloModes.VideoSfx,
            nameof(MusicPrompt) => mode is SoniloModes.VideoSound,
            nameof(SfxPrompt) => mode is SoniloModes.VideoSound,
            nameof(SegmentsJson) => true,
            _ => true
        };
    }
}
