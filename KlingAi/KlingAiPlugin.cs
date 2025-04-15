using PluginBase;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json.Nodes;

namespace KlingAiPlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class KlingAiImgToVidPlugin : IVideoPlugin, ISaveAndRefresh, IImportFromLyrics, IImportFromImage, IRequestContentUploader, IImagePlugin
    {
        public const string PluginName = "KlingAiImgToVidBuildIn";
        public string UniqueName { get => PluginName; }
        public string DisplayName { get => "KlingAi"; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Hosted by KlingAi. You need to have your authorization token";

        public bool AsynchronousGeneration { get; } = true;

        public string[] SettingsLinks => new[] { "https://klingai.com/global/dev/model/video#package", "https://console.klingai.com/console/access-control/accesskey-management" };

        public IPluginBase.TrackType CurrentTrackType { get; set; }

        private ConnectionSettings _connectionSettings = new ConnectionSettings();
        private Client _wrapper = new Client();

        public static int CurrentTasks = 0;

        public object DefaultPayloadForVideoItem()
        {
            return new ItemPayload();
        }

        public object DefaultPayloadForVideoTrack()
        {
            return new TrackPayload();
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
                if (JsonHelper.DeepCopy<TrackPayload>(trackPayload) is TrackPayload newTp && JsonHelper.DeepCopy<ItemPayload>(itemsPayload) is ItemPayload newIp)
                {
                    // combine prompts

                    // Also, when img2Vid

                    newTp.Settings.Prompt = (newIp.Prompt + " " + newTp.Settings.Prompt).Trim();
                    return await _wrapper.GetImgToVid(newTp.Settings, folderToSaveVideo, _connectionSettings, itemsPayload as ItemPayload, saveAndRefreshCallback);
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

        private Random rnd = new Random();

        public async Task<ImageResponse> GetImage(object trackPayload, object itemsPayload)
        {
            if (_connectionSettings == null || string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return new ImageResponse { Success = false, ErrorMsg = "Uninitialized" };
            }

            while (CurrentTasks > 3)
            {
                await Task.Delay(1000);
            }
            throw new NotImplementedException();
            /*CurrentTasks++;

            try
            {
                if (JsonHelper.DeepCopy<ImageTrackPayload>(trackPayload) is ImageTrackPayload newTp && JsonHelper.DeepCopy<ImageItemPayload>(itemsPayload) is ImageItemPayload newIp)
                {
                    // combine prompts

                    // Also, when img2Vid

                    newTp.Settings.prompt = newTp.Settings.prompt + " " + newIp.Prompt;

                    newTp.Settings.prompt = newTp.Settings.prompt.Trim();

                    // Upload to cloud first
                    if (!string.IsNullOrEmpty(newIp.CharacterRef))
                    {
                        newTp.CharacterReference = newIp.CharacterRef;
                    }

                    if (newIp.Seed > 0)
                    {
                        newTp.Settings.seed = newIp.Seed;
                    }

                    if (newTp.Settings.seed <= 0)
                    {
                        newTp.Settings.seed = rnd.NextInt64();
                        (itemsPayload as ImageItemPayload).Seed = newTp.Settings.seed;
                        saveAndRefreshCallback.Invoke();
                    }

                    return await _wrapper.GetImg(newTp.Settings, _connectionSettings);
                }
                else
                {
                    return new ImageResponse { ErrorMsg = "Track playoad or item payload object not valid", Success = false };
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                CurrentTasks--;
            }*/
        }

        public async Task<AudioResponse> GetAudio(object trackPayload, object itemsPayload, string folderToSaveAudio)
        {
            var fileName = Path.Combine(folderToSaveAudio, Guid.NewGuid() + ".wav");
            File.Copy("C:\\Users\\copyhere2\\Downloads\\Wistful Journey.wav", fileName);
            return new AudioResponse() { AudioFile = fileName, Success = true };
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

            if (propertyName == nameof(KlingVideoRequest.ModelName))
            {
                switch (CurrentTrackType)
                {
                    case IPluginBase.TrackType.Video:
                        return [/*"kling-v2", */"kling-v1-6"];

                    default:
                        break;
                }
            }

            if (propertyName == nameof(KlingVideoRequest.Mode))
            {
                switch (CurrentTrackType)
                {
                    case IPluginBase.TrackType.Video:
                        return ["std", "pro"];

                    default:
                        break;
                }
            }

            if (propertyName == nameof(KlingVideoRequest.AspectRatio))
            {
                switch (CurrentTrackType)
                {
                    case IPluginBase.TrackType.Video:
                        return ["16:9", "1:1", "9:16"];

                    default:
                        break;
                }
            }

            if (propertyName == nameof(KlingVideoRequest.Duration))
            {
                switch (CurrentTrackType)
                {
                    case IPluginBase.TrackType.Video:
                        return ["5", "10"];

                    default:
                        break;
                }
            }

            return Array.Empty<string>();
        }

        public object CopyPayloadForVideoTrack(object obj)
        {
            if (JsonHelper.DeepCopy<TrackPayload>(obj) is TrackPayload set)
            {
                return set;
            }
            return DefaultPayloadForVideoTrack();
        }

        public object CopyPayloadForVideoItem(object obj)
        {
            if (JsonHelper.DeepCopy<ItemPayload>(obj) is ItemPayload set)
            {
                return set;
            }
            return DefaultPayloadForVideoItem();
        }

        public object DeserializePayload(string fileName)
        {
            return JsonHelper.Deserialize<TrackPayload>(fileName);
        }

        public IPluginBase CreateNewInstance()
        {
            return new KlingAiImgToVidPlugin();
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
            if (payload is ItemPayload ip)
            {
                if (string.IsNullOrEmpty(_connectionSettings.AccessToken))
                {
                    return (false, "Auth token empty!!!");
                }
                if (string.IsNullOrEmpty(ip.Prompt))
                {
                    return (false, "Prompt empty");
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
                return new ItemPayload() { Prompt = text };
            }
            else
            {
                return new ImageItemPayload() { Prompt = text };
            }
        }

        public object ItemPayloadFromImageSource(string imgSource)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Audio)
            {
                return null; // Not supported, should we somehow differentiate the iporting stuff between video, image and audio?
            }
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                var output = new ItemPayload();
                //output.ImagePath = imgSource;
                return output;
            }
            else
            {
                return new ImageItemPayload() { CharacterRef = imgSource };
            }
        }

        public void ContentUploaderProvided(IContentUploader uploader)
        {
            _uploader = uploader;
        }

        public object ObjectToItemPayload(JsonObject obj)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                var resp = JsonHelper.ToExactType<ItemPayload>(obj);
                return resp;
            }

            return JsonHelper.ToExactType<ImageItemPayload>(obj);
        }

        public object ObjectToTrackPayload(JsonObject obj)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                var resp = JsonHelper.ToExactType<TrackPayload>(obj);
                return resp;
            }

            return JsonHelper.ToExactType<ImageTrackPayload>(obj);
        }

        public object ObjectToGeneralSettings(JsonObject obj)
        {
            return JsonHelper.ToExactType<ConnectionSettings>(obj);
        }

        // Image stuffs

        public object DefaultPayloadForImageTrack()
        {
            return new ImageTrackPayload();
        }

        public object DefaultPayloadForImageItem()
        {
            return new ImageItemPayload();
        }

        public object CopyPayloadForImageTrack(object obj)
        {
            if (obj is ImageTrackPayload ip)
            {
                return JsonHelper.DeepCopy<ImageTrackPayload>(ip);
            }
            return null;
        }

        public object CopyPayloadForImageItem(object obj)
        {
            if (obj is ImageItemPayload ip)
            {
                return JsonHelper.DeepCopy<ImageItemPayload>(ip);
            }
            return null;
        }

        public (bool payloadOk, string reasonIfNot) ValidateImagePayload(object payload)
        {
            if (payload is ImageItemPayload ip)
            {
                if (string.IsNullOrEmpty(_connectionSettings.AccessToken))
                {
                    return (false, "Auth token empty!!!");
                }

                if (string.IsNullOrEmpty(ip.Prompt))
                {
                    return (false, "Prompt empty");
                }
            }

            return (true, "");
        }

        public string TextualRepresentation(object itemPayload)
        {
            if (itemPayload is ItemPayload ip)
            {
                return ip.Prompt;
            }

            if (itemPayload is ImageItemPayload imgIp)
            {
                return imgIp.Prompt;
            }
            return "";
        }

        public object DefaultPayloadForTrack()
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Image:
                    return DefaultPayloadForImageTrack();

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
                case IPluginBase.TrackType.Image:
                    return DefaultPayloadForImageItem();

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
                case IPluginBase.TrackType.Image:
                    return CopyPayloadForImageTrack(obj);

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
                case IPluginBase.TrackType.Image:
                    return CopyPayloadForImageItem(obj);

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
                case IPluginBase.TrackType.Image:
                    return ValidateImagePayload(payload);

                case IPluginBase.TrackType.Video:
                    return ValidateVideoPayload(payload);

                case IPluginBase.TrackType.Audio:
                    return (true, "");

                default:
                    break;
            }
            return (true, "");
        }
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}