namespace SoniloPlugin;

internal static class SoniloModes
{
    internal const string AudioTextToMusic = "Text to music";
    internal const string AudioTextToSfx = "Text to SFX";
    internal const string AudioVideoToMusic = "Video to music";
    internal const string AudioVideoToSfx = "Video to SFX";
    internal const string AudioVideoToSound = "Video to sound (music + SFX)";

    internal static readonly string[] AudioModes =
    [
        AudioTextToMusic,
        AudioTextToSfx,
        AudioVideoToMusic,
        AudioVideoToSfx,
        AudioVideoToSound
    ];

    internal const string VideoMusic = "Music";
    internal const string VideoSfx = "SFX";
    internal const string VideoSound = "Sound (music + SFX)";

    internal static readonly string[] VideoModes = [VideoMusic, VideoSfx, VideoSound];

    // WAV and MP3 are the common Sonilo formats that LVS explicitly supports.
    internal static readonly string[] OutputFormats = ["wav", "mp3"];
}
