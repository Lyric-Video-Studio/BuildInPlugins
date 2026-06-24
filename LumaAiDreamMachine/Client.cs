using LumaAiDreamMachinePlugin.AddAudio;
using LumaAiDreamMachinePlugin.VideoUpscale;
using PluginBase;
using System.ComponentModel;
using System.Net;
using System.Net.Http.Headers;

namespace LumaAiDreamMachinePlugin
{
    public class Request
    {
        public string model { get; set; } = "ray-2";
        public string prompt { get; set; } = "";
        public bool loop { get; set; }

        public string aspect_ratio { get; set; } = "16:9";

        [Description("Only for ray-2 model. Resolutions higher than 720p only supported in txtToVideo")]
        public string resolution { get; set; } = "720p";

        [Description("Only for ray-2 model")]
        public string duration { get; set; } = "5s";

        [IgnoreDynamicEdit]
        public KeyFrames keyframes { get; set; } = new KeyFrames();
    }

    public class ImageRequest
    {
        public event EventHandler ModelChanged;
        private string _model = "photon-1";
        public string model 
        { 
            get => _model; 
            set
            {
                var notifi = IPayloadPropertyVisibility.UserInitiatedSet && model != value;
                _model = value;

                if (notifi)
                {
                    ModelChanged?.Invoke(this, null);
                }
            }
        }
        public string prompt { get; set; } = "";
        public string aspect_ratio { get; set; } = "16:9";

        [IgnoreDynamicEdit]
        public ImageRequestRefImage[] image_ref { get; set; }

        [IgnoreDynamicEdit]
        public ImageRequestRefImage[] style_ref { get; set; }

        [IgnoreDynamicEdit]
        public ImageRequestRefCharacter character_ref { get; set; }

        [IgnoreDynamicEdit]
        public ImageRequestRefImage modify_image_ref { get; set; }
    }

    public class LumaAgentsImageRequest
    {
        public string type { get; set; } = "image";
        public string model { get; set; } = "uni-1";
        public string prompt { get; set; } = "";
        public string aspect_ratio { get; set; }
        public string style { get; set; } = "auto";
        public string output_format { get; set; }
        public bool web_search { get; set; }
        public LumaAgentsImageReference[] image_ref { get; set; }
        public LumaAgentsImageReference source { get; set; }
    }

    public class ImageRequestRefImage
    {
        public string url { get; set; } = "";
        public double weight { get; set; } = 0.85;
    }

    public class LumaAgentsImageReference
    {
        public string url { get; set; }
        public string data { get; set; }
        public string media_type { get; set; }
        public string generation_id { get; set; }
    }

    public class LumaAgentsMediaReference : LumaAgentsImageReference
    {
    }

    public class ImageRequestRefCharacter
    {
        public RefCharacter identity0 { get; set; } = new RefCharacter();
    }

    public class RefCharacter
    {
        public string[] images { get; set; }
    }

    public class KeyFrames
    {
        [Description("Starting image/generation")]
        [ParentName("Start frame")]
        public KeyFrame frame0 { get; set; } = new KeyFrame();

        [Description("Ending image/generation")]
        [ParentName("End frame")]
        public KeyFrame frame1 { get; set; } = new KeyFrame();
    }

    public class KeyFrame
    {
        [IgnoreDynamicEdit]
        public string type { get; set; }

        [Description("Image source")]
        [EnableFileDrop]
        public string url { get; set; }

        [Description("For extending video, add pollingId of another luma ai video here")]
        public string id { get; set; }
    }

    public class ModifyRequest
    {
        public string model { get; set; } = "ray-2";
        public string prompt { get; set; } = "";
        public string mode { get; set; } = "";

        public MediaDelivery media { get; set; } = new MediaDelivery();
        public MediaDelivery first_frame { get; set; } = new MediaDelivery();
    }

    public class MediaDelivery
    {
        public string url { get; set; }
    }

    public class Response
    {
        public Guid id { get; set; }
        public string type { get; set; }
        public string model { get; set; }
        public string state { get; set; }
        public string failure_reason { get; set; }
        public string failure_code { get; set; }
        public string created_at { get; set; }

        public Asset assets { get; set; }
        public OutputItem[] output { get; set; }
    }

    public class Asset
    {
        public string video { get; set; }
        public string image { get; set; }
    }

    public class OutputItem
    {
        public string type { get; set; }
        public string url { get; set; }
    }

