using PluginBase;
using System.Text.Json.Nodes;

namespace ElevenLabsPlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class ElevenLabsPlugin : IAudioPlugin, ISaveAndRefresh, ITextualProgressIndication, ISaveConnectionSettings, IDisposable
    {
        public string UniqueName { get => "ElevenLabsPlugin"; }
        public string DisplayName { get => "ElevenLabs"; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Hosted by ElevenLabs. You need to have your authorization token";

        public bool AsynchronousGeneration { get; } = true;

        public string[] SettingsLinks => new[] { "https://try.elevenlabs.io/lvs" };

        public IPluginBase.TrackType CurrentTrackType { get; set; }

        private ConnectionSettings _connectionSettings = new ConnectionSettings();

        private static int _tasks = 0;

        private IDisposable _refresVoicesDisposable;

        public async Task<AudioResponse> GetAudio(object trackPayload, object itemsPayload, string folderToSaveAudio)
        {
            if (_connectionSettings == null || string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return new AudioResponse { Success = false, ErrorMsg = "Uninitialized" };
            }

            while (_tasks > 3)
            {
                await Task.Delay(1000);
            }

            _tasks++;

            try
            {
                if (JsonHelper.DeepCopy<ElevenLabsAudioTrackPayload>(trackPayload) is ElevenLabsAudioTrackPayload newTp &&
                    JsonHelper.DeepCopy<ElevenLabsItemPayload>(itemsPayload) is ElevenLabsItemPayload newIp)
                {
                    var voiceId = "";

                    if (!string.IsNullOrEmpty(newTp.VoiceId))
                    {
                        _voideNameIdDict.TryGetValue(newTp.VoiceId, out voiceId);
                    }

                    if (voiceId == Guid.Empty.ToString())
                    {
                        voiceId = null;
                    }

                    var length = newTp.Music ? newTp.Length * 1000 : 0;

                    return await ElevenLabsClient.GenerateSpeech(newIp.Prompt, voiceId,
                            folderToSaveAudio, _connectionSettings, length);
                }
                else
                {
                    return new AudioResponse { ErrorMsg = "Track playoad or item payload object not valid", Success = false };
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _tasks--;
            }
        }

        public async Task<string> Initialize(object settings)
        {
            if (JsonHelper.DeepCopy<ConnectionSettings>(settings) is ConnectionSettings s)
            {
                _connectionSettings = s;
                _isInitialized = true;
                return "";
            }
            else
            {
                return "Connection settings object not valid";
            }
        }

        private ElevenLabsAudioTrackPayload SetInstanceSubs(ElevenLabsAudioTrackPayload at)
        {
            _refresVoicesDisposable?.Dispose();
            _refresVoicesDisposable = null;
            _refresVoicesDisposable = at.RefreshAction.Subscribe(async _ =>
            {
                skipNextRefresh = false;
                await RefreshVoiceListAsync();
                saveAndRefreshCallback?.Invoke(true);
            });
            return at;
        }

        public void CloseConnection()
        {
        }

        private static Dictionary<string, string> _voideNameIdDict = new();
        private static Dictionary<string, string> _voicePathIdDict = new();

        public static bool VoiceListUpdatePending = false;

        public async Task<string[]> SelectionOptionsForProperty(string propertyName)
        {
            if (_connectionSettings == null)
            {
                return Array.Empty<string>();
            }

            if (propertyName == nameof(ElevenLabsAudioTrackPayload.VoiceId))
            {
                while (VoiceListUpdatePending)
                {
                    await Task.Delay(200);
                }

                VoiceListUpdatePending = true;

                try
                {
                    if (_voideNameIdDict.Count == 0)
                    {
                        // Try to serialize the voices
                        var storedVoices = _connectionSettings.Voices;

                        if (!string.IsNullOrEmpty(storedVoices))
                        {
                            foreach (var item in storedVoices.Split('\n').Where(s => !string.IsNullOrEmpty(s)))
                            {
                                _voideNameIdDict[item.Split(';')[0].Trim()] = item.Split(';')[1];
                                _voicePathIdDict[item.Split(';')[0].Trim()] = item.Split(';')[2];
                            }
                        }
                    }
                    // Still 0, refresh
                    if (_voideNameIdDict.Count == 0)
                    {
                        await RefreshVoiceListAsync();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
                finally
                {
                    VoiceListUpdatePending = false;
                }

                _voideNameIdDict["(none)"] = Guid.Empty.ToString();

                if (_voideNameIdDict.Count == 1 && _connectionSettings != null && !string.IsNullOrEmpty(_connectionSettings.AccessToken))
                {
                    _ = RefreshVoiceListAsync().ConfigureAwait(false);
                }

                return _voideNameIdDict.Keys.Order().ToArray();
            }

            return Array.Empty<string>();
        }

        bool isRefreshingVoices = false;
        bool skipNextRefresh = false;

        private string voiceListError = "";

        private async Task RefreshVoiceListAsync()
        {
            if (skipNextRefresh)
            {
                return;
            }
            if (_connectionSettings != null && !string.IsNullOrEmpty(_connectionSettings.AccessToken) && !isRefreshingVoices)
            {
                try
                {
                    isRefreshingVoices = true;
                    voiceListError = "";
                    var offset = 0;
                    var voiceResp = await ElevenLabsClient.GetVoices(_connectionSettings, "");
                    var newStoredVoices = "";
                    while (voiceResp != null && voiceResp.voices != null && voiceResp.voices.Count > 0)
                    {
                        voiceResp.voices.ForEach(voice =>
                        {
                            var voiceName = voice.name.Trim() + $" ({voice.labels.accent}, {voice.labels.age}, {voice.labels.gender}, {voice.labels.use_case})";
                            newStoredVoices += $"{voiceName};{voice.voice_id};{voice.preview_url}\n";
                            _voideNameIdDict[voiceName] = voice.voice_id;
                            _voicePathIdDict[voiceName] = voice.preview_url;
                        });
                        offset++;
                        voiceResp = await ElevenLabsClient.GetVoices(_connectionSettings, voiceResp.next_page_token);

                        if (!voiceResp.has_more)
                        {
                            break;
                        }
                    }

                    _connectionSettings.Voices = newStoredVoices;
                    saveConnectionSettings.Invoke(_connectionSettings);
                }
                catch (Exception ex)
                {
                    voiceListError = ex.Message;
                    skipNextRefresh = true;
                    throw;
                }
                finally
                {
                    isRefreshingVoices = false;
                    saveAndRefreshCallback?.Invoke(true);
                }
            }
        }

        public object DeserializePayload(string fileName)
        {
            return SetInstanceSubs(JsonHelper.Deserialize<ElevenLabsAudioTrackPayload>(fileName));
        }

        public IPluginBase CreateNewInstance()
        {
            return new ElevenLabsPlugin();
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

        private Action<bool> saveAndRefreshCallback;

        public void SetSaveAndRefreshCallback(Action<bool> saveAndRefreshCallback)
        {
            this.saveAndRefreshCallback = saveAndRefreshCallback;
        }

        public object ObjectToItemPayload(JsonObject obj)
        {
            return JsonHelper.ToExactType<ElevenLabsItemPayload>(obj);
        }

        public object ObjectToTrackPayload(JsonObject obj)
        {
            return SetInstanceSubs(JsonHelper.ToExactType<ElevenLabsAudioTrackPayload>(obj) as ElevenLabsAudioTrackPayload);
        }

        public object ObjectToGeneralSettings(JsonObject obj)
        {
            return JsonHelper.ToExactType<ConnectionSettings>(obj);
        }

        public string TextualRepresentation(object GenerationUpscaleItemPayload)
        {
            if (GenerationUpscaleItemPayload is ElevenLabsItemPayload ip)
            {
                return ip.Prompt;
            }

            return "";
        }

        public object DefaultPayloadForTrack()
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Audio:
                    return SetInstanceSubs(new ElevenLabsAudioTrackPayload());

                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public object DefaultPayloadForItem()
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Audio:
                    return new ElevenLabsItemPayload();

                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public object CopyPayloadForTrack(object obj)
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Audio:
                    return SetInstanceSubs(JsonHelper.DeepCopy<ElevenLabsAudioTrackPayload>(obj));

                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public object CopyPayloadForItem(object obj)
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Audio:
                    return JsonHelper.DeepCopy<ElevenLabsItemPayload>(obj);

                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public (bool payloadOk, string reasonIfNot) ValidatePayload(object payload)
        {
            if (_connectionSettings == null || string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return (false, "Access token is empty");
            }

            if (payload is ElevenLabsItemPayload itemPl)
            {
                return (!string.IsNullOrEmpty(itemPl.Prompt), "Prompt is empty");
            }

            if (!string.IsNullOrEmpty(voiceListError))
            {
                return (false, voiceListError);
            }

            return (true, "");
        }

        private Action<string> textualProgress;

        public void SetTextProgressCallback(Action<string> action)
        {
            textualProgress = action;
        }

        public List<string> FilePathsOnPayloads(object trackPayload, object itemPayload)
        {
            return new List<string>();
        }

        public void ReplaceFilePathsOnPayloads(List<string> originalPath, List<string> newPath, object trackPayload, object itemPayload)
        {
            // No need to do anything
        }

        public Action<object> saveConnectionSettings;

        public void SetSaveConnectionSettingsCallback(Action<object> saveConnectionSettings)
        {
            this.saveConnectionSettings = saveConnectionSettings;
        }

        public object ItemPayloadFromLyrics(string text)
        {
            return new ElevenLabsItemPayload() { Prompt = text };
        }

        public void AppendToPayloadFromLyrics(string text, object payload)
        {
            if (payload is ElevenLabsItemPayload ip)
            {
                ip.Prompt = text;
            }
        }

        public void UserDataDeleteRequested()
        {
            if (_connectionSettings != null)
            {
                _connectionSettings.DeleteTokens();
            }
        }

        public void Dispose()
        {
            _refresVoicesDisposable?.Dispose();
        }
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}