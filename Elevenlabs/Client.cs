using PluginBase;
using System.Net;
using System.Net.Http.Headers;

namespace ElevenLabsPlugin
{
    public class SpeechRequest
    {
        public string text { get; set; }
        public string model_id { get; set; } = "eleven_multilingual_v2";
    }

    public class Voice
    {
        public string voice_id { get; set; }
        public string name { get; set; }

        public string preview_url { get; set; }

        public VoiceLabel labels { get; set; } = new();
    }

    public class VoiceLabel
    {
        public string accent { get; set; }
        public string description { get; set; }
        public string age { get; set; }
        public string gender { get; set; }
        public string use_case { get; set; }
    }

    public class VoiceResponse
    {
        public bool has_more { get; set; }
        public List<Voice> voices { get; set; }
        public int total_count { get; set; }
        public string next_page_token { get; set; }
    }

    public class ElevenLabsClient
    {
        public static async Task<AudioResponse> GenerateSpeech(string pollingId, string text, string voiceId, string folderToSaveAudio,
            ConnectionSettings connectionSettings, ElevenLabsItemPayload ElevenLabsItemPayload, Action saveAndRefreshCallback, Action<string> textualProgress, CancellationToken cancellationToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("xi-api-key", connectionSettings.AccessToken);
            httpClient.BaseAddress = new Uri(connectionSettings.Url);

            var req = new SpeechRequest()
            {
                text = text
            };
            var json = JsonHelper.Serialize(req);
            var content = new StringContent(json);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            try
            {
                var resp = await httpClient.PostAsync($"/v1/text-to-speech/{voiceId}", content);

                if (resp.IsSuccessStatusCode)
                {
                    var respBytes = await resp.Content.ReadAsByteArrayAsync();

                    var path = Path.Combine(folderToSaveAudio, $"{Guid.NewGuid()}.mp3");
                    File.WriteAllBytes(path, respBytes);
                    return new AudioResponse() { Success = true, AudioFormat = "mp3", AudioFile = path };
                }
                else
                {
                    var respString = await resp.Content.ReadAsStringAsync();
                    return new AudioResponse() { Success = false, ErrorMsg = respString };
                }
            }
            catch (Exception ex)
            {
                return new AudioResponse() { Success = false, ErrorMsg = ex.Message };
            }
        }

        public static async Task<VoiceResponse> GetVoices(ConnectionSettings connectionSettings, string offset, int resultsPerPage = 100)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("xi-api-key", connectionSettings.AccessToken);
            httpClient.BaseAddress = new Uri(connectionSettings.Url);
            try
            {
                var nextPage = string.IsNullOrEmpty(offset) ? "" : $"&next_page_token={offset}";
                var generationResp = await httpClient.GetAsync($"v2/voices?page_size={resultsPerPage}{nextPage}");
                var respString = await generationResp.Content.ReadAsStringAsync();
                var respSerialized = JsonHelper.DeserializeString<VoiceResponse>(respString);
                return respSerialized;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return new VoiceResponse() { has_more = false };
            }
        }
    }
}