    internal class Client
    {
        public async Task<VideoResponse> GetRayVideo(LumaAgentsVideoRequest request, string folderToSave, ConnectionSettings connectionSettings,
            ItemPayload refItemPlayload, Action<bool> saveAndRefreshCallback, Action<string> textualProgressAction)
        {
            try
            {
                using var httpClient = CreateJsonClient(connectionSettings.AgentsUrl, connectionSettings.AccessTokenUni);

                if (!string.IsNullOrEmpty(refItemPlayload.PollingId))
                {
                    return await PollVideoResults(httpClient, null, Guid.Parse(refItemPlayload.PollingId), folderToSave, textualProgressAction, id => $"v1/generations/{id}");
                }

                var serialized = JsonHelper.Serialize(request);
                using var stringContent = new StringContent(serialized);
                stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var resp = await httpClient.PostAsync("v1/generations", stringContent);
                var respString = await resp.Content.ReadAsStringAsync();
                var respSerialized = JsonHelper.DeserializeString<Response>(respString);

                if (respSerialized != null && resp.IsSuccessStatusCode)
                {
                    refItemPlayload.PollingId = respSerialized.id.ToString();
                    saveAndRefreshCallback.Invoke(true);
                    return await PollVideoResults(httpClient, respSerialized, respSerialized.id, folderToSave, textualProgressAction, id => $"v1/generations/{id}");
                }

                return new VideoResponse() { ErrorMsg = $"Error: {resp.StatusCode}, details: {respString}", Success = false };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return new VideoResponse() { ErrorMsg = ex.Message, Success = false };
            }
        }

        public async Task<VideoResponse> GetImgToVid(object request, string folderToSave, ConnectionSettings connectionSettings,
            ItemPayload refItemPlayload, Action<bool> saveAndRefreshCallback, Action<string> textualProgressAction)
        {
            try
            {
                using var httpClient = CreateJsonClient(connectionSettings.Url, connectionSettings.AccessToken);

                if (!string.IsNullOrEmpty(refItemPlayload.PollingId))
                {
                    return await PollVideoResults(httpClient, null, Guid.Parse(refItemPlayload.PollingId), folderToSave, textualProgressAction, id => $"dream-machine/v1/generations/{id}");
                }

                if (request is Request req)
                {
                    req.keyframes.frame0.type = string.IsNullOrEmpty(req.keyframes.frame0.url) ? "generation" : "image";
                    req.keyframes.frame1.type = string.IsNullOrEmpty(req.keyframes.frame1.url) ? "generation" : "image";

                    if (string.IsNullOrEmpty(req.keyframes.frame0.url) && string.IsNullOrEmpty(req.keyframes.frame0.id))
                    {
                        req.keyframes.frame0 = null;
                    }

                    if (string.IsNullOrEmpty(req.keyframes.frame1.url) && string.IsNullOrEmpty(req.keyframes.frame1.id))
                    {
                        req.keyframes.frame1 = null;
                    }

                    if (req.keyframes.frame0 == null && req.keyframes.frame1 == null)
                    {
                        req.keyframes = null;
                    }
                }

                var serialized = JsonHelper.Serialize(request);
                using var stringContent = new StringContent(serialized);
                stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var extraPath = request is Request ? "" : "/modify";
                var resp = await httpClient.PostAsync($"dream-machine/v1/generations/video{extraPath}", stringContent);
                var respString = await resp.Content.ReadAsStringAsync();
                var respSerialized = JsonHelper.DeserializeString<Response>(respString);

                if (respSerialized != null && resp.IsSuccessStatusCode)
                {
                    refItemPlayload.PollingId = respSerialized.id.ToString();
                    saveAndRefreshCallback.Invoke(true);
                    return await PollVideoResults(httpClient, respSerialized, respSerialized.id, folderToSave, textualProgressAction, id => $"dream-machine/v1/generations/{id}");
                }

                return new VideoResponse() { ErrorMsg = $"Error: {resp.StatusCode}, details: {respString}", Success = false };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return new VideoResponse() { ErrorMsg = ex.Message, Success = false };
            }
        }

