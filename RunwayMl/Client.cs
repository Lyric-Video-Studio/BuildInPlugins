using PluginBase;
using System.ComponentModel;
using System.Net;
using System.Net.Http.Headers;

namespace RunwayMlPlugin
{
    public class Request
    {
        private string prompt = "";
        private string image = "";
        private int videoDuration = 5;
        private string videoRatio = "1280:720";
        private int? videoSeed;

        public string? promptText { get => prompt; set => prompt = value; }

        [IgnoreDynamicEdit]
        public string promptImage { get => image; set => image = value; }

        public string ratio { get => videoRatio; set => videoRatio = value; }

        [Description("Set to zero to get new random seed")]
        public int? seed { get => videoSeed; set => videoSeed = value; }

        [Description("Duration in seconds, not used in Act2")]
        public int duration { get => videoDuration; set => videoDuration = value; }

        private string modelToUse = "gen4_turbo";
        public string model { get => modelToUse; set => modelToUse = value; }
    }

    public class VideoUpscaleRequest
    {
        public string videoUri { get; set; }
        public string model { get; set; } = "upscale_v1";
    }

    public class Act2Request
    {
        public Reference character { get; set; } = new Reference();
        public Reference reference { get; set; } = new Reference();
        public bool body_control { get; set; }
        public int expressionIntensity { get; set; } = 3;
        public string model { get; set; } = "act_two";
        public string ratio { get; set; } = "1280:720";
        public ContentMod contentModeration { get; set; } = new ContentMod();

        [Description("Set to zero to get new random seed")]
        public int seed { get; set; }
    }

    public class AlephRequest
    {
        public string videoUri { get; set; }
        public string promptText { get; set; }
        public List<Reference> references { get; set; } = new();
        public string model { get; set; } = "act_two";
        public string ratio { get; set; } = "1280:720";
        public ContentMod contentModeration { get; set; } = new ContentMod();

        [Description("Set to zero to get new random seed")]
        public int seed { get; set; }
    }

    public class Reference
    {
        public string type { get; set; } = "video";
        public string uri { get; set; }
    }

    public class ContentMod
    {
        public string publicFigureThreshold { get; set; } = "low";
    }

    public class Response
    {
        public Guid id { get; set; }
        public string status { get; set; }
        public string createdAt { get; set; }
        public string[] output { get; set; }
        public string failure { get; set; }
    }

    public class ImageRequest
    {
        public string promptText { get; set; }
        public string ratio { get; set; }
        public int seed;
        public string model { get; set; } = "gen4_image";
        public List<ImageReference> referenceImages { get; set; } = new List<ImageReference>();
        public ContentMod contentModeration { get; set; } = new ContentMod();
    }

    public class ImageReference
    {
        public string uri { get; set; }
        public string tag { get; set; }
    }

    internal class Client
    {
        public async Task<VideoResponse> GetVideo(object request, string folderToSave, ConnectionSettings connectionSettings,
            ItemPayload refItemPlayload, Action saveAndRefreshCallback, Action<string> textualProgressAction)
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
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Runway-Version", "2024-09-13");

