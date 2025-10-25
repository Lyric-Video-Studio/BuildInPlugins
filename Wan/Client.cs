using PluginBase;
using System.ComponentModel;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace WanPlugin
{
    public class Request
    {
        private string modelToUse = "wan2.2-t2v-plus";

        public Input input { get; set; } = new Input();

        [IgnoreDynamicEdit]
        public string model { get => modelToUse; set => modelToUse = value; }

        public Parameters parameters { get; set; } = new Parameters();
    }

    public class Input
    {
        public string prompt { get; set; }
        public string negative_prompt { get; set; }

        [IgnoreDynamicEdit]
        public string img_url { get; set; }

        [IgnoreDynamicEdit]
        public string first_frame { get; set; }

        [IgnoreDynamicEdit]
        public string last_frame { get; set; }
    }

    public class Parameters
    {
        public string size { get; set; } = "1920×1080";
        public int seed { get; set; }

        [Description("Specifies whether to enable prompt rewriting. When enabled, an LLM is used to intelligently rewrite the input prompt. " +
            "This significantly improves the generation results for shorter prompts but increases the processing time.")]
        public bool prompt_extend { get; set; } = true;
    }

    public class Response
    {
        public string request_id { get; set; }
        public Output output { get; set; }

        public string code { get; set; }

        public string message { get; set; }
    }

    public class Output
    {
        public string task_status { get; set; }
        public string task_id { get; set; }
        public string video_url { get; set; }
        public string actual_prompt { get; set; }

        public string code { get; set; }

        public string message { get; set; }
    }

    internal class Client
    {
        public async Task<VideoResponse> GetVideo(Request request, string folderToSave, ConnectionSettings connectionSettings,
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
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("content-type", "application/json");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-DashScope-Async", "enable");

                if (!string.IsNullOrEmpty(refItemPlayload.PollingId))
                {
                    return await PollVideoResults(httpClient, refItemPlayload.PollingId, folderToSave, textualProgressAction);
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

                var endpoint = "video-generation/video-synthesis";

                if (!string.IsNullOrEmpty(request.input.first_frame))
                {
                    endpoint = "image2video/video-synthesis";
                    serialized = serialized.Replace("\"size\"", "\"resolution\"");
                }

                //System.Diagnostics.Debug.WriteLine(serialized);
                var stringContent = new StringContent(serialized);
                stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var finalEndPoint = $"services/aigc/{endpoint}";

                var resp = await httpClient.PostAsync(finalEndPoint, stringContent);
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
                    refItemPlayload.PollingId = respSerialized.output.task_id;
                    saveAndRefreshCallback.Invoke(true);
                    return await PollVideoResults(httpClient, respSerialized.output.task_id, folderToSave, textualProgressAction);
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

        private static async Task<VideoResponse> PollVideoResults(HttpClient httpClient, string id, string folderToSave,
            Action<string> textualProgressAction)
        {
            var pollingDelay = TimeSpan.FromSeconds(20);

            var videoUrl = "";
            var actualPrompt = "";

            while (string.IsNullOrEmpty(videoUrl))
            {
                // Wait for assets to be filled
                try
                {
                    var generationResp = await httpClient.GetAsync($"tasks/{id}");
                    var respString = await generationResp.Content.ReadAsStringAsync();
                    Response respSerialized = null;

                    try
                    {
                        respSerialized = JsonHelper.DeserializeString<Response>(respString);

                        if (!string.IsNullOrEmpty(respSerialized.code))
                        {
                            // Yet another different type pr response, sigh
                            return new VideoResponse()
                            {
                                Success = false,
                                ErrorMsg = "Wan API backend reported that video generating failed: " + respSerialized.code + ", " + respSerialized.message
                            };
                        }

                        if (respSerialized.output.task_status == "FAILED" || respSerialized.output.task_status == "CANCELED" || respSerialized.output.task_status == "UNKNOWN")
                        {
                            return new VideoResponse()
                            {
                                Success = false,
                                ErrorMsg = "Wan API backend reported that video generating failed: " + respSerialized.output.code + ", " + respSerialized.output.message
                            };
                        }

                        System.Diagnostics.Debug.WriteLine($"State: {respSerialized.output.task_status}");
                        textualProgressAction.Invoke(respSerialized.output.task_status);

                        videoUrl = respSerialized.output.video_url;
                        actualPrompt = respSerialized.output.actual_prompt;

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

            var downloadClient = new HttpClient { BaseAddress = new Uri(videoUrl.Replace(file, "")), Timeout = Timeout.InfiniteTimeSpan };

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
                return new VideoResponse() { Success = true, VideoFile = pathToVideo, Params = [("Actual prompt", actualPrompt)] };
            }
            else
            {
                return new VideoResponse() { ErrorMsg = $"Error: {videoResp.StatusCode}, details: {await videoResp.Content.ReadAsStringAsync()}", Success = false };
            }
        }
    }
}