using PluginBase;
using System.ComponentModel;
using System.Net;
using System.Net.Http.Headers;

namespace StabilityAiTxtToImgPlugin
{
    public class Request
    {
        public string prompt { get; set; }
        public string negative_prompt { get; set; }
        public string model { get; set; } = "sd3";

        [Description("Possible values: 16:9 1:1 21:9 2:3 3:2 4:5 5:4 9:16 9:21")]
        public string aspect_ratio { get; set; } = "16:9";

        public string seed { get; set; }
    }

    internal class Client
    {
        public async Task<ImageResponse> GetTxtToImg(Request request, ConnectionSettings connectionSettings)
        {
            try
            {
                using var httpClient = new HttpClient();
                var boundary = Guid.NewGuid().ToString();
                var content = new MultipartFormDataContent(boundary);
                content.Headers.TryAddWithoutValidation("accept", "application/json");

                // It's best to keep these here: use can change these from item settings
                httpClient.BaseAddress = new Uri(connectionSettings.Url);
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("authorization", connectionSettings.AccessToken);

                content.Add(new StringContent(request.prompt), $"\"{nameof(Request.prompt)}\"");

                if (!string.IsNullOrEmpty(request.negative_prompt))
                {
                    content.Add(new StringContent(request.negative_prompt), $"\"{nameof(Request.negative_prompt)}\"");
                }
                content.Add(new StringContent(request.aspect_ratio), $"\"{nameof(Request.aspect_ratio)}\"");

                if (!string.IsNullOrEmpty(request.seed))
                {
                    content.Add(new StringContent(request.seed), $"\"{nameof(Request.seed)}\"");
                }

                content.Add(new StringContent(request.model), $"\"{nameof(Request.model)}\"");

                var resp = await httpClient.PostAsync("/v2beta/stable-image/generate/sd3", content);

                var respBytes = await resp.Content.ReadAsByteArrayAsync();

                var respString = Convert.ToBase64String(respBytes);

                if (resp.IsSuccessStatusCode)
                {
                    var output = new ImageResponse()
                    {
                        Success = true,
                        Image = respString,
                        ImageFormat = "png"
                    };
                    return output;
                }
                else
                {
                    return new ImageResponse() { ErrorMsg = $"Error: {resp.StatusCode}, details: {respString}", Success = false };
                }
            }
            catch (Exception ex)
            {
                return new ImageResponse() { ErrorMsg = ex.Message, Success = false };
            }
        }
    }
}