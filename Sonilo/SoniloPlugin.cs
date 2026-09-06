using PluginBase;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SoniloPlugin;

#pragma warning disable CS1998 // Async signatures are part of the plugin contract.

public class SoniloPlugin : IAudioPlugin, IVideoPlugin, IImportFromVideo, ISaveAndRefresh,
    ITextualProgressIndication, ICancellableGeneration, IContentId, IValidateBothPayloads, IDisposable
{
    public const string PluginName = "SoniloBuildIn";

    public string UniqueName => PluginName;
    public string DisplayName => "Sonilo";
    public object GeneralDefaultSettings => new ConnectionSettings();
    public bool IsInitialized { get; private set; }
    public bool AsynchronousGeneration => true;
    public IPluginBase.TrackType CurrentTrackType { get; set; }

    public string SettingsHelpText =>
        "Powered by Sonilo. Create an API key at platform.sonilo.com. Sonilo generations use provider credits. " +
        "The plugin supports standalone music/SFX generation and adding generated audio directly to video.";

    public string[] SettingsLinks =>
    [
        "https://platform.sonilo.com/",
        "https://platform.sonilo.com/en/docs"
    ];

    private ConnectionSettings _connectionSettings = new();
    private SoniloClient? _client;
    private Action<bool>? _saveAndRefreshCallback;
    private Action<string>? _textProgressCallback;
    private CancellationToken _cancellationToken;

    public async Task<string> Initialize(object settings)
    {
        if (JsonHelper.DeepCopy<ConnectionSettings>(settings) is not ConnectionSettings copied)
        {
            return "Connection settings object not valid";
        }

        _client?.Dispose();
        _connectionSettings = copied;
        _client = new SoniloClient(_connectionSettings);
        IsInitialized = true;
        return "";
    }

    public async Task<string> TestInitialization()
    {
        if (string.IsNullOrWhiteSpace(_connectionSettings.ApiKey))
        {
            return "Sonilo API key is empty";
        }

        var client = _client ?? new SoniloClient(_connectionSettings);
        try
        {
            return await client.TestConnectionAsync(CancellationToken.None);
        }
        finally
        {
            if (_client == null)
            {
                client.Dispose();
            }
        }
    }

    public void CloseConnection()
    {
        _client?.Dispose();
        _client = null;
        IsInitialized = false;
    }

    public IPluginBase CreateNewInstance() => new SoniloPlugin();

    public async Task<AudioResponse> GetAudio(object trackPayload, object itemsPayload, string folderToSaveAudio)
    {
        if (!IsInitialized || _client == null || string.IsNullOrWhiteSpace(_connectionSettings.ApiKey))
        {
            return new AudioResponse { Success = false, ErrorMsg = "Sonilo plugin is uninitialized" };
        }

        if (JsonHelper.DeepCopy<AudioTrackPayload>(trackPayload) is not AudioTrackPayload track ||
            itemsPayload is not AudioItemPayload itemForState)
        {
            return new AudioResponse { Success = false, ErrorMsg = "Track payload or item payload object not valid" };
        }

        // Request fields are read-only. TaskId is the only intentional mutation of the original item payload.

        return await _client.GenerateAudioAsync(
            track, itemForState, folderToSaveAudio, _saveAndRefreshCallback, _textProgressCallback, _cancellationToken);
    }

    public async Task<VideoResponse> GetVideo(object trackPayload, object itemsPayload, string folderToSaveVideo)
    {
        if (!IsInitialized || _client == null || string.IsNullOrWhiteSpace(_connectionSettings.ApiKey))
        {
            return new VideoResponse { Success = false, ErrorMsg = "Sonilo plugin is uninitialized" };
        }

        if (JsonHelper.DeepCopy<VideoTrackPayload>(trackPayload) is not VideoTrackPayload track ||
            itemsPayload is not VideoItemPayload itemForState)
        {
            return new VideoResponse { Success = false, ErrorMsg = "Track payload or item payload object not valid" };
        }


        return await _client.GenerateVideoAsync(
            track, itemForState, folderToSaveVideo, _saveAndRefreshCallback, _textProgressCallback, _cancellationToken);
    }

    public Task<string[]> SelectionOptionsForProperty(string propertyName)
    {
        if (propertyName == nameof(AudioTrackPayload.Mode) || propertyName == nameof(VideoTrackPayload.Mode))
        {
            return Task.FromResult(CurrentTrackType == IPluginBase.TrackType.Audio
                ? SoniloModes.AudioModes
                : SoniloModes.VideoModes);
        }

        if (propertyName == nameof(AudioTrackPayload.OutputFormat))
        {
            return Task.FromResult(SoniloModes.OutputFormats);
        }

        return Task.FromResult(Array.Empty<string>());
    }

    public object DefaultPayloadForTrack()
    {
        return CurrentTrackType switch
        {
            IPluginBase.TrackType.Audio => new AudioTrackPayload(),
            IPluginBase.TrackType.Video => new VideoTrackPayload(),
            _ => throw new NotSupportedException("Sonilo supports audio and video tracks only")
        };
    }

    public object DefaultPayloadForItem()
    {
        return CurrentTrackType switch
        {
            IPluginBase.TrackType.Audio => new AudioItemPayload(),
            IPluginBase.TrackType.Video => new VideoItemPayload(),
            _ => throw new NotSupportedException("Sonilo supports audio and video tracks only")
        };
    }

    public object CopyPayloadForTrack(object obj)
    {
        return CurrentTrackType switch
        {
            IPluginBase.TrackType.Audio => JsonHelper.DeepCopy<AudioTrackPayload>(obj) ?? new AudioTrackPayload(),
            IPluginBase.TrackType.Video => JsonHelper.DeepCopy<VideoTrackPayload>(obj) ?? new VideoTrackPayload(),
            _ => throw new NotSupportedException("Sonilo supports audio and video tracks only")
        };
    }

    public object CopyPayloadForItem(object obj)
    {
        return CurrentTrackType switch
        {
            IPluginBase.TrackType.Audio => JsonHelper.DeepCopy<AudioItemPayload>(obj) ?? new AudioItemPayload(),
            IPluginBase.TrackType.Video => JsonHelper.DeepCopy<VideoItemPayload>(obj) ?? new VideoItemPayload(),
            _ => throw new NotSupportedException("Sonilo supports audio and video tracks only")
        };
    }

    public object DeserializePayload(string fileName)
    {
        return CurrentTrackType switch
        {
            IPluginBase.TrackType.Audio => JsonHelper.Deserialize<AudioTrackPayload>(fileName),
            IPluginBase.TrackType.Video => JsonHelper.Deserialize<VideoTrackPayload>(fileName),
            _ => throw new NotSupportedException("Sonilo supports audio and video tracks only")
        };
    }

    public object ObjectToItemPayload(JsonObject obj)
    {
        return CurrentTrackType switch
        {
            IPluginBase.TrackType.Audio => JsonHelper.ToExactType<AudioItemPayload>(obj),
            IPluginBase.TrackType.Video => JsonHelper.ToExactType<VideoItemPayload>(obj),
            _ => throw new NotSupportedException("Sonilo supports audio and video tracks only")
        };
    }

    public object ObjectToTrackPayload(JsonObject obj)
    {
        return CurrentTrackType switch
        {
            IPluginBase.TrackType.Audio => JsonHelper.ToExactType<AudioTrackPayload>(obj),
            IPluginBase.TrackType.Video => JsonHelper.ToExactType<VideoTrackPayload>(obj),
            _ => throw new NotSupportedException("Sonilo supports audio and video tracks only")
        };
    }

    public object ObjectToGeneralSettings(JsonObject obj) => JsonHelper.ToExactType<ConnectionSettings>(obj);

    public string TextualRepresentation(object itemPayload)
    {
        return itemPayload switch
        {
            AudioItemPayload audio => FirstNonEmpty(audio.Prompt, audio.MusicPrompt, audio.SfxPrompt),
            VideoItemPayload video => FirstNonEmpty(video.Prompt, video.MusicPrompt, video.SfxPrompt),
            _ => ""
        };
    }

    public (bool payloadOk, string reasonIfNot) ValidatePayload(object payload)
    {
        if (string.IsNullOrWhiteSpace(_connectionSettings.ApiKey))
        {
            return (false, "Sonilo API key is empty");
        }

        if (payload is AudioTrackPayload at)
        {
            if (!SoniloModes.AudioModes.Contains(at.Mode))
            {
                return (false, "Unknown Sonilo audio mode");
            }
            if (!SoniloModes.OutputFormats.Contains(at.OutputFormat))
            {
                return (false, "Output format must be WAV or MP3");
            }
        }

        if (payload is VideoTrackPayload vt && !SoniloModes.VideoModes.Contains(vt.Mode))
        {
            return (false, "Unknown Sonilo video mode");
        }

        return (true, "");
    }

    public (bool payloadOk, string reasonIfNot) ValidatePayloads(object trackPaylod, object itemPayload)
    {
        if (string.IsNullOrWhiteSpace(_connectionSettings.ApiKey))
        {
            return (false, "Sonilo API key is empty");
        }

        if (trackPaylod is AudioTrackPayload at && itemPayload is AudioItemPayload ai)
        {
            if (!string.IsNullOrWhiteSpace(ai.TaskId))
            {
                return (true, "");
            }

            if (!ValidateSegments(ai.SegmentsJson, out var segmentsError))
            {
                return (false, segmentsError);
            }

            if (!ValidatePrompts(ai.Prompt, ai.MusicPrompt, ai.SfxPrompt, out var promptError))
            {
                return (false, promptError);
            }

            return at.Mode switch
            {
                SoniloModes.AudioTextToMusic when string.IsNullOrWhiteSpace(ai.Prompt) => (false, "Prompt is required for text-to-music"),
                SoniloModes.AudioTextToMusic when at.DurationSeconds < 5 || at.DurationSeconds > 360 => (false, "Text-to-music duration must be 5-360 seconds"),
                SoniloModes.AudioTextToSfx when string.IsNullOrWhiteSpace(ai.Prompt) => (false, "Prompt is required for text-to-SFX"),
                SoniloModes.AudioTextToSfx when at.DurationSeconds < 1 || at.DurationSeconds > 180 => (false, "Text-to-SFX duration must be 1-180 seconds"),
                SoniloModes.AudioVideoToMusic or SoniloModes.AudioVideoToSfx or SoniloModes.AudioVideoToSound => ValidateVideoSource(ai.VideoSource),
                _ => (true, "")
            };
        }

        if (trackPaylod is VideoTrackPayload && itemPayload is VideoItemPayload vi)
        {
            if (!string.IsNullOrWhiteSpace(vi.TaskId))
            {
                return (true, "");
            }

            if (!ValidateSegments(vi.SegmentsJson, out var segmentsError))
            {
                return (false, segmentsError);
            }

            if (!ValidatePrompts(vi.Prompt, vi.MusicPrompt, vi.SfxPrompt, out var promptError))
            {
                return (false, promptError);
            }

            return ValidateVideoSource(vi.VideoSource);
        }

        return (false, "Sonilo track/item payload types do not match");
    }

    public object ItemPayloadFromVideoSource(string videoSource)
    {
        return CurrentTrackType switch
        {
            IPluginBase.TrackType.Audio => new AudioItemPayload { VideoSource = videoSource },
            IPluginBase.TrackType.Video => new VideoItemPayload { VideoSource = videoSource },
            _ => throw new NotSupportedException("Sonilo supports audio and video tracks only")
        };
    }

    public List<string> FilePathsOnPayloads(object trackPayload, object itemPayload)
    {
        var source = itemPayload switch
        {
            AudioItemPayload audio => audio.VideoSource,
            VideoItemPayload video => video.VideoSource,
            _ => ""
        };

        return IsPublicUrl(source) || string.IsNullOrWhiteSpace(source) ? new List<string>() : new List<string> { source };
    }

    public void ReplaceFilePathsOnPayloads(List<string> originalPath, List<string> newPath, object trackPayload, object itemPayload)
    {
        var count = Math.Min(originalPath.Count, newPath.Count);
        for (var i = 0; i < count; i++)
        {
            if (itemPayload is AudioItemPayload audio && audio.VideoSource == originalPath[i])
            {
                audio.VideoSource = newPath[i];
            }
            else if (itemPayload is VideoItemPayload video && video.VideoSource == originalPath[i])
            {
                video.VideoSource = newPath[i];
            }
        }
    }

    public object ItemPayloadFromLyrics(string text)
    {
        return CurrentTrackType switch
        {
            IPluginBase.TrackType.Audio => new AudioItemPayload { Prompt = text ?? "" },
            IPluginBase.TrackType.Video => new VideoItemPayload { Prompt = text ?? "", MusicPrompt = text ?? "" },
            _ => throw new NotSupportedException("Sonilo supports audio and video tracks only")
        };
    }

    public void AppendToPayloadFromLyrics(string text, object payload)
    {
        if (payload is AudioItemPayload audio)
        {
            audio.Prompt = text ?? "";
            audio.MusicPrompt = text ?? "";
        }
        else if (payload is VideoItemPayload video)
        {
            video.Prompt = text ?? "";
            video.MusicPrompt = text ?? "";
        }
    }

    public string GetContentFromPayloadId(object payload)
    {
        return payload switch
        {
            AudioItemPayload audio => audio.TaskId ?? "",
            VideoItemPayload video => video.TaskId ?? "",
            _ => ""
        };
    }

    public void SetSaveAndRefreshCallback(Action<bool> saveAndRefreshCallback) =>
        _saveAndRefreshCallback = saveAndRefreshCallback;

    public void SetTextProgressCallback(Action<string> action) => _textProgressCallback = action;

    public void SetCancallationToken(CancellationToken cancellationToken) => _cancellationToken = cancellationToken;

    public void UserDataDeleteRequested() => _connectionSettings.DeleteTokens();

    public void Dispose() => CloseConnection();

    private static (bool payloadOk, string reasonIfNot) ValidateVideoSource(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return (false, "Video source is required");
        }

        if (IsPublicUrl(source))
        {
            return (true, "");
        }

        var path = WorkspaceSettings.GetAbsolutePath(source, true);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return (false, $"Video source file not found: {source}");
        }

        var allowed = new[] { ".mp4", ".mov", ".webm", ".m4v", ".gif" };
        if (!allowed.Contains(Path.GetExtension(path).ToLowerInvariant()))
        {
            return (false, "Sonilo video input must be mp4, mov, webm, m4v, or animated gif");
        }

        return (true, "");
    }

    private static bool ValidateSegments(string segmentsJson, out string error)
    {
        error = "";
        if (string.IsNullOrWhiteSpace(segmentsJson))
        {
            return true;
        }

        try
        {
            using var json = JsonDocument.Parse(segmentsJson);
            if (json.RootElement.ValueKind != JsonValueKind.Array)
            {
                error = "Segments JSON must be an array";
                return false;
            }
            return true;
        }
        catch (JsonException ex)
        {
            error = $"Segments JSON is invalid: {ex.Message}";
            return false;
        }
    }

    private static bool ValidatePrompts(string prompt, string musicPrompt, string sfxPrompt, out string error)
    {
        error = "";
        foreach (var value in new[] { prompt, musicPrompt, sfxPrompt })
        {
            if (!string.IsNullOrEmpty(value) && value.Length > 2000)
            {
                error = "Sonilo prompts are limited to 2000 characters";
                return false;
            }
        }
        return true;
    }

    private static bool IsPublicUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static string FirstNonEmpty(params string[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
}

#pragma warning restore CS1998
