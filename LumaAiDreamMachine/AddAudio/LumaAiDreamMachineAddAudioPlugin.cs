﻿using LumaAiDreamMachinePlugin.VideoUpscale;
using PluginBase;
using System.Text.Json.Nodes;

namespace LumaAiDreamMachinePlugin.AddAudio
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class LumaAiDreamMachineGenerationAddAudioPlugin : IVideoPlugin, ISaveAndRefresh, IContentId, ITextualProgressIndication
    {
        public string UniqueName { get => "LumaAiDreamMachineGenerationAddAudtioBuildIn"; }
        public string DisplayName { get => "Dream Machine Add audio"; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Hosted by Luma AI. You need to have your authorization token";

        public bool AsynchronousGeneration { get; } = true;

        public string[] SettingsLinks => new[] { "https://lumalabs.ai/dream-machine/api/keys" };

        public IPluginBase.TrackType CurrentTrackType { get; set; }

        private ConnectionSettings _connectionSettings = new ConnectionSettings();
        private readonly Client client = new Client();

        public object DefaultPayloadForVideoItem()
        {
            return new GenerationAddAudioItemPayload();
        }

        public object DefaultPayloadForVideoTrack()
        {
            return new GenerationAddAudioTrackPayload();
        }

        public async Task<VideoResponse> GetVideo(object GenerationUpscaleTrackPayload, object itemsPayload, string folderToSaveVideo)
        {
            if (_connectionSettings == null || string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return new VideoResponse { Success = false, ErrorMsg = "Uninitialized" };
            }

            while (LumaAiDreamMachineImgToVidPlugin.CurrentTasks > 19)
            {
                await Task.Delay(1000);
            }

            LumaAiDreamMachineImgToVidPlugin.CurrentTasks++;

            try
            {
                if (JsonHelper.DeepCopy<GenerationAddAudioTrackPayload>(GenerationUpscaleTrackPayload) is GenerationAddAudioTrackPayload newTp &&
                    JsonHelper.DeepCopy<GenerationAddAudioItemPayload>(itemsPayload) is GenerationAddAudioItemPayload newIp)
                {
                    return await client.AddAudioToGeneration(newIp.GenerationId, newIp.Prompt + " " + newTp.Prompt, newIp.NegativePrompt + " " + newTp.NegativePrompt, folderToSaveVideo,
                        _connectionSettings, itemsPayload as GenerationAddAudioItemPayload, saveAndRefreshCallback, textualProgress);
                }
                else
                {
                    return new VideoResponse { ErrorMsg = "Track playoad or item payload object not valid", Success = false };
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                LumaAiDreamMachineImgToVidPlugin.CurrentTasks--;
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

        public async Task<string[]> SelectionOptionsForProperty(string propertyName)
        {
            if (_connectionSettings == null)
            {
                return Array.Empty<string>();
            }
            return Array.Empty<string>();
        }

        public object CopyPayloadForVideoTrack(object obj)
        {
            if (JsonHelper.DeepCopy<GenerationAddAudioTrackPayload>(obj) is GenerationAddAudioTrackPayload set)
            {
                return set;
            }
            return DefaultPayloadForVideoTrack();
        }

        public object CopyPayloadForVideoItem(object obj)
        {
            if (JsonHelper.DeepCopy<GenerationAddAudioItemPayload>(obj) is GenerationAddAudioItemPayload set)
            {
                return set;
            }
            return DefaultPayloadForVideoItem();
        }

        public object DeserializePayload(string fileName)
        {
            return JsonHelper.Deserialize<GenerationAddAudioTrackPayload>(fileName);
        }

        public IPluginBase CreateNewInstance()
        {
            return new LumaAiDreamMachineGenerationAddAudioPlugin();
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

        public (bool payloadOk, string reasonIfNot) ValidateVideoPayload(object payload)
        {
            if (payload is GenerationAddAudioItemPayload ip)
            {
                if (string.IsNullOrEmpty(_connectionSettings.AccessToken))
                {
                    return (false, "Auth token empty!!!");
                }
                if (string.IsNullOrEmpty(ip.GenerationId))
                {
                    return (false, "Generation id empty. Right click existing DreamMachine video item to copy id to clipboard");
                }

                if (string.IsNullOrEmpty(ip.Prompt))
                {
                    return (false, "Prompt empty");
                }
            }

            return (true, "");
        }

        private Action saveAndRefreshCallback;

        public void SetSaveAndRefreshCallback(Action saveAndRefreshCallback)
        {
            this.saveAndRefreshCallback = saveAndRefreshCallback;
        }

        public object ObjectToItemPayload(JsonObject obj)
        {
            return JsonHelper.ToExactType<GenerationAddAudioItemPayload>(obj);
        }

        public object ObjectToTrackPayload(JsonObject obj)
        {
            return JsonHelper.ToExactType<GenerationAddAudioTrackPayload>(obj);
        }

        public object ObjectToGeneralSettings(JsonObject obj)
        {
            return JsonHelper.ToExactType<ConnectionSettings>(obj);
        }

        public string TextualRepresentation(object GenerationUpscaleItemPayload)
        {
            /*if (GenerationUpscaleItemPayload is GenerationUpscaleItemPayload ip)
            {
                return ip.GenerationId;
            }*/

            return "";
        }

        public string GetContentFromPayloadId(object payload)
        {
            if (payload is GenerationAddAudioItemPayload ip)
            {
                return ip.PollingId;
            }
            return "";
        }

        public object DefaultPayloadForTrack()
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Video:
                    return DefaultPayloadForVideoTrack();

                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public object DefaultPayloadForItem()
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Video:
                    return DefaultPayloadForVideoItem();

                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public object CopyPayloadForTrack(object obj)
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Video:
                    return CopyPayloadForVideoTrack(obj);

                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public object CopyPayloadForItem(object obj)
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Video:
                    return CopyPayloadForVideoItem(obj);

                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public (bool payloadOk, string reasonIfNot) ValidatePayload(object payload)
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Video:
                    return ValidateVideoPayload(payload);

                case IPluginBase.TrackType.Audio:
                    return (true, "");

                default:
                    break;
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