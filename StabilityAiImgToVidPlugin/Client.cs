using PluginBase;
using System.Diagnostics;
using System.Net;

namespace StabilityAiImgToVidPlugin
{
    public class Request
    {
        public float cfg_scale { get; set; } = 1.8f;
        public int motion_bucket_id { get; set; } = 127;
        public string seed { get; set; } = "0";
    }

    internal class Client
    {
        public async Task<VideoResponse> GetImgToVid(Request request, string pathToSourceImage, string folderToSave, ConnectionSettings connectionSettings, ItemPayload refItemPlayload, Action<bool> saveAndRefreshCallback)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Remove("accept");

                // It's best to keep these here: use can change these from item settings
                httpClient.BaseAddress = new Uri(connectionSettings.Url);
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("authorization", connectionSettings.AccessToken);

                var boundary = Guid.NewGuid().ToString();
                var content = new MultipartFormDataContent(boundary);
                content.Headers.TryAddWithoutValidation("accept", "application/json");

                pathToSourceImage = pathToSourceImage.Replace("\"", "");

                var bytes = File.ReadAllBytes(pathToSourceImage);

                content.Add(new ByteArrayContent(bytes, 0, bytes.Length), "\"image\"", $"\"{Path.GetFileName(pathToSourceImage)}\"");
                content.Add(new StringContent($"{request.cfg_scale}"), $"\"{nameof(Request.cfg_scale)}\"");
                content.Add(new StringContent($"{request.motion_bucket_id}"), $"\"{nameof(Request.motion_bucket_id)}\"");
                content.Add(new StringContent(request.seed), $"\"{nameof(Request.seed)}\"");

                if (!string.IsNullOrEmpty(refItemPlayload.PollingId))
                {
                    return await PollVideoResults(httpClient, refItemPlayload.PollingId, refItemPlayload, folderToSave, saveAndRefreshCallback);
                }

                var resp = await httpClient.PostAsync("/v2beta/image-to-video", content);
                var respString = await resp.Content.ReadAsStringAsync();

                if (resp.IsSuccessStatusCode)
                {
                    respString = respString.Replace("\"", "").Replace("id:", "").Replace("{", "").Replace("}", "");
                    return await PollVideoResults(httpClient, respString, refItemPlayload, folderToSave, saveAndRefreshCallback);
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

        private static async Task<VideoResponse> PollVideoResults(HttpClient httpClient, string pollingId, ItemPayload refItemPlayload, string folderToSave, Action<bool> saveAndRefreshCallback)
        {
            var pollingDelay = TimeSpan.FromSeconds(20);

            await Task.Delay(pollingDelay);

            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "video/*");

            // Store video request token to disk, in case connection is broken or something

            refItemPlayload.PollingId = pollingId;
            saveAndRefreshCallback?.Invoke(true);

            var videoResp = await httpClient.GetAsync($"/v2beta/image-to-video/result/{pollingId}");

            var sw = new Stopwatch();
            sw.Start();

            while (videoResp.StatusCode != HttpStatusCode.OK)
            {
                await Task.Delay(pollingDelay);
                videoResp = await httpClient.GetAsync($"/v2beta/image-to-video/result/{pollingId}");

                if (sw.Elapsed.TotalMinutes > 6)
                {
                    return new VideoResponse() { ErrorMsg = $"Timeout, is too more than 6 minutes to wait for results, this is most likely service provider issue", Success = false };
                }
            }

            if (videoResp.StatusCode == HttpStatusCode.OK)
            {
                var respBytes = await videoResp.Content.ReadAsByteArrayAsync();
                var pathToVideo = Path.Combine(folderToSave, $"{Guid.NewGuid()}.mp4");
                await File.WriteAllBytesAsync(pathToVideo, respBytes);
                refItemPlayload.PollingId = "";
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