﻿using PluginBase;
using System.Text.Json.Nodes;

namespace StabilityAiTxtToImgPlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class StabilityAiTxtToImgPlugin : IImagePlugin, IImportFromLyrics
    {
        public string UniqueName { get => "StabilityAiTxtToImageBuildIn"; }
        public string DisplayName { get => "Stability Ai SD3 TxtToImg (stable diffusion)"; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Hosted by stability.ai. You need to have your authorization token";

        public string[] SettingsLinks => ["https://platform.stability.ai/account/keys"];

        public string ImageFormat => "png";

        public bool AsynchronousGeneration { get; } = true;

        private ConnectionSettings _connectionSettings = new ConnectionSettings();

        private StabilityAiWrapper _wrapper = new StabilityAiWrapper();

        public IPluginBase.TrackType CurrentTrackType { get; set; }

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
            if (_connectionSettings == null || string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return new ImageResponse { Success = false, ErrorMsg = "Uninitialized" };
            }

            if (JsonHelper.DeepCopy<TrackPayload>(trackPayload) is TrackPayload newTp && JsonHelper.DeepCopy<ItemPayload>(itemsPayload) is ItemPayload newIp)
            {
                newTp.Settings.prompt = $"{newIp.PositivePrompt} {newTp.Settings.prompt}";
                newTp.Settings.negative_prompt = $"{newIp.NegativePrompt} {newTp.Settings.negative_prompt}";

                newTp.Settings.prompt = newTp.Settings.prompt.Trim();
                newTp.Settings.negative_prompt = newTp.Settings.negative_prompt.Trim();

                newTp.Settings.seed = !string.IsNullOrEmpty(newIp.Seed) && newIp.Seed != "0" ? newIp.Seed : newTp.Settings.seed;
                return await _wrapper.GetTxtToImg(newTp.Settings, _connectionSettings);
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
                _isInitialized = true;
                _wrapper.InitializeClient();
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
            return JsonHelper.Deserialize<TrackPayload>(fileName);
        }

        public IPluginBase CreateNewInstance()
        {
            return new StabilityAiTxtToImgPlugin();
        }

        public object ItemPayloadFromLyrics(string lyric)
        {
            return new ItemPayload() { PositivePrompt = lyric };
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

        public (bool payloadOk, string reasonIfNot) ValidateImagePayload(object payload)
        {
            if (string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return (false, "Auth token missing");
            }

            if (payload is ItemPayload ip && string.IsNullOrEmpty(ip.PositivePrompt))
            {
                return (false, "Prompt missing");
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
                return ip?.PositivePrompt;
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
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}