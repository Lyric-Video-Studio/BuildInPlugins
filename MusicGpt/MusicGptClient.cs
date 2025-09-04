using PluginBase;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;

namespace MusicGptPlugin
{
    public class MusicRequest
    {
        public string prompt { get; set; }
        public string music_style { get; set; }
        public string lyrics { get; set; }
        public bool make_instrumental { get; set; }
        public bool vocal_only { get; set; }
        public string voice_id { get; set; }
        public string webhook_url { get; set; } = "";
    }

    public class SpeechRequest
    {
        public string text { get; set; }
        public string voice_id { get; set; }
        public string gender { get; set; }
    }

    public class MusicResponse
    {
        public bool success { get; set; }
        public string message { get; set; }
        public string task_id { get; set; }
        public string conversion_id_1 { get; set; }
        public string conversion_id_2 { get; set; }
        public int eta { get; set; }
    }

    public class ConversionResponse
    {
        public bool success { get; set; }
        public Conversion conversion { get; set; }
    }

    public class Conversion
    {
        public string task_id { get; set; }
        public string conversion_id { get; set; }
        public string status { get; set; }
        public string status_msg { get; set; }
        public string audio_url { get; set; }
        public double conversion_cost { get; set; }
        public string title { get; set; }
        public string lyrics { get; set; }
        public string music_style { get; set; }
        public string createdAt { get; set; }
        public string updatedAt { get; set; }
        public string conversion_path_wav { get; set; }
        public string conversion_path_wav_1 { get; set; }
        public string conversion_path_wav_2 { get; set; }
        public string album_cover_path { get; set; }
    }

    public class ErrorResp
    {
        public bool success { get; set; }
        public string error { get; set; }
    }

    public class Voice
    {
        public string voice_id { get; set; }
        public string voice_name { get; set; }
    }

    public class VoiceResponse
    {
        public bool success { get; set; }
        public List<Voice> voices { get; set; }
        public int limit { get; set; }
        public int page { get; set; }
        public int total { get; set; }
    }

    public class SpeechResponse
    {
        public bool success { get; set; }
        public string task_id { get; set; }
        public string conversion_id { get; set; }
        public string audio_url { get; set; }
        public string audio_url_wav { get; set; }
        public double conversion_cost { get; set; }
        public double conversion_duration { get; set; }
        public string detail { get; set; }
    }

    public class MusicGptClient
    {
        public static async Task<AudioResponse> GenerateAudio(string generationId, string prompt, string musicStyle, string lyrics, bool makeInstrumental, bool vocal_only, string voice_id,
            string folderToSaveAudio,
            ConnectionSettings connectionSettings, MusicGptItemPayload musicGptItemPayload,
            Action saveAndRefreshCallback, Action<string> textualProgress, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(musicGptItemPayload.PollingId))
            {
                return await GetConversionResponse(musicGptItemPayload.PollingId, connectionSettings, textualProgress, folderToSaveAudio, cancellationToken, false, false);
            }

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", connectionSettings.AccessToken);
            httpClient.BaseAddress = new Uri(connectionSettings.Url);

            var req = new MusicRequest()
            {
                lyrics = lyrics,
                make_instrumental = makeInstrumental,
                vocal_only = vocal_only,
                voice_id = voice_id,
                music_style = musicStyle,
                prompt = prompt
            };
            var json = JsonHelper.Serialize(req);
            var content = new StringContent(json);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            try
            {
                var resp = await httpClient.PostAsync("MusicAI", content);
                var respString = await resp.Content.ReadAsStringAsync();

                if (resp.IsSuccessStatusCode)
                {
                    var actualResp = JsonHelper.DeserializeString<MusicResponse>(respString);
                    if (actualResp.success)
                    {
                        musicGptItemPayload.PollingId = actualResp.task_id;
                        saveAndRefreshCallback.Invoke();
                        textualProgress.Invoke(actualResp.message);
                        return await GetConversionResponse(musicGptItemPayload.PollingId, connectionSettings, textualProgress, folderToSaveAudio, cancellationToken, true, false);
                    }
                    else
                    {
                        return new AudioResponse() { Success = false, ErrorMsg = actualResp.message };
                    }
                }
                else
                {
                    var ep = JsonHelper.DeserializeString<ErrorResp>(respString);
                    return new AudioResponse() { Success = false, ErrorMsg = ep.error ?? resp.StatusCode.ToString() };
                }
            }
            catch (Exception ex)
            {
                return new AudioResponse() { Success = false, ErrorMsg = ex.Message };
            }
        }

