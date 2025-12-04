using PluginBase;
using System.Text.Json.Nodes;

namespace A1111TxtToImgPlugin
{
    public class TxtToImgPlugin : IImagePlugin
    {
        public string UniqueName { get => "Automatic1111TxtToImageBuildIn"; }
        public string DisplayName { get => "Automatic1111 TxtToImg"; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Locally hosted Automatic1111 instance. Set full path to webui-user.bat";

        public string[] SettingsLinks => new[] { "https://www.ulti.fi" };

        private ConnectionSettings _connectionSettings = new ConnectionSettings();

        private A1111Wrapper _wrapper = new A1111Wrapper();

        public IPluginBase.TrackType CurrentTrackType { get; set; }

        public bool AsynchronousGeneration { get; } = false;

        public object DefaultPayloadForImageItem()
        {
            return new ItemPayload();
        }

        public object DefaultPayloadForImageTrack()
        {
            return new TrackPayload();
        }

        public async Task<ImageResponse> GetImage(object trackPayload, object itemsPayload)
        {
            if (_connectionSettings == null)
            {
                return new ImageResponse { ErrorMsg = "Cnnection settings was null, this was not expected", Success = false };
            }

            var newTp = JsonHelper.DeepCopy<TrackPayload>(trackPayload);
            var newIp = JsonHelper.DeepCopy<ItemPayload>(itemsPayload);

            if (newTp != null && newIp != null)
            {
#pragma warning disable CS8602
                newTp.Settings.Prompt = $"{newIp.PositivePrompt} {newTp.Settings.Prompt}"; // TODO: Asetukseen tämä, track levelille? Mutta sinne en oikein kivasti saa custom asioita, vielä, täytyy tehdä wrapperi
                newTp.Settings.Negative_prompt = $"{newIp.NegativePrompt} {newTp.Settings.Negative_prompt}";
                newTp.Settings.Prompt = newTp.Settings.Prompt.Trim();
                newTp.Settings.Negative_prompt = newTp.Settings.Negative_prompt.Trim();

                if (newIp.Seed != 0)
                {
                    newTp.Settings.Seed = newIp.Seed;
                }

#pragma warning restore CS8602
                return await _wrapper.GetTxtToImg(newTp.Settings, _connectionSettings, newTp.Sd_model_checkpoint);
            }
            else
            {
                return new ImageResponse { ErrorMsg = "Track playoad or item payload object not valid", Success = false };
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        public async Task<string> Initialize(object settings)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var settingsSerialized = JsonHelper.DeepCopy<ConnectionSettings>(settings);
            if (settingsSerialized is ConnectionSettings s)
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
            _wrapper.CloseConnection();
        }

        public double HeigthForEditor(string propertyName)
        {
            return -1; // TODO Promptit isompina
        }

        public async Task<string[]> SelectionOptionsForProperty(string propertyName)
        {
            if (_connectionSettings == null)
            {
                return Array.Empty<string>();
            }
            return await _wrapper.GetSelectionForProperty(propertyName, _connectionSettings);
        }

        public object CopyPayloadForImageTrack(object obj)
        {
            if (JsonHelper.DeepCopy<TrackPayload>(obj) is TrackPayload set)
            {
                return set;
            }
            return DefaultPayloadForImageTrack();
        }

        public object CopyPayloadForImageItem(object obj)
        {
            if (JsonHelper.DeepCopy<ItemPayload>(obj) is ItemPayload set)
            {
                return set;
            }
            return DefaultPayloadForImageItem();
        }

        public object DeserializePayload(string fileName)
        {
            return JsonHelper.Deserialize<TrackPayload>(fileName) ?? new TrackPayload();
        }

        public IPluginBase CreateNewInstance()
        {
            return new TxtToImgPlugin();
        }

        public object ItemPayloadFromLyrics(string lyric)
        {
            return new ItemPayload() { PositivePrompt = lyric };
        }

        public async Task<string> TestInitialization()
        {
            try
            {
                var res = await _wrapper.PingConnection(_connectionSettings);

                if (res)
                {
                    return "";
                }
                else
                {
                    return "Initialization failed";
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public (bool payloadOk, string reasonIfNot) ValidateImagePayload(object payload)
        {
            if (payload is ItemPayload ip && string.IsNullOrEmpty(ip.PositivePrompt))
            {
                return (false, "Positive prompt missing");
            }
            return (true, "");
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
                return ip.PositivePrompt;
            }
            return "";
        }

        public object DefaultPayloadForTrack()
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Image:
                    return DefaultPayloadForImageTrack();

                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public object DefaultPayloadForItem()
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Image:
                    return DefaultPayloadForImageItem();

                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public object CopyPayloadForTrack(object obj)
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Image:
                    return CopyPayloadForImageTrack(obj);

                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public object CopyPayloadForItem(object obj)
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Image:
                    return CopyPayloadForImageItem(obj);

                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public (bool payloadOk, string reasonIfNot) ValidatePayload(object payload)
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Image:
                    return ValidateImagePayload(payload);

                case IPluginBase.TrackType.Audio:
                    return (true, "");

                default:
                    break;
            }
            return (true, "");
        }

        public List<string> FilePathsOnPayloads(object trackPayload, object itemPayload)
        {
            return [];
        }

        public void ReplaceFilePathsOnPayloads(List<string> originalPath, List<string> newPath, object trackPayload, object itemPayload)
        {
        }

        public void AppendToPayloadFromLyrics(string text, object payload)
        {
            if (payload is ItemPayload ip)
            {
                ip.PositivePrompt = text;
            }
        }
    }
}