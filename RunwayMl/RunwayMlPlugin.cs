using PluginBase;
using System.Reactive.Disposables;
using System.Text.Json.Nodes;

namespace RunwayMlPlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class RunwayMlImgToVidPlugin : IVideoPlugin, ISaveAndRefresh, IImportFromLyrics, IImportFromImage, IRequestContentUploader, ITextualProgressIndication,
        IImportFromVideo, IImagePlugin, IDisposable
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

        public async Task<ImageResponse> GetImage(object trackPayload, object itemsPayload)
        {
            if (JsonHelper.DeepCopy<ImageTrackPayload>(trackPayload) is ImageTrackPayload newTp && JsonHelper.DeepCopy<ImageItemPayload>(itemsPayload) is ImageItemPayload newIp)
            {
            }
            return new ImageResponse { Success = false, ErrorMsg = "Unknown error" };
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
            if (CurrentTrackType == IPluginBase.TrackType.Video)
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
            }

            return Array.Empty<string>();
        }

        public object DeserializePayload(string fileName)
        {
            return JsonHelper.Deserialize<TrackPayload>(fileName);
        }

        private CompositeDisposable _disposable = new CompositeDisposable();

        public IPluginBase CreateNewInstance()
        {
            var plug = new RunwayMlImgToVidPlugin();
            _disposable.Add(ImageTrackPayload.Refresh.Subscribe(_ =>
            {
                saveAndRefreshCallback?.Invoke();
            }));

            _disposable.Add(ImageItemPayload.Refresh.Subscribe(_ =>
            {
                saveAndRefreshCallback?.Invoke();
            }));
            return plug;
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

        private (bool payloadOk, string reasonIfNot) ValidateImagePayload(object payload)
        {
            // TODO: Se tuplavalidointi kun saadaan kumpaankin kahva
            if (payload is ImageItemPayload tp)
            {
                return (!string.IsNullOrEmpty(tp.Prompt), "Image prmpt empty");
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
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                return new ItemPayload() { Prompt = text };
            }

            return new ImageItemPayload() { Prompt = text };
        }

        public object ItemPayloadFromImageSource(string imgSource)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                return new ItemPayload() { ImageSource = imgSource };
            }

            var imagePl = new ImageItemPayload();
            imagePl.ReferenceImages.Add(new ImagePayloadReference() { FilePath = imgSource });
            return imagePl;
        }

        public void ContentUploaderProvided(IContentUploader uploader)
        {
            _contentUploader = uploader;
        }

        public object ObjectToItemPayload(JsonObject obj)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                return JsonHelper.ToExactType<ItemPayload>(obj);
            }
            return JsonHelper.ToExactType<ImageItemPayload>(obj);
        }

        public object ObjectToTrackPayload(JsonObject obj)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                return JsonHelper.ToExactType<TrackPayload>(obj);
            }
            return JsonHelper.ToExactType<ImageTrackPayload>(obj);
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

            if (itemPayload is ImageItemPayload ipi)
            {
                return ipi.Prompt;
            }
            return "";
        }

        public object DefaultPayloadForTrack()
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Video:
                    return new TrackPayload();

                case IPluginBase.TrackType.Image:
                    return new ImageTrackPayload();

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
                    return new ItemPayload();

                case IPluginBase.TrackType.Image:
                    return new ImageItemPayload();

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
                    return JsonHelper.DeepCopy<TrackPayload>(obj);

                case IPluginBase.TrackType.Image:
                    return JsonHelper.DeepCopy<ImageTrackPayload>(obj);

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
                    return JsonHelper.DeepCopy<ItemPayload>(obj);

                case IPluginBase.TrackType.Image:
                    return JsonHelper.DeepCopy<ImageItemPayload>(obj);

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

                case IPluginBase.TrackType.Image:
                    return ValidateImagePayload(payload);

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
                return new List<string>() { ip.ImageSource, ip.VideoSource, tp.ReferenceImage, tp.ReferenceVideo };
            }

            if (trackPayload is ImageTrackPayload tpi && itemPayload is ImageItemPayload ipi)
            {
                return tpi.ReferenceImages.Select(s => s.FilePath).Concat(ipi.ReferenceImages.Select(s => s.FilePath)).ToList();
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

                    if (originalPath[i] == ip.VideoSource)
                    {
                        ip.VideoSource = newPath[i];
                    }

                    if (originalPath[i] == tp.ReferenceImage)
                    {
                        tp.ReferenceImage = newPath[i];
                    }

                    if (originalPath[i] == tp.ReferenceVideo)
                    {
                        tp.ReferenceVideo = newPath[i];
                    }
                }
            }

            if (trackPayload is ImageTrackPayload tpi && itemPayload is ImageItemPayload ipi)
            {
                for (int i = 0; i < originalPath.Count; i++)
                {
                    tpi.ReferenceImages.ForEach(s =>
                    {
                        if (s.FilePath == originalPath[i])
                        {
                            s.FilePath = newPath[i];
                        }
                    });

                    ipi.ReferenceImages.ForEach(s =>
                    {
                        if (s.FilePath == originalPath[i])
                        {
                            s.FilePath = newPath[i];
                        }
                    });
                }
            }
        }

        public object ItemPayloadFromVideoSource(string videoSource)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                return new ItemPayload() { VideoSource = videoSource };
            }
            return new ImageItemPayload();
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}