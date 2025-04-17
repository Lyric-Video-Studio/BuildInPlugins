using Microsoft.IdentityModel.Tokens;
using PluginBase;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;

namespace KlingAiPlugin
{
    public class KlingVideoRequest
    {
        [JsonPropertyName("model_name")]
        public string ModelName { get; set; } = "kling-v1-6"; // Default example

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("negative_prompt")]
        public string? NegativePrompt { get; set; }

        [JsonPropertyName("aspect_ratio")]
        public string AspectRatio { get; set; } = "16:9"; // Default example

        [JsonPropertyName("cfg_scale")]
        public double CfgScale { get; set; } = 0.5; // Specific seed value

        [JsonPropertyName("duration")]
        public int Duration { get; set; } = 5; // Specific seed value

        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "std";

        [JsonPropertyName("image")]
        public string StartFramePath { get; set; }

        [JsonPropertyName("image_tail")]
        public string EndFramePath { get; set; }
    }

    public class KlingImageRequest
    {
        [JsonPropertyName("model_name")]
        public string ModelName { get; set; } = "kling-v1"; // Default example

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("negative_prompt")]
        public string? NegativePrompt { get; set; }

        [JsonPropertyName("aspect_ratio")]
        public string AspectRatio { get; set; } = "16:9"; // Default example

        [JsonPropertyName("image")]
        public string ImageReferencePath { get; set; }

        [JsonPropertyName("image_reference")]
        public string ImageType { get; set; } = "subject";

        [JsonPropertyName("image_fidelity")]
        public double ImageFidelity { get; set; } = 0.5;

        [JsonPropertyName("human_fidelity")]
        public double HumanFidelity { get; set; } = 0.5;
    }

    public class KlingLipsyncRequest
    {
        // Well, that's lame, KlingAi, your document says this is fine, but it aint... Thanks...
        [JsonPropertyName("video_url")]
        [IgnoreDynamicEdit]
        public string VideoUrl { get; set; } = string.Empty;

        [JsonPropertyName("video_id")]
        [IgnoreDynamicEdit]
        public string VideoId { get; set; } = string.Empty;

        [IgnoreDynamicEdit]
        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "text2video";

        [JsonPropertyName("text")]
        [IgnoreDynamicEdit]
        public string Text { get; set; }

        [JsonPropertyName("voice_id")]
        [Description("Required value when using text, see https://lyricvideo.studio/plugins for more info")]
        public string VoiceId { get; set; }

        [Range(0.8, 2)]
        [JsonPropertyName("voice_speed")]
        public double VoiceSpeed { get; set; } = 1.0;

        [JsonPropertyName("voice_language")]
        [IgnoreDynamicEdit]
        public string VoiceLanguage { get; set; } = "en";

        [JsonPropertyName("audio_type")]
        [IgnoreDynamicEdit]
        public string AudioType { get; set; } = "url";

        [JsonPropertyName("audio_url")]
        [IgnoreDynamicEdit]
        public string AudioUrl { get; set; }

        [JsonIgnore]
        public static string[,] AvailableVoices = new string[,]
        {
            // Chinese Voices (zh)
            { "genshin_vindi2", "zh" },
            { "zhinen_xuesheng", "zh" },
            { "tiyuxi_xuedi", "zh" },
            { "ai_shatang", "zh" },
            { "genshin_klee2", "zh" },
            { "genshin_kirara", "zh" },
            { "ai_kaiya", "zh" },
            { "tiexin_nanyou", "zh" },
            { "ai_chenjiahao_712", "zh" },
            { "girlfriend_1_speech02", "zh" },
            { "chat1_female_new-3", "zh" },
            { "girlfriend_2_speech02", "zh" },
            { "cartoon-boy-07", "zh" },
            { "cartoon-girl-01", "zh" },
            { "ai_huangyaoshi_712", "zh" },
            { "you_pingjing", "zh" },
            { "ai_laoguowang_712", "zh" },
            { "chengshu_jiejie", "zh" },
            { "zhuxi_speech02", "zh" },
            { "uk_oldman3", "zh" }, // Note: ID seems English, but marked 'zh' in source data
            { "laopopo_speech02", "zh" },
            { "heainainai_speech02", "zh" },
            { "dongbeilaotie_speech02", "zh" },
            { "chongqingxiaohuo_speech02", "zh" },
            { "chuanmeizi_speech02", "zh" },
            { "chaoshandashu_speech02", "zh" },
            { "ai_taiwan_man2_speech02", "zh" },
            { "xianzhanggui_speech02", "zh" },
            { "tianjinjiejie_speech02", "zh" },
            { "diyinnansang_DB_CN_M_04-v2", "zh" },
            { "yizhipiannan-v1", "zh" },
            { "guanxiaofang-v2", "zh" },
            { "tianmeixuemei-v1", "zh" },
            { "daopianyansang-v1", "zh" },
            { "mengwa-v1", "zh" },

            // English Voices (en)
            { "genshin_vindi2", "en" },
            { "zhinen_xuesheng", "en" }, // Note: ID seems Chinese, but marked 'en' in source data
            { "AOT", "en" },
            { "ai_shatang", "en" }, // Note: ID seems Chinese, but marked 'en' in source data
            { "genshin_klee2", "en" },
            { "genshin_kirara", "en" },
            { "ai_kaiya", "en" }, // Note: ID seems Chinese, but marked 'en' in source data
            { "oversea_male1", "en" },
            { "ai_chenjiahao_712", "en" }, // Note: ID seems Chinese, but marked 'en' in source data
            { "girlfriend_4_speech02", "en" },
            { "chat1_female_new-3", "en" },
            { "chat_0407_5-1", "en" },
            { "cartoon-boy-07", "en" },
            { "uk_boy1", "en" },
            { "cartoon-girl-01", "en" },
            { "PeppaPig_platform", "en" },
            { "ai_huangzhong_712", "en" }, // Note: ID seems Chinese, but marked 'en' in source data
            { "ai_huangyaoshi_712", "en" }, // Note: ID seems Chinese, but marked 'en' in source data
            { "ai_laoguowang_712", "en" }, // Note: ID seems Chinese, but marked 'en' in source data
            { "chengshu_jiejie", "en" }, // Note: ID seems Chinese, but marked 'en' in source data
            { "you_pingjing", "en" }, // Note: ID seems Chinese, but marked 'en' in source data
            { "calm_story1", "en" },
            { "uk_man2", "en" },
            { "laopopo_speech02", "en" }, // Note: ID seems Chinese, but marked 'en' in source data
            { "heainainai_speech02", "en" }, // Note: ID seems Chinese, but marked 'en' in source data
            { "reader_en_m-v1", "en" },
            { "commercial_lady_en_f-v1", "en" }
        };

        public static List<string> GetPrintableVoices()
        {
            var output = new List<string>();
            for (int i = 0; i < AvailableVoices.GetLength(0); i++)
            {
                output.Add(AvailableVoices[i, 0] + $" ({AvailableVoices[i, 1]})");
            }

            return output;
        }
    }

    // --- Response DTOs ---
    public class KlingSuccessResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public KlingResponseData? Data { get; set; }
    }

    public class KlingResponseData
    {
        [JsonPropertyName("task_id")]
        public string TaskId { get; set; } = string.Empty;

        [JsonPropertyName("task_status")]
        public string TaskStatus { get; set; } = string.Empty;
    }

    // --- Optional: Error Response DTO ---
    public class KlingErrorResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        // Add other potential error fields if the API provides them
    }

    // --- Custom Exception ---
    public class KlingApiException : Exception
    {
        public int ErrorCode { get; }
        public string? ApiMessage { get; }

        public KlingApiException(string message, int errorCode = -1, string? apiMessage = null) : base(message)
        {
            ErrorCode = errorCode;
            ApiMessage = apiMessage ?? message;
        }

        public KlingApiException(string message, Exception innerException, int errorCode = -1, string? apiMessage = null) : base(message, innerException)
        {
            ErrorCode = errorCode;
            ApiMessage = apiMessage ?? message;
        }
    }

    /// <summary>
    /// Represents the root object of the Kling API response for task status/results.
    /// </summary>
    public class KlingTaskStatusResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; } // Error code; 0 typically means success

        [JsonPropertyName("message")]
        public string? Message { get; set; } // Status or error message

        [JsonPropertyName("request_id")]
        public string RequestId { get; set; } // Unique ID for the request

        [JsonPropertyName("data")]
        public KlingTaskData? Data { get; set; } // Contains the task details and results
    }

    /// <summary>
    /// Represents the main data payload containing task information and results.
    /// </summary>
    public class KlingTaskData
    {
        [JsonPropertyName("task_id")]
        public string TaskId { get; set; } = string.Empty; // System-generated Task ID

        [JsonPropertyName("task_status")]
        public string TaskStatus { get; set; } = string.Empty; // e.g., "submitted", "processing", "succeed", "failed"

        [JsonPropertyName("task_status_msg")]
        public string? TaskStatusMsg { get; set; } // Optional message, especially for failures

        [JsonPropertyName("task_info")]
        public KlingTaskInfo? TaskInfo { get; set; } // Information provided during task creation

        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; } // Task creation time (Unix timestamp, milliseconds)

        [JsonPropertyName("updated_at")]
        public long UpdatedAt { get; set; } // Task last update time (Unix timestamp, milliseconds)

        [JsonPropertyName("task_result")]
        public KlingTaskResult? TaskResult { get; set; } // Results of the task (e.g., video URLs), likely present only on success
    }

    /// <summary>
    /// Represents parameters associated with the task creation.
    /// </summary>
    public class KlingTaskInfo
    {
        [JsonPropertyName("external_task_id")]
        public string? ExternalTaskId { get; set; } // Optional customer-defined task ID
    }

    /// <summary>
    /// Represents the successful result of a video generation task.
    /// </summary>
    public class KlingTaskResult
    {
        [JsonPropertyName("videos")]
        public List<KlingVideoInfo> Videos { get; set; } = new List<KlingVideoInfo>(); // List of generated videos
    }

    /// <summary>
    /// Represents information about a single generated video.
    /// </summary>
    public class KlingVideoInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty; // Globally unique video ID

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty; // URL to access the video

        [JsonPropertyName("duration")]
        public string Duration { get; set; } = string.Empty; // Total video duration as a string (unit: seconds)

        // Note: Even though it's a duration, the API provides it as a string.
        // You might want to parse this to a numeric type (e.g., double or decimal) if needed for calculations.
        // Example: double.TryParse(Duration, out double durationSeconds);
    }

    /// <summary>
    /// Represents the root object of the Kling API response for an image task status/result.
    /// </summary>
    public class KlingImageTaskStatusResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; } // Error code; 0 typically means success

        [JsonPropertyName("message")]
        public string? Message { get; set; } // Status or error message, potentially null

        [JsonPropertyName("request_id")]
        public string? RequestId { get; set; } // Unique ID for the request, potentially null

        [JsonPropertyName("data")]
        public KlingImageTaskData? Data { get; set; } // Contains the task details and results, potentially null if the request itself failed fundamentally
    }

    /// <summary>
    /// Represents the main data payload containing image task information and results.
    /// </summary>
    public class KlingImageTaskData
    {
        [JsonPropertyName("task_id")]
        public string TaskId { get; set; } = string.Empty; // System-generated Task ID

        [JsonPropertyName("task_status")]
        public string TaskStatus { get; set; } = string.Empty; // e.g., "submitted", "processing", "succeed", "failed"

        [JsonPropertyName("task_status_msg")]
        public string? TaskStatusMsg { get; set; } // Optional message, especially for failures, potentially null

        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; } // Task creation time (Unix timestamp, milliseconds)

        [JsonPropertyName("updated_at")]
        public long UpdatedAt { get; set; } // Task last update time (Unix timestamp, milliseconds)

        [JsonPropertyName("task_result")]
        public KlingImageTaskResult? TaskResult { get; set; } // Results of the task (e.g., image URLs), likely present only on success, potentially null
    }

    /// <summary>
    /// Represents the successful result of an image generation task.
    /// </summary>
    public class KlingImageTaskResult
    {
        [JsonPropertyName("images")]
        public List<KlingImageInfo> Images { get; set; } = new List<KlingImageInfo>(); // List of generated images, initialized to prevent null reference
    }

    /// <summary>
    /// Represents information about a single generated image.
    /// </summary>
    public class KlingImageInfo
    {
        [JsonPropertyName("index")]
        public int Index { get; set; } // Image number (0-9)

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty; // URL to access the image
    }

    internal class Client
    {
        public async Task<VideoResponse> GetImgToVid(KlingVideoRequest request, string folderToSave, ConnectionSettings connectionSettings,
            ItemPayload refItemPlayload, Action saveAndRefreshCallback, Action<string> textualProgressIndication)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Remove("accept");

                // It's best to keep these here: use can change these from item settings
                httpClient.BaseAddress = new Uri(connectionSettings.Url);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", EncodeJwtToken(connectionSettings.AccessToken, connectionSettings.AccessSecret));
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("accept", "application/json");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("authority", "api.KlingAii.chat");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("content-type", "application/json");

                var endPoint = !string.IsNullOrEmpty(request.StartFramePath) ? "image2video" : "text2video";

                if (!string.IsNullOrEmpty(refItemPlayload.PollingId))
                {
                    var res = await PollVideoResults(httpClient, refItemPlayload.PollingId, folderToSave, endPoint, textualProgressIndication);
                    refItemPlayload.VideoId = res.videoId;
                    saveAndRefreshCallback.Invoke();
                    return res.Item2;
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

                var resp = await httpClient.PostAsync($"v1/videos/{endPoint}", stringContent);
                var respString = await resp.Content.ReadAsStringAsync();
                KlingSuccessResponse? respSerialized = null;
                string? errMsg = null;
                try
                {
                    respSerialized = JsonHelper.DeserializeString<KlingSuccessResponse>(respString);
                    errMsg = respSerialized?.Message;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    return new VideoResponse() { ErrorMsg = $"Error parsing response, {ex.Message}", Success = false };
                }

                if (respSerialized != null && resp.IsSuccessStatusCode && respSerialized.Code == 0)
                {
                    refItemPlayload.PollingId = respSerialized.Data?.TaskId.ToString();
                    saveAndRefreshCallback.Invoke();
                    var res = await PollVideoResults(httpClient, respSerialized.Data?.TaskId, folderToSave, endPoint, textualProgressIndication);
                    refItemPlayload.VideoId = res.videoId;
                    saveAndRefreshCallback.Invoke();
                    return res.Item2;
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

        public async Task<VideoResponse> GetImgToVid(KlingLipsyncRequest request, string folderToSave, ConnectionSettings connectionSettings,
            ItemPayloadLipsync refItemPlayload, Action saveAndRefreshCallback, Action<string> textualProgressIndication)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Remove("accept");

                // It's best to keep these here: use can change these from item settings
                httpClient.BaseAddress = new Uri(connectionSettings.Url);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", EncodeJwtToken(connectionSettings.AccessToken, connectionSettings.AccessSecret));
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("accept", "application/json");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("authority", "api.KlingAii.chat");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("content-type", "application/json");

                var endPoint = "lip-sync";

                if (!string.IsNullOrEmpty(refItemPlayload.PollingId))
                {
                    var res = await PollVideoResults(httpClient, refItemPlayload.PollingId, folderToSave, endPoint, textualProgressIndication);
                    return res.Item2;
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

                var resp = await httpClient.PostAsync($"v1/videos/{endPoint}", stringContent);
                var respString = await resp.Content.ReadAsStringAsync();
                KlingSuccessResponse? respSerialized = null;
                string? errMsg = null;
                try
                {
                    respSerialized = JsonHelper.DeserializeString<KlingSuccessResponse>(respString);
                    errMsg = respSerialized?.Message;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    return new VideoResponse() { ErrorMsg = $"Error parsing response, {ex.Message}", Success = false };
                }

                if (respSerialized != null && resp.IsSuccessStatusCode && respSerialized.Code == 0)
                {
                    refItemPlayload.PollingId = respSerialized.Data?.TaskId.ToString();
                    saveAndRefreshCallback.Invoke();
                    var res = await PollVideoResults(httpClient, respSerialized.Data?.TaskId, folderToSave, endPoint, textualProgressIndication);
                    return res.Item2;
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

        public async Task<ImageResponse> GetImg(KlingImageRequest request, ConnectionSettings connectionSettings, ImageItemPayload ip, Action saveAndRefresh)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Remove("accept");

                // It's best to keep these here: use can change these from item settings
                httpClient.BaseAddress = new Uri(connectionSettings.Url);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", EncodeJwtToken(connectionSettings.AccessToken, connectionSettings.AccessSecret));
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("accept", "application/json");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("content-type", "application/json");

                if (string.IsNullOrEmpty(request.ImageReferencePath))
                {
                    request.ImageType = "";
                }

                var serialized = "";

                if (!string.IsNullOrEmpty(ip.PollingId))
                {
                    return await PollImageResults(httpClient, ip.PollingId);
                }

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

                var resp = await httpClient.PostAsync("/v1/images/generations", stringContent);
                var respString = await resp.Content.ReadAsStringAsync();
                KlingImageTaskStatusResponse? respSerialized = null;

                try
                {
                    respSerialized = JsonHelper.DeserializeString<KlingImageTaskStatusResponse>(respString);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    return new ImageResponse() { ErrorMsg = $"Error parsing response, {ex.Message}", Success = false };
                }

                if (respSerialized != null && resp.IsSuccessStatusCode && respSerialized.Code == 0)
                {
                    ip.PollingId = respSerialized.RequestId;
                    saveAndRefresh.Invoke();
                    return await PollImageResults(httpClient, ip.PollingId);
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

        private static async Task<(string videoId, VideoResponse)> PollVideoResults(HttpClient httpClient, string id, string folderToSave, string endPoint, Action<string> textualProgressIndication)
        {
            var pollingDelay = TimeSpan.FromSeconds(7);

            var videoUrl = "";
            var videoId = "";

            while (string.IsNullOrEmpty(videoUrl))
            {
                // Wait for assets to be filled
                try
                {
                    var generationResp = await httpClient.GetAsync($"/v1/videos/{endPoint}/{id}");
                    var respString = await generationResp.Content.ReadAsStringAsync();
                    KlingTaskStatusResponse? respSerialized = null;

                    try
                    {
                        respSerialized = JsonHelper.DeserializeString<KlingTaskStatusResponse>(respString);

                        if (respSerialized.Code != 0)
                        {
                            return (videoId, new VideoResponse() { Success = false, ErrorMsg = $"KlingAi reported that video generating failed: {respSerialized.Code}" });
                        }

                        videoUrl = respSerialized?.Data?.TaskResult?.Videos?.Select(v => v.Url)?.FirstOrDefault() ?? "";
                        videoId = respSerialized?.Data?.TaskResult?.Videos?.Select(v => v.Id)?.FirstOrDefault() ?? "";

                        System.Diagnostics.Debug.WriteLine($"State: {respSerialized?.Data?.TaskStatus}");

                        textualProgressIndication.Invoke(respSerialized?.Data?.TaskStatus);

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

            // Store video request token to disk, in case connection is broken or something
            var file = Path.GetFileName(videoUrl);

            using var downloadClient = new HttpClient { BaseAddress = new Uri(videoUrl.Replace(file, "")) };

            downloadClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "video/*");

            // Store video request token to disk, in case connection is broken or something

            textualProgressIndication.Invoke("Downloading");

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
                return (videoId, new VideoResponse() { Success = true, VideoFile = pathToVideo });
            }
            else
            {
                return (videoId, new VideoResponse() { ErrorMsg = $"Error: {videoResp.StatusCode}, details: {await videoResp.Content.ReadAsStringAsync()}", Success = false });
            }
        }

        private static async Task<ImageResponse> PollImageResults(HttpClient httpClient, string id)
        {
            var pollingDelay = TimeSpan.FromSeconds(7);

            var videoUrl = "";

            while (string.IsNullOrEmpty(videoUrl))
            {
                // Wait for assets to be filled
                try
                {
                    var generationResp = await httpClient.GetAsync($"/v1/images/generations/{id}");
                    var respString = await generationResp.Content.ReadAsStringAsync();
                    KlingImageTaskStatusResponse? respSerialized = null;

                    try
                    {
                        respSerialized = JsonHelper.DeserializeString<KlingImageTaskStatusResponse>(respString);

                        if (respSerialized.Code != 0)
                        {
                            return new ImageResponse() { Success = false, ErrorMsg = $"KlingAi reported that video generating failed: {respSerialized.Code}" };
                        }

                        videoUrl = respSerialized?.Data?.TaskResult?.Images?.Select(v => v.Url)?.FirstOrDefault() ?? "";

                        System.Diagnostics.Debug.WriteLine($"State: {respSerialized?.Data?.TaskStatus}");

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

            // Store video request token to disk, in case connection is broken or something
            var file = Path.GetFileName(videoUrl);

            using var downloadClient = new HttpClient { BaseAddress = new Uri(videoUrl.Replace(file, "")) };

            // Store video request token to disk, in case connection is broken or something

            var imageResp = await downloadClient.GetAsync(file);

            while (imageResp.StatusCode != HttpStatusCode.OK)
            {
                await Task.Delay(pollingDelay);
                imageResp = await downloadClient.GetAsync(file);
            }

            if (imageResp.StatusCode == HttpStatusCode.OK)
            {
                var respBytes = await imageResp.Content.ReadAsByteArrayAsync();
                return new ImageResponse() { Success = true, Image = Convert.ToBase64String(respBytes), ImageFormat = Path.GetExtension(file).Replace(".", "") };
            }
            else
            {
                return new ImageResponse() { ErrorMsg = $"Error: {imageResp.StatusCode}", Success = false };
            }
        }

        public static string EncodeJwtToken(string accessKey, string secretKey)
        {
            if (string.IsNullOrEmpty(accessKey))
                throw new ArgumentNullException(nameof(accessKey));
            if (string.IsNullOrEmpty(secretKey))
                throw new ArgumentNullException(nameof(secretKey));

            // 1. Create the Security Key using the secret
            // HS256 requires a symmetric key. UTF8 encoding is common for string-based secrets.
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            // 2. Create the Signing Credentials
            // Specify the key and the algorithm (HS256)
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // 3. Define Headers (Optional - alg/typ are usually handled)
            // The JwtHeader is created automatically based on credentials,
            // but you could customize it if needed. "typ": "JWT" is standard.
            var header = new JwtHeader(credentials);
            // header["typ"] = "JWT"; // Usually added by default by the handler

            // 4. Define Payload (Claims)
            var now = DateTimeOffset.UtcNow; // Use UTC time
            var expirationTime = now.AddHours(25); // Server might be on different time zone
            var notBeforeTime = now.AddHours(-25); // Server might be on different time zone, dunno if this is good idea :D Lemme know it not :)

            // Standard claims like 'iss', 'exp', 'nbf' are often handled via JwtPayload constructor or properties
            var payload = new JwtPayload(
                issuer: accessKey,                // "iss" claim
                audience: null,                   // "aud" claim (not used in the Python example)
                claims: null,                     // No custom claims in this example
                notBefore: notBeforeTime.DateTime,// "nbf" claim (Needs DateTime)
                expires: expirationTime.DateTime, // "exp" claim (Needs DateTime)
                issuedAt: now.DateTime            // "iat" claim (Good practice to include)
            );
            // The handler will convert DateTime back to Unix timestamps for 'exp' and 'nbf' in the final JWT string.

            // 5. Create the JWT Security Token Object
            var securityToken = new JwtSecurityToken(header, payload);

            // 6. Create the Handler and Write the Token String
            var handler = new JwtSecurityTokenHandler();
            string token = handler.WriteToken(securityToken);

            return token;
        }
    }
}