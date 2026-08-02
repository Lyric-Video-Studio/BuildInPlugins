using PluginBase;
using System.ComponentModel;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace MinimaxPlugin
{
    public class Request
    {
        [IgnoreDynamicEdit]
        public string model { get; set; } = "MiniMax-Hailuo-02";

        public string prompt { get; set; } = "";
        public bool prompt_optimizer { get; set; } = true;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string first_frame_image { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [IgnoreDynamicEdit]
        public KeyFrame[] subject_reference { get; set; }

        [IgnoreDynamicEdit]
        public string resolution { get; set; } = "768P";
    }

    public class ImageRequest
    {
        [IgnoreDynamicEdit]
        public string model { get; set; } = "image-01";

        public string prompt { get; set; } = "";
        public bool prompt_optimizer { get; set; } = true;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [IgnoreDynamicEdit]
        public KeyFrameImage[] subject_reference { get; set; }

        public string aspect_ratio { get; set; } = "16:9";

        [IgnoreDynamicEdit]
        public string response_format { get; set; } = "base64";

        [Description("0 = use random seed")]
        public long seed { get; set; }

        [IgnoreDynamicEdit]
        public int n { get; set; } = 1;
    }

    public class Response
    {
        public string task_id { get; set; }
        public BaseResponse base_resp { get; set; }
    }

    public class MiniMaxImageResponse
    {
        public ImageData data { get; set; }
        public BaseResponse base_resp { get; set; }
    }

    public class ImageData
    {
        public string[] image_urls { get; set; }
        public string[] image_base64 { get; set; }
    }

    public class BaseResponse
    {
        public int status_code { get; set; }
        public string status_msg { get; set; }
    }

    public class VideoTaskResponse
    {
        public string task_id { get; set; }
        public string status { get; set; }
        public string file_id { get; set; }
        public BaseResponse base_resp { get; set; }
    }

    public class VideoFileResponse
    {
        public VideoFile file { get; set; }
        public BaseResponse base_resp { get; set; }
    }

    public class VideoFile
    {
        public ulong file_id { get; set; }
        public ulong bytes { get; set; }
        public ulong created_at { get; set; }
        public string filename { get; set; }
        public string purpose { get; set; }
        public string download_url { get; set; }
    }

    public class KeyFrame
    {
        [IgnoreDynamicEdit]
        public string type { get; set; } = "character";

        public string[] image { get; set; }
    }

    public class KeyFrameImage
    {
        [IgnoreDynamicEdit]
        public string type { get; set; } = "character";

        [EnableFileDrop]
        public string image_file { get; set; }
    }

    internal class Client
    {
        public async Task<VideoResponse> GetImgToVid(Request request, string folderToSave, ConnectionSettings connectionSettings,
            ItemPayload refItemPlayload, Action<bool> saveAndRefreshCallback, Action<string> textualProgressAction)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Remove("accept");

                // It's best to keep these here: use can change these from item settings
                httpClient.BaseAddress = new Uri(connectionSettings.Url);
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("authorization", $"Bearer {connectionSettings.AccessToken}");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("accept", "application/json");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("authority", "api.minimaxi.chat");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("content-type", "application/json");

                if (!string.IsNullOrEmpty(refItemPlayload.PollingId))
                {
                    return await PollVideoResults(httpClient, refItemPlayload.PollingId, folderToSave, textualProgressAction);
                }

                var serialized = "";

                if (request.model == "MiniMax-Hailuo-02")
                {
                    request.resolution = "1080P";
                }

                try
                {
                    serialized = JsonHelper.Serialize(request);
                    // Bit tricky, but remove empty value, "first_frame_image": "",
                    serialized = serialized.Replace("\"first_frame_image\": \"\",", "");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    return new VideoResponse() { ErrorMsg = $"Error: parsing request, details: {ex.Message}", Success = false };
                }

                var stringContent = new StringContent(serialized);
                stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var resp = await httpClient.PostAsync("v1/video_generation", stringContent);
                var respString = await resp.Content.ReadAsStringAsync();
                Response respSerialized = null;
                string? errMsg = null;
                try
                {
                    respSerialized = JsonHelper.DeserializeString<Response>(respString);
                    errMsg = respSerialized?.base_resp?.status_msg;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    return new VideoResponse() { ErrorMsg = $"Error parsing response, {ex.Message}", Success = false };
                }

                if (respSerialized != null && resp.IsSuccessStatusCode && respSerialized.base_resp.status_code == 0)
                {
                    refItemPlayload.PollingId = respSerialized.task_id.ToString();
                    saveAndRefreshCallback.Invoke(true);
                    return await PollVideoResults(httpClient, respSerialized.task_id, folderToSave, textualProgressAction);
                }
                else
                {
                    return new VideoResponse() { ErrorMsg = $"Error: {resp.StatusCode}, details: {(errMsg ?? respString)}", Success = false };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return new VideoResponse() { ErrorMsg = ex.Message, Success = false };
            }
        }

        public async Task<ImageResponse> GetImg(ImageRequest request, ConnectionSettings connectionSettings)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Remove("accept");

                // It's best to keep these here: use can change these from item settings
                httpClient.BaseAddress = new Uri(connectionSettings.Url);
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("authorization", $"Bearer {connectionSettings.AccessToken}");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("accept", "application/json");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("content-type", "application/json");

                var serialized = "";

                try
                {
                    serialized = JsonHelper.Serialize(request);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    return new ImageResponse() { ErrorMsg = $"Error: parsing request, details: {ex.Message}", Success = false };
                }

                var stringContent = new StringContent(serialized);
                stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var resp = await httpClient.PostAsync("/v1/image_generation", stringContent);
                var respString = await resp.Content.ReadAsStringAsync();
                MiniMaxImageResponse respSerialized = null;

                try
                {
                    respSerialized = JsonHelper.DeserializeString<MiniMaxImageResponse>(respString);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    return new ImageResponse() { ErrorMsg = $"Error parsing response, {ex.Message}", Success = false };
                }

                if (respSerialized != null && resp.IsSuccessStatusCode && respSerialized.base_resp.status_code == 0)
                {
                    return new ImageResponse() { Success = true, Image = respSerialized.data.image_base64.First(), ImageFormat = "jpg" };
                }
                else
                {
                    return new ImageResponse() { ErrorMsg = $"Error: {resp.StatusCode}, details: {respString}", Success = false };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return new ImageResponse() { ErrorMsg = ex.Message, Success = false };
            }
        }

        private static async Task<VideoResponse> PollVideoResults(HttpClient httpClient, string id, string folderToSave, Action<string> textualProgressAction)
        {
            var pollingDelay = TimeSpan.FromSeconds(7);

            var videoUrl = "";

            while (string.IsNullOrEmpty(videoUrl))
            {
                // Wait for assets to be filled
                try
                {
                    var generationResp = await httpClient.GetAsync($"v1/query/video_generation?task_id={id}");
                    var respString = await generationResp.Content.ReadAsStringAsync();
                    VideoTaskResponse respSerialized = null;

                    try
                    {
                        respSerialized = JsonHelper.DeserializeString<VideoTaskResponse>(respString);

                        if (respSerialized.status == "Fail")
                        {
                            return new VideoResponse() { Success = false, ErrorMsg = $"Minimax reported that video generating failed: {respSerialized.base_resp.status_msg}" };
                        }

                        videoUrl = respSerialized?.file_id;

                        System.Diagnostics.Debug.WriteLine($"State: {respSerialized.status}");
                        textualProgressAction.Invoke(respSerialized.status);

                        if (string.IsNullOrEmpty(respSerialized?.file_id))
                        {
                            await Task.Delay(pollingDelay);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }

            // Store video request token to disk, in case connection is broken or something

            VideoFileResponse fileResp = null;

            while (string.IsNullOrEmpty(fileResp?.file?.download_url))
            {
                // Wait for assets to be filled
                try
                {
                    var generationResp = await httpClient.GetAsync($"v1/files/retrieve?file_id={videoUrl}");
                    var respString = await generationResp.Content.ReadAsStringAsync();

                    try
                    {
                        fileResp = JsonHelper.DeserializeString<VideoFileResponse>(respString);

                        if (fileResp?.base_resp?.status_code != 0)
                        {
                            return new VideoResponse() { Success = false, ErrorMsg = $"Minimax reported that video generating failed: {fileResp?.base_resp?.status_msg}" };
                        }

                        videoUrl = fileResp?.file?.download_url;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }

            var file = Path.GetFileName(videoUrl);

            using var downloadClient = new HttpClient { BaseAddress = new Uri(videoUrl.Replace(file, "")), Timeout = Timeout.InfiniteTimeSpan };

            downloadClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "video/*");

            // Store video request token to disk, in case connection is broken or something
            textualProgressAction.Invoke("Downloading");
            var videoResp = await downloadClient.GetAsync(file);

            while (videoResp.StatusCode != HttpStatusCode.OK)
            {
                await Task.Delay(pollingDelay);
                videoResp = await downloadClient.GetAsync(file);
            }

            if (videoResp.StatusCode == HttpStatusCode.OK)
            {
                var respBytes = await videoResp.Content.ReadAsByteArrayAsync();
                var pathToVideo = Path.Combine(folderToSave, $"{id}.mp4");
                await File.WriteAllBytesAsync(pathToVideo, respBytes);
                return new VideoResponse() { Success = true, VideoFile = pathToVideo };
            }
            else
            {
                return new VideoResponse() { ErrorMsg = $"Error: {videoResp.StatusCode}, details: {await videoResp.Content.ReadAsStringAsync()}", Success = false };
            }
        }
            public async Task<VideoResponse> GetH3Video(H3Request request, string folder, ConnectionSettings settings, ItemPayload item, Action<bool> save, Action<string> progress)
        {
            try
            {
                using var client = new HttpClient { BaseAddress = new Uri(settings.Url.Replace("minimaxi.chat", "minimax.io")) };
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {settings.AccessToken}");
                if (!string.IsNullOrEmpty(item.PollingId)) return await PollH3(client, item.PollingId, folder, progress);
                var body = new StringContent(JsonHelper.Serialize(request)); body.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                var response = await client.PostAsync("v2/video_generation", body); var text = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode) return new VideoResponse { Success = false, ErrorMsg = $"Error: {response.StatusCode}, details: {text}" };
                var created = JsonHelper.DeserializeString<H3CreateResponse>(text);
                if (string.IsNullOrEmpty(created?.task_id)) return new VideoResponse { Success = false, ErrorMsg = $"Invalid H3 response: {text}" };
                item.PollingId = created.task_id; save?.Invoke(true); return await PollH3(client, created.task_id, folder, progress);
            }
            catch (Exception ex) { return new VideoResponse { Success = false, ErrorMsg = ex.Message }; }
        }
        private static async Task<VideoResponse> PollH3(HttpClient client, string id, string folder, Action<string> progress)
        {
            while (true)
            {
                var response = await client.GetAsync($"v2/query/video_generation/{Uri.EscapeDataString(id)}"); var text = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode) return new VideoResponse { Success = false, ErrorMsg = $"H3 query failed: {response.StatusCode}, {text}" };
                var task = JsonHelper.DeserializeString<H3QueryResponse>(text)?.task;
                if (task == null) return new VideoResponse { Success = false, ErrorMsg = $"Invalid H3 query response: {text}" };
                progress?.Invoke(task.status);
                if (task.status == "failed" || task.status == "cancelled" || task.status == "expired") return new VideoResponse { Success = false, ErrorMsg = task.error?.message ?? $"H3 task {task.status}" };
                if (task.status == "succeeded" && !string.IsNullOrEmpty(task.content?.url)) { using var download = new HttpClient(); var output = Path.Combine(folder, $"{id}.mp4"); await File.WriteAllBytesAsync(output, await download.GetByteArrayAsync(task.content.url)); return new VideoResponse { Success = true, VideoFile = output }; }
                await Task.Delay(TimeSpan.FromSeconds(7));
            }
        }}
}