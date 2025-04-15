using Microsoft.IdentityModel.Tokens;
using PluginBase;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;

namespace KlingAiPlugin
{
    public class KlingVideoRequest
    {
        [JsonPropertyName("model_name")]
        public string ModelName { get; set; } = "kling-v1-6"; // Default example

        [JsonPropertyName("style")]
        public string Style { get; set; } = "cinematic"; // Default example

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

    internal class Client
    {
        public async Task<VideoResponse> GetImgToVid(KlingVideoRequest request, string folderToSave, ConnectionSettings connectionSettings,
            ItemPayload refItemPlayload, Action saveAndRefreshCallback)
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
                    return await PollVideoResults(httpClient, refItemPlayload.PollingId, folderToSave, endPoint);
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
                    return await PollVideoResults(httpClient, respSerialized.Data?.TaskId, folderToSave, endPoint);
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

        /*public async Task<ImageResponse> GetImg(ImageRequest request, ConnectionSettings connectionSettings)
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

                var resp = await httpClient.PostAsync("/v1/image_generation", stringContent);
                var respString = await resp.Content.ReadAsStringAsync();
                KlingAiImageResponse respSerialized = null;

                try
                {
                    respSerialized = JsonHelper.DeserializeString<KlingAiImageResponse>(respString);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    return new ImageResponse() { ErrorMsg = $"Error parsing response, {ex.Message}", Success = false };
                }

                if (respSerialized != null && resp.IsSuccessStatusCode && respSerialized.base_resp.status_code == 0)
                {
                    return new ImageResponse() { Success = true, Image = respSerialized.data.image_base64.First(), ImageFormat = "jpg" };
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

        private static async Task<VideoResponse> PollVideoResults(HttpClient httpClient, string id, string folderToSave, string endPoint)
        {
            var pollingDelay = TimeSpan.FromSeconds(7);

            var videoUrl = "";

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
                            return new VideoResponse() { Success = false, ErrorMsg = $"KlingAi reported that video generating failed: {respSerialized.Code}" };
                        }

                        videoUrl = respSerialized?.Data?.TaskResult?.Videos?.Select(v => v.Url)?.FirstOrDefault() ?? "";

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
                return new VideoResponse() { ErrorMsg = $"Error: {videoResp.StatusCode}, details: {await videoResp.Content.ReadAsStringAsync()}", Success = false };
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