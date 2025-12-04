using PluginBase;
using System.Linq;
using System.Text.Json.Nodes;

namespace FalAiPlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class FalAiImgToVidPlugin : IVideoPlugin, ISaveAndRefresh, IImportFromImage, IRequestContentUploader, ITextualProgressIndication,
        IImportFromVideo, IImagePlugin, IValidateBothPayloads, IAudioPlugin
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
                //reg.aspect_ratio = newTp.AspectRatio;
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
                    saveAndRefreshCallback.Invoke(true);
                    reg.seed = ipOld.Seed;
                }

                var imageInput = string.IsNullOrEmpty(newIp.ImageSource) ? newTp.ImageSource : newIp.ImageSource;
                var tempRes1 = await UploadSource(imageInput);

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
                    reg.generate_audio = newTp.GenerateAudio;
                }

                if (newTp.Model.StartsWith("ltx") || newTp.Model.StartsWith("lucy-edit"))
                {
                    reg.resolution = newTp.ResolutionLtx;
                }

                if (newTp.Model.StartsWith("lucy-edit"))
                {
                    reg.seed = null;
                    reg.negative_prompt = null;
                }

                tempRes1 = await UploadSource(newIp.AudioSource);

                if (!tempRes1.Success)
                {
                    return tempRes1;
                }
                else if (!string.IsNullOrEmpty(tempRes1.VideoFile))
                {
                    reg.audio_url = tempRes1.VideoFile;
                }

                tempRes1 = await UploadSource(newIp.VideoSource);

                if (tempRes1.Success)
                {
                    reg.video_url = tempRes1.VideoFile;
                }

                if (newTp.Model.StartsWith("pixverse"))
                {
                    reg.style = newTp.Style;
                    reg.camera_movement = newTp.CameraMovement;
                    reg.duration = newIp.DurationPixverse;
                }

                if (newTp.Model.StartsWith("veo"))
                {
                    reg.duration = newIp.DurationVeo;
                }

                var model = newTp.Model;

                if (newTp.Model.Contains("sora", StringComparison.CurrentCultureIgnoreCase))
                {
                    reg.duration = null;
                    reg.durationInt = newIp.DurationSora;
                    reg.aspect_ratio = newTp.AspectRatioSora;
                    if (reg.resolution == "1080p")
                    {
                        model += "/pro";
                    }
                }

                if (model.Contains("upscale"))
                {
                    reg.upscale_factor = newIp.UpscaleFactor;
                }

                foreach (var img in newTp.ImageSourceCont.ImageSources.Concat(newIp.ImageSourceCont.ImageSources))
                {
                    if (reg.image_urls == null)
                    {
                        reg.image_urls = new List<string>();
                    }

                    var res = await UploadSource(img.FileSource);

                    if (!string.IsNullOrEmpty(res.VideoFile))
                    {
                        reg.image_urls.Add(res.VideoFile);
                    }
                }

                tempRes1 = await UploadSource(newIp.FirstFrame);

                if (tempRes1.Success)
                {
                    reg.first_frame_url = tempRes1.VideoFile;
                }

                tempRes1 = await UploadSource(newIp.LastFrame);

                if (tempRes1.Success)
                {
                    reg.last_frame_url = tempRes1.VideoFile;
                }

                if (model.Contains("ltxv-2"))
                {
                    reg.durationInt = newIp.DurationLtx2; // Use int based duration
                    reg.resolution = newTp.ResolutionLtx2;
                    reg.frames_per_second = newTp.FramesPerSecondLtx2;
                    reg.aspect_ratio = "16:9";
                    reg.generate_audio = newTp.GenerateAudio;
                }

                var videoResp = await new Client().GetVideo(reg, folderToSaveVideo, _connectionSettings, itemsPayload as ItemPayload, saveAndRefreshCallback,
                    textualProgressAction, model);
                return videoResp;
            }
            else
            {
                return new VideoResponse { ErrorMsg = "Track playoad or item payload object not valid", Success = false };
            }
        }

        public async Task<AudioResponse> GetAudio(object trackPayload, object itemsPayload, string folderToSaveAudio)
        {
            if (trackPayload is AudioTrackPayload ap && itemsPayload is AudioItemPayload ip)
            {
                var audioReg = new AudioRequest() { cfg_scale = ap.Cfg, script = $"{ap.Prompt} {ip.Prompt}".Trim(), speakers = new() };
                foreach (var item in ap.Speakers)
                {
                    var speaker = new SpeakerRequest() { preset = item.Preset };
                    var uploaded = await UploadSource(item.AudioFile);

                    if (!string.IsNullOrEmpty(uploaded.VideoFile))
                    {
                        speaker.url = uploaded.VideoFile;
                    }

                    audioReg.speakers.Add(speaker);
                }

                return await Client.GetAudio(audioReg, folderToSaveAudio, ip, _connectionSettings, ap.Model, saveAndRefreshCallback, textualProgressAction);
            }

            throw new Exception("Internal error");
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
                    saveAndRefreshCallback.Invoke(true);
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

                foreach (var img in newTp.ImageSource.ImageSources.Concat(newIp.ImageSources.ImageSources))
                {
                    if (imageReg.image_urls == null)
                    {
                        imageReg.image_urls = new List<string>();
                    }

                    var res = await UploadSource(img.FileSource);

                    if (!string.IsNullOrEmpty(res.VideoFile))
                    {
                        imageReg.image_urls.Add(res.VideoFile);
                    }
                }

                return await new Client().GetImage(imageReg, _connectionSettings, itemsPayload as ImageItemPayload, saveAndRefreshCallback, textualProgressAction, newTp.Model);
            }
            return new ImageResponse { Success = false, ErrorMsg = "Unknown error" };
        }

        private async Task<VideoResponse> UploadSource(string source)
        {
            if (!string.IsNullOrEmpty(source))
            {
                var fileUpload = await _contentUploader.RequestContentUpload(source);

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
                        return ["sora-2/text-to-video", "sora-2/image-to-video",
                            "veo3.1", "veo3.1/fast", "veo3.1/image-to-video", "veo3.1/fast/image-to-video", "veo3.1/reference-to-video", "veo3.1/first-last-frame-to-video",
                            "veo3", "veo3/fast", "veo3/image-to-video", "veo3/fast/image-to-video",
                            "minimax/hailuo-02-fast/image-to-video", "minimax/hailuo-02/pro/image-to-video", "minimax/hailuo-02/pro/text-to-video",
                                "minimax/hailuo-02/standard/image-to-video", "minimax/hailuo-02/standard/text-to-video",
                            "wan-25-preview/text-to-video", "wan-25-preview/image-to-video",
                            "wan-alpha",
                            "wan/v2.2-a14b/image-to-video", "wan/v2.2-a14b/text-to-video", "wan/v2.2-14b/speech-to-video",
                            "kling-video/v2.5-turbo/pro/image-to-video", "kling-video/v2.5-turbo/pro/text-to-video",
                            "kling-video/v2.1/master/image-to-video", "kling-video/v2.1/master/text-to-video", "kling-video/v2.1/pro/image-to-video", "kling-video/v2.1/standard/image-to-video",
                            "ltxv-2/text-to-video/fast", "ltxv-2/text-to-video", "ltxv-2/image-to-video/fast", "ltxv-2/image-to-video", "ltxv-13b-098-distilled/image-to-video",
                            "pixverse/v5/image-to-video", "pixverse/v5/text-to-video", "" +
                            "lucy-edit/pro",
                            "bytedance/omnihuman/v1.5",
                            "seedvr/upscale/video"];

                    case nameof(TrackPayload.AspectRatio):
                        return ["16:9", "9:16", "1:1"];

                    case nameof(TrackPayload.AspectRatioSora):
                        return ["16:9", "9:16"];

                    case nameof(TrackPayload.Resolution):
                        return ["1080p", "720p"];

                    case nameof(TrackPayload.ResolutionMinimax):
                        return ["768P", "512P"];

                    case nameof(TrackPayload.ResolutionLtx):
                        return ["720p", "480p"];

                    case nameof(TrackPayload.ResolutionLtx2):
                        return ["1080p", "1440p", "2160p"];

                    case nameof(TrackPayload.FramesPerSecondLtx2):
                        return ["25", "50"];

                    case nameof(TrackPayload.ResolutionWan):
                        return ["720p", "580p", "480p"];

                    case nameof(ItemPayload.Duration):
                        return ["10", "5"];

                    case nameof(ItemPayload.DurationMinimax):
                        return ["10", "6"];

                    case nameof(ItemPayload.DurationPixverse):
                        return ["5", "8"];

                    case nameof(ItemPayload.DurationVeo):
                        return ["8s"];

                    case nameof(ItemPayload.DurationLtx2):
                        return ["6", "8", "10"];

                    case nameof(ItemPayload.DurationSora):
                        return ["4", "8", "12"];

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
            else if (CurrentTrackType == IPluginBase.TrackType.Image)
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
                    return ["qwen-image", "qwen-image-edit-plus", "imagen4/preview", "wan/v2.2-a14b/text-to-image", "hidream-i1-full",
                        "wan-25-preview/text-to-image", "wan-25-preview/image-to-image",
                        "bytedance/seedream/v4/text-to-image", "bytedance/seedream/v4/edit",
                        "gpt-image-1-mini", "gpt-image-1-mini/edit"];
                }
            }
            else
            {
                if (propertyName == nameof(AudioTrackPayload.Model))
                {
                    return ["vibevoice/7b", "vibevoice"];
                }
                if (propertyName == nameof(Speaker.Preset))
                {
                    return ["Alice [EN]", "Alice [EN] (Background Music)", "Carter [EN]", "Frank [EN]", "Maya [EN]", "Anchen [ZH] (Background Music)", "Bowen [ZH]", "Xinran [ZH]"];
                }
            }

            return Array.Empty<string>();
        }

        public object DeserializePayload(string fileName)
        {
            return JsonHelper.Deserialize<TrackPayload>(fileName);
        }

        // Intentiaonally empty
        public FalAiImgToVidPlugin()
        {
        }

        private readonly bool _isActualInstance;

        public FalAiImgToVidPlugin(bool isActualInstance)
        {
            _isActualInstance = isActualInstance; // This is not actually used...
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                TrackPayload.ModelChanged += (s, e) =>
                {
                    saveAndRefreshCallback?.Invoke(false);
                };
            }

            if (CurrentTrackType == IPluginBase.TrackType.Image)
            {
                ImageTrackPayload.ModelChanged += (s, e) =>
                {
                    saveAndRefreshCallback?.Invoke(false);
                };
            }
        }

        public IPluginBase CreateNewInstance()
        {
            var plug = new FalAiImgToVidPlugin(true);

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

            if (CurrentTrackType == IPluginBase.TrackType.Audio)
            {
                return new AudioItemPayload() { Prompt = text };
            }

            return new ImageItemPayload() { Prompt = text };
        }

        public object ItemPayloadFromImageSource(string imgSource)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                return new ItemPayload() { ImageSource = imgSource };
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

            if (CurrentTrackType == IPluginBase.TrackType.Audio)
            {
                if (payload is AudioItemPayload ip)
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
            else if (CurrentTrackType == IPluginBase.TrackType.Audio)
            {
                return JsonHelper.ToExactType<AudioItemPayload>(obj);
            }
            return JsonHelper.ToExactType<ImageItemPayload>(obj);
        }

        public object ObjectToTrackPayload(JsonObject obj)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                return JsonHelper.ToExactType<TrackPayload>(obj);
            }
            else if (CurrentTrackType == IPluginBase.TrackType.Audio)
            {
                return JsonHelper.ToExactType<AudioTrackPayload>(obj);
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

            if (itemPayload is AudioItemPayload ipa)
            {
                return ipa.Prompt;
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

                case IPluginBase.TrackType.Audio:
                    return new AudioTrackPayload();

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

                case IPluginBase.TrackType.Audio:
                    return new AudioItemPayload();

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

                case IPluginBase.TrackType.Audio:
                    return JsonHelper.DeepCopy<AudioTrackPayload>(obj);

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

                case IPluginBase.TrackType.Audio:
                    return JsonHelper.DeepCopy<AudioItemPayload>(obj);

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

                case IPluginBase.TrackType.Audio:
                    {
                        if (payload is AudioItemPayload ip)
                        {
                            if (string.IsNullOrEmpty(ip.Prompt))
                            {
                                return (false, "Promp empty");
                            }

                            var speakerIndex = ip.Prompt.IndexOf("speaker", StringComparison.CurrentCultureIgnoreCase);
                            while (speakerIndex >= 0)
                            {
                                if (speakerIndex >= 0)
                                {
                                    if (speakerIndex > 0 && ip.Prompt[speakerIndex - 1] != '\n')
                                    {
                                        return (false, "Speaker must be also separated by new line");
                                    }
                                    speakerIndex = ip.Prompt.IndexOf("speaker", speakerIndex + 1, StringComparison.CurrentCultureIgnoreCase);
                                }
                            }
                        }
                        break;
                    }

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
                return new List<string>() { ip.ImageSource, ip.AudioSource };
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

                    if (originalPath[i] == ip.AudioSource)
                    {
                        ip.AudioSource = newPath[i];
                    }
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
            return (true, "");
        }
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}