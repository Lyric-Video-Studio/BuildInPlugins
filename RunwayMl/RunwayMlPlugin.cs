using PluginBase;
using System.Text.Json.Nodes;

namespace RunwayMlPlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class RunwayMlImgToVidPlugin : IVideoPlugin, ISaveAndRefresh, IImportFromImage, IRequestContentUploader, ITextualProgressIndication,
        IImportFromVideo, IImagePlugin, IValidateBothPayloads
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
                    saveAndRefreshCallback.Invoke(true);
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
                    newTp.Request.ratio = "1280:720";
                }

                if (newTp.Request.duration == -1)
                {
                    newTp.Request.duration = 5;
                }

                // Act2
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

                    var actualReferenceImage = string.IsNullOrEmpty(newIp.ReferenceImage) ? newTp.ReferenceImage : newIp.ReferenceImage;

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
                    else if (!string.IsNullOrEmpty(actualReferenceImage))
                    {
                        var characterImageUpload = await _contentUploader.RequestContentUpload(actualReferenceImage);
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
                        saveAndRefreshCallback.Invoke(true);
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
                else if (newTp.Request.model == models[2]) // Aleph
                {
                    var actualVideoSource = string.IsNullOrEmpty(newIp.VideoSource) ? newTp.ReferenceVideo : newIp.VideoSource;
                    if (string.IsNullOrEmpty(actualVideoSource))
                    {
                        return new VideoResponse() { Success = false, ErrorMsg = "Video file not defined" };
                    }

                    var videoFileUpload = await _contentUploader.RequestContentUpload(actualVideoSource);
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

                    var request = new AlephRequest();
                    request.videoUri = uploadedVideo;
                    request.model = newTp.Request.model;

                    foreach (var item in newIp.References)
                    {
                        var refUpload = await _contentUploader.RequestContentUpload(item.Path);
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
                            request.references.Add(new Reference() { type = "image", uri = refUpload.uploadedUrl });
                        }
                    }

                    if (newIp.Seed != 0)
                    {
                        request.seed = newIp.Seed;
                    }
                    else if (itemsPayload is ItemPayload ipOld)
                    {
                        ipOld.Seed = new Random().Next(1, int.MaxValue);
                        saveAndRefreshCallback.Invoke(true);
                        request.seed = ipOld.Seed;
                    }

                    request.promptText = newIp.Prompt;
                    request.ratio = newTp.Request.ratio;

                    // Aleph
                    var videoResp = await new Client().GetVideo(request, folderToSaveVideo,
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
                var imageReg = new ImageRequest();

                if (newIp.Seed != 0)
                {
                    imageReg.seed = newIp.Seed;
                }
                else if (itemsPayload is ImageItemPayload ipOld)
                {
                    ipOld.Seed = new Random().Next(1, int.MaxValue);
                    saveAndRefreshCallback.Invoke(true);
                    imageReg.seed = ipOld.Seed;
                }

                imageReg.promptText = newTp.Prompt + " " + newIp.Prompt;
                imageReg.ratio = newTp.Ratio;

                foreach (var refe in newTp.ReferenceImages.Concat(newIp.ReferenceImages))
                {
                    var nRef = new ImageReference();
                    nRef.tag = refe.Tag;

                    var upload = await _contentUploader.RequestContentUpload(refe.FilePath);
                    var uploadedReference = "";
                    if (upload.isLocalFile)
                    {
                        return new ImageResponse() { Success = false, ErrorMsg = "File must be public url or you must apply your content delivery credentials in Settings-view" };
                    }
                    else if (upload.responseCode != System.Net.HttpStatusCode.OK)
                    {
                        return new ImageResponse() { Success = false, ErrorMsg = $"Error uploading to content delivery: {upload.responseCode}" };
                    }
                    else
                    {
                        uploadedReference = upload.uploadedUrl;
                    }
                    nRef.uri = uploadedReference;
                    imageReg.referenceImages.Add(nRef);
                }

                return await new Client().GetImage(imageReg, _connectionSettings, itemsPayload as ImageItemPayload, saveAndRefreshCallback, textualProgressAction);
            }
            return new ImageResponse { Success = false, ErrorMsg = "Unknown error" };
        }

        public async Task<string> Initialize(object settings)
        {
            if (JsonHelper.DeepCopy<ConnectionSettings>(settings) is ConnectionSettings s)
            {
                _connectionSettings = s;
                _isInitialized = !string.IsNullOrEmpty(_connectionSettings.AccessToken);
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

        private static string[] models = ["act_two", "upscale_v1", "gen4_aleph", "gen4_turbo", "gen3a_turbo"]; // Remember, these are referenced by indexes!!!
        private static string[] ratios = ["1280:720", "720:1280", "1104:832", "832:1104", "960:960", "1584:672"];

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
            else
            {
                if (propertyName == nameof(ImageTrackPayload.Ratio))
                {
                    return ["1920:1080", "1080:1920", "1024:1024", "1360:768", "1080:1080", "1168:880", "1440:1080", "1080:1440",
                        "1808:768", "2112:912", "1280:720", "720:1280", "720:720", "960:720", "720:960", "1680:720"];
                }
            }

            return Array.Empty<string>();
        }

        public object DeserializePayload(string fileName)
        {
            return JsonHelper.Deserialize<TrackPayload>(fileName);
        }

        public IPluginBase CreateNewInstance()
        {
            var plug = new RunwayMlImgToVidPlugin();
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
                return (!string.IsNullOrEmpty(tp.Prompt), "Image prompt empty");
            }

            return (true, "");
        }

        private Action<bool> saveAndRefreshCallback;

        public void SetSaveAndRefreshCallback(Action<bool> saveAndRefreshCallback)
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

        public void AppendToPayloadFromLyrics(string text, object payload)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                if (payload is ItemPayload ip)
                {
                    ip.Prompt += text;
                }
            }
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
                return new List<string>() { ip.ImageSource, ip.VideoSource, tp.ReferenceImage, tp.ReferenceVideo }
                    .Concat(ip.References.Where(v => !string.IsNullOrEmpty(v.Path)).Select(s => s.Path)).ToList();
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

                    foreach (var r in ip.References)
                    {
                        if (originalPath[i] == r.Path)
                        {
                            r.Path = newPath[i];
                        }
                    }
                }
            }

            if (trackPayload is ImageTrackPayload tpi && itemPayload is ImageItemPayload ipi)
            {
                for (int i = 0; i < originalPath.Count; i++)
                {
                    tpi.ReferenceImages.ToList().ForEach(s =>
                    {
                        if (s.FilePath == originalPath[i])
                        {
                            s.FilePath = newPath[i];
                        }
                    });

                    ipi.ReferenceImages.ToList().ForEach(s =>
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

        public (bool payloadOk, string reasonIfNot) ValidatePayloads(object trackPaylod, object itemPayload)
        {
            if (trackPaylod is ImageTrackPayload ipt && itemPayload is ImageItemPayload ipi && ipt.ReferenceImages.Count + ipi.ReferenceImages.Count > 3)
            {
                return (false, "Too many references, three is maximum");
            }

            if (trackPaylod is TrackPayload tpv && itemPayload is ItemPayload ipv)
            {
                if (string.IsNullOrEmpty(ipv.ImageSource) && models.Skip(3).Contains(tpv.Request.model))
                {
                    return (false, $"Image source must not be empty (for models {string.Join(", ", models.Skip(3))})");
                }

                if (string.IsNullOrEmpty(ipv.VideoSource) && tpv.Request.model == models[1])
                {
                    return (false, $"Image source must not be empty (for model {models[1]})");
                }

                if (string.IsNullOrEmpty(ipv.Prompt) && string.IsNullOrEmpty(tpv.Request.promptText) && tpv.Request.model == models[2])
                {
                    return (false, $"Prompt must not be empty (for model {models[2]})");
                }

                if ((string.IsNullOrEmpty(ipv.VideoSource) && string.IsNullOrEmpty(tpv.ReferenceVideo)) && tpv.Request.model == models[2])
                {
                    return (false, $"Video source must not be empty (for model {models[2]})");
                }

                if (tpv.Request.model == models[0] && string.IsNullOrEmpty(tpv.ReferenceVideo) && string.IsNullOrEmpty(tpv.ReferenceImage) && string.IsNullOrEmpty(ipv.ReferenceImage))
                {
                    return (false, $"Either reference video or image must be set (for model {models[0]})");
                }

                if (tpv.Request.model == models[0] && string.IsNullOrEmpty(ipv.VideoSource))
                {
                    return (false, $"Item payload is missing movement / reference video (for model {models[0]})");
                }
            }

            return (true, "");
        }

        public void UserDataDeleteRequested()
        {
            if (_connectionSettings != null)
            {
                _connectionSettings.DeleteTokens();
            }
        }
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}