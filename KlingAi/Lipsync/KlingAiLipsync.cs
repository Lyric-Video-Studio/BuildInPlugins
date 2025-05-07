using PluginBase;
using System.Text.Json.Nodes;

namespace KlingAiPlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class KlingAiLipSync : ITextualProgressIndication, IVideoPlugin, ISaveAndRefresh, IImportFromLyrics, IRequestContentUploader, IImportContentId, IImportFromVideo
    {
        public const string PluginName = "KlingAiImgToVidBuildInLipSync";
        public string UniqueName { get => PluginName; }
        public string DisplayName { get => "KlingAi Lipsync"; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Hosted by KlingAi. You need to have your authorization token";

        public bool AsynchronousGeneration { get; } = true;

        public string[] SettingsLinks => new[] {
            "https://klingai.com/global/dev/model/video#package",
            "https://docs.qingque.cn/s/home/eZQDvafJ4vXQkP8T9ZPvmye8S?identityId=2E3S0NySBQy",
            "https://console.klingai.com/console/access-control/accesskey-management" };

        public IPluginBase.TrackType CurrentTrackType { get; set; }

        private ConnectionSettings _connectionSettings = new ConnectionSettings();
        private Client _wrapper = new Client();

        public static int CurrentTasks = 0;

        public object DefaultPayloadForVideoItem()
        {
            return new ItemPayloadLipsync();
        }

        public object DefaultPayloadForVideoTrack()
        {
            return new TrackPayloadLipsync();
        }

        public async Task<VideoResponse> GetVideo(object trackPayload, object itemsPayload, string folderToSaveVideo)
        {
            if (_connectionSettings == null || string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return new VideoResponse { Success = false, ErrorMsg = "Uninitialized" };
            }

            while (CurrentTasks > 3)
            {
                await Task.Delay(1000);
            }

            CurrentTasks++;

            try
            {
                if (JsonHelper.DeepCopy<TrackPayloadLipsync>(trackPayload) is TrackPayloadLipsync newTp && JsonHelper.DeepCopy<ItemPayloadLipsync>(itemsPayload) is ItemPayloadLipsync newIp)
                {
                    // combine prompts

                    // Also, when img2Vid

                    newTp.Settings.Text = newIp.Text;

                    if (!string.IsNullOrEmpty(newIp.AudioFile))
                    {
                        var newUrl = await _uploader.RequestContentUpload(newIp.AudioFile);

                        if (newUrl.responseCode != System.Net.HttpStatusCode.OK)
                        {
                            return new VideoResponse { ErrorMsg = $"Failed to upload audio, response code: {newUrl.responseCode}", Success = false };
                        }

                        newTp.Settings.AudioUrl = newUrl.uploadedUrl;
                        newTp.Settings.Mode = "audio2video";
                    }
                    else
                    {
                        newTp.Settings.AudioType = "";
                    }

                    newTp.Settings.VideoId = newIp.InputVideoId;
                    if (!string.IsNullOrEmpty(newIp.InputVideoPath))
                    {
                        var newUrl = await _uploader.RequestContentUpload(newIp.InputVideoPath);

                        if (newUrl.responseCode != System.Net.HttpStatusCode.OK)
                        {
                            return new VideoResponse { ErrorMsg = $"Failed to upload audio, response code: {newUrl.responseCode}", Success = false };
                        }

                        newTp.Settings.VideoUrl = newUrl.uploadedUrl;
                        newTp.Settings.VideoId = "";
                    }
                    else
                    {
                        newTp.Settings.AudioType = "";
                    }

                    var voiceIndex = KlingLipsyncRequest.GetPrintableVoices().IndexOf(newTp.Settings.VoiceId);

                    if (voiceIndex >= 0)
                    {
                        newTp.Settings.VoiceId = KlingLipsyncRequest.AvailableVoices[voiceIndex, 0];
                        newTp.Settings.VoiceLanguage = KlingLipsyncRequest.AvailableVoices[voiceIndex, 1];
                    }
                    else
                    {
                        newTp.Settings.VoiceId = "";
                    }

                    return await _wrapper.GetImgToVid(newTp.Settings, folderToSaveVideo, _connectionSettings, itemsPayload as ItemPayloadLipsync, saveAndRefreshCallback, textProgressAction);
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
                CurrentTasks--;
            }
        }

        public async Task<string> Initialize(object settings)
        {
            if (JsonHelper.DeepCopy<ConnectionSettings>(settings) is ConnectionSettings s)
            {
                _connectionSettings = s;
                _isInitialized = !string.IsNullOrEmpty(s.AccessToken) && !string.IsNullOrEmpty(s.AccessSecret);
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

            if (propertyName == nameof(KlingLipsyncRequest.VoiceId))
            {
                switch (CurrentTrackType)
                {
                    case IPluginBase.TrackType.Video:
                        return [.. KlingLipsyncRequest.GetPrintableVoices()];

                    default:
                        break;
                }
            }

            return Array.Empty<string>();
        }

        public object CopyPayloadForVideoTrack(object obj)
        {
            if (JsonHelper.DeepCopy<TrackPayloadLipsync>(obj) is TrackPayloadLipsync set)
            {
                return set;
            }
            return DefaultPayloadForVideoTrack();
        }

        public object CopyPayloadForVideoItem(object obj)
        {
            if (JsonHelper.DeepCopy<ItemPayloadLipsync>(obj) is ItemPayloadLipsync set)
            {
                return set;
            }
            return DefaultPayloadForVideoItem();
        }

        public object DeserializePayload(string fileName)
        {
            return JsonHelper.Deserialize<TrackPayloadLipsync>(fileName);
        }

        public IPluginBase CreateNewInstance()
        {
            //throw new NotImplementedException("If you can see this, I'm sorry, something went horribly wrong :) This plugin should not be selectedble...");
            return new KlingAiLipSync();
        }

        public async Task<string> TestInitialization()
        {
            try
            {
                return ""; // TODO: jaa. oisko joku ping
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public (bool payloadOk, string reasonIfNot) ValidateVideoPayload(object payload)
        {
            if (payload is ItemPayloadLipsync ip)
            {
                if (string.IsNullOrEmpty(_connectionSettings.AccessToken))
                {
                    return (false, "Auth token empty!!!");
                }

                if (string.IsNullOrEmpty(ip.InputVideoId) && string.IsNullOrEmpty(ip.InputVideoPath))
                {
                    return (false, "Input video missing, use path or video file id of existing klingAi video");
                }

                if (string.IsNullOrEmpty(ip.Text) && string.IsNullOrEmpty(ip.AudioFile))
                {
                    return (false, "Text or audio file needed");
                }

                if (!string.IsNullOrEmpty(ip.Text) && !string.IsNullOrEmpty(ip.AudioFile))
                {
                    return (false, "Both text and audio is not supported");
                }
            }

            return (true, "");
        }

        private Action saveAndRefreshCallback;
        private IContentUploader _uploader;

        public void SetSaveAndRefreshCallback(Action saveAndRefreshCallback)
        {
            this.saveAndRefreshCallback = saveAndRefreshCallback;
        }

        public object ItemPayloadFromLyrics(string text)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Audio)
            {
                return null; // Not supported, should we somehow differentiate the iporting stuff between video, image and audio?
            }
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                return new ItemPayloadLipsync() { Text = text };
            }
            throw new NotImplementedException();
        }

        public void ContentUploaderProvided(IContentUploader uploader)
        {
            _uploader = uploader;
        }

        public object ObjectToItemPayload(JsonObject obj)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                var resp = JsonHelper.ToExactType<ItemPayloadLipsync>(obj);
                return resp;
            }

            throw new NotImplementedException();
        }

        public object ObjectToTrackPayload(JsonObject obj)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                var resp = JsonHelper.ToExactType<TrackPayloadLipsync>(obj);
                return resp;
            }

            throw new NotImplementedException();
        }

        public object ObjectToGeneralSettings(JsonObject obj)
        {
            return JsonHelper.ToExactType<ConnectionSettings>(obj);
        }

        public string TextualRepresentation(object itemPayload)
        {
            if (itemPayload is ItemPayloadLipsync ip)
            {
                return ip.Text;
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
            if (_connectionSettings == null)
            {
                return (false, "Uninitized");
            }

            if (string.IsNullOrEmpty(_connectionSettings.AccessSecret))
            {
                return (false, "Access secret missing");
            }

            if (string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return (false, "Access key");
            }

            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Video:
                    return ValidateVideoPayload(payload);

                default:
                    break;
            }
            return (true, "");
        }

        public bool CanImportFrom(string pluginUniqueName, IPluginBase.TrackType track)
        {
            return track == IPluginBase.TrackType.Video && pluginUniqueName == KlingAiImgToVidPlugin.PluginName;
        }

        public object ItemPayloadFromContentId(string id)
        {
            return new ItemPayloadLipsync() { InputVideoId = id };
        }

        private Action<string> textProgressAction;

        public void SetTextProgressCallback(Action<string> action)
        {
            textProgressAction = action;
        }

        public List<string> FilePathsOnPayloads(object trackPayload, object itemPayload)
        {
            var output = new List<string>();

            if (trackPayload is TrackPayloadLipsync ls)
            {
                output.Add(ls.Settings.VideoUrl);
                output.Add(ls.Settings.AudioUrl);
            }

            if (itemPayload is ItemPayloadLipsync iLs)
            {
                output.Add(iLs.AudioFile);
            }

            return output;
        }

        public void ReplaceFilePathsOnPayloads(List<string> originalPath, List<string> newPath, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayloadLipsync tp && itemPayload is ItemPayloadLipsync ip)
            {
                for (int i = 0; i < originalPath.Count; i++)
                {
                    if (originalPath[i] == tp.Settings.VideoUrl)
                    {
                        tp.Settings.VideoUrl = newPath[i];
                    }

                    if (originalPath[i] == tp.Settings.AudioUrl)
                    {
                        tp.Settings.AudioUrl = newPath[i];
                    }

                    if (originalPath[i] == ip.AudioFile)
                    {
                        ip.AudioFile = newPath[i];
                    }
                }
            }
        }

        public object ItemPayloadFromVideoSource(string videoSource)
        {
            return new ItemPayloadLipsync() { InputVideoPath = videoSource };
        }
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}