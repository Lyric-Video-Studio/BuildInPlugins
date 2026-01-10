using Avalonia.Controls;
using PluginBase;
using System.Linq;
using System.Text.Json.Nodes;

namespace FalAiPlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class FalAiImgToVidPlugin : IVideoPlugin, ISaveAndRefresh, IImportFromImage, IRequestContentUploader, ITextualProgressIndication,
        IImportFromVideo, IImagePlugin, IValidateBothPayloads, IAudioPlugin, ICancellableGeneration, IGenerationCost, IContentId, ITrackPayloadFromModel, IMenuSelectionOptionsForProperty
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
                reg.prompt = (newIp.Prompt + " " + newTp.Prompt).Trim();
                reg.negative_prompt = (newIp.NegativePrompt + " " + newTp.NegativePrompt).Trim();
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

                if (newIp.ShouldPropertyBeVisible(nameof(newIp.Seed), newTp, newIp))
                {
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

                if (newTp.ShouldPropertyBeVisible(nameof(newTp.ResolutionWan), newTp, newIp))
                {
                    reg.resolution = newTp.ResolutionWan;
                }

                if (newTp.Model.StartsWith("veo"))
                {
                    reg.duration = newIp.Duration;
                }

                if (newTp.ShouldPropertyBeVisible(nameof(newTp.GenerateAudio), newTp, newIp))
                {
                    reg.generate_audio = newTp.GenerateAudio;
                }

                if (newTp.ShouldPropertyBeVisible(nameof(newTp.ResolutionLtx), newTp, newIp))
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
                    reg.duration = newIp.DurationPixverse;
                }

                if (newTp.Model.StartsWith("veo"))
                {
                    reg.duration = newIp.DurationVeo;
                }

                if (newTp.Model.Contains("pixverse/v5.5"))
                {
                    reg.generate_audio_switch = newTp.GenerateAudio;
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
                }

                if (model == "editto")
                {
                    reg.resolution = "auto";
                }

                if (model.Contains("kling", StringComparison.InvariantCultureIgnoreCase))
                {
                    reg.duration = newIp.Duration;
                }

                if (model.Contains("kling-video/o1/", StringComparison.InvariantCultureIgnoreCase))
                {
                    reg.start_image_url = reg.first_frame_url;
                    reg.end_image_url = reg.last_frame_url;
                    reg.first_frame_url = null;
                    reg.last_frame_url = null;
                }

                if (model == "creatify/aurora")
                {
                    reg.resolution = "720p";
                }

                if (newTp.ShouldPropertyBeVisible(nameof(TrackPayload.AspectRatioWan26), newTp, newIp))
                {
                    reg.aspect_ratio = newTp.AspectRatioWan26;
                }

                if (newIp.ShouldPropertyBeVisible(nameof(ItemPayload.DurationWan26), newTp, newIp))
                {
                    reg.duration = newIp.DurationWan26;
                }

                if (!newTp.ShouldPropertyBeVisible(nameof(TrackPayload.FramesPerSecond), newTp, newIp))
                {
                    reg.frames_per_second = null;
                }

                if (!newTp.ShouldPropertyBeVisible(nameof(TrackPayload.NumberOfFrames), newTp, newIp))
                {
                    reg.num_frames = null;
                }

                if (!newTp.ShouldPropertyBeVisible(nameof(ItemPayload.DurationSeedream), newTp, newIp))
                {
                    reg.duration = newIp.DurationSeedream;
                }

                if (model.Contains("seedance/v1.5/pro/image-to-video") && reg.last_frame_url != null && !string.IsNullOrEmpty(reg.last_frame_url))
                {
                    reg.end_image_url = reg.last_frame_url;
                    reg.first_frame_url = null;
                    reg.last_frame_url = null;
                }

                if(model.Contains("one-to-all-animation"))
                {
                    reg.duration = null;
                }

                if (newIp.ShouldPropertyBeVisible(nameof(ItemPayload.CharacterOrientation), newTp, newIp))
                {
                    reg.character_orientation = newIp.CharacterOrientation;
                }

                var videoResp = await new Client().GetVideo(reg, folderToSaveVideo, _connectionSettings, itemsPayload as ItemPayload, saveAndRefreshCallback,
                    textualProgressAction, model, _ct);
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

                if (newIp.ShouldPropertyBeVisible(nameof(newIp.Seed), newTp, newIp))
                {
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
                }

                imageReg.prompt = newTp.Prompt + " " + newIp.Prompt;

                if (newTp.ShouldPropertyBeVisible(nameof(ImageTrackPayload.SizeImagen4), newTp, newIp))
                {
                    imageReg.aspect_ratio = newTp.SizeImagen4;
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

                if (newTp.ShouldPropertyBeVisible(nameof(ImageTrackPayload.BackGround), newTp, newIp))
                {
                    imageReg.background = newTp.BackGround;
                }

                if (newTp.ShouldPropertyBeVisible(nameof(ImageTrackPayload.SizeGpt15), newTp, newIp))
                {
                    imageReg.image_size = newTp.SizeGpt15;
                }

                if (newTp.ShouldPropertyBeVisible(nameof(ImageTrackPayload.WidthPx), newTp, newIp))
                {
                    imageReg.image_size_custom = new ImageSizeCustom() { height = newTp.HeigthPx, width = newTp.WidthPx };
                    imageReg.image_size = null;
                }

                return await new Client().GetImage(imageReg, _connectionSettings, itemsPayload as ImageItemPayload, saveAndRefreshCallback, textualProgressAction, newTp.Model, _ct);
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

        public Task<Dictionary<string, string[]>> MenuSelectionOptionsForProperty(string propertyName)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video && propertyName == nameof(TrackPayload.Model))
            {
                var output = new Dictionary<string, string[]>();
                output["openAi"] = ["sora-2/text-to-video", "sora-2/image-to-video"];
                output["Google"] = ["veo3.1", "veo3.1/fast", "veo3.1/image-to-video", "veo3.1/fast/image-to-video", "veo3.1/reference-to-video", "veo3.1/first-last-frame-to-video",
                                    "veo3", "veo3/fast", "veo3/image-to-video", "veo3/fast/image-to-video"];
                output["Minimax"] = ["minimax/hailuo-2.3-fast/standard/image-to-video", "minimax/hailuo-2.3-fast/pro/image-to-video",
                                    "minimax/hailuo-02-fast/image-to-video", "minimax/hailuo-02/pro/image-to-video", "minimax/hailuo-02/pro/text-to-video",
                                    "minimax/hailuo-02/standard/image-to-video", "minimax/hailuo-02/standard/text-to-video"];

                output["Wan"] = ["wan/v2.6/text-to-video", "wan/v2.6/image-to-video",
                                "wan-25-preview/text-to-video", "wan-25-preview/image-to-video",
                                "wan-alpha",
                                "wan/v2.2-a14b/image-to-video", "wan/v2.2-a14b/text-to-video", "wan/v2.2-14b/speech-to-video"];

                output["KlingAi"] = ["kling-video/ai-avatar/v2/pro", "kling-video/v2.6/pro/text-to-video", "kling-video/v2.6/pro/image-to-video", "kling-video/o1/image-to-video",
                            "kling-video/v2.6/pro/motion-control", "kling-video/v2.6/standard/motion-control", 
                            "kling-video/v2.5-turbo/pro/image-to-video", "kling-video/v2.5-turbo/pro/text-to-video",
                            "kling-video/v2.1/master/image-to-video", "kling-video/v2.1/master/text-to-video", 
                            "kling-video/v2.1/pro/image-to-video", "kling-video/v2.1/standard/image-to-video"];

                output["Ltxv"] = ["ltxv-2/text-to-video/fast", "ltxv-2/text-to-video", "ltxv-2/image-to-video/fast", "ltxv-2/image-to-video", "ltxv-13b-098-distilled/image-to-video"];
                output["Pixverse"] = ["pixverse/v5.5/text-to-video", "pixverse/v5.5/image-to-video", "pixverse/v5/image-to-video", "pixverse/v5/text-to-video"];
                output["Bytedance"] = ["bytedance/seedance/v1.5/pro/text-to-video", "bytedance/seedance/v1.5/pro/image-to-video", "bytedance/omnihuman/v1.5"];
                output["Upscale"] = ["seedvr/upscale/video"];
                output["Edit videos"] = ["lucy-edit/pro", "editto", "one-to-all-animation/1.3b", "one-to-all-animation/14b"];
                output["Misc"] = ["creatify/aurora"];
                return Task.FromResult(output);
            }
            else if(CurrentTrackType == IPluginBase.TrackType.Image && propertyName == nameof(ImageTrackPayload.Model))
            {
                var output = new Dictionary<string, string[]>();
                output["General"] = ["z-image/turbo", "ovis-image", "hidream-i1-full"];
                output["Qwen"] = ["qwen-image-2512", "qwen-image-edit-2511"];
                output["Google"] = ["imagen4/preview"];
                output["Wan"] = ["wan/v2.2-a14b/text-to-image", "wan-25-preview/text-to-image", "wan-25-preview/image-to-image"];
                output["Bytedance"] = ["bytedance/seedream/v4.5/text-to-image", "bytedance/seedream/v4/text-to-image", "bytedance/seedream/v4/edit"];
                output["OpenAi"] = ["gpt-image-1.5", "gpt-image-1.5/edit", "gpt-image-1-mini", "gpt-image-1-mini/edit"];


            }
            return Task.FromResult(new Dictionary<string, string[]>());
        }

        public async Task<string[]> SelectionOptionsForProperty(string propertyName)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                switch (propertyName)
                {
                    case nameof(TrackPayload.AspectRatio):
                        return ["16:9", "9:16", "1:1"];

                    case nameof(TrackPayload.AspectRatioWan26):
                        return ["16:9", "9:16", "1:1", "4:3", "3:4"];

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

                    case nameof(ItemPayload.DurationWan26):
                        return ["5", "10", "15"];
                    case nameof(ItemPayload.DurationSeedream):
                        return ["4", "5", "6", "7", "8", "9", "10", "11", "12"];

                    case nameof(TrackPayload.Style):
                        return ["anime", "3d_animation", "clay", "comic", "cyberpunk"];
                    case nameof(ItemPayload.CharacterOrientation):
                        return ["image", "video"];

                    default:
                        break;
                }
            }
            else if (CurrentTrackType == IPluginBase.TrackType.Image)
            {
                if (propertyName == nameof(ImageTrackPayload.SizeImagen4))
                {
                    return ["16:9", "9:16", "1:1", "3:4", "4:3"];
                }

                if (propertyName == nameof(ImageTrackPayload.SizeGpt15))
                {
                    return ["1024x1024", "1536x1024", "1024x1536"];
                }

                if (propertyName == nameof(ImageTrackPayload.BackGround))
                {
                    return ["auto", "transparent", "opaque"];
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

        private void SetupConnections(TrackPayload tp)
        {
            tp.ModelChanged += (s, e) =>
            {
                saveAndRefreshCallback?.Invoke(false);

                if (s is TrackPayload tp)
                {
                    GetPricingForModel(tp.Model);
                }
            };
        }

        private void SetupConnections(ImageTrackPayload tp)
        {
            tp.ModelChanged += (s, e) =>
            {
                saveAndRefreshCallback?.Invoke(false);

                if (s is ImageTrackPayload tp)
                {
                    GetPricingForModel(tp.Model);
                }
            };
        }

        private void GetPricingForModel(string model)
        {
            // Not doing anything because API not working...
            /*Task.Run(async () =>
            {
                if (_connectionSettings != null && !string.IsNullOrEmpty(_connectionSettings.AccessToken))
                {
                    var res = await Client.GetPrice(_connectionSettings, model);

                    if (res != null && res.prices != null && res.prices.Count > 1)
                    {
                        cost.Invoke(res.prices.FirstOrDefault().unit_price + res.prices.FirstOrDefault().currency);
                    }
                }
            });*/
            }

        public IPluginBase CreateNewInstance()
        {
            return new FalAiImgToVidPlugin();
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
                var tp = JsonHelper.ToExactType<TrackPayload>(obj);
                SetupConnections((TrackPayload)tp);
                return tp;
            }
            else if (CurrentTrackType == IPluginBase.TrackType.Audio)
            {
                return JsonHelper.ToExactType<AudioTrackPayload>(obj);
            }
            var tp1 = JsonHelper.ToExactType<ImageTrackPayload>(obj);
            SetupConnections((ImageTrackPayload)tp1);
            return tp1;
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
                    var tp = new TrackPayload();
                    SetupConnections(tp);
                    return tp;

                case IPluginBase.TrackType.Image:
                    var tp1 = new ImageTrackPayload();
                    SetupConnections(tp1);
                    return tp1;

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
                    var tp = JsonHelper.DeepCopy<TrackPayload>(obj);
                    SetupConnections(tp);
                    return tp;

                case IPluginBase.TrackType.Image:
                    var tp1 = JsonHelper.DeepCopy<ImageTrackPayload>(obj);
                    SetupConnections(tp1);
                    return tp1;

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

        private CancellationToken _ct;

        public void SetCancallationToken(CancellationToken cancellationToken)
        {
            _ct = cancellationToken;
        }

        private Action<string> cost;

        public void SetShowCostAction(Action<string> cost)
        {
            this.cost = cost;
        }

        public void UserDataDeleteRequested()
        {
            _connectionSettings.DeleteTokens();
        }

        public string GetContentFromPayloadId(object payload)
        {
            if (payload is ItemPayload ip)
            {
                return ip.PollingId;
            }

            if (payload is ImageItemPayload imageItemPayload)
            {
                return imageItemPayload.PollingId;
            }

            if (payload is AudioItemPayload aip)
            {
                return aip.PollingId;
            }

            return "";
        }

        public object TrackPayloadFromModel(string model)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Image)
            {
                return new ImageTrackPayload() { Model = model };
            }

            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                return new TrackPayload() { Model = model };
            }
            return null;
        }        
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}