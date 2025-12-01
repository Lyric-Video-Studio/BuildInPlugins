using PluginBase;
using System.Text.Json.Nodes;

namespace MusicGptPlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class MusicGptPlugin : IAudioPlugin, ISaveAndRefresh, IContentId, ITextualProgressIndication, ISaveConnectionSettings, IImportFromLyrics, ICancellableGeneration
    {
        public string UniqueName { get => "MusicGptPlugin"; }
        public string DisplayName { get => "MusicGpt"; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Hosted by MusicGpt. You need to have your authorization token";

        public bool AsynchronousGeneration { get; } = true;

        public string[] SettingsLinks => new[] { "https://musicgpt.com/api-dashboard" };

        public IPluginBase.TrackType CurrentTrackType { get; set; }

        private ConnectionSettings _connectionSettings = new ConnectionSettings();

        public object DefaultPayloadForAudioItem()
        {
            return new MusicGptItemPayload();
        }

        public object DefaultPayloadForAudioTrack()
        {
            return new MusicGptAudioTrackPayload();
        }

        private static int _tasks = 0;

        public async Task<AudioResponse> GetAudio(object trackPayload, object itemsPayload, string folderToSaveAudio)
        {
            if (_connectionSettings == null || string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return new AudioResponse { Success = false, ErrorMsg = "Uninitialized" };
            }

            while (_tasks > 9)
            {
                await Task.Delay(1000);
            }

            _tasks++;

            try
            {
                if (JsonHelper.DeepCopy<MusicGptAudioTrackPayload>(trackPayload) is MusicGptAudioTrackPayload newTp &&
                    JsonHelper.DeepCopy<MusicGptItemPayload>(itemsPayload) is MusicGptItemPayload newIp)
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

                    if (newTp.SpeechOnly)
                    {
                        return await MusicGptClient.GenerateSpeech(newIp.PollingId, newIp.Lyrics + " " + newTp.Prompt, newTp.Gender, voiceId,
                            folderToSaveAudio, _connectionSettings, itemsPayload as MusicGptItemPayload, saveAndRefreshCallback, textualProgress, cancellationToken);
                    }
                    else
                    {
                        return await MusicGptClient.GenerateAudio(newIp.PollingId, newIp.Prompt + " " + newTp.Prompt, newTp.MusicStyle, newIp.Lyrics, newTp.Instrumental, newTp.VoiceOnly, voiceId,
                            folderToSaveAudio, _connectionSettings, itemsPayload as MusicGptItemPayload, saveAndRefreshCallback, textualProgress, cancellationToken, newIp.AudioSource, newTp.Gender);
                    }
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
                _connectionSettings.SetVoiceRefreshCallback(async () =>
                {
                    await RefreshVoiceListAsync();
                    saveAndRefreshCallback?.Invoke(true);
                });
                _isInitialized = true;
                return "";
            }
            else
            {
                return "Connection settings object not valid";
            }
        }

        public void CloseConnection()
        {
        }

        private static Dictionary<string, string> _voideNameIdDict = new();

        public static bool VoiceListUpdatePending = false;

        public async Task<string[]> SelectionOptionsForProperty(string propertyName)
        {
            if (_connectionSettings == null)
            {
                return Array.Empty<string>();
            }

            if (propertyName == nameof(MusicGptAudioTrackPayload.Gender))
            {
                return ["male", "female"];
            }

            if (propertyName == nameof(MusicGptAudioTrackPayload.VoiceId))
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

                return _voideNameIdDict.Keys.Order().ToArray();
            }

            return Array.Empty<string>();
        }

        private async Task RefreshVoiceListAsync()
        {
            if (_connectionSettings != null && !string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                var offset = 0;
                var voiceResp = await MusicGptClient.GetVoices(_connectionSettings, offset);
                var newStoredVoices = "";
                while (voiceResp != null && voiceResp.total > 0 && voiceResp.voices != null && voiceResp.voices.Count > 0)
                {
                    voiceResp.voices.ForEach(voice =>
                    {
                        newStoredVoices += $"{voice.voice_name.Trim()};{voice.voice_id}\n";
                        _voideNameIdDict[voice.voice_name.Trim()] = voice.voice_id;
                    });
                    offset++;
                    voiceResp = await MusicGptClient.GetVoices(_connectionSettings, offset);
                }

                _connectionSettings.Voices = newStoredVoices;
                saveConnectionSettings.Invoke(_connectionSettings);
            }
        }

        public object DeserializePayload(string fileName)
        {
            return JsonHelper.Deserialize<MusicGptAudioTrackPayload>(fileName);
        }

        public IPluginBase CreateNewInstance()
        {
            return new MusicGptPlugin();
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
            return JsonHelper.ToExactType<MusicGptItemPayload>(obj);
        }

        public object ObjectToTrackPayload(JsonObject obj)
        {
            return JsonHelper.ToExactType<MusicGptAudioTrackPayload>(obj);
        }

        public object ObjectToGeneralSettings(JsonObject obj)
        {
            return JsonHelper.ToExactType<ConnectionSettings>(obj);
        }

        public string TextualRepresentation(object GenerationUpscaleItemPayload)
        {
            if (GenerationUpscaleItemPayload is MusicGptItemPayload ip)
            {
                return ip.Lyrics;
            }

            return "";
        }

        public string GetContentFromPayloadId(object payload)
        {
            if (payload is MusicGptItemPayload ip)
            {
                return ip.PollingId;
            }
            return "";
        }

        public object DefaultPayloadForTrack()
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Audio:
                    return DefaultPayloadForAudioTrack();

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
                    return DefaultPayloadForAudioItem();

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
                    return JsonHelper.DeepCopy<MusicGptAudioTrackPayload>(obj);

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
                    return JsonHelper.DeepCopy<MusicGptItemPayload>(obj);

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

            if (payload is MusicGptItemPayload itemPl)
            {
                return (!string.IsNullOrEmpty(itemPl.Prompt), "Prompt is empty");
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
            return new MusicGptItemPayload() { Lyrics = text };
        }

        private CancellationToken cancellationToken;

        public void SetCancallationToken(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
        }
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}