        public async Task<ImageResponse> GetImg(ImageRequest request, ConnectionSettings connectionSettings,
            ImageItemPayload refItemPlayload, Action<bool> saveAndRefreshCallback)
        {
            try
            {
                using var httpClient = CreateJsonClient(connectionSettings.Url, connectionSettings.AccessToken);

                if (!string.IsNullOrEmpty(refItemPlayload.PollingId))
                {
                    return await PollLegacyImageResults(httpClient, null, Guid.Parse(refItemPlayload.PollingId));
                }

                var serialized = JsonHelper.Serialize(request);
                using var stringContent = new StringContent(serialized);
                stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var resp = await httpClient.PostAsync("dream-machine/v1/generations/image", stringContent);
                var respString = await resp.Content.ReadAsStringAsync();
                var respSerialized = JsonHelper.DeserializeString<Response>(respString);

                if (respSerialized != null && resp.IsSuccessStatusCode)
                {
                    refItemPlayload.PollingId = respSerialized.id.ToString();
                    saveAndRefreshCallback.Invoke(true);
                    return await PollLegacyImageResults(httpClient, respSerialized.assets, respSerialized.id);
                }

                return new ImageResponse() { ErrorMsg = $"Error: {resp.StatusCode}, details: {respString}", Success = false };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return new ImageResponse() { ErrorMsg = ex.Message, Success = false };
            }
        }

        public async Task<ImageResponse> GetUniImage(LumaAgentsImageRequest request, ConnectionSettings connectionSettings,
            ImageItemPayload refItemPlayload, Action<bool> saveAndRefreshCallback)
        {
            try
            {
                using var httpClient = CreateJsonClient(connectionSettings.AgentsUrl, connectionSettings.AccessTokenUni);

                if (!string.IsNullOrEmpty(refItemPlayload.PollingId))
                {
                    return await PollUniImageResults(httpClient, null, Guid.Parse(refItemPlayload.PollingId), request.output_format);
                }

                var serialized = JsonHelper.Serialize(request);
                using var stringContent = new StringContent(serialized);
                stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var resp = await httpClient.PostAsync("v1/generations", stringContent);
                var respString = await resp.Content.ReadAsStringAsync();
                var respSerialized = JsonHelper.DeserializeString<Response>(respString);

                if (respSerialized != null && resp.IsSuccessStatusCode)
                {
                    refItemPlayload.PollingId = respSerialized.id.ToString();
                    saveAndRefreshCallback.Invoke(true);
                    return await PollUniImageResults(httpClient, respSerialized, respSerialized.id, request.output_format);
                }

                return new ImageResponse() { ErrorMsg = $"Error: {resp.StatusCode}, details: {respString}", Success = false };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return new ImageResponse() { ErrorMsg = ex.Message, Success = false };
            }
        }

        public async Task<VideoResponse> UpscaleGeneration(string generationId, string resolution, string folderToSave, ConnectionSettings connectionSettings,
            GenerationUpscaleItemPayload refItemPlayload, Action<bool> saveAndRefreshCallback, Action<string> textualProgress)
        {
            try
            {
                using var httpClient = CreateJsonClient(connectionSettings.Url, connectionSettings.AccessToken);

                if (!string.IsNullOrEmpty(refItemPlayload.PollingId))
                {
                    return await PollVideoResults(httpClient, null, Guid.Parse(refItemPlayload.PollingId), folderToSave, textualProgress, id => $"dream-machine/v1/generations/{id}");
                }

                using var stringContent = new StringContent("{\"resolution\":\"" + resolution + "\"}");
                stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var resp = await httpClient.PostAsync($"dream-machine/v1/generations/{generationId}/upscale", stringContent);
                var respString = await resp.Content.ReadAsStringAsync();
                var respSerialized = JsonHelper.DeserializeString<Response>(respString);

                if (respSerialized != null && resp.IsSuccessStatusCode)
                {
                    refItemPlayload.PollingId = respSerialized.id.ToString();
                    saveAndRefreshCallback.Invoke(true);
                    return await PollVideoResults(httpClient, respSerialized, respSerialized.id, folderToSave, textualProgress, id => $"dream-machine/v1/generations/{id}");
                }

                return new VideoResponse() { ErrorMsg = $"Error: {resp.StatusCode}, details: {respString}", Success = false };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return new VideoResponse() { ErrorMsg = ex.Message, Success = false };
            }
        }

