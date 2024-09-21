using Microsoft.Maui.Storage;
using PluginBase;
using System.ComponentModel;
using System.Net;
using System.Net.Http.Headers;

namespace LumaAiDreamMachinePlugin
{
    public class Request
    {
        public string prompt { get; set; } = "";
        public bool loop { get; set; }
        public string aspect_ratio { get; set; } = "16:9";

        [IgnoreDynamicEdit]
        public KeyFrames keyFrames { get; set; } = new KeyFrames();
    }

    public class KeyFrames
    {
        [Description("Starting image/generarion")]
        [ParentName("Start frame")]
        public KeyFrame frame0 { get; set; } = new KeyFrame();

        [Description("Ending image/generarion")]
        [ParentName("End frame")]
        public KeyFrame frame1 { get; set; } = new KeyFrame();
    }

    public class KeyFrame
    {
        [IgnoreDynamicEdit]
        public string type { get; set; }

        [Description("Image source, this needs to be publicly available url")]
        public string url { get; set; }

        [Description("For extending video, add pollingId of another luma ai video here")]
        public string id { get; set; }
    }

    public class Response
    {
        public Guid id { get; set; }
        public string state { get; set; }
        public string failure_reason { get; set; }
        public string created_at { get; set; }

        public Asset assets { get; set; }
    }

    public class Asset
    {
        public string video { get; set; }
    }

    internal class Client
    {
        public async Task<VideoResponse> GetImgToVid(Request request, string folderToSave, ConnectionSettings connectionSettings,
            ItemPayload refItemPlayload, Action saveAndRefreshCallback)
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

                if (!string.IsNullOrEmpty(refItemPlayload.PollingId))
                {
                    return await PollVideoResults(httpClient, null, Guid.Parse(refItemPlayload.PollingId), refItemPlayload, folderToSave, saveAndRefreshCallback);
                }

                request.keyFrames.frame0.type = string.IsNullOrEmpty(request.keyFrames.frame0.url) ? "generation" : "image";
                request.keyFrames.frame1.type = string.IsNullOrEmpty(request.keyFrames.frame1.url) ? "generation" : "image";

                if (string.IsNullOrEmpty(request.keyFrames.frame0.url) && string.IsNullOrEmpty(request.keyFrames.frame0.id))
                {
                    request.keyFrames.frame0 = null;
                }

                if (string.IsNullOrEmpty(request.keyFrames.frame1.url) && string.IsNullOrEmpty(request.keyFrames.frame1.id))
                {
                    request.keyFrames.frame1 = null;
                }

                if (request.keyFrames.frame0 == null && request.keyFrames.frame1 == null)
                {
                    request.keyFrames = null;
                }

                var serialized = JsonHelper.Serialize(request);
                var stringContent = new StringContent(serialized);
                stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var resp = await httpClient.PostAsync("v1/generations", stringContent);
                var respString = await resp.Content.ReadAsStringAsync();
                Response respSerialized = null;

                try
                {
                    respSerialized = JsonHelper.DeserializeString<Response>(respString);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                if (respSerialized != null && resp.IsSuccessStatusCode)
                {
                    return await PollVideoResults(httpClient, respSerialized.assets, respSerialized.id, refItemPlayload, folderToSave, saveAndRefreshCallback);
                }
                else
                {
                    return new VideoResponse() { ErrorMsg = $"Error: {resp.StatusCode}, details: {respString}", Success = false };
                }
            }
            catch (Exception ex)
            {
                return new VideoResponse() { ErrorMsg = ex.Message, Success = false };
            }
        }

        private static async Task<VideoResponse> PollVideoResults(HttpClient httpClient, Asset assets, Guid id, ItemPayload refItemPlayload, string folderToSave, Action saveAndRefreshCallback)
        {
            var pollingDelay = TimeSpan.FromSeconds(20);

            refItemPlayload.PollingId = id.ToString();
            saveAndRefreshCallback?.Invoke();

            var videoUrl = assets?.video ?? "";

            while (string.IsNullOrEmpty(assets?.video))
            {
                // Wait for assets to be filled
                try
                {
                    var generationResp = await httpClient.GetAsync($"v1/generations/{id}");
                    var respString = await generationResp.Content.ReadAsStringAsync();
                    Response respSerialized = null;

                    try
                    {
                        respSerialized = JsonHelper.DeserializeString<Response>(respString);
                        assets = respSerialized.assets;
                        Console.WriteLine($"State: {respSerialized.state}");

                        if (assets == null)
                        {
                            await Task.Delay(pollingDelay);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }

            videoUrl = assets?.video ?? "";

            var file = Path.GetFileName(videoUrl);

            var downloadClient = new HttpClient { BaseAddress = new Uri(videoUrl.Replace(file, "")) };

            downloadClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "video/*");

            // Store video request token to disk, in case connection is broken or something

            var videoResp = await downloadClient.GetAsync(file);

            while (videoResp.StatusCode != HttpStatusCode.OK)
            {
                await Task.Delay(pollingDelay);
                videoResp = await downloadClient.GetAsync(file);
            }

            if (videoResp.StatusCode == HttpStatusCode.OK)
            {
                var respBytes = await videoResp.Content.ReadAsByteArrayAsync();
                var pathToVideo = Path.Combine(folderToSave, $"{id}.{Path.GetExtension(file)}");
                await File.WriteAllBytesAsync(pathToVideo, respBytes);
                return new VideoResponse() { Success = true, VideoFile = pathToVideo };
            }
            else
            {
                refItemPlayload.PollingId = "";
                return new VideoResponse() { ErrorMsg = $"Error: {videoResp.StatusCode}, details: {await videoResp.Content.ReadAsStringAsync()}", Success = false };
            }
        }
    }
}