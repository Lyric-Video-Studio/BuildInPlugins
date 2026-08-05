using PluginBase;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace BflTxtToImgPlugin
{
    /// <summary>
    /// FLUX 3 Video uses BFL's asynchronous video endpoint. It is kept as a
    /// separate plugin entry because the existing BFL plugin has persisted
    /// image payload types and presets.
    /// </summary>
    public class BflFlux3VideoPlugin
    {
        private const string Flux3VideoEndpoint = "https://api.bfl.ai/v1/flux-3-video";
        private ConnectionSettings connectionSettings = new();
        private CancellationToken cancellationToken;
        private Action<bool> saveAndRefreshCallback;
        private Action<string> showCostAction;
        private Action<string> progressCallback;

        public IPluginBase.TrackType CurrentTrackType { get; set; }

        public object DefaultPayloadForVideoItem() => new VideoItemPayload();
        public object DefaultPayloadForVideoTrack() => new VideoTrackPayload();

        public async Task<VideoResponse> GetVideo(object trackPayload, object itemsPayload, string folderToSaveVideo)
        {
            if (string.IsNullOrWhiteSpace(connectionSettings.AccessToken))
            {
                return new VideoResponse { Success = false, ErrorMsg = "Auth token missing" };
            }

            if (trackPayload is not VideoTrackPayload track || itemsPayload is not VideoItemPayload item)
            {
                return new VideoResponse { Success = false, ErrorMsg = "Track payload or item payload object not valid" };
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(item.PollingUrl))
                {
                    return await PollVideoAsync(item.PollingUrl, folderToSaveVideo);
                }

                var request = CreateRequest(track, item);
                var submitted = await SubmitVideoAsync(request);
                item.PollingUrl = submitted.PollingUrl;
                saveAndRefreshCallback?.Invoke(true);

                if (submitted.Cost is > 0)
                {
                    showCostAction?.Invoke((submitted.Cost.Value / 100).ToString("0.00") + "€");
                }

                return await PollVideoAsync(submitted.PollingUrl, folderToSaveVideo);
            }
            catch (OperationCanceledException)
            {
                return new VideoResponse { Success = false, ErrorMsg = "Cancelled by user" };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return new VideoResponse { Success = false, ErrorMsg = ex.Message };
            }
        }

        private static Dictionary<string, object> CreateRequest(VideoTrackPayload track, VideoItemPayload item)
        {
            var prompt = $"{item.Prompt} {track.Prompt}".Trim();
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new InvalidOperationException("Prompt missing");
            }

            var request = new Dictionary<string, object>
            {
                ["prompt"] = prompt,
                ["aspect_ratio"] = track.AspectRatio,
                ["duration"] = track.Duration == "auto" ? "auto" : int.Parse(track.Duration),
                ["resolution"] = track.Resolution,
                ["generate_audio"] = track.GenerateAudio,
                ["safety_tolerance"] = track.SafetyTolerance,
                ["draft"] = track.Draft,
            };

            switch (track.Mode)
            {
                case VideoTrackPayload.ModeTextToVideo:
                    request["mode"] = "t2v";
                    break;

                case VideoTrackPayload.ModeImageToVideo:
                    var images = GetImagePaths(item);
                    if (images.Count == 0)
                    {
                        throw new InvalidOperationException("At least one input image is required for Image to Video");
                    }
                    request["mode"] = "i2v";
                    request["keyframes"] = images.Select(path => Convert.ToBase64String(File.ReadAllBytes(path))).ToArray();
                    break;

                case VideoTrackPayload.ModeVideoContinuation:
                    var inputVideo = GetExistingPath(item.InputVideo);
                    request["mode"] = "v2v";
                    request["start_video"] = Convert.ToBase64String(File.ReadAllBytes(inputVideo));
                    break;

                default:
                    throw new InvalidOperationException("Unsupported FLUX 3 video mode");
            }

            return request;
        }

        private async Task<Flux3SubmitResponse> SubmitVideoAsync(Dictionary<string, object> request)
        {
            using var client = CreateAuthorizedClient();
            using var message = new HttpRequestMessage(HttpMethod.Post, Flux3VideoEndpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(request))
            };
            message.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            using var response = await client.SendAsync(message, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"BFL FLUX 3 request failed ({(int)response.StatusCode}): {GetApiError(responseText)}");
            }

            var result = JsonSerializer.Deserialize<Flux3SubmitResponse>(responseText);
            if (result == null || string.IsNullOrWhiteSpace(result.PollingUrl))
            {
                throw new InvalidOperationException("BFL FLUX 3 request did not return a polling URL");
            }

            return result;
        }

        private async Task<VideoResponse> PollVideoAsync(string pollingUrl, string folderToSaveVideo)
        {
            using var client = CreateAuthorizedClient();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var response = await client.GetAsync(pollingUrl, cancellationToken);
                var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return new VideoResponse { Success = false, ErrorMsg = $"BFL polling failed ({(int)response.StatusCode}): {GetApiError(responseText)}" };
                }

                using var document = JsonDocument.Parse(responseText);
                var root = document.RootElement;
                var status = root.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : null;
                progressCallback?.Invoke(FormatProgress(status, root));

                if (string.Equals(status, "Ready", StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryGetVideoUrl(root, out var videoUrl))
                    {
                        return new VideoResponse { Success = false, ErrorMsg = "BFL FLUX 3 result did not include a video URL" };
                    }

                    Directory.CreateDirectory(folderToSaveVideo);
                    var target = Path.Combine(folderToSaveVideo, $"{Guid.NewGuid()}.mp4");
                    using var downloadClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
                    await File.WriteAllBytesAsync(target, await downloadClient.GetByteArrayAsync(videoUrl, cancellationToken), cancellationToken);
                    return new VideoResponse { Success = true, VideoFile = target, Fps = 24 };
                }

                if (status is "Error" or "Failed" or "Task not found" or "Request Moderated" or "Content Moderated")
                {
                    return new VideoResponse { Success = false, ErrorMsg = GetApiError(responseText) };
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        private HttpClient CreateAuthorizedClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            client.DefaultRequestHeaders.Add("x-key", connectionSettings.AccessToken);
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            return client;
        }

        private static bool TryGetVideoUrl(JsonElement root, out string videoUrl)
        {
            videoUrl = null;
            if (!root.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (var propertyName in new[] { "sample", "video", "url" })
            {
                if (result.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    videoUrl = value.GetString();
                    return !string.IsNullOrWhiteSpace(videoUrl);
                }
            }

            return false;
        }

        private static string FormatProgress(string status, JsonElement root)
        {
            if (root.TryGetProperty("progress", out var progress) && progress.ValueKind == JsonValueKind.Number && progress.TryGetDouble(out var value))
            {
                return $"{status} ({value:P0})";
            }
            return status ?? "Polling";
        }

        private static string GetApiError(string json)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;
                if (root.TryGetProperty("detail", out var detail))
                {
                    return detail.ValueKind == JsonValueKind.String ? detail.GetString() : detail.ToString();
                }
                if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                {
                    return message.GetString();
                }
            }
            catch (JsonException)
            {
            }
            return json;
        }

        private static List<string> GetImagePaths(VideoItemPayload item)
        {
            return new[] { item.InputImage, item.InputImage2, item.InputImage3, item.InputImage4, item.InputImage5, item.InputImage6, item.InputImage7, item.InputImage8, item.InputImage9, item.InputImage10 }
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(GetExistingPath)
                .ToList();
        }

        private static string GetExistingPath(string path)
        {
            var absolutePath = WorkspaceSettings.GetAbsolutePath(path);
            if (string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath))
            {
                throw new FileNotFoundException("Input file missing", path);
            }
            return absolutePath;
        }

        public async Task<string> Initialize(object settings)
        {
            if (JsonHelper.DeepCopy<ConnectionSettings>(settings) is not ConnectionSettings parsed)
            {
                return "Connection settings object not valid";
            }

            connectionSettings = parsed;
            isInitialized = !string.IsNullOrWhiteSpace(parsed.AccessToken);
            return "";
        }

        public void CloseConnection() { }
        public Task<string[]> SelectionOptionsForProperty(string propertyName) => Task.FromResult(Array.Empty<string>());
        public object DeserializePayload(string fileName) => JsonHelper.Deserialize<VideoTrackPayload>(fileName);
        public object ObjectToItemPayload(JsonObject obj) => JsonHelper.ToExactType<VideoItemPayload>(obj);
        public object ObjectToTrackPayload(JsonObject obj) => JsonHelper.ToExactType<VideoTrackPayload>(obj);
        public object ObjectToGeneralSettings(JsonObject obj) => JsonHelper.ToExactType<ConnectionSettings>(obj);
        public string TextualRepresentation(object itemPayload) => itemPayload is VideoItemPayload item ? item.Prompt : "";
        public object DefaultPayloadForTrack() => CurrentTrackType == IPluginBase.TrackType.Video ? DefaultPayloadForVideoTrack() : throw new NotImplementedException();
        public object DefaultPayloadForItem() => CurrentTrackType == IPluginBase.TrackType.Video ? DefaultPayloadForVideoItem() : throw new NotImplementedException();
        public object CopyPayloadForTrack(object obj) => CurrentTrackType == IPluginBase.TrackType.Video ? JsonHelper.DeepCopy<VideoTrackPayload>(obj) : throw new NotImplementedException();
        public object CopyPayloadForItem(object obj) => CurrentTrackType == IPluginBase.TrackType.Video ? JsonHelper.DeepCopy<VideoItemPayload>(obj) : throw new NotImplementedException();
        public object ItemPayloadFromLyrics(string lyric) => CurrentTrackType == IPluginBase.TrackType.Video ? new VideoItemPayload { Prompt = lyric } : null;
        public object ItemPayloadFromImageSource(string imageSource) => CurrentTrackType == IPluginBase.TrackType.Video ? new VideoItemPayload { InputImage = imageSource } : null;
        public void AppendToPayloadFromLyrics(string text, object payload) { if (payload is VideoItemPayload item) item.Prompt = text; }
        public Task<string> TestInitialization() => Task.FromResult("");
        public void SetCancallationToken(CancellationToken token) => cancellationToken = token;
        public void SetSaveAndRefreshCallback(Action<bool> callback) => saveAndRefreshCallback = callback;
        public void SetShowCostAction(Action<string> action) => showCostAction = action;
        public void SetTextProgressCallback(Action<string> action) => progressCallback = action;

        public (bool payloadOk, string reasonIfNot) ValidatePayload(object payload)
        {
            return string.IsNullOrWhiteSpace(connectionSettings.AccessToken) ? (false, "Auth token missing") : (true, "");
        }

        public (bool payloadOk, string reasonIfNot) ValidatePayloads(object trackPayload, object itemPayload)
        {
            if (ValidatePayload(itemPayload) is var auth && !auth.payloadOk)
            {
                return auth;
            }
            if (trackPayload is not VideoTrackPayload track || itemPayload is not VideoItemPayload item)
            {
                return (false, "Track payload or item payload object not valid");
            }
            if (string.IsNullOrWhiteSpace($"{track.Prompt} {item.Prompt}"))
            {
                return (false, "Prompt missing");
            }
            try
            {
                if (track.Mode == VideoTrackPayload.ModeImageToVideo && GetImagePaths(item).Count == 0)
                {
                    return (false, "At least one input image is required for Image to Video");
                }
                if (track.Mode == VideoTrackPayload.ModeVideoContinuation)
                {
                    GetExistingPath(item.InputVideo);
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
            return (true, "");
        }

        public List<string> FilePathsOnPayloads(object trackPayload, object itemPayload)
        {
            if (itemPayload is not VideoItemPayload item) return new();
            return new[] { item.InputImage, item.InputImage2, item.InputImage3, item.InputImage4, item.InputImage5, item.InputImage6, item.InputImage7, item.InputImage8, item.InputImage9, item.InputImage10, item.InputVideo }
                .Where(path => !string.IsNullOrWhiteSpace(path)).ToList();
        }

        public void ReplaceFilePathsOnPayloads(List<string> originalPaths, List<string> newPaths, object trackPayload, object itemPayload)
        {
            if (itemPayload is not VideoItemPayload item) return;
            var paths = new[] { item.InputImage, item.InputImage2, item.InputImage3, item.InputImage4, item.InputImage5, item.InputImage6, item.InputImage7, item.InputImage8, item.InputImage9, item.InputImage10, item.InputVideo };
            for (var index = 0; index < originalPaths.Count && index < newPaths.Count; index++)
            {
                for (var pathIndex = 0; pathIndex < paths.Length; pathIndex++)
                {
                    if (paths[pathIndex] == originalPaths[index]) paths[pathIndex] = newPaths[index];
                }
            }
            item.InputImage = paths[0]; item.InputImage2 = paths[1]; item.InputImage3 = paths[2]; item.InputImage4 = paths[3]; item.InputImage5 = paths[4];
            item.InputImage6 = paths[5]; item.InputImage7 = paths[6]; item.InputImage8 = paths[7]; item.InputImage9 = paths[8]; item.InputImage10 = paths[9]; item.InputVideo = paths[10];
        }

        public void UserDataDeleteRequested() => connectionSettings?.DeleteTokens();

        private sealed class Flux3SubmitResponse
        {
            [JsonPropertyName("polling_url")] public string PollingUrl { get; set; }
            [JsonPropertyName("cost")] public double? Cost { get; set; }
        }
    }
}