        public async Task<VideoResponse> AddAudioToGeneration(string generationId, string positivePrompt, string megativePrompt, string folderToSave, ConnectionSettings connectionSettings,
            GenerationAddAudioItemPayload refItemPlayload, Action<bool> saveAndRefreshCallback, Action<string> textualProgress)
        {
            try
            {
                using var httpClient = CreateJsonClient(connectionSettings.Url, connectionSettings.AccessToken);

                if (!string.IsNullOrEmpty(refItemPlayload.PollingId))
                {
                    return await PollVideoResults(httpClient, null, Guid.Parse(refItemPlayload.PollingId), folderToSave, textualProgress, id => $"dream-machine/v1/generations/{id}");
                }

                var payload = "{\"prompt\":\"" + positivePrompt.Trim() + "\",\"negative_prompt\":\"" + megativePrompt.Trim() + "\"}";
                using var stringContent = new StringContent(payload);
                stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var resp = await httpClient.PostAsync($"dream-machine/v1/generations/{generationId}/audio", stringContent);
                var respString = await resp.Content.ReadAsStringAsync();
                var respSerialized = JsonHelper.DeserializeString<Response>(respString);

                if (respSerialized != null && resp.IsSuccessStatusCode)
                {
                    refItemPlayload.PollingId = respSerialized.id.ToString();
                    saveAndRefreshCallback.Invoke(true);
                    return await PollVideoResults(httpClient, respSerialized, respSerialized.id, folderToSave, textualProgress, id => $"dream-machine/v1/generations/{id}");
                }

                return new VideoResponse() { ErrorMsg = $"Error: {resp.StatusCode}, details: {respString}", Success = false };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return new VideoResponse() { ErrorMsg = ex.Message, Success = false };
            }
        }

        private static HttpClient CreateJsonClient(string baseUrl, string accessToken)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Remove("accept");
            httpClient.BaseAddress = new Uri(baseUrl);
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("authorization", $"Bearer {accessToken}");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("accept", "application/json");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("content-type", "application/json");
            return httpClient;
        }

