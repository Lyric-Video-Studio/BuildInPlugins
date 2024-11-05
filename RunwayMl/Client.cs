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
        private string videoRatio = "16:9";
        private int? videoSeed;

        public string? promptText { get => prompt; set => prompt = value; }
        public string promptImage { get => image; set => image = value; }
        public string ratio { get => videoRatio; set => videoRatio = value; }
        public int? seed { get => videoSeed; set => videoSeed = value; }

        [Description("Duration in seconds")]
        public int duration { get => videoDuration; set => videoDuration = value; }

        private string modelToUse = "gen3a_turbo";

        [IgnoreDynamicEdit]
        public string model { get => modelToUse; set => modelToUse = value; }
    }

    public class Response
    {
        public Guid id { get; set; }
        public string status { get; set; }
        public string createdAt { get; set; }
        public string[] output { get; set; }
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
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Runway-Version", "2024-09-13");

                if (!string.IsNullOrEmpty(refItemPlayload.PollingId))
                {
                    return await PollVideoResults(httpClient, null, Guid.Parse(refItemPlayload.PollingId), refItemPlayload, folderToSave, saveAndRefreshCallback);
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

                var resp = await httpClient.PostAsync("v1/image_to_video", stringContent);
                var respString = await resp.Content.ReadAsStringAsync();

                if (resp.StatusCode != HttpStatusCode.OK)
                {
                    return new VideoResponse() { ErrorMsg = $"Error: {resp.StatusCode}, details: {respString}", Success = false };
                }

                var tempFile = Path.Combine(folderToSave, "tempReq.txt");

                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }

                File.WriteAllText(tempFile, respString);

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
                    return await PollVideoResults(httpClient, respSerialized.output, respSerialized.id, refItemPlayload, folderToSave, saveAndRefreshCallback);
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

        private static async Task<VideoResponse> PollVideoResults(HttpClient httpClient, string[] assets, Guid id, ItemPayload refItemPlayload, string folderToSave, Action saveAndRefreshCallback)
        {
            var pollingDelay = TimeSpan.FromSeconds(20);

            refItemPlayload.PollingId = id.ToString();
            saveAndRefreshCallback?.Invoke();

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
                            return new VideoResponse() { Success = false, ErrorMsg = "Runway ML backend reported that video generating failed" };
                        }

                        System.Diagnostics.Debug.WriteLine($"State: {respSerialized.status}");

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
                var pathToVideo = Path.Combine(folderToSave, $"{id}.mp4");
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