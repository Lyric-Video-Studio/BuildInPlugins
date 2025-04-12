using PluginBase;
using System.ComponentModel;
using System.Net;
using System.Net.Http.Headers;

namespace MinimaxPlugin
{
    public class Request
    {
        public string model { get; set; } = "S2V-01";
        public string prompt { get; set; } = "";
        public bool prompt_optimizerboolean { get; set; } = true;
        public string first_frame_image { get; set; }
        public KeyFrame[] subject_reference { get; set; }

        /*public bool loop { get; set; }

        public string aspect_ratio { get; set; } = "16:9";

        [Description("Only for ray-2 model. Resolutions higher than 720p only supported in txtToVideo")]
        public string resolution { get; set; } = "720p";

        [Description("Only for ray-2 model")]
        public string duration { get; set; } = "5s";

        [IgnoreDynamicEdit]
        public KeyFrames keyframes { get; set; } = new KeyFrames();*/
    }

    public class Response
    {
        public string task_id { get; set; }
        public BaseResponse base_resp { get; set; }
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
        public string task_id { get; set; }
        public ulong bytes { get; set; }
        public ulong created_at { get; set; }
        public string filename { get; set; }
        public string purpose { get; set; }
        public string download_url { get; set; }
        public BaseResponse base_resp { get; set; }
    }

    /*public class ImageRequest
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
    }*/

    public class KeyFrame
    {
        [IgnoreDynamicEdit]
        public string type { get; set; } = "character";

        public string[] image { get; set; }
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
                    return await PollVideoResults(httpClient, refItemPlayload.PollingId, folderToSave, connectionSettings.GroupId);
                }

                /*request.keyframes.frame0.type = string.IsNullOrEmpty(request.keyframes.frame0.url) ? "generation" : "image";
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
                }*/

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
                    saveAndRefreshCallback.Invoke();
                    return await PollVideoResults(httpClient, respSerialized.task_id, folderToSave, connectionSettings.GroupId);
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

        /*public async Task<ImageResponse> GetImg(ImageRequest request, ConnectionSettings connectionSettings,
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
        }*/

        private static async Task<VideoResponse> PollVideoResults(HttpClient httpClient, string id, string folderToSave, string groupId)
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

            while (string.IsNullOrEmpty(fileResp?.download_url))
            {
                // Wait for assets to be filled
                try
                {
                    var generationResp = await httpClient.GetAsync($"v1/files/retrieve?GroupId={groupId}&file_id={videoUrl}'");
                    var respString = await generationResp.Content.ReadAsStringAsync();

                    try
                    {
                        fileResp = JsonHelper.DeserializeString<VideoFileResponse>(respString);

                        if (fileResp?.base_resp?.status_code != 0)
                        {
                            return new VideoResponse() { Success = false, ErrorMsg = $"Minimax reported that video generating failed: {fileResp?.base_resp?.status_msg}" };
                        }

                        videoUrl = fileResp?.download_url;
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
                var pathToVideo = Path.Combine(folderToSave, $"{id}.{Path.GetExtension(file)}");
                await File.WriteAllBytesAsync(pathToVideo, respBytes);
                return new VideoResponse() { Success = true, VideoFile = pathToVideo };
            }
            else
            {
                return new VideoResponse() { ErrorMsg = $"Error: {videoResp.StatusCode}, details: {await videoResp.Content.ReadAsStringAsync()}", Success = false };
            }
        }

        /*private static async Task<ImageResponse> PollImageResults(HttpClient httpClient, Asset assets, Guid id)
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

            var downloadClient = new HttpClient { BaseAddress = new Uri(imageUrl.Replace(file, "")) };

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
        }*/
    }
}