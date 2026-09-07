using PluginBase;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SoniloPlugin;

internal sealed class SoniloClient : IDisposable
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    private readonly ConnectionSettings _settings;

    internal SoniloClient(ConnectionSettings settings)
    {
        _settings = settings;
    }

    internal async Task<string> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await SendWithRetryAsync(
                () => CreateRequest(HttpMethod.Get, "account/services"), cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return await ReadErrorAsync(response, cancellationToken);
            }

            return "";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    internal async Task<AudioResponse> GenerateAudioAsync(
        AudioTrackPayload track,
        AudioItemPayload item,
        string folderToSaveAudio,
        Action<bool>? saveAndRefresh,
        Action<string>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            var endpoint = track.Mode switch
            {
                SoniloModes.AudioTextToMusic => "text-to-music",
                SoniloModes.AudioTextToSfx => "text-to-sfx",
                SoniloModes.AudioVideoToMusic => "video-to-music",
                SoniloModes.AudioVideoToSfx => "video-to-sfx",
                SoniloModes.AudioVideoToSound => "video-to-sound",
                _ => throw new InvalidOperationException($"Unsupported Sonilo audio mode: {track.Mode}")
            };

            var task = await SubmitOrResumeTaskAsync(
                endpoint,
                item.TaskId,
                taskId =>
                {
                    item.TaskId = taskId;
                    saveAndRefresh?.Invoke(true);
                },
                () => CreateAudioMultipart(track, item),
                progress,
                cancellationToken);

            if (!task.Success)
            {
                if (task.Terminal)
                {
                    item.TaskId = "";
                    saveAndRefresh?.Invoke(true);
                }
                return new AudioResponse { Success = false, ErrorMsg = task.Error };
            }

            using var resultJson = task.Json!;
            var root = resultJson.RootElement;
            var (url, contentType) = ExtractAudioResult(root, track);
            if (string.IsNullOrWhiteSpace(url))
            {
                item.TaskId = "";
                saveAndRefresh?.Invoke(true);
                return new AudioResponse { Success = false, ErrorMsg = "Sonilo task succeeded but no audio URL was returned." };
            }

            progress?.Invoke("Downloading Sonilo audio");
            var audioPath = await DownloadAsync(url, contentType, folderToSaveAudio, false, cancellationToken);

            item.TaskId = "";
            saveAndRefresh?.Invoke(true);

            return new AudioResponse
            {
                Success = true,
                AudioFile = audioPath,
                AudioFormat = Path.GetExtension(audioPath).TrimStart('.'),
                Params = BuildParams(root, task.TaskId)
            };
        }
        catch (OperationCanceledException)
        {
            // Deliberately keep TaskId. Sonilo has no server-side cancel endpoint and the job may still finish.
            return new AudioResponse
            {
                Success = false,
                ErrorMsg = "Cancelled locally. The Sonilo task may still be running and billed; retry generation to resume polling the same task."
            };
        }
        catch (Exception ex)
        {
            return new AudioResponse { Success = false, ErrorMsg = ex.Message };
        }
    }

    internal async Task<VideoResponse> GenerateVideoAsync(
        VideoTrackPayload track,
        VideoItemPayload item,
        string folderToSaveVideo,
        Action<bool>? saveAndRefresh,
        Action<string>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            var endpoint = track.Mode switch
            {
                SoniloModes.VideoMusic => "video-to-video-music",
                SoniloModes.VideoSfx => "video-to-video-sfx",
                SoniloModes.VideoSound => "video-to-video-sound",
                _ => throw new InvalidOperationException($"Unsupported Sonilo video mode: {track.Mode}")
            };

            var task = await SubmitOrResumeTaskAsync(
                endpoint,
                item.TaskId,
                taskId =>
                {
                    item.TaskId = taskId;
                    saveAndRefresh?.Invoke(true);
                },
                () => CreateVideoMultipart(track, item),
                progress,
                cancellationToken);

            if (!task.Success)
            {
                if (task.Terminal)
                {
                    item.TaskId = "";
                    saveAndRefresh?.Invoke(true);
                }
                return new VideoResponse { Success = false, ErrorMsg = task.Error };
            }

            using var resultJson = task.Json!;
            var root = resultJson.RootElement;
            var (url, contentType) = ExtractVideoResult(root, track.Mode);
            if (string.IsNullOrWhiteSpace(url))
            {
                item.TaskId = "";
                saveAndRefresh?.Invoke(true);
                return new VideoResponse { Success = false, ErrorMsg = "Sonilo task succeeded but no video URL was returned." };
            }

            progress?.Invoke("Downloading Sonilo video");
            var videoPath = await DownloadAsync(url, contentType, folderToSaveVideo, true, cancellationToken);

            item.TaskId = "";
            saveAndRefresh?.Invoke(true);

            return new VideoResponse
            {
                Success = true,
                VideoFile = videoPath,
                Params = BuildParams(root, task.TaskId)
            };
        }
        catch (OperationCanceledException)
        {
            return new VideoResponse
            {
                Success = false,
                ErrorMsg = "Cancelled locally. The Sonilo task may still be running and billed; retry generation to resume polling the same task."
            };
        }
        catch (Exception ex)
        {
            return new VideoResponse { Success = false, ErrorMsg = ex.Message };
        }
    }

    private MultipartFormDataContent CreateAudioMultipart(AudioTrackPayload track, AudioItemPayload item)
    {
        var form = new MultipartFormDataContent();

        switch (track.Mode)
        {
            case SoniloModes.AudioTextToMusic:
                AddString(form, "prompt", item.Prompt);
                AddString(form, "duration", track.DurationSeconds.ToString());
                AddString(form, "mode", "async");
                AddString(form, "output_format", track.OutputFormat);
                break;

            case SoniloModes.AudioTextToSfx:
                AddString(form, "prompt", item.Prompt);
                AddString(form, "duration", track.DurationSeconds.ToString());
                AddString(form, "audio_format", track.OutputFormat);
                break;

            case SoniloModes.AudioVideoToMusic:
                AddVideoSource(form, item.VideoSource);
                AddOptionalString(form, "prompt", item.Prompt);
                AddOptionalString(form, "segments", item.SegmentsJson);
                AddString(form, "mode", "async");
                AddString(form, "output_format", track.OutputFormat);
                AddBool(form, "preserve_speech", track.PreserveSpeech);
                AddBool(form, "ducking", track.Ducking);
                break;

            case SoniloModes.AudioVideoToSfx:
                AddVideoSource(form, item.VideoSource);
                AddOptionalString(form, "prompt", item.Prompt);
                AddOptionalString(form, "segments", item.SegmentsJson);
                AddString(form, "audio_format", track.OutputFormat);
                break;

            case SoniloModes.AudioVideoToSound:
                AddVideoSource(form, item.VideoSource);
                AddOptionalString(form, "music_prompt", item.MusicPrompt);
                AddOptionalString(form, "sfx_prompt", item.SfxPrompt);
                AddOptionalString(form, "segments", item.SegmentsJson);
                AddBool(form, "preserve_speech", track.PreserveSpeech);
                AddBool(form, "ducking", track.Ducking);
                AddString(form, "output_format", track.OutputFormat);
                break;
        }

        return form;
    }

    private MultipartFormDataContent CreateVideoMultipart(VideoTrackPayload track, VideoItemPayload item)
    {
        var form = new MultipartFormDataContent();
        AddVideoSource(form, item.VideoSource);

        switch (track.Mode)
        {
            case SoniloModes.VideoMusic:
                AddOptionalString(form, "prompt", item.Prompt);
                AddOptionalString(form, "segments", item.SegmentsJson);
                AddBool(form, "preserve_speech", track.PreserveSpeech);
                AddBool(form, "ducking", track.Ducking);
                break;

            case SoniloModes.VideoSfx:
                AddOptionalString(form, "prompt", item.Prompt);
                AddOptionalString(form, "segments", item.SegmentsJson);
                break;

            case SoniloModes.VideoSound:
                AddOptionalString(form, "music_prompt", item.MusicPrompt);
                AddOptionalString(form, "sfx_prompt", item.SfxPrompt);
                AddOptionalString(form, "segments", item.SegmentsJson);
                AddBool(form, "preserve_speech", track.PreserveSpeech);
                AddBool(form, "keep_original_sound", track.KeepOriginalSound);
                AddBool(form, "ducking", track.Ducking);
                break;
        }

        return form;
    }

    private async Task<TaskResult> SubmitOrResumeTaskAsync(
        string endpoint,
        string existingTaskId,
        Action<string> persistTaskId,
        Func<MultipartFormDataContent> multipartFactory,
        Action<string>? progress,
        CancellationToken cancellationToken)
    {
        var taskId = existingTaskId?.Trim() ?? "";

        if (string.IsNullOrEmpty(taskId))
        {
            progress?.Invoke("Submitting to Sonilo");
            using var response = await SendWithRetryAsync(() =>
            {
                var request = CreateRequest(HttpMethod.Post, endpoint);
                request.Content = multipartFactory();
                return request;
            }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new TaskResult(false, false, "", null, await ReadErrorAsync(response, cancellationToken));
            }

            var json = await ParseJsonAsync(response, cancellationToken);
            taskId = GetString(json.RootElement, "task_id");
            if (string.IsNullOrWhiteSpace(taskId))
            {
                json.Dispose();
                return new TaskResult(false, false, "", null, "Sonilo did not return a task_id.");
            }

            persistTaskId(taskId);
            json.Dispose();
        }
        else
        {
            progress?.Invoke($"Resuming Sonilo task {ShortId(taskId)}");
        }

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var response = await SendWithRetryAsync(
                () => CreateRequest(HttpMethod.Get, $"tasks/{Uri.EscapeDataString(taskId)}"), cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var terminal = response.StatusCode == HttpStatusCode.NotFound;
                return new TaskResult(false, terminal, taskId, null, await ReadErrorAsync(response, cancellationToken));
            }

            var json = await ParseJsonAsync(response, cancellationToken);
            var status = GetString(json.RootElement, "status").ToLowerInvariant();
            progress?.Invoke(string.IsNullOrEmpty(status) ? "Sonilo processing" : $"Sonilo: {status}");

            if (status is "succeeded" or "completed")
            {
                return new TaskResult(true, true, taskId, json, "");
            }

            if (status is "failed" or "canceled" or "cancelled")
            {
                var error = ExtractTaskError(json.RootElement);
                json.Dispose();
                return new TaskResult(false, true, taskId, null,
                    string.IsNullOrWhiteSpace(error) ? $"Sonilo task ended with status '{status}'." : error);
            }

            json.Dispose();
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
        }
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 4;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = requestFactory();
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            var retryable = response.StatusCode == (HttpStatusCode)429 ||
                            response.StatusCode is HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;

            if (!retryable || attempt == maxAttempts)
            {
                return response;
            }

            var delay = GetRetryDelay(response, attempt);
            response.Dispose();
            await Task.Delay(delay, cancellationToken);
        }

        throw new InvalidOperationException("Sonilo request retry loop ended unexpectedly.");
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath)
    {
        var baseUrl = (_settings.Url ?? "https://api.sonilo.com/v1").TrimEnd('/');
        var request = new HttpRequestMessage(method, $"{baseUrl}/{relativePath.TrimStart('/')}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        request.Headers.UserAgent.ParseAdd("LyricVideoStudio-SoniloPlugin/1.0");
        return request;
    }

    private void AddVideoSource(MultipartFormDataContent form, string source)
    {
        if (Uri.TryCreate(source, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            AddString(form, "video_url", source);
            return;
        }

        var path = WorkspaceSettings.GetAbsolutePath(source, true);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            throw new FileNotFoundException($"Video source not found: {source}", path);
        }

        var stream = File.OpenRead(path);
        var file = new StreamContent(stream);
        file.Headers.ContentType = new MediaTypeHeaderValue(GetVideoContentType(path));
        form.Add(file, "video", Path.GetFileName(path));
    }

    private static void AddString(MultipartFormDataContent form, string name, string value)
    {
        form.Add(new StringContent(value ?? ""), name);
    }

    private static void AddOptionalString(MultipartFormDataContent form, string name, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            AddString(form, name, value.Trim());
        }
    }

    private static void AddBool(MultipartFormDataContent form, string name, bool value)
    {
        if (value)
        {
            AddString(form, name, "true");
        }
    }

    private static string GetVideoContentType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".webm" => "video/webm",
            ".m4v" => "video/x-m4v",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }

    private async Task<string> DownloadAsync(
        string url,
        string contentType,
        string destinationFolder,
        bool video,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destinationFolder);

        using var response = await SendWithRetryAsync(() => new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
        }

        var actualContentType = response.Content.Headers.ContentType?.MediaType ?? contentType;
        var extension = ExtensionFromContentType(actualContentType, video);
        var target = Path.Combine(destinationFolder, $"{Guid.NewGuid():N}{extension}");

        await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var output = File.Create(target))
        {
            await input.CopyToAsync(output, cancellationToken);
        }

        if (!File.Exists(target) || new FileInfo(target).Length == 0)
        {
            throw new IOException("Sonilo download completed without usable file data.");
        }

        return target;
    }

    private static (string Url, string ContentType) ExtractAudioResult(JsonElement root, AudioTrackPayload track)
    {
        if (track.Mode == SoniloModes.AudioVideoToSound)
        {
            return (GetString(root, "output_url"), GetString(root, "content_type"));
        }

        if (track.Mode is SoniloModes.AudioTextToSfx or SoniloModes.AudioVideoToSfx)
        {
            if (root.TryGetProperty("audio", out var audio) && audio.ValueKind == JsonValueKind.Object)
            {
                return (GetString(audio, "url"), GetString(audio, "content_type"));
            }
            return ("", "");
        }

        if (track.Ducking && TryGetFirstMedia(root, "ducked", out var ducked))
        {
            return ducked;
        }

        if (track.PreserveSpeech && TryGetFirstMedia(root, "mux", out var mux))
        {
            return mux;
        }

        return TryGetFirstMedia(root, "audio", out var audioResult) ? audioResult : ("", "");
    }

    private static (string Url, string ContentType) ExtractVideoResult(JsonElement root, string mode)
    {
        if (mode == SoniloModes.VideoSound)
        {
            return (GetString(root, "output_url"), "video/mp4");
        }

        if (root.TryGetProperty("video", out var video) && video.ValueKind == JsonValueKind.Object)
        {
            return (GetString(video, "url"), GetString(video, "content_type"));
        }

        if (TryGetFirstMedia(root, "videos", out var firstVideo))
        {
            return firstVideo;
        }

        return ("", "");
    }

    private static bool TryGetFirstMedia(JsonElement root, string propertyName, out (string Url, string ContentType) result)
    {
        result = ("", "");
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        JsonElement media;
        if (property.ValueKind == JsonValueKind.Array)
        {
            if (property.GetArrayLength() == 0)
            {
                return false;
            }
            media = property.EnumerateArray().First();
        }
        else if (property.ValueKind == JsonValueKind.Object)
        {
            media = property;
        }
        else
        {
            return false;
        }

        var url = GetString(media, "url");
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        result = (url, GetString(media, "content_type"));
        return true;
    }

    private static List<(string Key, string Value)> BuildParams(JsonElement root, string taskId)
    {
        var result = new List<(string Key, string Value)> { ("Sonilo task", taskId) };

        if (root.TryGetProperty("title", out var title))
        {
            var titleText = title.ValueKind == JsonValueKind.String
                ? title.GetString()
                : title.TryGetProperty("title", out var nested) ? nested.GetString() : null;

            if (!string.IsNullOrWhiteSpace(titleText))
            {
                result.Add(("Title", titleText!));
            }
        }

        return result;
    }

    private static string ExtensionFromContentType(string contentType, bool video)
    {
        if (video)
        {
            return ".mp4";
        }

        var normalized = (contentType ?? "").Split(';')[0].Trim().ToLowerInvariant();
        return normalized switch
        {
            "audio/wav" or "audio/x-wav" => ".wav",
            "audio/mpeg" => ".mp3",
            "audio/mp4" or "audio/x-m4a" => ".m4a",
            "audio/aac" => ".aac",
            "audio/flac" => ".flac",
            _ => ".wav"
        };
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        if (response.Headers.RetryAfter?.Delta is TimeSpan delta && delta > TimeSpan.Zero)
        {
            return delta;
        }

        if (response.Headers.RetryAfter?.Date is DateTimeOffset date)
        {
            var until = date - DateTimeOffset.UtcNow;
            if (until > TimeSpan.Zero)
            {
                return until;
            }
        }

        return TimeSpan.FromSeconds(Math.Pow(2, attempt));
    }

    private static async Task<JsonDocument> ParseJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = body;

        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            message = ExtractTaskError(root);
            if (string.IsNullOrWhiteSpace(message) && root.TryGetProperty("message", out var msg))
            {
                message = msg.GetString() ?? body;
            }
        }
        catch
        {
            // Keep raw response text.
        }

        var prefix = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Sonilo authentication failed",
            HttpStatusCode.PaymentRequired => "Sonilo balance/credits are insufficient",
            HttpStatusCode.Forbidden => "Sonilo service access is forbidden or disabled",
            HttpStatusCode.TooManyRequests => "Sonilo rate limit reached",
            _ => $"Sonilo HTTP {(int)response.StatusCode}"
        };

        return string.IsNullOrWhiteSpace(message) ? prefix : $"{prefix}: {message}";
    }

    private static string ExtractTaskError(JsonElement root)
    {
        if (!root.TryGetProperty("error", out var error))
        {
            return "";
        }

        if (error.ValueKind == JsonValueKind.String)
        {
            return error.GetString() ?? "";
        }

        if (error.ValueKind == JsonValueKind.Object)
        {
            var code = GetString(error, "code");
            var message = GetString(error, "message");
            if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(message))
            {
                return $"{code}: {message}";
            }
            return !string.IsNullOrWhiteSpace(message) ? message : error.ToString();
        }

        return error.ToString();
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return "";
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() ?? "" : property.ToString();
    }

    private static string ShortId(string id) => id.Length <= 8 ? id : id[..8];

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private sealed record TaskResult(bool Success, bool Terminal, string TaskId, JsonDocument? Json, string Error);
}
