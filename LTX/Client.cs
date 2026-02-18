using PluginBase;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace LTXPlugin
{
    public class Request
    {
        public string prompt { get; set; }
        public string model { get; set; }
        public string resolution { get; set; }
        public int duration { get; set; }
        
        public int fps { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string camera_motion { get; set; }


        public bool generate_audio { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string image_uri { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string audio_uri { get; set; }
    }

    public class ErrorResp
    {
        public string type { get; set; }
        public Error error { get; set; }
    }

    public class Error
    {
        public string type { get; set; }
        public string message { get; set; }
    }

    internal class Client
    {
        public async Task<VideoResponse> GetVideo(Request request, string folderToSave, ConnectionSettings connectionSettings, CancellationToken token)
        {
            try
            {
                using var httpClient = new HttpClient() { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
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
                    return new VideoResponse() { ErrorMsg = $"Error: parsing request, details: {ex.Message}", Success = false };
                }

                var stringContent = new StringContent(serialized);
                stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var path = "text-to-video";
                if (!string.IsNullOrEmpty(request.audio_uri))
                {
                    path = "audio-to-video";
                } 
                else if (!string.IsNullOrEmpty(request.image_uri))
                {
                    path = "image-to-video";
                }

                var resp = await httpClient.PostAsync($"v1/{path}", stringContent, token);

                if (resp.StatusCode != HttpStatusCode.OK)
                {
                    var asString = await resp.Content.ReadAsStringAsync();

                    try
                    {
                        var actError = JsonHelper.DeserializeString<ErrorResp>(asString);
                        if (actError != null && actError.error != null && !string.IsNullOrEmpty(actError.error.message))
                        {
                            asString = actError.error.message;
                        }
                    }
                    catch (Exception)
                    {

                        throw;
                    }

                    return new VideoResponse() { ErrorMsg = $"Error: {resp.StatusCode}, msg: {asString}", Success = false };
                }
                else
                {
                    var file = Path.Combine(folderToSave, $"{Guid.NewGuid()}.mp4");
                    File.WriteAllBytes(file, await resp.Content.ReadAsByteArrayAsync());
                    return new VideoResponse() { Success = true, Fps = request.fps, VideoFile = file };
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return new VideoResponse() { ErrorMsg = ex.Message, Success = false };
            }
        }        
    }
}