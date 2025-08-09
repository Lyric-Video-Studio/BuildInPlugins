using PluginBase;
using System.Text.Json.Nodes;

namespace MistralTxtToImgPlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class MistralTxtToImgPlugin : IImagePlugin, IImportFromLyrics, ISaveConnectionSettings
    {
        public string UniqueName { get => "MistralTxtToImageBuildIn"; }
        public string DisplayName { get => "Mistral AI"; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Hosted by blackforestlabs.ai/. You need to have your authorization token to use this API";

        public string[] SettingsLinks => ["https://api.mistral.ai", "https://console.mistral.ai/api-keys"];

        public bool AsynchronousGeneration { get; } = true;

        private ConnectionSettings _connectionSettings = new ConnectionSettings();

        public IPluginBase.TrackType CurrentTrackType { get; set; }

        public async Task<ImageResponse> GetImage(object trackPayload, object itemsPayload)
        {
            if (_connectionSettings == null || string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return new ImageResponse { Success = false, ErrorMsg = "Uninitialized" };
            }

            if (trackPayload is TrackPayload newTp && itemsPayload is ItemPayload newIp)
            {
                return await MistralImages.CreateImage($"{newTp.Prompt.Trim()} {newIp.Prompt.Trim()}".Trim(), _connectionSettings, saveConnectionSettings);
            }
            else
            {
                return new ImageResponse { ErrorMsg = "Track playoad or item payload object not valid", Success = false };
            }
        }

        public async Task<string> Initialize(object settings)
        {
            if (JsonHelper.DeepCopy<ConnectionSettings>(settings) is ConnectionSettings s)
            {
                _connectionSettings = s;
                _isInitialized = !string.IsNullOrEmpty(s.AccessToken);
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

        public async Task<string[]> SelectionOptionsForProperty(string propertyName)
        {
            return Array.Empty<string>();
        }

        public object DeserializePayload(string fileName)
        {
            return JsonHelper.Deserialize<TrackPayload>(fileName);
        }

        public IPluginBase CreateNewInstance()
        {
            return new MistralTxtToImgPlugin();
        }

        public object ItemPayloadFromLyrics(string lyric)
        {
            return new ItemPayload() { Prompt = lyric };
        }

        public async Task<string> TestInitialization()
        {
            try
            {
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public object ObjectToItemPayload(JsonObject obj)
        {
            return JsonHelper.ToExactType<ItemPayload>(obj);
        }

        public object ObjectToTrackPayload(JsonObject obj)
        {
            return JsonHelper.ToExactType<TrackPayload>(obj);
        }

        public object ObjectToGeneralSettings(JsonObject obj)
        {
            return JsonHelper.ToExactType<ConnectionSettings>(obj);
        }

        public string TextualRepresentation(object itemPayload)
        {
            if (itemPayload is ItemPayload ip)
            {
                return ip.Prompt;
            }
            return "";
        }

        public object DefaultPayloadForTrack()
        {
            return new TrackPayload();
        }

        public object DefaultPayloadForItem()
        {
            return new ItemPayload();
        }

        public object CopyPayloadForTrack(object obj)
        {
            return JsonHelper.DeepCopy<TrackPayload>(obj);
        }

        public object CopyPayloadForItem(object obj)
        {
            return JsonHelper.DeepCopy<ItemPayload>(obj);
        }

        public (bool payloadOk, string reasonIfNot) ValidatePayload(object payload)
        {
            if (string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return (false, "Auth token missing");
            }

            if (payload is ItemPayload ip && string.IsNullOrEmpty(ip.Prompt))
            {
                return (false, "Prompt missing");
            }

            return (true, "");
        }

        public List<string> FilePathsOnPayloads(object trackPayload, object itemPayload)
        {
            return new List<string>();
        }

        public void ReplaceFilePathsOnPayloads(List<string> originalPath, List<string> newPath, object trackPayload, object itemPayload)
        {
            // No need to do anything
        }

        private Action<object> saveConnectionSettings;

        public void SetSaveConnectionSettingsCallback(Action<object> saveConnectionSettings)
        {
            this.saveConnectionSettings = saveConnectionSettings;
        }
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}