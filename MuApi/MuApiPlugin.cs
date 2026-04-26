using MuApiPlugin.Models.GptImage2;
using MuApiPlugin.Models.MidjourneyV8;
using MuApiPlugin.Models.Seedance2;
using MuApiPlugin.Models.ViduQ2Turbo;
using PluginBase;
using System.Text.Json.Nodes;
using static System.Net.WebRequestMethods;

namespace MuApiPlugin
{
#pragma warning disable CS1998
    public class MuApiVideoPlugin : IVideoPlugin, IImagePlugin, ISaveAndRefresh, IImportFromImage, IValidateBothPayloads, ICancellableGeneration, ITextualProgressIndication, ITrackPayloadFromModel
    {
        public string UniqueName => "MuApiBuildIn";

        public string DisplayName => "MuApi";

        public object GeneralDefaultSettings => new ConnectionSettings();

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Powered by MuApi. Add your MuApi API key, then generate images or videos with the currently selected model.";

        public string[] SettingsLinks => ["https://muapi.ai/access-keys"];

        public bool AsynchronousGeneration { get; } = true;

        public IPluginBase.TrackType CurrentTrackType { get; set; }

        private bool _isInitialized;
        private ConnectionSettings _connectionSettings = new();
        public static Action<bool> _saveAndRefreshCallback;
        public static Action<string> _textualProgressAction;
        public static CancellationToken _cancellationToken;

        public async Task<VideoResponse> GetVideo(object trackPayload, object itemsPayload, string folderToSaveVideo)
        {
            if (_connectionSettings == null || string.IsNullOrWhiteSpace(_connectionSettings.AccessToken))
            {
                return new VideoResponse() { Success = false, ErrorMsg = "Uninitialized" };
            }

            if (trackPayload is TrackPayload tp && itemsPayload is ItemPayload ip)
            {
                if (TrackPayload.IsSeedance2(tp))
                {
                    return await Seedance2VideoHandler.GetVideo(_connectionSettings, tp.Seedance2, ip.Seedance2, folderToSaveVideo, tp.Model);
                }
                if (TrackPayload.IsViduQ2Turbo(tp))
                {
                    return await ViduQ2TurboVideoHandler.GetVideo(_connectionSettings, tp.ViduQ2Turbo, ip.ViduQ2Turbo, folderToSaveVideo, tp.Model);
                }
            }

            throw new NotImplementedException();
        }

        public async Task<ImageResponse> GetImage(object trackPayload, object itemsPayload)
        {
            if (_connectionSettings == null || string.IsNullOrWhiteSpace(_connectionSettings.AccessToken))
            {
                return new ImageResponse() { Success = false, ErrorMsg = "Uninitialized" };
            }

            if (trackPayload is ImageTrackPayload tp && itemsPayload is ImageItemPayload ip)
            {
                if (ImageTrackPayload.IsGptImage2(tp))
                {
                    return await GptImage2ImageHandler.GetImage(_connectionSettings, tp.GptImage2, ip.GptImage2, tp.Model);
                }

                if (ImageTrackPayload.IsMidjourneyV8(tp))
                {
                    return await MidjourneyV8ImageHandler.GetImage(_connectionSettings, tp.MidjourneyV8, ip.MidjourneyV8, tp.Model);
                }
            }

            throw new NotImplementedException();
        }

        public async Task<string> Initialize(object settings)
        {
            if (JsonHelper.DeepCopy<ConnectionSettings>(settings) is ConnectionSettings typedSettings)
            {
                _connectionSettings = typedSettings;
                _isInitialized = !string.IsNullOrWhiteSpace(_connectionSettings.AccessToken);
                return "";
            }

            return "Connection settings object not valid";
        }

        public void CloseConnection()
        {
        }

        public async Task<string[]> SelectionOptionsForProperty(string propertyName)
        {
            return Array.Empty<string>();
        }

        public object DeserializePayload(string fileName)
        {
            return CurrentTrackType switch
            {
                IPluginBase.TrackType.Video => SetupConnections(JsonHelper.Deserialize<TrackPayload>(fileName) as TrackPayload),
                IPluginBase.TrackType.Image => SetupConnections(JsonHelper.Deserialize<ImageTrackPayload>(fileName) as ImageTrackPayload),
                _ => null
            };
        }

        public IPluginBase CreateNewInstance()
        {
            return new MuApiVideoPlugin();
        }

        public async Task<string> TestInitialization()
        {
            return string.IsNullOrWhiteSpace(_connectionSettings?.AccessToken) ? "API key missing" : "";
        }

