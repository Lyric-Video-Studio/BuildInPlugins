using PluginBase;
using System.Text.Json.Nodes;

namespace MusicGptPlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class MusicGptPlugin : IAudioPlugin, ISaveAndRefresh, IContentId, ITextualProgressIndication
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
                    return await MusicGptClient.GenerateAudio(newIp.GenerationId, newIp.Prompt + " " + newTp.Prompt, newTp.MusicStyle, newIp.Lyrics, newTp.Instumental, newTp.VoiceOnly, newTp.VoiceId, 
                        folderToSaveAudio, _connectionSettings, itemsPayload as MusicGptItemPayload, saveAndRefreshCallback, textualProgress);
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

        public void CloseConnection()
        {
        }

        private static Dictionary<string, string> _voideNameIdDict = new();
        public async Task<string[]> SelectionOptionsForProperty(string propertyName)
        {
            if (_connectionSettings == null)
            {
                return Array.Empty<string>();
            }

            // TODO: VoiceId's, cache to name/id map


            if (propertyName == nameof(MusicGptAudioTrackPayload.VoiceId))
            {
                return _voideNameIdDict.Keys.ToArray();
            }

            return Array.Empty<string>();
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

        private Action saveAndRefreshCallback;

        public void SetSaveAndRefreshCallback(Action saveAndRefreshCallback)
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
                return ip.Prompt;
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
            if(_connectionSettings == null || string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return (false, "Access token is empty");
            }

            if(payload is MusicGptItemPayload itemPl)
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
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}