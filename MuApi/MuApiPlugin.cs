using MuApiPlugin.Models.GptImage2;
using MuApiPlugin.Models.GeminiOmni;
using MuApiPlugin.Models.HappyHorse1;
using MuApiPlugin.Models.MidjourneyV8;
using MuApiPlugin.Models.Seedance2;
using MuApiPlugin.Models.ViduQ2Turbo;
using PluginBase;
using System.Text.Json.Nodes;

namespace MuApiPlugin
{
#pragma warning disable CS1998
    public class MuApiVideoPlugin : IVideoPlugin, IImagePlugin, IAudioPlugin, ISaveAndRefresh, ISaveConnectionSettings, IImportFromImage, IValidateBothPayloads, ICancellableGeneration, ITextualProgressIndication, ITrackPayloadFromModel
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
        private Action<object> _saveConnectionSettingsCallback;

        public async Task<VideoResponse> GetVideo(object trackPayload, object itemsPayload, string folderToSaveVideo)
        {
            if (_connectionSettings == null || string.IsNullOrWhiteSpace(_connectionSettings.AccessToken))
            {
                return new VideoResponse() { Success = false, ErrorMsg = "Uninitialized" };
            }

            if (trackPayload is TrackPayload tp && itemsPayload is ItemPayload ip)
            {
                if (TrackPayload.IsGeminiOmni(tp))
                {
                    return await GeminiOmniVideoHandler.GetVideo(_connectionSettings, tp.GeminiOmni, ip.GeminiOmni, folderToSaveVideo, tp.Model, ip);
                }

                if (TrackPayload.IsSeedance2(tp))
                {
                    return await Seedance2VideoHandler.GetVideo(_connectionSettings, tp.Seedance2, ip.Seedance2, folderToSaveVideo, tp.Model, ip);
                }

                if (TrackPayload.IsHappyHorse1(tp))
                {
                    return await HappyHorse1VideoHandler.GetVideo(_connectionSettings, tp.HappyHorse1, ip.HappyHorse1, folderToSaveVideo, tp.Model, ip);
                }

                if (TrackPayload.IsViduQ2Turbo(tp))
                {
                    return await ViduQ2TurboVideoHandler.GetVideo(_connectionSettings, tp.ViduQ2Turbo, ip.ViduQ2Turbo, folderToSaveVideo, tp.Model, ip);
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
                if (ImageTrackPayload.IsGeminiOmniCharacter(tp))
                {
                    return await GeminiOmniCharacterImageHandler.GetImage(_connectionSettings, tp.GeminiOmniCharacter, ip.GeminiOmniCharacter, tp.Model, ip, _saveConnectionSettingsCallback);
                }

                if (ImageTrackPayload.IsGptImage2(tp))
                {
                    return await GptImage2ImageHandler.GetImage(_connectionSettings, tp.GptImage2, ip.GptImage2, tp.Model, ip);
                }

                if (ImageTrackPayload.IsMidjourneyV8(tp))
                {
                    return await MidjourneyV8ImageHandler.GetImage(_connectionSettings, tp.MidjourneyV8, ip.MidjourneyV8, tp.Model, ip);
                }
            }

            throw new NotImplementedException();
        }

        public async Task<AudioResponse> GetAudio(object trackPayload, object itemsPayload, string folderToSaveAudio)
        {
            if (_connectionSettings == null || string.IsNullOrWhiteSpace(_connectionSettings.AccessToken))
            {
                return new AudioResponse() { Success = false, ErrorMsg = "Uninitialized" };
            }

            if (trackPayload is AudioTrackPayload tp && itemsPayload is AudioItemPayload ip)
            {
                if (AudioTrackPayload.IsGeminiOmniAudio(tp))
                {
                    return await GeminiOmniAudioHandler.GetAudio(_connectionSettings, tp.GeminiOmniAudio, ip.GeminiOmniAudio, tp.Model, ip, _saveConnectionSettingsCallback);
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
            if (propertyName == nameof(GeminiOmniAudioTrackPayload.PresetVoiceId))
            {
                return GeminiOmniAudioTrackPayload.PresetVoiceIds;
            }

            if (propertyName is nameof(GeminiOmniItemPayload.AudioId1) or nameof(GeminiOmniItemPayload.AudioId2) or nameof(GeminiOmniItemPayload.AudioId3)
                or nameof(GeminiOmniCharacterItemPayload.AudioId1) or nameof(GeminiOmniCharacterItemPayload.AudioId2) or nameof(GeminiOmniCharacterItemPayload.AudioId3))
            {
                return [GeminiOmniProfileOptions.None, .. (_connectionSettings?.GetGeminiOmniAudioProfileNames() ?? Array.Empty<string>())];
            }

            if (propertyName is nameof(GeminiOmniItemPayload.CharacterId1) or nameof(GeminiOmniItemPayload.CharacterId2) or nameof(GeminiOmniItemPayload.CharacterId3))
            {
                return [GeminiOmniProfileOptions.None, .. (_connectionSettings?.GetGeminiOmniCharacterProfileNames() ?? Array.Empty<string>())];
            }

            return Array.Empty<string>();
        }

        public object DeserializePayload(string fileName)
        {
            return CurrentTrackType switch
            {
                IPluginBase.TrackType.Video => SetupConnections(JsonHelper.Deserialize<TrackPayload>(fileName) as TrackPayload),
                IPluginBase.TrackType.Image => SetupConnections(JsonHelper.Deserialize<ImageTrackPayload>(fileName) as ImageTrackPayload),
                IPluginBase.TrackType.Audio => SetupConnections(JsonHelper.Deserialize<AudioTrackPayload>(fileName) as AudioTrackPayload),
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

            if (payload is GeminiOmniItemPayload geminiOmniItem && geminiOmniItem.Duration <= 0)
            {
                return (false, "Duration must be greater than zero");
            }

            if (payload is HappyHorse1ItemPayload happyHorseItem && happyHorseItem.Duration <= 0)
            {
                return (false, "Duration must be greater than zero");
            }

            if (payload is ViduQ2TurboItemPayload viduItem && viduItem.Duration <= 0)
            {
                return (false, "Duration must be greater than zero");
            }

            if (payload is AudioItemPayload audioPayload)
            {
                return ValidateGeminiOmniAudioPayload(audioPayload.GeminiOmniAudio, null);
            }

            if (payload is GeminiOmniAudioItemPayload geminiOmniAudioPayload)
            {
                return ValidateGeminiOmniAudioPayload(geminiOmniAudioPayload, null);
            }

            return (true, "");
        }

        public (bool payloadOk, string reasonIfNot) ValidatePayloads(object trackPaylod, object itemPayload)
        {
            if (trackPaylod is TrackPayload track && itemPayload is ItemPayload item)
            {
                if (TrackPayload.IsGeminiOmni(track))
                {
                    if (item.GeminiOmni.Duration <= 0)
                    {
                        return (false, "Duration must be greater than zero");
                    }

                    if (string.IsNullOrWhiteSpace($"{track.GeminiOmni.Prompt} {item.GeminiOmni.Prompt}".Trim()))
                    {
                        return (false, "Prompt missing");
                    }

                    var audioIdCount = CountIds(item.GeminiOmni.AudioId1, item.GeminiOmni.AudioId2, item.GeminiOmni.AudioId3);
                    if (audioIdCount > 3)
                    {
                        return (false, "Gemini Omni supports up to 3 audio IDs");
                    }

                    var characterIdCount = CountIds(item.GeminiOmni.CharacterId1, item.GeminiOmni.CharacterId2, item.GeminiOmni.CharacterId3);
                    if (characterIdCount > 3)
                    {
                        return (false, "Gemini Omni supports up to 3 character IDs");
                    }

                    if (track.Model == GeminiOmniTrackPayload.ModelI2V)
                    {
                        var imageCount = CountFiles(track.GeminiOmni.ImageReferences.ImageSources.Select(i => i.ImageFile))
                            + CountFiles(item.GeminiOmni.ImageReferences.ImageSources.Select(i => i.ImageFile));

                        if (imageCount == 0)
                        {
                            return (false, "Gemini Omni image-to-video requires at least one input image");
                        }

                        if (imageCount > 5)
                        {
                            return (false, "Gemini Omni image-to-video supports up to 5 input images");
                        }
                    }
                }

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

                if (TrackPayload.IsHappyHorse1(track))
                {
                    if (item.HappyHorse1.Duration <= 0)
                    {
                        return (false, "Duration must be greater than zero");
                    }

                    if (string.IsNullOrWhiteSpace($"{track.HappyHorse1.Prompt} {item.HappyHorse1.Prompt}".Trim()))
                    {
                        return (false, "Prompt missing");
                    }

                    if (track.Model == HappyHorse1TrackPayload.ModelI2V1080p)
                    {
                        var imageCount = CountFiles(track.HappyHorse1.ImageReferences.ImageSources.Select(i => i.ImageFile))
                            + CountFiles(item.HappyHorse1.ImageReferences.ImageSources.Select(i => i.ImageFile));

                        if (imageCount == 0)
                        {
                            return (false, "Happy Horse 1 image-to-video requires an input image");
                        }

                        if (imageCount > 1)
                        {
                            return (false, "Happy Horse 1 image-to-video supports a single input image");
                        }
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

            if (trackPaylod is AudioTrackPayload audioTrack && itemPayload is AudioItemPayload audioItem)
            {
                if (AudioTrackPayload.IsGeminiOmniAudio(audioTrack))
                {
                    if (string.IsNullOrWhiteSpace(audioTrack.GeminiOmniAudio.PresetVoiceId))
                    {
                        return (false, "Base preset voice missing");
                    }

                    return ValidateGeminiOmniAudioPayload(audioItem.GeminiOmniAudio, audioTrack.GeminiOmniAudio);
                }
            }

            if (trackPaylod is ImageTrackPayload imageTrack && itemPayload is ImageItemPayload imageItem)
            {
                if (ImageTrackPayload.IsGeminiOmniCharacter(imageTrack))
                {
                    if (string.IsNullOrWhiteSpace(imageItem.GeminiOmniCharacter.Descriptions))
                    {
                        return (false, "Character description missing");
                    }

                    var imageCount = CountFiles(imageItem.GeminiOmniCharacter.ImageReferences.ImageSources.Select(i => i.ImageFile));
                    if (imageCount != 1)
                    {
                        return (false, "Gemini Omni Character requires exactly one reference image");
                    }

                    var audioIdCount = CountIds(imageItem.GeminiOmniCharacter.AudioId1, imageItem.GeminiOmniCharacter.AudioId2, imageItem.GeminiOmniCharacter.AudioId3);
                    if (audioIdCount > 3)
                    {
                        return (false, "Gemini Omni Character supports up to 3 audio IDs");
                    }
                }

                if (ImageTrackPayload.IsGptImage2(imageTrack))
                {
                    if (string.IsNullOrWhiteSpace($"{imageTrack.GptImage2.Prompt} {imageItem.GptImage2.Prompt}".Trim()))
                    {
                        return (false, "Prompt missing");
                    }

                    var aspectRatio = imageTrack.GptImage2.AspectRatio?.Trim();
                    var resolution = imageTrack.GptImage2.Resolution?.Trim();

                    if ((string.IsNullOrWhiteSpace(aspectRatio) || aspectRatio.Equals("auto", StringComparison.OrdinalIgnoreCase)) &&
                        !string.Equals(resolution, "1K", StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, "GPT Image 2 requires resolution 1K when aspect ratio is auto");
                    }

                    if (string.Equals(aspectRatio, "1:1", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(resolution, "4K", StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, "GPT Image 2 does not support 4K with aspect ratio 1:1");
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
                IPluginBase.TrackType.Audio => new AudioItemPayload(text),
                _ => null
            };
        }

        public object ItemPayloadFromImageSource(string imgSource)
        {
            return CurrentTrackType switch
            {
                IPluginBase.TrackType.Video => new ItemPayload(imgSource, true),
                IPluginBase.TrackType.Image => new ImageItemPayload(imgSource, true),
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
                itemPayload.GeminiOmni.Prompt = text;
                itemPayload.Seedance2.Prompt = text;
                itemPayload.HappyHorse1.Prompt = text;
                itemPayload.ViduQ2Turbo.Prompt = text;
            }
            else if (CurrentTrackType == IPluginBase.TrackType.Audio)
            {
                if (payload is not AudioItemPayload audioItemPayload)
                {
                    return;
                }

                audioItemPayload.GeminiOmniAudio.ExampleDialogue = text?.Length > 120 ? text[..120] : text;
            }
            else if (CurrentTrackType == IPluginBase.TrackType.Image)
            {
                if (payload is not ImageItemPayload imageItemPayload)
                {
                    return;
                }

                imageItemPayload.GptImage2.Prompt = text;
                imageItemPayload.MidjourneyV8.Prompt = text;
                imageItemPayload.GeminiOmniCharacter.Descriptions = text;
            }
        }

        public object ObjectToItemPayload(JsonObject obj)
        {
            return CurrentTrackType switch
            {
                IPluginBase.TrackType.Video => JsonHelper.ToExactType<ItemPayload>(obj),
                IPluginBase.TrackType.Image => JsonHelper.ToExactType<ImageItemPayload>(obj),
                IPluginBase.TrackType.Audio => JsonHelper.ToExactType<AudioItemPayload>(obj),
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
                    return SetupConnections(JsonHelper.ToExactType<AudioTrackPayload>(obj) as AudioTrackPayload);
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
                if (!string.IsNullOrWhiteSpace(typedImagePayload.GeminiOmniCharacter.Descriptions))
                {
                    return typedImagePayload.GeminiOmniCharacter.Descriptions;
                }

                return typedImagePayload.GptImage2.Prompt
                    ?? typedImagePayload.MidjourneyV8.Prompt
                    ?? "";
            }

            if (CurrentTrackType == IPluginBase.TrackType.Audio && itemPayload is AudioItemPayload typedAudioPayload)
            {
                return typedAudioPayload.GeminiOmniAudio.Name
                    ?? typedAudioPayload.GeminiOmniAudio.ExampleDialogue
                    ?? typedAudioPayload.GeminiOmniAudio.VoiceDescription
                    ?? "";
            }

            if (itemPayload is ItemPayload typedPayload)
            {
                return typedPayload.GeminiOmni.Prompt
                    ?? typedPayload.Seedance2.Prompt
                    ?? typedPayload.HappyHorse1.Prompt
                    ?? typedPayload.ViduQ2Turbo.Prompt
                    ?? "";
            }

            return "";
        }

        public object DefaultPayloadForTrack()
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                return SetupConnections(new TrackPayload());
            }

            if (CurrentTrackType == IPluginBase.TrackType.Audio)
            {
                return SetupConnections(new AudioTrackPayload());
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

            if (CurrentTrackType == IPluginBase.TrackType.Audio)
            {
                return new AudioItemPayload();
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

            if (CurrentTrackType == IPluginBase.TrackType.Audio)
            {
                return JsonHelper.DeepCopy<AudioTrackPayload>(obj);
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

            if (CurrentTrackType == IPluginBase.TrackType.Audio)
            {
                return JsonHelper.DeepCopy<AudioItemPayload>(obj);
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
                output.AddRange(imageItem.GeminiOmniCharacter.ImageReferences.ImageSources.Select(i => i.ImageFile));
                output.AddRange(imageTrack.GptImage2.ImageReferences.ImageSources.Select(i => i.ImageFile));
                output.AddRange(imageItem.GptImage2.ImageReferences.ImageSources.Select(i => i.ImageFile));
                output.AddRange(imageTrack.MidjourneyV8.ImageReferences.ImageSources.Select(i => i.ImageFile));
                output.AddRange(imageItem.MidjourneyV8.ImageReferences.ImageSources.Select(i => i.ImageFile));
                return output;
            }

            if (trackPayload is AudioTrackPayload && itemPayload is AudioItemPayload)
            {
                return [];
            }

            if (itemPayload is ItemPayload typedPayload)
            {
                var output = new List<string>();
                if (trackPayload is TrackPayload videoTrack)
                {
                    output.AddRange(videoTrack.GeminiOmni.ImageReferences.ImageSources.Select(i => i.ImageFile));
                    output.AddRange(videoTrack.Seedance2.ImageReferences.ImageSources.Select(i => i.ImageFile));
                    output.AddRange(videoTrack.Seedance2.AudioReferences.AudioSources.Select(i => i.AudioFile));
                    output.AddRange(videoTrack.Seedance2.VideoReferences.VideoSources.Select(i => i.VideoFile));
                    output.AddRange(videoTrack.HappyHorse1.ImageReferences.ImageSources.Select(i => i.ImageFile));
                }

                output.AddRange(typedPayload.GeminiOmni.ImageReferences.ImageSources.Select(i => i.ImageFile));
                output.AddRange(typedPayload.Seedance2.ImageReferences.ImageSources.Select(i => i.ImageFile));
                output.AddRange(typedPayload.Seedance2.AudioReferences.AudioSources.Select(i => i.AudioFile));
                output.AddRange(typedPayload.Seedance2.VideoReferences.VideoSources.Select(i => i.VideoFile));
                output.AddRange(typedPayload.HappyHorse1.ImageReferences.ImageSources.Select(i => i.ImageFile));
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
                foreach (var imageRef in imageItem.GeminiOmniCharacter.ImageReferences.ImageSources)
                {
                    imageRef.ImageFile = ReplacePath(imageRef.ImageFile, originalPath, newPath);
                }

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

            if (trackPayload is AudioTrackPayload && itemPayload is AudioItemPayload)
            {
                return;
            }

            if (itemPayload is not ItemPayload typedPayload)
            {
                return;
            }

            if (trackPayload is TrackPayload videoTrackPayload)
            {
                foreach (var geminiImageItem in videoTrackPayload.GeminiOmni.ImageReferences.ImageSources)
                {
                    geminiImageItem.ImageFile = ReplacePath(geminiImageItem.ImageFile, originalPath, newPath);
                }

                foreach (var seedanceImageItem in videoTrackPayload.Seedance2.ImageReferences.ImageSources)
                {
                    seedanceImageItem.ImageFile = ReplacePath(seedanceImageItem.ImageFile, originalPath, newPath);
                }

                foreach (var audioItem in videoTrackPayload.Seedance2.AudioReferences.AudioSources)
                {
                    audioItem.AudioFile = ReplacePath(audioItem.AudioFile, originalPath, newPath);
                }

                foreach (var videoItem in videoTrackPayload.Seedance2.VideoReferences.VideoSources)
                {
                    videoItem.VideoFile = ReplacePath(videoItem.VideoFile, originalPath, newPath);
                }

                foreach (var happyHorseImageItem in videoTrackPayload.HappyHorse1.ImageReferences.ImageSources)
                {
                    happyHorseImageItem.ImageFile = ReplacePath(happyHorseImageItem.ImageFile, originalPath, newPath);
                }
            }

            foreach (var geminiImageItem in typedPayload.GeminiOmni.ImageReferences.ImageSources)
            {
                geminiImageItem.ImageFile = ReplacePath(geminiImageItem.ImageFile, originalPath, newPath);
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

            foreach (var happyHorseImageItem in typedPayload.HappyHorse1.ImageReferences.ImageSources)
            {
                happyHorseImageItem.ImageFile = ReplacePath(happyHorseImageItem.ImageFile, originalPath, newPath);
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

        public void SetSaveConnectionSettingsCallback(Action<object> saveConnectionSettings)
        {
            _saveConnectionSettingsCallback = saveConnectionSettings;
        }

        private static int CountFiles(IEnumerable<string> additionalFiles)
        {
            return additionalFiles.Count(file => !string.IsNullOrWhiteSpace(file));
        }

        private static int CountIds(params string[] ids)
        {
            return ids.Count(id => !string.IsNullOrWhiteSpace(id) && !string.Equals(id, GeminiOmniProfileOptions.None, StringComparison.OrdinalIgnoreCase));
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

        private AudioTrackPayload SetupConnections(AudioTrackPayload tp)
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
                IPluginBase.TrackType.Audio => SetupConnections(new AudioTrackPayload() { Model = model }),
                _ => null
            };
        }

        private static (bool payloadOk, string reasonIfNot) ValidateGeminiOmniAudioPayload(GeminiOmniAudioItemPayload payload, GeminiOmniAudioTrackPayload trackPayload)
        {
            if (payload == null)
            {
                return (false, "Audio payload missing");
            }

            if (trackPayload != null && string.IsNullOrWhiteSpace(trackPayload.PresetVoiceId))
            {
                return (false, "Base preset voice missing");
            }

            if (string.IsNullOrWhiteSpace(payload.Name))
            {
                return (false, "Voice profile name missing");
            }

            if (payload.Name.Length > 210)
            {
                return (false, "Voice profile name exceeds 210 characters");
            }

            if (!string.IsNullOrWhiteSpace(payload.VoiceDescription) && payload.VoiceDescription.Length > 20000)
            {
                return (false, "Voice description exceeds 20,000 characters");
            }

            if (!string.IsNullOrWhiteSpace(payload.ExampleDialogue) && payload.ExampleDialogue.Length > 120)
            {
                return (false, "Example dialogue exceeds 120 characters");
            }

            return (true, "");
        }
    }
#pragma warning restore CS1998
}
