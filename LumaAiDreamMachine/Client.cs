using LumaAiDreamMachinePlugin.AddAudio;
using LumaAiDreamMachinePlugin.VideoUpscale;
using PluginBase;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection.PortableExecutable;

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
        public string model { get; set; } = "photon-1";
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

        //[IgnoreDynamicEdit]
        //public KeyFrames keyframes { get; set; } = new KeyFrames();
    }

    public class ImageRequestRefImage
    {
        public string url { get; set; } = "";
        public double weight { get; set; } = 0.85;

        //[IgnoreDynamicEdit]
        //public KeyFrames keyframes { get; set; } = new KeyFrames();
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
        public string image { get; set; }
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
                    return await PollVideoResults(httpClient, null, Guid.Parse(refItemPlayload.PollingId), folderToSave);
                }

                request.keyframes.frame0.type = string.IsNullOrEmpty(request.keyframes.frame0.url) ? "generation" : "image";
                request.keyframes.frame1.type = string.IsNullOrEmpty(request.keyframes.frame1.url) ? "generation" : "image";

                if (string.IsNullOrEmpty(request.keyframes.frame0.url) && string.IsNullOrEmpty(request.keyframes.frame0.id))
                {
                    request.keyframes.frame0 = null;
                }

                if (string.IsNullOrEmpty(request.keyframes.frame1.url) && string.IsNullOrEmpty(request.keyframes.frame1.id))
                {
                    request.keyframes.frame1 = null;
                }

                if (request.keyframes.frame0 == null && request.keyframes.frame1 == null)
                {
                    request.keyframes = null;
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

                var resp = await httpClient.PostAsync("dream-machine/v1/generations", stringContent);
                var respString = await resp.Content.ReadAsStringAsync();
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
                    return await PollVideoResults(httpClient, respSerialized.assets, respSerialized.id, folderToSave);
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

        public async Task<ImageResponse> GetImg(ImageRequest request, ConnectionSettings connectionSettings,
            ImageItemPayload refItemPlayload, Action saveAndRefreshCallback)
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
                    return await PollImageResults(httpClient, null, Guid.Parse(refItemPlayload.PollingId));
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

                var resp = await httpClient.PostAsync("dream-machine/v1/generations/image", stringContent);
                var respString = await resp.Content.ReadAsStringAsync();
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
                    return await PollImageResults(httpClient, respSerialized.assets, respSerialized.id);
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

        public async Task<VideoResponse> UpscaleGeneration(string generationId, string resolution, string folderToSave, ConnectionSettings connectionSettings,
            GenerationUpscaleItemPayload refItemPlayload, Action saveAndRefreshCallback)
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
                    return await PollVideoResults(httpClient, null, Guid.Parse(refItemPlayload.PollingId), folderToSave);
                }

                var stringContent = new StringContent("{\"resolution\":\"" + resolution + "\"}");
                stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var resp = await httpClient.PostAsync($"dream-machine/v1/generations/{generationId}/upscale", stringContent);
                var respString = await resp.Content.ReadAsStringAsync();
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
                    return await PollVideoResults(httpClient, respSerialized.assets, respSerialized.id, folderToSave);
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

        public async Task<VideoResponse> AddAudioToGeneration(string generationId, string positivePrompt, string megativePrompt, string folderToSave, ConnectionSettings connectionSettings,
            GenerationAddAudioItemPayload refItemPlayload, Action saveAndRefreshCallback)
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
                    return await PollVideoResults(httpClient, null, Guid.Parse(refItemPlayload.PollingId), folderToSave);
                }
                var test = "{\"prompt\":\"" + positivePrompt.Trim() + "\",\"negative_prompt\":\"" + megativePrompt.Trim() + "\"}";
                var stringContent = new StringContent(test);
                stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                var resp = await httpClient.PostAsync($"dream-machine/v1/generations/{generationId}/audio", stringContent);
                var respString = await resp.Content.ReadAsStringAsync();
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
                    return await PollVideoResults(httpClient, respSerialized.assets, respSerialized.id, folderToSave);
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

        private static async Task<VideoResponse> PollVideoResults(HttpClient httpClient, Asset assets, Guid id, string folderToSave)
        {
            var pollingDelay = TimeSpan.FromSeconds(7);

            var videoUrl = assets?.video ?? "";

            while (string.IsNullOrEmpty(assets?.video))
            {
                // Wait for assets to be filled
                try
                {
                    var generationResp = await httpClient.GetAsync($"dream-machine/v1/generations/{id}");
                    var respString = await generationResp.Content.ReadAsStringAsync();
                    Response respSerialized = null;

                    try
                    {
                        respSerialized = JsonHelper.DeserializeString<Response>(respString);
                        assets = respSerialized.assets;

                        if (respSerialized.state == "failed")
                        {
                            return new VideoResponse() { Success = false, ErrorMsg = "Luma Ai backend reported that video generating failed" };
                        }

                        System.Diagnostics.Debug.WriteLine($"State: {respSerialized.state}");

                        if (string.IsNullOrEmpty(assets?.video))
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

            videoUrl = assets?.video ?? "";

            var file = Path.GetFileName(videoUrl);

            using var downloadClient = new HttpClient { BaseAddress = new Uri(videoUrl.Replace(file, "")) };

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
                var pathToVideo = Path.Combine(folderToSave, $"{id}{Path.GetExtension(file)}");
                await File.WriteAllBytesAsync(pathToVideo, respBytes);
                return new VideoResponse() { Success = true, VideoFile = pathToVideo };
            }
            else
            {
                return new VideoResponse() { ErrorMsg = $"Error: {videoResp.StatusCode}, details: {await videoResp.Content.ReadAsStringAsync()}", Success = false };
            }
        }

        private static async Task<ImageResponse> PollImageResults(HttpClient httpClient, Asset assets, Guid id)
        {
            var pollingDelay = TimeSpan.FromSeconds(7);

            var imageUrl = assets?.image ?? "";

            while (string.IsNullOrEmpty(assets?.image))
            {
                // Wait for assets to be filled
                try
                {
                    var generationResp = await httpClient.GetAsync($"dream-machine/v1/generations/{id}");
                    var respString = await generationResp.Content.ReadAsStringAsync();
                    Response respSerialized = null;

                    try
                    {
                        respSerialized = JsonHelper.DeserializeString<Response>(respString);
                        assets = respSerialized.assets;

                        if (respSerialized.state == "failed")
                        {
                            return new ImageResponse() { Success = false, ErrorMsg = "Luma Ai backend reported that video generating failed" };
                        }

                        System.Diagnostics.Debug.WriteLine($"State: {respSerialized.state}");

                        if (string.IsNullOrEmpty(assets?.image))
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

            imageUrl = assets?.image ?? "";

            var file = Path.GetFileName(imageUrl);

            using var downloadClient = new HttpClient { BaseAddress = new Uri(imageUrl.Replace(file, "")) };

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
                return new ImageResponse() { Success = true, Image = Convert.ToBase64String(respBytes), ImageFormat = "jpg" };
            }
            else
            {
                return new ImageResponse() { ErrorMsg = $"Error: {videoResp.StatusCode}, details: {await videoResp.Content.ReadAsStringAsync()}", Success = false };
            }
        }
    }
}