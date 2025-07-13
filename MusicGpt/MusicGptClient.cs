using PluginBase;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection.PortableExecutable;

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
        public string conversion_path_wav_1 { get; set; }
        public string conversion_path_wav_2 { get; set; }
        public string album_cover_path { get; set; }
    }

    public class ErrorResp
    {
        public bool success { get; set; }
        public string error { get; set; }
    }

    public class MusicGptClient
    {
        public static async Task<AudioResponse> GenerateAudio(string generationId, string prompt, string musicStyle, string lyrics, bool makeInstrumental, bool vocal_only, string voice_id,
            string folderToSaveAudio, 
            ConnectionSettings connectionSettings, MusicGptItemPayload musicGptItemPayload, 
            Action saveAndRefreshCallback, Action<string> textualProgress)
        {
            if (!string.IsNullOrEmpty(musicGptItemPayload.PollingId))
            {
                return await GetConversionResponse(musicGptItemPayload.PollingId, connectionSettings, textualProgress, folderToSaveAudio);
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

                if(resp.IsSuccessStatusCode)
                {
                    var actualResp = JsonHelper.DeserializeString<MusicResponse>(respString);
                    if(actualResp.success)
                    {
                        musicGptItemPayload.PollingId = actualResp.task_id;
                        saveAndRefreshCallback.Invoke();
                        textualProgress.Invoke(actualResp.message);
                        return await GetConversionResponse(musicGptItemPayload.PollingId, connectionSettings, textualProgress, folderToSaveAudio);
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

        private static async Task<AudioResponse> GetConversionResponse(string taskId, ConnectionSettings connectionSettings, Action<string> textualProgress, string folderToSaveAudio)
        {
            var pollingDelay = TimeSpan.FromSeconds(7);

            var audioUrl = "";
            var alternativeAudioUrl = "";
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", connectionSettings.AccessToken);
            httpClient.BaseAddress = new Uri(connectionSettings.Url);
            while (string.IsNullOrEmpty(audioUrl))
            {
                // Wait for assets to be filled
                try
                {
                    var generationResp = await httpClient.GetAsync($"byId?conversionType=MUSIC_AI&task_id={taskId}");
                    var respString = await generationResp.Content.ReadAsStringAsync();
                    ConversionResponse respSerialized = null;

                    try
                    {
                        respSerialized = JsonHelper.DeserializeString<ConversionResponse>(respString);
                        audioUrl = respSerialized.conversion?.conversion_path_wav_1;
                        alternativeAudioUrl = respSerialized.conversion?.conversion_path_wav_2;

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
                catch (Exception)
                {
                    throw;
                }
            }

            var file = Path.GetFileName(audioUrl);

            using var downloadClient = new HttpClient { BaseAddress = new Uri(audioUrl.Replace(file, "")) };

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
    }
}
