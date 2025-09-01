using PluginBase;
using System.Text.Json.Nodes;

namespace FalAiPlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class FalAiImgToVidPlugin : IVideoPlugin, ISaveAndRefresh, IImportFromLyrics, IImportFromImage, IRequestContentUploader, ITextualProgressIndication,
        IImportFromVideo, IImagePlugin, IValidateBothPayloads, IAppendLyrics
    {
        public string UniqueName { get => "FalAiBuildIn"; }
        public string DisplayName { get => "Fal AI (multi-model)"; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Powered by Fal AI. You need to have your authorization token";

        public bool AsynchronousGeneration { get; } = true;

        public string[] SettingsLinks => new[] { "https://fal.ai/dashboard/keys", "https://fal.ai" };

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
                var reg = new VideoRequest();
                reg.prompt = newIp.Prompt + " " + reg.prompt;
                reg.negative_prompt = (newIp.NegativePrompt + " " + reg.negative_prompt).Trim();
                reg.aspect_ratio = newTp.AspectRatio;
                reg.resolution = newTp.Resolution;

                if (newTp.Model.StartsWith("wan"))
                {
                    reg.num_frames = newTp.NumberOfFrames;
                    reg.frames_per_second = newTp.FramesPerSecond;
                }

                if (newTp.Model.StartsWith("veo") || newTp.Model.StartsWith("wan") || newTp.Model.StartsWith("kling"))
                {
                    reg.aspect_ratio = newTp.AspectRatio;
                }

                if (newIp.Seed != 0)
                {
                    reg.seed = newIp.Seed;
                }
                else if (itemsPayload is ItemPayload ipOld)
                {
                    ipOld.Seed = new Random().Next(1, int.MaxValue);
                    saveAndRefreshCallback.Invoke();
                    reg.seed = ipOld.Seed;
                }

                var tempRes1 = await UploadSource(reg, newIp.ImageSource);

                if (!tempRes1.Success)
                {
                    return tempRes1;
                }
                else if (!string.IsNullOrEmpty(tempRes1.VideoFile))
                {
                    reg.image_url = tempRes1.VideoFile;
                }

                if (newTp.Model.StartsWith("minimax") && newTp.Model.Contains("standard"))
                {
                    reg.resolution = newTp.ResolutionMinimax;
                    reg.duration = newIp.DurationMinimax;
                }

                if (newTp.Model.StartsWith("wan"))
                {
                    reg.resolution = newTp.ResolutionWan;
                }

                if (newTp.Model.StartsWith("veo"))
                {
                    reg.duration = newIp.Duration;
                }

                if (newTp.Model.StartsWith("ltx"))
                {
                    reg.resolution = newTp.ResolutionLtx;
                }

                tempRes1 = await UploadSource(reg, newIp.AudioSource);

                if (!tempRes1.Success)
                {
                    return tempRes1;
                }
                else if (!string.IsNullOrEmpty(tempRes1.VideoFile))
                {
                    reg.audio_url = tempRes1.VideoFile;
                }

                if (newTp.Model.StartsWith("pixverse"))
                {
                    reg.style = newTp.Style;
                    reg.camera_movement = newTp.CameraMovement;
                    reg.duration = newIp.DurationPixverse;
                }

                var videoResp = await new Client().GetVideo(reg, folderToSaveVideo, _connectionSettings, itemsPayload as ItemPayload, saveAndRefreshCallback,
                    textualProgressAction, newTp.Model);
                return videoResp;
            }
            else
            {
                return new VideoResponse { ErrorMsg = "Track playoad or item payload object not valid", Success = false };
            }
        }

        private async Task<VideoResponse> UploadSource(VideoRequest reg, string imageSource)
        {
            if (!string.IsNullOrEmpty(imageSource))
            {
                var fileUpload = await _contentUploader.RequestContentUpload(imageSource);

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
                    return new VideoResponse() { Success = true, VideoFile = fileUpload.uploadedUrl };
                }
            }
            return new VideoResponse() { Success = true };
        }

        public async Task<ImageResponse> GetImage(object trackPayload, object itemsPayload)
        {
            if (JsonHelper.DeepCopy<ImageTrackPayload>(trackPayload) is ImageTrackPayload newTp && JsonHelper.DeepCopy<ImageItemPayload>(itemsPayload) is ImageItemPayload newIp)
            {
                var imageReg = new Request();

                if (newIp.Seed != 0)
                {
                    imageReg.seed = newIp.Seed;
                }
                else if (itemsPayload is ImageItemPayload ipOld)
                {
                    ipOld.Seed = new Random().Next(1, int.MaxValue);
                    saveAndRefreshCallback.Invoke();
                    imageReg.seed = ipOld.Seed;
                }

                imageReg.prompt = newTp.Prompt + " " + newIp.Prompt;

                switch (newTp.Model)
                {
                    case "qwen-image":
                    case "wan/v2.2-a14b/text-to-image":
                    case "hidream-i1-full":
                        imageReg.image_size = newTp.SizeQwen;
                        break;

                    case "imagen4/preview":
                        imageReg.aspect_ratio = newTp.SizeImagen4;
                        break;

                    default:
                        break;
                }

                return await new Client().GetImage(imageReg, _connectionSettings, itemsPayload as ImageItemPayload, saveAndRefreshCallback, textualProgressAction, newTp.Model);
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

        public async Task<string[]> SelectionOptionsForProperty(string propertyName)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                switch (propertyName)
                {
                    case nameof(TrackPayload.Model):
                        return ["veo3", "veo3/fast",
                            "minimax/hailuo-02-fast/image-to-video", "minimax/hailuo-02/pro/image-to-video", "minimax/hailuo-02/pro/text-to-video",
                                "minimax/hailuo-02/standard/image-to-video", "minimax/hailuo-02/standard/text-to-video",
                            "wan/v2.2-a14b/image-to-video", "wan/v2.2-a14b/text-to-video", "wan/v2.2-14b/speech-to-video",
                            "kling-video/v2.1/master/image-to-video", "kling-video/v2.1/master/text-to-video", "kling-video/v2.1/pro/image-to-video", "kling-video/v2.1/standard/image-to-video",
                            "ltxv-13b-098-distilled/image-to-video",
                            /*"pixverse/v5/image-to-video", "pixverse/v5/text-to-video"*/];

                    case nameof(TrackPayload.AspectRatio):
                        return ["16:9", "9:16", "1:1"];

                    case nameof(TrackPayload.Resolution):
                        return ["1080p", "720p"];

                    case nameof(TrackPayload.ResolutionMinimax):
                        return ["768P", "512P"];

                    case nameof(TrackPayload.ResolutionLtx):
                        return ["720p", "480p"];

                    case nameof(TrackPayload.ResolutionWan):
                        return ["720p", "580p", "480p"];

                    case nameof(ItemPayload.Duration):
                        return ["10", "5"];

                    case nameof(ItemPayload.DurationMinimax):
                        return ["10", "6"];

                    case nameof(ItemPayload.DurationPixverse):
                        return ["5", "8"];

                    case nameof(TrackPayload.Style):
                        return ["anime", "3d_animation", "clay", "comic", "cyberpunk"];

                    case nameof(TrackPayload.CameraMovement):
                        return ["horizontal_left", "horizontal_right", "vertical_up", "vertical_down",
                            "zoom_in", "zoom_out", "crane_up", "quickly_zoom_in", "quickly_zoom_out", "smooth_zoom_in",
                        "camera_rotation", "robo_arm", "super_dolly_out", "whip_pan", "hitchcock", "left_follow", "hitchcock", "right_follow", "pan_left", "pan_right", "fix_bg"];

                    default:
                        break;
                }
            }
            else
            {
                if (propertyName == nameof(ImageTrackPayload.SizeQwen))
                {
                    return ["landscape_16_9", "landscape_4_3", "portrait_16_9", "portrait_4_3", "square", "square_hd"];
                }

                if (propertyName == nameof(ImageTrackPayload.SizeImagen4))
                {
                    return ["16:9", "9:16", "1:1", "3:4", "4:3"];
                }

                if (propertyName == nameof(ImageTrackPayload.Model))
                {
                    return ["qwen-image", "imagen4/preview", "wan/v2.2-a14b/text-to-image", "hidream-i1-full"];
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
            var plug = new FalAiImgToVidPlugin();
            return plug;
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

        public (bool payloadOk, string reasonIfNot) ValidateVideoPayload(object payload)
        {
            if (payload is ItemPayload ip)
            {
                if (string.IsNullOrEmpty(_connectionSettings.AccessToken))
                {
                    return (false, "Auth token empty!!!");
                }
            }

            /*if (payload is TrackPayload tp)
            {
                if (tp.Request.model == models[3])
                {
                    if (tp.Request.ratio != ratios[^1] && tp.Request.ratio != ratios[^2])
                    {
                        return (false, $"{tp.Request.model} supports only  {ratios[^1]} & {ratios[^2]} ratios");
                    }
                }
            }*/

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
                return new ItemPayload() { /*ImageSource = imgSource*/ };
            }

            return null;
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
            /*if (trackPayload is TrackPayload tp && itemPayload is ItemPayload ip)
            {
                return new List<string>() { ip.ImageSource, ip.VideoSource, tp.ReferenceImage, tp.ReferenceVideo };
            }*/

            /*if (trackPayload is ImageTrackPayload tpi && itemPayload is ImageItemPayload ipi)
            {
                return tpi.ReferenceImages.Select(s => s.FilePath).Concat(ipi.ReferenceImages.Select(s => s.FilePath)).ToList();
            }*/

            return new List<string>();
        }

        public void ReplaceFilePathsOnPayloads(List<string> originalPath, List<string> newPath, object trackPayload, object itemPayload)
        {
            // No need to do anything
            /*if (trackPayload is TrackPayload tp && itemPayload is ItemPayload ip)
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
            }*/

            if (trackPayload is ImageTrackPayload tpi && itemPayload is ImageItemPayload ipi)
            {
                for (int i = 0; i < originalPath.Count; i++)
                {
                    /*tpi.ReferenceImages.ToList().ForEach(s =>
                    {
                        if (s.FilePath == originalPath[i])
                        {
                            s.FilePath = newPath[i];
                        }
                    });*/

                    /*ipi.ReferenceImages.ToList().ForEach(s =>
                    {
                        if (s.FilePath == originalPath[i])
                        {
                            s.FilePath = newPath[i];
                        }
                    });*/
                }
            }
        }

        public object ItemPayloadFromVideoSource(string videoSource)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                return new ItemPayload() { };
            }
            return new ImageItemPayload();
        }

        public (bool payloadOk, string reasonIfNot) ValidatePayloads(object trackPaylod, object itemPayload)
        {
            /*if (trackPaylod is TrackPayload tpv && itemPayload is ItemPayload ipv)
            {
                if (string.IsNullOrEmpty(ipv.ImageSource) && models.Skip(2).Contains(tpv.Request.model))
                {
                    return (false, $"Image source must not be empty (for models {string.Join(", ", models.Skip(2))})");
                }

                if (string.IsNullOrEmpty(ipv.VideoSource) && tpv.Request.model == models[1])
                {
                    return (false, $"Image source must not be empty (for model {models[1]})");
                }

                if (tpv.Request.model == models[0] && string.IsNullOrEmpty(tpv.ReferenceVideo) && string.IsNullOrEmpty(tpv.ReferenceImage) && string.IsNullOrEmpty(ipv.ReferenceImage))
                {
                    return (false, $"Either reference video or image must be set (for model {models[0]})");
                }

                if (tpv.Request.model == models[0] && string.IsNullOrEmpty(ipv.VideoSource))
                {
                    return (false, $"Item payload is missing movement / reference video (for model {models[0]})");
                }
            }*/

            return (true, "");
        }
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}