        public (bool payloadOk, string reasonIfNot) ValidatePayload(object payload)
        {
            if (string.IsNullOrWhiteSpace(_connectionSettings?.AccessToken))
            {
                return (false, "API key missing");
            }

            if (payload is Seedance2ItemPayload omniItem && omniItem.Duration <= 0)
            {
                return (false, "Duration must be greater than zero");
            }
            if (payload is ViduQ2TurboItemPayload viduItem && viduItem.Duration <= 0)
            {
                return (false, "Duration must be greater than zero");
            }

            if (payload is ImageItemPayload gptImage2Payload && string.IsNullOrWhiteSpace(gptImage2Payload.GptImage2?.Prompt))
            {
                return (false, "Prompt missing");
            }

            return (true, "");
        }

        public (bool payloadOk, string reasonIfNot) ValidatePayloads(object trackPaylod, object itemPayload)
        {
            if (trackPaylod is TrackPayload track && itemPayload is ItemPayload item)
            {
                if (TrackPayload.IsSeedance2(track))
                {
                    if (string.IsNullOrWhiteSpace($"{track.Seedance2.Prompt} {item.Seedance2.Prompt}".Trim()))
                    {
                        return (false, "Prompt missing");
                    }

                    var imageCount = CountFiles(item.Seedance2.ImageReferences.ImageSources.Select(i => i.ImageFile));
                    if (imageCount > 9)
                    {
                        return (false, "MuApi supports up to 9 image references");
                    }

                    var videoCount = CountFiles(item.Seedance2.VideoReferences.VideoSources.Select(i => i.VideoFile));
                    if (videoCount > 3)
                    {
                        return (false, "MuApi supports up to 3 video references");
                    }

                    var audioCount = CountFiles(item.Seedance2.AudioReferences.AudioSources.Select(i => i.AudioFile));
                    if (audioCount > 3)
                    {
                        return (false, "MuApi supports up to 3 audio references");
                    }
                }                

                if (TrackPayload.IsViduQ2Turbo(track))
                {
                    if (item.ViduQ2Turbo.Duration <= 0)
                    {
                        return (false, "Duration must be greater than zero");
                    }

                    if (string.IsNullOrWhiteSpace($"{track.ViduQ2Turbo.Prompt} {item.ViduQ2Turbo.Prompt}".Trim()))
                    {
                        return (false, "Prompt missing");
                    }

                    if (track.ViduQ2Turbo.Bgm && item.ViduQ2Turbo.Duration != 4)
                    {
                        return (false, "Vidu Q2 Turbo requires duration 4 when background music is enabled");
                    }

                    if (track.Model == ViduQ2TurboTrackPayload.ModelI2V &&
                        string.IsNullOrWhiteSpace(item.ViduQ2Turbo.StartImage))
                    {
                        return (false, "Vidu Q2 Turbo image-to-video requires an input image");
                    }

                    if (track.Model == ViduQ2TurboTrackPayload.ModelStartEnd)
                    {
                        if (string.IsNullOrWhiteSpace(item.ViduQ2Turbo.StartImage) || string.IsNullOrWhiteSpace(item.ViduQ2Turbo.EndImage))
                        {
                            return (false, "Vidu Q2 Turbo start-end video requires both start and end images");
                        }
                    }
                }
            }

            if (trackPaylod is ImageTrackPayload imageTrack && itemPayload is ImageItemPayload imageItem)
            {
                if (ImageTrackPayload.IsGptImage2(imageTrack))
                {
                    if (string.IsNullOrWhiteSpace($"{imageTrack.GptImage2.Prompt} {imageItem.GptImage2.Prompt}".Trim()))
                    {
                        return (false, "Prompt missing");
                    }

                    var imageCount = CountFiles(imageTrack.GptImage2.ImageReferences.ImageSources.Select(i => i.ImageFile))
                        + CountFiles(imageItem.GptImage2.ImageReferences.ImageSources.Select(i => i.ImageFile));

                    if (imageTrack.Model == GptImage2TrackPayload.ModelImgToImg && imageCount == 0)
                    {
                        return (false, "At least one input image is required");
                    }

                    if (imageCount > 16)
                    {
                        return (false, "MuApi supports up to 16 input images");
                    }
                }

                if (ImageTrackPayload.IsMidjourneyV8(imageTrack))
                {
                    if (string.IsNullOrWhiteSpace($"{imageTrack.MidjourneyV8.Prompt} {imageItem.MidjourneyV8.Prompt}".Trim()))
                    {
                        return (false, "Prompt missing");
                    }

                    var imageCount = CountFiles(imageTrack.MidjourneyV8.ImageReferences.ImageSources.Select(i => i.ImageFile))
                        + CountFiles(imageItem.MidjourneyV8.ImageReferences.ImageSources.Select(i => i.ImageFile));

                    if (imageCount > 1)
                    {
                        return (false, "Midjourney V8 supports a single reference image");
                    }
                }
            }

            return (true, "");
        }

