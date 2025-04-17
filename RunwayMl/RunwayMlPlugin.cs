using PluginBase;
using System.Text.Json.Nodes;

namespace RunwayMlPlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class RunwayMlImgToVidPlugin : IVideoPlugin, ISaveAndRefresh, IImportFromLyrics, IImportFromImage, IRequestContentUploader, ITextualProgressIndication
    {
        public string UniqueName { get => "RunwayMlImgToVidBuildIn"; }
        public string DisplayName { get => "Runway ML "; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Powered by runway ML. You need to have your authorization token";

        public bool AsynchronousGeneration { get; } = true;

        public string[] SettingsLinks => new[] { "https://dev.runwayml.com/", "https://Runwayml.com" };

        private ConnectionSettings _connectionSettings = new ConnectionSettings();
        private IContentUploader _contentUploader;

        public IPluginBase.TrackType CurrentTrackType { get; set; }

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

            if (JsonHelper.DeepCopy<TrackPayload>(trackPayload) is TrackPayload newTp && JsonHelper.DeepCopy<ItemPayload>(itemsPayload) is ItemPayload newIp)
            {
                // combine prompts

                // Also, when img2Vid

                newTp.Request.promptText = newIp.Request.promptText + " " + newTp.Request.promptText;

                if (newIp.Request.seed.HasValue && newIp.Request.seed != 0)
                {
                    newTp.Request.seed = newIp.Request.seed.Value;
                }
                else if (itemsPayload is ItemPayload ipOld)
                {
                    ipOld.Request.seed = new Random().Next(1, int.MaxValue);
                    saveAndRefreshCallback.Invoke();
                    newTp.Request.seed = ipOld.Request.seed;
                }

                if (string.IsNullOrEmpty(newIp.Request.ratio))
                {
                    newTp.Request.ratio = newIp.Request.ratio;
                }

                if (newIp.Request.duration != -1)
                {
                    newTp.Request.duration = newIp.Request.duration;
                }

                var fileUpload = await _contentUploader.RequestContentUpload(newIp.ImageSource);

                if (fileUpload.isLocalFile)
                {
                    return new VideoResponse() { Success = false, ErrorMsg = "File must be public url or you must apply your content delivery credentials in Settings-view" };
                }
                else if (fileUpload.responseCode != System.Net.HttpStatusCode.OK)
                {
                    return new VideoResponse() { Success = false, ErrorMsg = $"Error uploading to content delivery: {fileUpload.responseCode}" };
                }
                else
                {
                    newTp.Request.promptImage = fileUpload.uploadedUrl;
                    saveAndRefreshCallback.Invoke();
                }

                if (string.IsNullOrEmpty(newTp.Request.ratio))
                {
                    newTp.Request.ratio = "16:9";
                }

                if (newTp.Request.duration == -1)
                {
                    newTp.Request.duration = 5;
                }

                var videoResp = await new Client().GetImgToVid(newTp.Request, folderToSaveVideo, _connectionSettings, itemsPayload as ItemPayload, saveAndRefreshCallback, textualProgressAction);
                return videoResp;
            }
            else
            {
                return new VideoResponse { ErrorMsg = "Track playoad or item payload object not valid", Success = false };
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
            switch (propertyName)
            {
                case nameof(Request.ratio):
                    return ["", "16:9", "9:16"];

                case nameof(Request.duration):
                    return ["-1", "5", "10"];

                default:
                    break;
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
            return new RunwayMlImgToVidPlugin();
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

                if (string.IsNullOrEmpty(ip.ImageSource))
                {
                    return (false, "Image source must not be empty");
                }

                /*if (string.IsNullOrEmpty(ip.PathToImage))
                {
                    return (false, "No source");
                }

                if (!File.Exists(ip.PathToImage))
                {
                    return (false, $"Source file {ip.PathToImage} missing");
                }
                else
                {
                    try
                    {
                        var imageInfo = SKBitmap.Decode(ip.PathToImage);
                        var supportedSizes = new List<string>() { "1024x576", "576x1024", "768x768" };

                        var imageSizeAsString = $"{imageInfo.Width}x{imageInfo.Height}";

                        if (!supportedSizes.Any(s => s == imageSizeAsString))
                        {
                            return (false, $"Image is not correct size, supported sizes are: {string.Join(", ", supportedSizes)}, selected image was: {imageSizeAsString}");
                        }
                    }
                    catch (Exception ex)
                    {
                        return (false, ex.Message);
                    }
                }*/
            }
            return (true, "");
        }

        private Action saveAndRefreshCallback;

        public void SetSaveAndRefreshCallback(Action saveAndRefreshCallback)
        {
            this.saveAndRefreshCallback = saveAndRefreshCallback;
        }

        public object ItemPayloadFromLyrics(string text)
        {
            var output = new ItemPayload();
            output.Request.promptText = text;
            return output;
        }

        public object ItemPayloadFromImageSource(string imgSource)
        {
            var output = new ItemPayload();
            output.ImageSource = imgSource;
            return output;
        }

        public void ContentUploaderProvided(IContentUploader uploader)
        {
            _contentUploader = uploader;
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
                return ip.Request?.promptText;
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

        private Action<string> textualProgressAction;

        public void SetTextProgressCallback(Action<string> action)
        {
            textualProgressAction = action;
        }
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}