        private static async Task<VideoResponse> PollVideoResults(HttpClient httpClient, Response initialResponse, Guid id, string folderToSave,
            Action<string> textualProgressAction, Func<Guid, string> generationPathFactory)
        {
            var pollingDelay = TimeSpan.FromSeconds(7);
            var currentResponse = initialResponse ?? new Response();
            var videoUrl = GetVideoUrl(currentResponse);

            while (string.IsNullOrWhiteSpace(videoUrl))
            {
                try
                {
                    var generationResp = await httpClient.GetAsync(generationPathFactory(id));
                    var respString = await generationResp.Content.ReadAsStringAsync();
                    currentResponse = JsonHelper.DeserializeString<Response>(respString);
                    videoUrl = GetVideoUrl(currentResponse);

                    if (currentResponse.state == "failed")
                    {
                        var reason = string.IsNullOrWhiteSpace(currentResponse.failure_reason) ? currentResponse.failure_code : currentResponse.failure_reason;
                        return new VideoResponse() { Success = false, ErrorMsg = $"Luma Ai backend reported that video generating failed: {reason}" };
                    }

                    System.Diagnostics.Debug.WriteLine($"State: {currentResponse.state}");
                    textualProgressAction?.Invoke(currentResponse.state);

                    if (string.IsNullOrWhiteSpace(videoUrl))
                    {
                        await Task.Delay(pollingDelay);
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }

            using var downloadClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
            downloadClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "video/*");

            textualProgressAction?.Invoke("Downloading");

            var videoResp = await downloadClient.GetAsync(videoUrl);

            while (videoResp.StatusCode != HttpStatusCode.OK)
            {
                await Task.Delay(pollingDelay);
                videoResp = await downloadClient.GetAsync(videoUrl);
            }

            if (videoResp.StatusCode == HttpStatusCode.OK)
            {
                var respBytes = await videoResp.Content.ReadAsByteArrayAsync();
                var pathToVideo = Path.Combine(folderToSave, $"{id}{GetVideoExtension(videoUrl)}");
                await File.WriteAllBytesAsync(pathToVideo, respBytes);
                return new VideoResponse() { Success = true, VideoFile = pathToVideo };
            }

            return new VideoResponse() { ErrorMsg = $"Error: {videoResp.StatusCode}, details: {await videoResp.Content.ReadAsStringAsync()}", Success = false };
        }

        private static async Task<ImageResponse> PollLegacyImageResults(HttpClient httpClient, Asset assets, Guid id)
        {
            var pollingDelay = TimeSpan.FromSeconds(7);

            while (string.IsNullOrEmpty(assets?.image))
            {
                try
                {
                    var generationResp = await httpClient.GetAsync($"dream-machine/v1/generations/{id}");
                    var respString = await generationResp.Content.ReadAsStringAsync();
                    var respSerialized = JsonHelper.DeserializeString<Response>(respString);
                    assets = respSerialized.assets;

                    if (respSerialized.state == "failed")
                    {
                        return new ImageResponse() { Success = false, ErrorMsg = $"Luma Ai backend reported that image generating failed: {respSerialized.failure_reason}" };
                    }

                    System.Diagnostics.Debug.WriteLine($"State: {respSerialized.state}");

                    if (string.IsNullOrEmpty(assets?.image))
                    {
                        await Task.Delay(pollingDelay);
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }

            return await DownloadImageResponse(assets.image, null);
        }

        private static async Task<ImageResponse> PollUniImageResults(HttpClient httpClient, Response initialResponse, Guid id, string requestedOutputFormat)
        {
            var pollingDelay = TimeSpan.FromSeconds(7);
            var currentResponse = initialResponse ?? new Response();
            var imageUrl = GetImageUrl(currentResponse);

            while (string.IsNullOrWhiteSpace(imageUrl))
            {
                try
                {
                    var generationResp = await httpClient.GetAsync($"v1/generations/{id}");
                    var respString = await generationResp.Content.ReadAsStringAsync();
                    currentResponse = JsonHelper.DeserializeString<Response>(respString);
                    imageUrl = GetImageUrl(currentResponse);

                    if (currentResponse.state == "failed")
                    {
                        var reason = string.IsNullOrWhiteSpace(currentResponse.failure_reason) ? currentResponse.failure_code : currentResponse.failure_reason;
                        return new ImageResponse() { Success = false, ErrorMsg = $"Luma Agents backend reported that image generating failed: {reason}" };
                    }

                    System.Diagnostics.Debug.WriteLine($"State: {currentResponse.state}");

                    if (string.IsNullOrWhiteSpace(imageUrl))
                    {
                        await Task.Delay(pollingDelay);
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }

            return await DownloadImageResponse(imageUrl, requestedOutputFormat);
        }

        private static async Task<ImageResponse> DownloadImageResponse(string imageUrl, string requestedOutputFormat)
        {
            var pollingDelay = TimeSpan.FromSeconds(7);
            using var downloadClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
            downloadClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "image/*");

            var imageResp = await downloadClient.GetAsync(imageUrl);
            while (imageResp.StatusCode != HttpStatusCode.OK)
            {
                await Task.Delay(pollingDelay);
                imageResp = await downloadClient.GetAsync(imageUrl);
            }

            if (imageResp.StatusCode == HttpStatusCode.OK)
            {
                var respBytes = await imageResp.Content.ReadAsByteArrayAsync();
                return new ImageResponse()
                {
                    Success = true,
                    Image = Convert.ToBase64String(respBytes),
                    ImageFormat = GetImageFormat(imageUrl, imageResp.Content.Headers.ContentType?.MediaType, requestedOutputFormat)
                };
            }

            return new ImageResponse() { ErrorMsg = $"Error: {imageResp.StatusCode}, details: {await imageResp.Content.ReadAsStringAsync()}", Success = false };
        }

        private static string GetImageUrl(Response response)
        {
            if (!string.IsNullOrWhiteSpace(response?.assets?.image))
            {
                return response.assets.image;
            }

            return response?.output?.FirstOrDefault(s => s.type == "image")?.url ?? "";
        }

        private static string GetVideoUrl(Response response)
        {
            if (!string.IsNullOrWhiteSpace(response?.assets?.video))
            {
                return response.assets.video;
            }

            return response?.output?.FirstOrDefault(s => s.type == "video")?.url ?? "";
        }

        private static string GetVideoExtension(string videoUrl)
        {
            if (Uri.TryCreate(videoUrl, UriKind.Absolute, out var uri))
            {
                var extension = Path.GetExtension(uri.AbsolutePath);
                if (!string.IsNullOrWhiteSpace(extension))
                {
                    return extension;
                }
            }

            return ".mp4";
        }

        private static string GetImageFormat(string imageUrl, string contentType, string requestedOutputFormat)
        {
            if (!string.IsNullOrWhiteSpace(requestedOutputFormat))
            {
                return requestedOutputFormat == "jpeg" ? "jpg" : requestedOutputFormat;
            }

            if (!string.IsNullOrWhiteSpace(contentType))
            {
                if (contentType.Contains("png", StringComparison.OrdinalIgnoreCase))
                {
                    return "png";
                }

                if (contentType.Contains("jpeg", StringComparison.OrdinalIgnoreCase) || contentType.Contains("jpg", StringComparison.OrdinalIgnoreCase))
                {
                    return "jpg";
                }
            }

            if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
            {
                var extension = Path.GetExtension(uri.AbsolutePath).TrimStart('.');
                if (string.Equals(extension, "jpeg", StringComparison.OrdinalIgnoreCase))
                {
                    return "jpg";
                }

                if (!string.IsNullOrWhiteSpace(extension))
                {
                    return extension;
                }
            }

            return "jpg";
        }
    }
}