        public object ItemPayloadFromLyrics(string text)
        {
            return CurrentTrackType switch
            {
                IPluginBase.TrackType.Video => new ItemPayload(text, false),
                IPluginBase.TrackType.Image => new ImageItemPayload(text),
                _ => null
            };
        }

        public object ItemPayloadFromImageSource(string imgSource)
        {
            return CurrentTrackType switch
            {
                IPluginBase.TrackType.Video => new ItemPayload(imgSource, true),
                _ => null
            };
        }

        public void AppendToPayloadFromLyrics(string text, object payload)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                if (payload is not ItemPayload itemPayload)
                {
                    return;
                }
                itemPayload.Seedance2.Prompt += text;
                itemPayload.ViduQ2Turbo.Prompt += text;
            }
            else if (CurrentTrackType == IPluginBase.TrackType.Image)
            {
                if (payload is not ImageItemPayload imageItemPayload)
                {
                    return;
                }
                imageItemPayload.GptImage2.Prompt += text;
                imageItemPayload.MidjourneyV8.Prompt += text;
            }
        }

        public object ObjectToItemPayload(JsonObject obj)
        {
            return CurrentTrackType switch
            {
                IPluginBase.TrackType.Video => JsonHelper.ToExactType<ItemPayload>(obj),
                IPluginBase.TrackType.Image => JsonHelper.ToExactType<ImageItemPayload>(obj),
                _ => null
            };
        }

        public object ObjectToTrackPayload(JsonObject obj)
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Image:
                    return SetupConnections(JsonHelper.ToExactType<ImageTrackPayload>(obj) as ImageTrackPayload);
                case IPluginBase.TrackType.Video:
                    return SetupConnections(JsonHelper.ToExactType<TrackPayload>(obj) as TrackPayload);
                case IPluginBase.TrackType.Audio:
                    return null;
                default:
                    break;
            }
            return CurrentTrackType == IPluginBase.TrackType.Video ? JsonHelper.ToExactType<TrackPayload>(obj) : null;
        }

        public object ObjectToGeneralSettings(JsonObject obj)
        {
            return JsonHelper.ToExactType<ConnectionSettings>(obj);
        }

        public string TextualRepresentation(object itemPayload)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Image && itemPayload is ImageItemPayload typedImagePayload)
            {
                return typedImagePayload.GptImage2.Prompt
                    ?? typedImagePayload.MidjourneyV8.Prompt
                    ?? "";
            }

            if (itemPayload is ItemPayload typedPayload)
            {
                return typedPayload.Seedance2.Prompt ?? "";
            }

            return "";
        }

        public object DefaultPayloadForTrack()
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                return SetupConnections(new TrackPayload());
            }

            if (CurrentTrackType == IPluginBase.TrackType.Image)
            {
                return SetupConnections(new ImageTrackPayload());
            }

            throw new NotImplementedException();
        }

        public object DefaultPayloadForItem()
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                return new ItemPayload();
            }

            if (CurrentTrackType == IPluginBase.TrackType.Image)
            {
                return new ImageItemPayload();
            }

            throw new NotImplementedException();
        }

        public object CopyPayloadForTrack(object obj)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                return JsonHelper.DeepCopy<TrackPayload>(obj);
            }

            if (CurrentTrackType == IPluginBase.TrackType.Image)
            {
                return JsonHelper.DeepCopy<ImageTrackPayload>(obj);
            }

            throw new NotImplementedException();
        }

        public object CopyPayloadForItem(object obj)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                return JsonHelper.DeepCopy<ItemPayload>(obj);
            }

            if (CurrentTrackType == IPluginBase.TrackType.Image)
            {
                return JsonHelper.DeepCopy<ImageItemPayload>(obj);
            }

            throw new NotImplementedException();
        }

        public List<string> FilePathsOnPayloads(object trackPayload, object itemPayload)
        {
            if (trackPayload is ImageTrackPayload imageTrack && itemPayload is ImageItemPayload imageItem)
            {
                var output = new List<string>();
                output.AddRange(imageTrack.GptImage2.ImageReferences.ImageSources.Select(i => i.ImageFile));
                output.AddRange(imageItem.GptImage2.ImageReferences.ImageSources.Select(i => i.ImageFile));
                output.AddRange(imageTrack.MidjourneyV8.ImageReferences.ImageSources.Select(i => i.ImageFile));
                output.AddRange(imageItem.MidjourneyV8.ImageReferences.ImageSources.Select(i => i.ImageFile));
                return output;
            }

            if (itemPayload is ItemPayload typedPayload)
            {
                var output = new List<string>();
                output.AddRange(typedPayload.Seedance2.ImageReferences.ImageSources.Select(i => i.ImageFile));
                output.AddRange(typedPayload.Seedance2.AudioReferences.AudioSources.Select(i => i.AudioFile));
                output.AddRange(typedPayload.Seedance2.VideoReferences.VideoSources.Select(i => i.VideoFile));
                output.Add(typedPayload.ViduQ2Turbo.StartImage);
                output.Add(typedPayload.ViduQ2Turbo.EndImage);
                return output;
            }

            return [];
        }

        public void ReplaceFilePathsOnPayloads(List<string> originalPath, List<string> newPath, object trackPayload, object itemPayload)
        {
            if (trackPayload is ImageTrackPayload imageTrack && itemPayload is ImageItemPayload imageItem)
            {
                foreach (var imageRef in imageTrack.GptImage2.ImageReferences.ImageSources)
                {
                    imageRef.ImageFile = ReplacePath(imageRef.ImageFile, originalPath, newPath);
                }

                foreach (var imageRef in imageItem.GptImage2.ImageReferences.ImageSources)
                {
                    imageRef.ImageFile = ReplacePath(imageRef.ImageFile, originalPath, newPath);
                }

                foreach (var imageRef in imageTrack.MidjourneyV8.ImageReferences.ImageSources)
                {
                    imageRef.ImageFile = ReplacePath(imageRef.ImageFile, originalPath, newPath);
                }

                foreach (var imageRef in imageItem.MidjourneyV8.ImageReferences.ImageSources)
                {
                    imageRef.ImageFile = ReplacePath(imageRef.ImageFile, originalPath, newPath);
                }
                return;
            }

            if (itemPayload is not ItemPayload typedPayload)
            {
                return;
            }

            foreach (var seedanceImageItem in typedPayload.Seedance2.ImageReferences.ImageSources)
            {
                seedanceImageItem.ImageFile = ReplacePath(seedanceImageItem.ImageFile, originalPath, newPath);
            }

            foreach (var audioItem in typedPayload.Seedance2.AudioReferences.AudioSources)
            {
                audioItem.AudioFile = ReplacePath(audioItem.AudioFile, originalPath, newPath);
            }

            foreach (var videoItem in typedPayload.Seedance2.VideoReferences.VideoSources)
            {
                videoItem.VideoFile = ReplacePath(videoItem.VideoFile, originalPath, newPath);
            }
            typedPayload.ViduQ2Turbo.StartImage = ReplacePath(typedPayload.ViduQ2Turbo.StartImage, originalPath, newPath);
            typedPayload.ViduQ2Turbo.EndImage = ReplacePath(typedPayload.ViduQ2Turbo.EndImage, originalPath, newPath);
        }

        public void UserDataDeleteRequested()
        {
            _connectionSettings?.DeleteTokens();
        }

        public void SetSaveAndRefreshCallback(Action<bool> saveAndRefreshCallback)
        {
            _saveAndRefreshCallback = saveAndRefreshCallback;
        }

        public void SetCancallationToken(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        public void SetTextProgressCallback(Action<string> action)
        {
            _textualProgressAction = action;
        }

        private static int CountFiles(IEnumerable<string> additionalFiles)
        {
            return additionalFiles.Count(file => !string.IsNullOrWhiteSpace(file));
        }

        private static string ReplacePath(string currentPath, List<string> originalPath, List<string> newPath)
        {
            for (var i = 0; i < originalPath.Count; i++)
            {
                if (originalPath[i] == currentPath)
                {
                    return newPath[i];
                }
            }

            return currentPath;
        }

        private TrackPayload SetupConnections(TrackPayload tp)
        {
            tp.ModelChanged += (s, e) =>
            {
                _saveAndRefreshCallback?.Invoke(false);                
            };
            return tp;
        }

        private ImageTrackPayload SetupConnections(ImageTrackPayload tp)
        {
            tp.ModelChanged += (s, e) =>
            {
                _saveAndRefreshCallback?.Invoke(false);
            };
            return tp;
        }

        public object TrackPayloadFromModel(string model)
        {
            return CurrentTrackType switch
            {
                IPluginBase.TrackType.Video => SetupConnections(new TrackPayload() { Model = model }),
                IPluginBase.TrackType.Image => SetupConnections(new ImageTrackPayload() { Model = model }),
                _ => null
            };
        }
    }
#pragma warning restore CS1998
}
