using PluginBase;
using System.Text.Json.Nodes;

namespace RunwayMlPlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class RunwayMlImgToVidPlugin : IVideoPlugin, ISaveAndRefresh, IImportFromLyrics, IImportFromImage, IRequestContentUploader, ITextualProgressIndication, IImportFromVideo
    {
        public string UniqueName { get => "RunwayMlImgToVidBuildIn"; }
        public string DisplayName { get => "Runway ML"; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Powered by Runway ML. You need to have your authorization token";

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

                newTp.Request.promptText = newIp.Prompt + " " + newTp.Request.promptText;

                if (newIp.Seed != 0)
                {
                    newTp.Request.seed = newIp.Seed;
                }
                else if (itemsPayload is ItemPayload ipOld)
                {
                    ipOld.Seed = new Random().Next(1, int.MaxValue);
                    saveAndRefreshCallback.Invoke();
                    newTp.Request.seed = ipOld.Seed;
                }

                if (!string.IsNullOrEmpty(newIp.ImageSource))
                {
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
                    }
                }

                if (string.IsNullOrEmpty(newTp.Request.ratio))
                {
                    newTp.Request.ratio = "16:9";
                }

                if (newTp.Request.duration == -1)
                {
                    newTp.Request.duration = 5;
                }
                if (newTp.Request.model == models[0])
                {
                    var req = new Act2Request();
                    if (string.IsNullOrEmpty(newIp.VideoSource))
                    {
                        return new VideoResponse() { Success = false, ErrorMsg = "Video file not defined" };
                    }

                    var videoFileUpload = await _contentUploader.RequestContentUpload(newIp.VideoSource);
                    var uploadedVideo = "";
                    var uploadedCharacterVideo = "";
                    var uploadedCharacterImage = "";
                    if (videoFileUpload.isLocalFile)
                    {
                        return new VideoResponse() { Success = false, ErrorMsg = "File must be public url or you must apply your content delivery credentials in Settings-view" };
                    }
                    else if (videoFileUpload.responseCode != System.Net.HttpStatusCode.OK)
                    {
                        return new VideoResponse() { Success = false, ErrorMsg = $"Error uploading to content delivery: {videoFileUpload.responseCode}" };
                    }
                    else
                    {
                        uploadedVideo = videoFileUpload.uploadedUrl;
                    }

                    if (!string.IsNullOrEmpty(newTp.ReferenceVideo))
                    {
                        var characterVideoUpload = await _contentUploader.RequestContentUpload(newTp.ReferenceVideo);
                        if (characterVideoUpload.isLocalFile)
                        {
                            return new VideoResponse() { Success = false, ErrorMsg = "File must be public url or you must apply your content delivery credentials in Settings-view" };
                        }
                        else if (characterVideoUpload.responseCode != System.Net.HttpStatusCode.OK)
                        {
                            return new VideoResponse() { Success = false, ErrorMsg = $"Error uploading to content delivery: {characterVideoUpload.responseCode}" };
                        }
                        else
                        {
                            uploadedCharacterVideo = characterVideoUpload.uploadedUrl;
                        }
                    }
                    else if (!string.IsNullOrEmpty(newTp.ReferenceImage))
                    {
                        var characterImageUpload = await _contentUploader.RequestContentUpload(newTp.ReferenceImage);
                        if (characterImageUpload.isLocalFile)
                        {
                            return new VideoResponse() { Success = false, ErrorMsg = "File must be public url or you must apply your content delivery credentials in Settings-view" };
                        }
                        else if (characterImageUpload.responseCode != System.Net.HttpStatusCode.OK)
                        {
                            return new VideoResponse() { Success = false, ErrorMsg = $"Error uploading to content delivery: {characterImageUpload.responseCode}" };
                        }
                        else
                        {
                            uploadedCharacterImage = characterImageUpload.uploadedUrl;
                        }
                    }

                    if (!string.IsNullOrEmpty(uploadedCharacterImage))
                    {
                        req.character.uri = uploadedCharacterImage;
                        req.character.type = "image";
                    }
                    else if (!string.IsNullOrEmpty(uploadedCharacterVideo))
                    {
                        req.character.uri = uploadedCharacterVideo;
                        req.character.type = "video";
                    }
                    else
                    {
                        return new VideoResponse() { Success = false, ErrorMsg = "Character video or image is needed" };
                    }

                    req.reference.uri = uploadedVideo;
                    req.ratio = newTp.Request.ratio;
                    req.body_control = newIp.BodyControl;
                    req.expressionIntensity = newIp.ExpressionIntensity;

                    if (newIp.Seed != 0)
                    {
                        newTp.Request.seed = newIp.Seed;
                    }
                    else if (itemsPayload is ItemPayload ipOld)
                    {
                        ipOld.Seed = new Random().Next(1, int.MaxValue);
                        saveAndRefreshCallback.Invoke();
                        newTp.Request.seed = newTp.Request.seed;
                    }

                    req.seed = newTp.Request.seed.Value;

                    // Video upscale
                    var videoResp = await new Client().GetVideo(req, folderToSaveVideo,
                        _connectionSettings, itemsPayload as ItemPayload, saveAndRefreshCallback, textualProgressAction);
                    return videoResp;
                }
                else if (newTp.Request.model == models[1])
                {
                    if (string.IsNullOrEmpty(newIp.VideoSource))
                    {
                        return new VideoResponse() { Success = false, ErrorMsg = "Video file not defined" };
                    }

                    var videoFileUpload = await _contentUploader.RequestContentUpload(newIp.VideoSource);
                    var uploadedVideo = "";
                    if (videoFileUpload.isLocalFile)
                    {
                        return new VideoResponse() { Success = false, ErrorMsg = "File must be public url or you must apply your content delivery credentials in Settings-view" };
                    }
                    else if (videoFileUpload.responseCode != System.Net.HttpStatusCode.OK)
                    {
                        return new VideoResponse() { Success = false, ErrorMsg = $"Error uploading to content delivery: {videoFileUpload.responseCode}" };
                    }
                    else
                    {
                        uploadedVideo = videoFileUpload.uploadedUrl;
                    }

                    // Video upscale
                    var videoResp = await new Client().GetVideo(new VideoUpscaleRequest() { videoUri = uploadedVideo }, folderToSaveVideo,
                        _connectionSettings, itemsPayload as ItemPayload, saveAndRefreshCallback, textualProgressAction);
                    return videoResp;
                }
                else
                {
                    var videoResp = await new Client().GetVideo(newTp.Request, folderToSaveVideo, _connectionSettings, itemsPayload as ItemPayload, saveAndRefreshCallback, textualProgressAction);
                    return videoResp;
                }
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

        private static string[] models = ["act_two", "upscale_v1", "gen4_turbo", "gen3a_turbo"];
        private static string[] ratios = ["1280:720", "720:1280", "1104:832", "832:1104", "960:960", "1584:672", "1280:768", "768:1280"];

        public async Task<string[]> SelectionOptionsForProperty(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(Request.ratio):
                    return ratios;

                case nameof(Request.duration):
                    return ["-1", "5", "10"];

                case nameof(Request.model):
                    return models;

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
                    return (false, $"Image source must not be empty (for models {string.Join(", ", models.Skip(2))})");
                }

                if (string.IsNullOrEmpty(ip.VideoSource))
                {
                    return (false, $"Image source must not be empty (for model {models[1]})");
                }
            }

            if (payload is TrackPayload tp)
            {
                if (tp.Request.model == models[3])
                {
                    if (tp.Request.ratio != ratios[^1] && tp.Request.ratio != ratios[^2])
                    {
                        return (false, $"{tp.Request.model} supports only  {ratios[^1]} & {ratios[^2]} ratios");
                    }
                }
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
            output.Prompt = text;
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
                return ip.Prompt;
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

        public List<string> FilePathsOnPayloads(object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp && itemPayload is ItemPayload ip)
            {
                return new List<string>() { ip.ImageSource };
            }

            return new List<string>();
        }

        public void ReplaceFilePathsOnPayloads(List<string> originalPath, List<string> newPath, object trackPayload, object itemPayload)
        {
            // No need to do anything
            if (trackPayload is TrackPayload tp && itemPayload is ItemPayload ip)
            {
                for (int i = 0; i < originalPath.Count; i++)
                {
                    if (originalPath[i] == ip.ImageSource)
                    {
                        ip.ImageSource = newPath[i];
                    }
                }
            }
        }

        public object ItemPayloadFromVideoSource(string videoSource)
        {
            return new ItemPayload() { VideoSource = videoSource };
        }
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}