using PluginBase;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace MinimaxPlugin.Audio
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class MinimaxAudioPlugin : IAudioPlugin, IImportFromLyrics, ISaveConnectionSettings, ISaveAndRefresh
    {
        public const string PluginName = "MinimaxAudioBuildIn";
        public string UniqueName { get => PluginName; }
        public string DisplayName { get => "Minimax text to speech"; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Hosted by Minimax. You need to have your authorization token";

        public bool AsynchronousGeneration { get; } = true;

        public string[] SettingsLinks => new[] { "https://www.minimax.io/platform/user-center/basic-information/interface-key", "https://www.minimax.io/platform/user-center/basic-information" };

        public IPluginBase.TrackType CurrentTrackType { get; set; }

        private ConnectionSettings _connectionSettings = new ConnectionSettings();

        public static int CurrentTasks = 0;

        public object DefaultPayloadForVideoItem()
        {
            return new ItemPayload();
        }

        public object DefaultPayloadForVideoTrack()
        {
            return new T2ARequest();
        }

        public async Task<AudioResponse> GetAudio(object trackPayload, object itemsPayload, string folderToSaveAudio)
        {

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Remove("accept");

            // It's best to keep these here: use can change these from item settings
            httpClient.BaseAddress = new Uri(_connectionSettings.Url);
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("authorization", $"Bearer {_connectionSettings.AccessToken}");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("accept", "application/json");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("authority", "api.minimaxi.chat");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("content-type", "application/json");

            if (trackPayload is T2ARequest tp && itemsPayload is ItemPayload ip)
            {
                tp.Text = ip.Text;
                tp.TimberWeights = [new TimberWeight() { Weight = 1, VoiceId = tp.VoiceSetting.VoiceId }];

                var serialized = "";

                try
                {
                    serialized = JsonHelper.Serialize(tp);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    return new AudioResponse() { ErrorMsg = $"Error: parsing request, details: {ex.Message}", Success = false };
                }

                var stringContent = new StringContent(serialized);
                stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var resp = await httpClient.PostAsync("v1/t2a_v2", stringContent);
                var respString = await resp.Content.ReadAsStringAsync();
                T2AResponse respSerialized = null;
                try
                {
                    respSerialized = JsonHelper.DeserializeString<T2AResponse>(respString);
                    if (respSerialized != null)
                    {
                        if(respSerialized.BaseResp.StatusCode != 0)
                        {
                            return new AudioResponse() { ErrorMsg = $"Minimax api returned an error: {respSerialized.BaseResp.StatusMsg}", Success = false };
                        }
                        else
                        {
                            var b = Convert.FromHexString(respSerialized.Data.Audio);
                            var fileName = Path.Combine(folderToSaveAudio, $"{respSerialized.TraceId}.{respSerialized.ExtraInfo.AudioFormat}");

                            if(File.Exists(fileName))
                            {
                                File.Delete(fileName);
                            }

                            File.WriteAllBytes(fileName, b);
                            return new AudioResponse() { AudioFormat = respSerialized.ExtraInfo.AudioFormat, AudioFile = fileName, Success = true };
                        }
                    }
                    else
                    {
                        return new AudioResponse() { ErrorMsg = $"Error generating response", Success = false };
                    }

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    return new AudioResponse() { ErrorMsg = $"Error parsing response, {ex.Message}", Success = false };
                }
            }

            return new AudioResponse() { Success = false, ErrorMsg = "Unknown error" };
        }        

        public async Task<string> Initialize(object settings)
        {
            if (JsonHelper.DeepCopy<ConnectionSettings>(settings) is ConnectionSettings s)
            {
                _connectionSettings = s;
                _connectionSettings.SetVoiceRefreshCallback(async () =>
                {
                    await RefreshVoiceListAsync();
                    if (saveAndRefreshCallback != null)
                    {
                        saveAndRefreshCallback.Invoke();
                    }
                });
                _isInitialized = !string.IsNullOrEmpty(s.AccessToken);
                return "";
            }
            else
            {
                return "Connection settings object not valid";
            }
        }

        private async Task RefreshVoiceListAsync()
        {
            if(_connectionSettings != null && !string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Remove("accept");

                // It's best to keep these here: use can change these from item settings
                httpClient.BaseAddress = new Uri(_connectionSettings.Url);
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("authorization", $"Bearer {_connectionSettings.AccessToken}");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("accept", "application/json");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("authority", "api.minimaxi.chat");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("content-type", "application/json");

                var stringContent = new StringContent("{\r\n    \"voice_type\":\"system\"\r\n}");
                stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                try
                {
                    var resp = await httpClient.PostAsync("v1/get_voice", stringContent);
                    var respString = await resp.Content.ReadAsStringAsync();

                    var respSerialized = JsonHelper.DeserializeString<VoiceListResponse>(respString);
                    _connectionSettings.SpeechVoices = [.. respSerialized.SystemVoice.Where(s => !s.VoiceId
                        .Contains("russia", StringComparison.InvariantCultureIgnoreCase)).Select(s => s.VoiceId).Order()];
                    saveConnectionSettings.Invoke(_connectionSettings);

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }

        public void CloseConnection()
        {
        }

        private static bool _voiceListUpdating = false;
        public async Task<string[]> SelectionOptionsForProperty(string propertyName)
        {
            if (_connectionSettings == null)
            {
                return Array.Empty<string>();
            }

            if (propertyName == nameof(T2ARequest.Model))
            {
                return ["speech-02-hd", "speech-01-turbo", "speech-01-hd", "speech-01-turbo"];
            }

            if (propertyName == nameof(VoiceSetting.VoiceId))
            {
                // TODO: Use voice api

                while (_voiceListUpdating)
                {
                    await Task.Delay(200);
                }

                if(_connectionSettings.SpeechVoices.Count > 0)
                {
                    return _connectionSettings.SpeechVoices.ToArray();
                }

                try
                {
                    _voiceListUpdating = true;
                    await RefreshVoiceListAsync();

                }
                catch (Exception)
                {
                }
                finally
                {
                    _voiceListUpdating = false;
                }

                return _connectionSettings.SpeechVoices.ToArray();
            }

            if (propertyName == nameof(VoiceSetting.Emotion))
            {
                return ["happy", "sad", "angry", "fearful", "disgusted", "surprised", "neutral"];
            }

            if (propertyName == nameof(AudioSetting.SampleRate))
            {
                return ["44100", "32000", "24000", "22050", "16000", "8000"];
            }

            if (propertyName == nameof(AudioSetting.Bitrate))
            {
                return ["256000", "128000", "64000", "32000"];
            }

            if (propertyName == nameof(AudioSetting.Format))
            {
                return ["mp3", "pcm", "flac"];
            }

            if (propertyName == nameof(AudioSetting.Channel))
            {
                return ["2", "1"];
            }

            if (propertyName == nameof(T2ARequest.LanguageBoost))
            {
                return ["auto", "English", "Chinese", "Chinese,Yue", "Arabic", "Spanish", "French", "Portuguese", "German", "Turkish", "Dutch", "Ukrainian", "Vietnamese", 
                    "Indonesian", "Japanese", "Italian", "Korean", "Thai", "Polish", "Romanian", "Greek", "Czech", "Finnish", "Hindi"];
            }

            return Array.Empty<string>();
        }

        public object DeserializePayload(string fileName)
        {
            return JsonHelper.Deserialize<T2ARequest>(fileName);
        }

        public IPluginBase CreateNewInstance()
        {
            return new MinimaxAudioPlugin();
        }

        public async Task<string> TestInitialization()
        {
            try
            {
                return ""; // TODO: jaa. oisko joku ping
                /*var res = await _wrapper.PingConnection(_connectionSettings);

                if(res)
                {
                    return "";
                }
                else
                {
                    return "Initialization failed";
                }*/
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public object ItemPayloadFromLyrics(string text)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Audio)
            {
                return null; // Not supported, should we somehow differentiate the iporting stuff between video, image and audio?
            }
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                return new ItemPayload() { Text = text };
            }
            else
            {
                return new ImageItemPayload() { Prompt = text };
            }
        }

        public object ObjectToItemPayload(JsonObject obj)
        {
            return JsonHelper.ToExactType<ItemPayload>(obj);
        }

        public object ObjectToTrackPayload(JsonObject obj)
        {
            return JsonHelper.ToExactType<T2ARequest>(obj);
        }

        public object ObjectToGeneralSettings(JsonObject obj)
        {
            return JsonHelper.ToExactType<ConnectionSettings>(obj);
        }


        public string TextualRepresentation(object itemPayload)
        {
            if (itemPayload is ItemPayload ip)
            {
                return ip.Text;
            }
            return "";
        }

        public object DefaultPayloadForTrack()
        {
            return new T2ARequest();
        }

        public object DefaultPayloadForItem()
        {
            return new ItemPayload();
        }

        public object CopyPayloadForTrack(object obj)
        {
            return JsonHelper.DeepCopy<T2ARequest>(obj);
        }

        public object CopyPayloadForItem(object obj)
        {
            return JsonHelper.DeepCopy<ItemPayload>(obj);
        }

        public (bool payloadOk, string reasonIfNot) ValidatePayload(object payload)
        {
            if (payload is ItemPayload ip)
            {
                if (string.IsNullOrEmpty(_connectionSettings.AccessToken))
                {
                    return (false, "Auth token empty!!!");
                }
                if (string.IsNullOrEmpty(ip.Text))
                {
                    return (false, "Text empty");
                }
            }

            return (true, "");
        }

        public List<string> FilePathsOnPayloads(object T2ARequest, object itemPayload)
        {
            return new List<string>();
        }

        public void ReplaceFilePathsOnPayloads(List<string> originalPath, List<string> newPath, object T2ARequest, object itemPayload)
        {
            // No need to do anything            
        }

        private Action<object> saveConnectionSettings;
        public void SetSaveConnectionSettingsCallback(Action<object> saveConnectionSettings)
        {
            this.saveConnectionSettings = saveConnectionSettings;
        }

        Action saveAndRefreshCallback;
        public void SetSaveAndRefreshCallback(Action saveAndRefreshCallback)
        {
            this.saveAndRefreshCallback = saveAndRefreshCallback;
        }
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}