                if (!string.IsNullOrEmpty(refItemPlayload.PollingId))
                {
                    return await PollVideoResults(httpClient, null, Guid.Parse(refItemPlayload.PollingId), folderToSave, textualProgressAction);
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
                var apiPath = "image_to_video";
                switch (request)
                {
                    case AlephRequest:
                        apiPath = "video_to_video";
                        break;

                    case VideoUpscaleRequest:
                        apiPath = "video_upscale";
                        break;

                    case Act2Request:
                        apiPath = "character_performance";
                        break;

                    default:
                        break;
                }

                var resp = await httpClient.PostAsync($"v1/{apiPath}", stringContent);
                var respString = await resp.Content.ReadAsStringAsync();

                if (resp.StatusCode != HttpStatusCode.OK)
                {
                    return new VideoResponse() { ErrorMsg = $"Error: {resp.StatusCode}, details: {respString}", Success = false };
                }

                Response respSerialized = null;

                try
                {
                    respSerialized = JsonHelper.DeserializeString<Response>(respString);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    return new VideoResponse() { ErrorMsg = $"Error parsing response, {ex.Message}", Success = false };
                }

                if (respSerialized != null && resp.IsSuccessStatusCode)
                {
                    refItemPlayload.PollingId = respSerialized.id.ToString();
                    saveAndRefreshCallback.Invoke();
                    return await PollVideoResults(httpClient, respSerialized.output, respSerialized.id, folderToSave, textualProgressAction);
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

        public async Task<ImageResponse> GetImage(ImageRequest request, ConnectionSettings connectionSettings,
            ImageItemPayload refItemPlayload, Action saveAndRefreshCallback, Action<string> textualProgressAction)
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
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Runway-Version", "2024-09-13");

                if (!string.IsNullOrEmpty(refItemPlayload.PollingId))
                {
                    return await PollImageResults(httpClient, null, Guid.Parse(refItemPlayload.PollingId), "", textualProgressAction);
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

                var resp = await httpClient.PostAsync($"v1/text_to_image", stringContent);
                var respString = await resp.Content.ReadAsStringAsync();

                if (resp.StatusCode != HttpStatusCode.OK)
                {
                    return new ImageResponse() { ErrorMsg = $"Error: {resp.StatusCode}, details: {respString}", Success = false };
                }

                Response respSerialized = null;

                try
                {
                    respSerialized = JsonHelper.DeserializeString<Response>(respString);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    return new ImageResponse() { ErrorMsg = $"Error parsing response, {ex.Message}", Success = false };
                }

                if (respSerialized != null && resp.IsSuccessStatusCode)
                {
                    refItemPlayload.PollingId = respSerialized.id.ToString();
                    saveAndRefreshCallback.Invoke();

                    return await PollImageResults(httpClient, respSerialized.output, respSerialized.id, "", textualProgressAction);
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

        private static async Task<ImageResponse> PollImageResults(HttpClient httpClient, string[] assets, Guid id, string folderToSave,
            Action<string> textualProgressAction)
        {
            var resp = await PollVideoResults(httpClient, assets, id, folderToSave, textualProgressAction, true);
            return new ImageResponse() { Success = resp.Success, ErrorMsg = resp.ErrorMsg, Image = resp.VideoFile, ImageFormat = "png" };
        }

        private static async Task<VideoResponse> PollVideoResults(HttpClient httpClient, string[] assets, Guid id, string folderToSave,
            Action<string> textualProgressAction, bool isActuallyImage = false)
        {
            var pollingDelay = TimeSpan.FromSeconds(20);

            var videoUrl = assets != null && assets.Length > 0 ? assets[0] : "";

            while (string.IsNullOrEmpty(videoUrl))
            {
                // Wait for assets to be filled
                try
                {
                    var generationResp = await httpClient.GetAsync($"v1/tasks/{id}");
                    var respString = await generationResp.Content.ReadAsStringAsync();
                    Response respSerialized = null;

                    try
                    {
                        respSerialized = JsonHelper.DeserializeString<Response>(respString);

                        if (respSerialized.status == "FAILED")
                        {
                            return new VideoResponse() { Success = false, ErrorMsg = "Runway ML backend reported that video generating failed. " + respSerialized.failure };
                        }

                        System.Diagnostics.Debug.WriteLine($"State: {respSerialized.status}");
                        textualProgressAction.Invoke(respSerialized.status);

                        videoUrl = respSerialized.output != null && respSerialized.output.Length > 0 ? respSerialized.output[0] : "";

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
                    return new VideoResponse() { Success = true, VideoFile = Convert.ToBase64String(respBytes) };
                }
                else
                {
                    var pathToVideo = Path.Combine(folderToSave, $"{id}.mp4");
                    await File.WriteAllBytesAsync(pathToVideo, respBytes);
                    return new VideoResponse() { Success = true, VideoFile = pathToVideo };
                }
            }
            else
            {
                return new VideoResponse() { ErrorMsg = $"Error: {videoResp.StatusCode}, details: {await videoResp.Content.ReadAsStringAsync()}", Success = false };
            }
        }
    }
}