        public static async Task<AudioResponse> GenerateSpeech(string pollingId, string text, string gender, string voiceId, string folderToSaveAudio,
            ConnectionSettings connectionSettings, MusicGptItemPayload musicGptItemPayload, Action saveAndRefreshCallback, Action<string> textualProgress, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(musicGptItemPayload.PollingId))
            {
                return await GetConversionResponse(musicGptItemPayload.PollingId, connectionSettings, textualProgress, folderToSaveAudio, cancellationToken, false, true);
            }

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", connectionSettings.AccessToken);
            httpClient.BaseAddress = new Uri(connectionSettings.Url);

            var req = new SpeechRequest()
            {
                text = text,
                gender = gender,
                voice_id = voiceId
            };
            var json = JsonHelper.Serialize(req);
            var content = new StringContent(json);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            try
            {
                var resp = await httpClient.PostAsync("TextToSpeech", content);
                var respString = await resp.Content.ReadAsStringAsync();

                if (resp.IsSuccessStatusCode)
                {
                    var actualResp = JsonHelper.DeserializeString<MusicResponse>(respString);
                    if (actualResp.success)
                    {
                        musicGptItemPayload.PollingId = actualResp.task_id;
                        saveAndRefreshCallback.Invoke();
                        textualProgress.Invoke(actualResp.message);
                        return await GetConversionResponse(musicGptItemPayload.PollingId, connectionSettings, textualProgress, folderToSaveAudio, cancellationToken, true, true);
                    }
                    else
                    {
                        return new AudioResponse() { Success = false, ErrorMsg = actualResp.message };
                    }
                }
                else
                {
                    var ep = JsonHelper.DeserializeString<ErrorResp>(respString);
                    return new AudioResponse() { Success = false, ErrorMsg = ep.error ?? resp.StatusCode.ToString() };
                }
            }
            catch (Exception ex)
            {
                return new AudioResponse() { Success = false, ErrorMsg = ex.Message };
            }
        }

        private static async Task<AudioResponse> GetConversionResponse(string taskId, ConnectionSettings connectionSettings, Action<string> textualProgress, string folderToSaveAudio,
            CancellationToken cancellationToken, bool firstTry, bool isTts)
        {
            var pollingDelay = TimeSpan.FromSeconds(7);

            var audioUrl = "";
            var alternativeAudioUrl = "";
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", connectionSettings.AccessToken);
            httpClient.BaseAddress = new Uri(connectionSettings.Url);
            while (string.IsNullOrEmpty(audioUrl) && !cancellationToken.IsCancellationRequested)
            {
                // Wait for assets to be filled

                if (firstTry)
                {
                    await Task.Delay(pollingDelay);
                }

                try
                {
                    var cType = isTts ? "TEXT_TO_SPEECH" : "MUSIC_AI";
                    var generationResp = await httpClient.GetAsync($"byId?conversionType={cType}&task_id={taskId}");
                    var respString = await generationResp.Content.ReadAsStringAsync();
                    ConversionResponse respSerialized = null;

                    try
                    {
                        respSerialized = JsonHelper.DeserializeString<ConversionResponse>(respString);
                        audioUrl = respSerialized.conversion?.conversion_path_wav_1;
                        alternativeAudioUrl = respSerialized.conversion?.conversion_path_wav_2;

                        if (string.IsNullOrEmpty(audioUrl))
                        {
                            audioUrl = respSerialized.conversion?.conversion_path_wav;
                        }

                        if (!respSerialized.success)
                        {
                            return new AudioResponse() { Success = false, ErrorMsg = "MusicGpt backend reported that audio generating failed" };
                        }

                        System.Diagnostics.Debug.WriteLine($"State: {respSerialized.conversion.status}");
                        textualProgress.Invoke(respSerialized.conversion.status);

                        if (string.IsNullOrEmpty(audioUrl))
                        {
                            await Task.Delay(pollingDelay);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                    }
                }
                catch (Exception ex)
                {
                    return new AudioResponse() { Success = false, ErrorMsg = ex.Message };
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return new AudioResponse() { Success = false, ErrorMsg = "Cancelled by user" };
            }

            var file = Path.GetFileName(audioUrl);

            using var downloadClient = new HttpClient { BaseAddress = new Uri(audioUrl.Replace(file, "")), Timeout = Timeout.InfiniteTimeSpan };

            downloadClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "file/*");

            textualProgress.Invoke("Downloading audio 1");

            var audioResp = await downloadClient.GetAsync(file);

            while (audioResp.StatusCode != HttpStatusCode.OK)
            {
                await Task.Delay(pollingDelay);
                audioResp = await downloadClient.GetAsync(file);
            }

            if (audioResp.StatusCode == HttpStatusCode.OK)
            {
                var respBytes = await audioResp.Content.ReadAsByteArrayAsync();
                var finalPath = Path.Combine(folderToSaveAudio, file);
                var audio1 = finalPath;

                if (!File.Exists(finalPath))
                {
                    await File.WriteAllBytesAsync(finalPath, respBytes);
                }

                var altFile = "";
                if (!string.IsNullOrEmpty(alternativeAudioUrl))
                {
                    file = Path.GetFileName(alternativeAudioUrl);
                    using var downloadClient2 = new HttpClient { BaseAddress = new Uri(alternativeAudioUrl.Replace(file, "")) };
                    downloadClient2.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "file/*");

                    textualProgress.Invoke("Downloading audio 2");

                    audioResp = await downloadClient2.GetAsync(file);

                    while (audioResp.StatusCode != HttpStatusCode.OK)
                    {
                        await Task.Delay(pollingDelay);
                        audioResp = await downloadClient2.GetAsync(file);
                    }

                    if (audioResp.StatusCode == HttpStatusCode.OK)
                    {
                        respBytes = await audioResp.Content.ReadAsByteArrayAsync();
                        finalPath = Path.Combine(folderToSaveAudio, file);

                        if (!File.Exists(finalPath))
                        {
                            await File.WriteAllBytesAsync(finalPath, respBytes);
                        }

                        altFile = finalPath;
                    }
                }

                return new AudioResponse() { Success = true, AudioFormat = Path.GetExtension(file), AudioFile = audio1, AlternativeAudioFile = altFile };
            }
            else
            {
                return new AudioResponse() { ErrorMsg = $"Error: {audioResp.StatusCode}, details: {await audioResp.Content.ReadAsStringAsync()}", Success = false };
            }
        }

        public static async Task<VoiceResponse> GetVoices(ConnectionSettings connectionSettings, int offset, int resultsPerPage = 300)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", connectionSettings.AccessToken);
            httpClient.BaseAddress = new Uri(connectionSettings.Url);
            try
            {
                var generationResp = await httpClient.GetAsync($"getAllVoices?limit={resultsPerPage}&page={offset}");
                var respString = await generationResp.Content.ReadAsStringAsync();
                var respSerialized = JsonHelper.DeserializeString<VoiceResponse>(respString);
                return respSerialized;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return new VoiceResponse() { total = 0 };
            }
        }
    }
}