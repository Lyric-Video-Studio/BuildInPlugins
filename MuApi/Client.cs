using MuApiPlugin.Models.Seedance2;
using PluginBase;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace MuApiPlugin
{
    public class GenerationRequest
    {
        public string prompt { get; set; }
        public string aspect_ratio { get; set; }
        public string quality { get; set; }
        public int duration { get; set; }
        public List<string> images_list { get; set; }
        public List<string> audio_files { get; set; }
        public List<string>  video_files { get; set; }
    }

    public class ImageGenerationRequest
    {
        public string prompt { get; set; }
        public List<string> images_list { get; set; }
    }

    internal class Client
    {
        private static readonly TimeSpan PollingDelay = TimeSpan.FromSeconds(10);

        public async Task<string> UploadFile(string path, ConnectionSettings connectionSettings, CancellationToken cancellationToken)
        {
            var absolutePath = WorkspaceSettings.GetAbsolutePath(path);
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
            {
                throw new FileNotFoundException($"Reference file not found: {path}");
            }

            using var httpClient = CreateApiClient(connectionSettings);
            using var multipart = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(absolutePath);
            using var streamContent = new StreamContent(fileStream);

            var mimeType = CommonConstants.GetMimeType(Path.GetExtension(absolutePath));
            if (!string.IsNullOrEmpty(mimeType))
            {
                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType);
            }

            multipart.Add(streamContent, "file", Path.GetFileName(absolutePath));

            var response = await httpClient.PostAsync("upload_file", multipart, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Upload failed: {response.StatusCode}, details: {GetErrorMessage(payload)}");
            }

            var node = JsonNode.Parse(payload);
            var uploadedUrl = GetString(node, "url") ?? GetString(node, "data", "url");

            if (string.IsNullOrEmpty(uploadedUrl))
            {
                throw new Exception("Upload succeeded but MuApi did not return a file URL.");
            }

            return uploadedUrl;
        }

        public async Task<VideoResponse> GetVideo(GenerationRequest request, string endpointPath, string folderToSave,
            ConnectionSettings connectionSettings, IMuApiPollingPayload originalItemPayload, Action<bool> saveAndRefreshCallback,
            Action<string> textualProgressAction, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var httpClient = CreateApiClient(connectionSettings);

                if (!string.IsNullOrWhiteSpace(originalItemPayload?.PollingId))
                {
                    return await PollVideoResult(httpClient, originalItemPayload.PollingId, folderToSave, originalItemPayload, saveAndRefreshCallback, textualProgressAction, cancellationToken);
                }

                var serialized = JsonHelper.Serialize(request);
                using var stringContent = new StringContent(serialized);
                stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var response = await httpClient.PostAsync(endpointPath.TrimStart('/'), stringContent, cancellationToken);
                var payload = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return new VideoResponse() { Success = false, ErrorMsg = $"Error: {response.StatusCode}, details: {GetErrorMessage(payload)}" };
                }

                var node = JsonNode.Parse(payload);
                var requestId = GetString(node, "data", "request_id") ?? GetString(node, "request_id");

                if (string.IsNullOrEmpty(requestId))
                {
                    return new VideoResponse() { Success = false, ErrorMsg = "MuApi response did not contain a request_id." };
                }

                if (originalItemPayload != null)
                {
                    originalItemPayload.PollingId = requestId;
                    saveAndRefreshCallback?.Invoke(true);
                }

                return await PollVideoResult(httpClient, requestId, folderToSave, originalItemPayload, saveAndRefreshCallback, textualProgressAction, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return new VideoResponse() { Success = false, ErrorMsg = "User cancelled" };
            }
            catch (Exception ex)
            {
                return new VideoResponse() { Success = false, ErrorMsg = ex.Message };
            }
        }

        public async Task<ImageResponse> GetImage(ImageGenerationRequest request, string endpointPath,
            ConnectionSettings connectionSettings, IMuApiPollingPayload originalItemPayload, Action<bool> saveAndRefreshCallback,
            Action<string> textualProgressAction, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var httpClient = CreateApiClient(connectionSettings);

                if (!string.IsNullOrWhiteSpace(originalItemPayload?.PollingId))
                {
                    return await PollImageResult(httpClient, originalItemPayload.PollingId, originalItemPayload, saveAndRefreshCallback, textualProgressAction, cancellationToken);
                }

                var serialized = JsonHelper.Serialize(request);
                using var stringContent = new StringContent(serialized);
                stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var response = await httpClient.PostAsync(endpointPath.TrimStart('/'), stringContent, cancellationToken);
                var payload = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return new ImageResponse() { Success = false, ErrorMsg = $"Error: {response.StatusCode}, details: {GetErrorMessage(payload)}" };
                }

                var node = JsonNode.Parse(payload);
                var requestId = GetString(node, "data", "request_id") ?? GetString(node, "request_id");

                if (string.IsNullOrEmpty(requestId))
                {
                    return new ImageResponse() { Success = false, ErrorMsg = "MuApi response did not contain a request_id." };
                }

                if (originalItemPayload != null)
                {
                    originalItemPayload.PollingId = requestId;
                    saveAndRefreshCallback?.Invoke(true);
                }

                return await PollImageResult(httpClient, requestId, originalItemPayload, saveAndRefreshCallback, textualProgressAction, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return new ImageResponse() { Success = false, ErrorMsg = "User cancelled" };
            }
            catch (Exception ex)
            {
                return new ImageResponse() { Success = false, ErrorMsg = ex.Message };
            }
        }

        private static async Task<VideoResponse> PollVideoResult(HttpClient httpClient, string requestId, string folderToSave,
            IMuApiPollingPayload originalItemPayload, Action<bool> saveAndRefreshCallback, Action<string> textualProgressAction, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var response = await httpClient.GetAsync($"predictions/{requestId}/result", cancellationToken);
                var payload = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return new VideoResponse() { Success = false, ErrorMsg = $"Polling failed: {response.StatusCode}, details: {GetErrorMessage(payload)}" };
                }

                var node = JsonNode.Parse(payload);
                var status = (GetString(node, "data", "status") ?? GetString(node, "status") ?? "").ToLowerInvariant();

                if (status is "completed" or "succeeded" or "success")
                {
                    var arrRes = node["outputs"] as JsonArray;
                    var videoUrl = arrRes[0].ToString();

                    if (string.IsNullOrEmpty(videoUrl))
                    {
                        return new VideoResponse() { Success = false, ErrorMsg = "MuApi completed the request, but no video URL was returned." };
                    }

                    textualProgressAction?.Invoke("Downloading");
                    var outputPath = Path.Combine(folderToSave, $"{requestId}.mp4");
                    var downloadBytes = await DownloadBytes(videoUrl, cancellationToken);
                    await File.WriteAllBytesAsync(outputPath, downloadBytes, cancellationToken);

                    textualProgressAction?.Invoke("");
                    return new VideoResponse() { Success = true, VideoFile = outputPath };
                }

                if (status is "failed" or "error" or "cancelled" or "canceled")
                {
                    return new VideoResponse() { Success = false, ErrorMsg = GetErrorMessage(payload) };
                }

                textualProgressAction?.Invoke(string.IsNullOrEmpty(status) ? "Processing" : status);
                await Task.Delay(PollingDelay, cancellationToken);
            }
        }

        private static async Task<ImageResponse> PollImageResult(HttpClient httpClient, string requestId,
            IMuApiPollingPayload originalItemPayload, Action<bool> saveAndRefreshCallback, Action<string> textualProgressAction, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var response = await httpClient.GetAsync($"predictions/{requestId}/result", cancellationToken);
                var payload = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return new ImageResponse() { Success = false, ErrorMsg = $"Polling failed: {response.StatusCode}, details: {GetErrorMessage(payload)}" };
                }

                var node = JsonNode.Parse(payload);
                var status = (GetString(node, "data", "status") ?? GetString(node, "status") ?? "").ToLowerInvariant();

                if (status is "completed" or "succeeded" or "success")
                {
                    var arrRes = node["outputs"] as JsonArray;
                    var imageUrl = arrRes?[0]?.ToString();

                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        return new ImageResponse() { Success = false, ErrorMsg = "MuApi completed the request, but no image URL was returned." };
                    }

                    textualProgressAction?.Invoke("Downloading");
                    var downloadBytes = await DownloadBytes(imageUrl, cancellationToken);
                    var format = GetImageFormat(imageUrl);

                    if (originalItemPayload != null)
                    {
                        originalItemPayload.PollingId = "";
                        saveAndRefreshCallback?.Invoke(true);
                    }

                    textualProgressAction?.Invoke("");
                    return new ImageResponse() { Success = true, Image = Convert.ToBase64String(downloadBytes), ImageFormat = format };
                }

                if (status is "failed" or "error" or "cancelled" or "canceled")
                {
                    return new ImageResponse() { Success = false, ErrorMsg = GetErrorMessage(payload) };
                }

                textualProgressAction?.Invoke(string.IsNullOrEmpty(status) ? "Processing" : status);
                await Task.Delay(PollingDelay, cancellationToken);
            }
        }

        private static HttpClient CreateApiClient(ConnectionSettings connectionSettings)
        {
            var httpClient = new HttpClient() { Timeout = Timeout.InfiniteTimeSpan };
            httpClient.BaseAddress = new Uri(connectionSettings.Url);
            httpClient.DefaultRequestHeaders.Remove("accept");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", connectionSettings.AccessToken);
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("accept", "application/json");
            return httpClient;
        }

        private static async Task<byte[]> DownloadBytes(string url, CancellationToken cancellationToken)
        {
            using var downloadClient = new HttpClient() { Timeout = Timeout.InfiniteTimeSpan };
            downloadClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            
            var response = await downloadClient.GetByteArrayAsync(url, cancellationToken);
            
            return response;
        }

        private static string GetErrorMessage(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return "Unknown MuApi error";
            }

            try
            {
                var node = JsonNode.Parse(payload);
                return GetString(node, "error") ??
                    GetString(node, "message") ??
                    GetString(node, "detail") ??
                    GetString(node, "data", "error") ??
                    payload;
            }
            catch
            {
                return payload;
            }
        }

        private static string GetString(JsonNode node, params string[] path)
        {
            JsonNode current = node;
            foreach (var segment in path)
            {
                if (current == null)
                {
                    return null;
                }

                current = current[segment];
            }

            return current?.GetValue<string>();
        }

        private static string GetImageFormat(string url)
        {
            var extension = Path.GetExtension(url)?.TrimStart('.').ToLowerInvariant();
            return extension switch
            {
                "png" => "png",
                "webp" => "png",
                _ => "jpg"
            };
        }
    }
}
