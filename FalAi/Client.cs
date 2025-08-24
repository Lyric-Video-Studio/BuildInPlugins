using PluginBase;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace FalAiPlugin
{
    public class ContentResponse
    {
        public string prompt { get; set; }
        public List<ImageItem> images { get; set; }
        public VideoResp video { get; set; }

        // Sigh, would be nice if all responses would be the same type...
        public VideoResp image { get; set; }
    }

    public class VideoResp
    {
        public string url { get; set; }
    }

    public class ImageItem
    {
        public string content_type { get; set; }
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public class VideoRequest : Request
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool generate_audio { get; set; } = true;

        public string resolution { get; set; }
        public string aspect_ratio { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string image_url { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? num_frames { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? frames_per_second { get; set; }
    }

    public class Request
    {
        public string prompt { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault)]
        public string negative_prompt { get; set; } = "";

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string image_size { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string aspect_ratio { get; set; }

        public int seed { get; set; }
    }

    public class RequestResponse
    {
        public string status { get; set; }
        public string request_id { get; set; }
    }

    /*
         *
         * {"status": "IN_QUEUE",
         * "request_id": "9c51c99e-33e0-4490-8e85-709fe0535f70",
         * "response_url": "https://queue.fal.run/fal-ai/qwen-image/requests/9c51c99e-33e0-4490-8e85-709fe0535f70",
         * "status_url": "https://queue.fal.run/fal-ai/qwen-image/requests/9c51c99e-33e0-4490-8e85-709fe0535f70/status",
         * "cancel_url": "https://queue.fal.run/fal-ai/qwen-image/requests/9c51c99e-33e0-4490-8e85-709fe0535f70/cancel",
         * "logs": null,
         * "metrics": {}, "
         * queue_position": 0}
         */

    internal class Client
    {
        public async Task<VideoResponse> GetVideo(VideoRequest request, string folderToSave, ConnectionSettings connectionSettings,
            ItemPayload refItemPlayload, Action saveAndRefreshCallback, Action<string> textualProgressAction, string model)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Remove("accept");

                // It's best to keep these here: use can change these from item settings
                httpClient.BaseAddress = new Uri(connectionSettings.Url);
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("authorization", $"Key {connectionSettings.AccessToken}");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("accept", "application/json");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("content-type", "application/json");

                if (!string.IsNullOrEmpty(refItemPlayload.PollingId))
                {
                    return await PollVideoResults(httpClient, refItemPlayload.PollingId, folderToSave, textualProgressAction, model);
                }

                var serialized = "";

                try
                {
                    serialized = JsonHelper.Serialize(request);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    return new VideoResponse() { ErrorMsg = $"Error: parsing request, details: {ex.Message}", Success = false };
                }

                var stringContent = new StringContent(serialized);
                stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                var resp = await httpClient.PostAsync(model, stringContent);
                var respString = await resp.Content.ReadAsStringAsync();

                if (resp.StatusCode != HttpStatusCode.OK)
                {
                    return new VideoResponse() { ErrorMsg = $"Error: {resp.StatusCode}, details: {respString}", Success = false };
                }

                RequestResponse respSerialized = null;

                try
                {
                    respSerialized = JsonHelper.DeserializeString<RequestResponse>(respString);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    return new VideoResponse() { ErrorMsg = $"Error parsing response, {ex.Message}", Success = false };
                }

                if (respSerialized != null && resp.IsSuccessStatusCode)
                {
                    refItemPlayload.PollingId = respSerialized.request_id.ToString();
                    saveAndRefreshCallback.Invoke();
                    return await PollVideoResults(httpClient, respSerialized.request_id, folderToSave, textualProgressAction, model);
                }
                else
                {
                    return new VideoResponse() { ErrorMsg = $"Error: {resp.StatusCode}, details: {respString}", Success = false };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return new VideoResponse() { ErrorMsg = ex.Message, Success = false };
            }
        }

        // Fail response: {"detail": "User is locked. Reason: Exhausted balance. Top up your balance at fal.ai/dashboard/billing."}

        public async Task<ImageResponse> GetImage(Request request, ConnectionSettings connectionSettings,
            ImageItemPayload refItemPlayload, Action saveAndRefreshCallback, Action<string> textualProgressAction, string model)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Remove("accept");

                // It's best to keep these here: use can change these from item settings
                httpClient.BaseAddress = new Uri(connectionSettings.Url);
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("authorization", $"Key {connectionSettings.AccessToken}");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("accept", "application/json");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("content-type", "application/json");

                if (!string.IsNullOrEmpty(refItemPlayload.PollingId))
                {
                    return await PollImageResults(httpClient, refItemPlayload.PollingId, "", textualProgressAction, model);
                }

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

                var resp = await httpClient.PostAsync(model, stringContent);
                var respString = await resp.Content.ReadAsStringAsync();

                if (resp.StatusCode != HttpStatusCode.OK)
                {
                    return new ImageResponse() { ErrorMsg = $"Error: {resp.StatusCode}, details: {respString}", Success = false };
                }

                RequestResponse respSerialized = null;

                try
                {
                    respSerialized = JsonHelper.DeserializeString<RequestResponse>(respString);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    return new ImageResponse() { ErrorMsg = $"Error parsing response, {ex.Message}", Success = false };
                }

                if (respSerialized != null && resp.IsSuccessStatusCode)
                {
                    refItemPlayload.PollingId = respSerialized.request_id.ToString();
                    saveAndRefreshCallback.Invoke();

                    return await PollImageResults(httpClient, respSerialized.request_id, "", textualProgressAction, model);
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

        private static async Task<ImageResponse> PollImageResults(HttpClient httpClient, string id, string folderToSave,
            Action<string> textualProgressAction, string model)
        {
            var resp = await PollVideoResults(httpClient, id, folderToSave, textualProgressAction, model, true);
            return new ImageResponse() { Success = resp.Success, ErrorMsg = resp.ErrorMsg, Image = resp.VideoFile, ImageFormat = "png" };
        }

        private static async Task<VideoResponse> PollVideoResults(HttpClient httpClient, string id, string folderToSave,
            Action<string> textualProgressAction, string model, bool isActuallyImage = false)
        {
            var pollingDelay = TimeSpan.FromSeconds(20);

            var videoUrl = "";

            while (string.IsNullOrEmpty(videoUrl))
            {
                // Wait for assets to be filled
                try
                {
                    if (model.Contains('/'))
                    {
                        model = model.Split('/')[0];
                    }
                    var generationResp = await httpClient.GetAsync($"{model}/requests/{id}");
                    var respString = await generationResp.Content.ReadAsStringAsync();

                    if (respString.Contains("still in progress"))
                    {
                        await Task.Delay(pollingDelay);
                        continue;
                    }

                    if (!generationResp.IsSuccessStatusCode)
                    {
                        return new VideoResponse() { Success = false, ErrorMsg = respString };
                    }

                    ContentResponse respSerialized = null;

                    try
                    {
                        respSerialized = JsonHelper.DeserializeString<ContentResponse>(respString);

                        /*if (respSerialized.status == "FAILED")
                        {
                            return new VideoResponse() { Success = false, ErrorMsg = "Runway ML backend reported that video generating failed. " + respSerialized.failure };
                        }*/

                        //System.Diagnostics.Debug.WriteLine($"State: {respSerialized.status}");
                        //textualProgressAction.Invoke(respSerialized.status);

                        videoUrl = respSerialized.images != null && respSerialized.images.Count > 0 ? respSerialized.images[0].url : respSerialized.video?.url;

                        if (string.IsNullOrEmpty(videoUrl))
                        {
                            videoUrl = respSerialized?.image.url;
                        }

                        if (string.IsNullOrEmpty(videoUrl))
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

            var file = Path.GetFileName(videoUrl);

            var downloadClient = new HttpClient { BaseAddress = new Uri(videoUrl.Replace(file, "")) };

            if (isActuallyImage)
            {
                downloadClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "image/*");
            }
            else
            {
                downloadClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "video/*");
            }

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
                if (isActuallyImage)
                {
                    textualProgressAction.Invoke("");
                    return new VideoResponse() { Success = true, VideoFile = Convert.ToBase64String(respBytes) };
                }
                else
                {
                    var pathToVideo = Path.Combine(folderToSave, $"{id}.mp4");
                    await File.WriteAllBytesAsync(pathToVideo, respBytes);
                    textualProgressAction.Invoke("");
                    return new VideoResponse() { Success = true, VideoFile = pathToVideo };
                }
            }
            else
            {
                textualProgressAction.Invoke("");
                return new VideoResponse() { ErrorMsg = $"Error: {videoResp.StatusCode}, details: {await videoResp.Content.ReadAsStringAsync()}", Success = false };
            }
        }
    }
}