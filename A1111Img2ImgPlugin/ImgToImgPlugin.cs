using PluginBase;
using System.Text.Json.Nodes;

namespace A1111ImgToImgPlugin
{
    public class ImgToImgPlugin : IImagePlugin, IImportFromImage, IVideoPlugin, IImportFromVideoFrames, ICancellableGeneration, IProgressIndication
    {
        public string UniqueName { get => "Automatic1111ImgToImageBuildIn"; }
        public string DisplayName { get => "Automatic1111 ImgToImg / Vid2Vid / Upscale"; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Locally hosted Automatic1111 instance. Set full path to webui-user.bat";

        public string[] SettingsLinks => new[] { "https://www.ulti.fi" };

        private ConnectionSettings _connectionSettings = new ConnectionSettings();

        private A1111Wrapper _wrapper = new A1111Wrapper();

        public bool AsynchronousGeneration { get; } = false;

        public IPluginBase.TrackType CurrentTrackType { get; set; }

        public object DefaultPayloadForImageItem()
        {
            return new ItemPayload();
        }

        public object DefaultPayloadForImageTrack()
        {
            return new TrackPayload();
        }

        public async Task<ImageResponse> GetImage(object trackPayload, object itemsPayload)
        {
            if (_connectionSettings == null)
            {
                return new ImageResponse { ErrorMsg = "Cnnection settings was null, this was not expected", Success = false };
            }

            var newTp = JsonHelper.DeepCopy<TrackPayload>(trackPayload);
            var newIp = JsonHelper.DeepCopy<ItemPayload>(itemsPayload);

            if (newTp != null && newIp != null)
            {
#pragma warning disable CS8602
                newTp.Settings.Prompt = $"{newIp.PositivePrompt} {newTp.Settings.Prompt}"; // TODO: Asetukseen tämä, track levelille? Mutta sinne en oikein kivasti saa custom asioita, vielä, täytyy tehdä wrapperi
                newTp.Settings.Negative_prompt = $"{newIp.NegativePrompt} {newTp.Settings.Negative_prompt}";
                newTp.Settings.Prompt = newTp.Settings.Prompt.Trim();
                newTp.Settings.Negative_prompt = newTp.Settings.Negative_prompt.Trim();

                var inputImages = new List<object>();
                newTp.Settings.Init_images = inputImages;

                if (newTp.Upscale)
                {
                    newTp.Settings.Script_name = "SD upscale";
                    byte[] imageArray = File.ReadAllBytes(newIp.PathToImage);
                    inputImages.Add(Convert.ToBase64String(imageArray));
                    var parameterList = new List<object>();
                    parameterList.Add(null);
                    parameterList.Add(newTp.TileOverlap);
                    parameterList.Add(newTp.Upscaler);
                    parameterList.Add(newTp.ScaleFactor);
                    newTp.Settings.Script_args = parameterList;
                }

                if (newIp.Seed != 0)
                {
                    newTp.Settings.Seed = newIp.Seed;
                }

#pragma warning restore CS8602
                return await _wrapper.GetImgToImg(newTp.Settings, _connectionSettings, newTp.Sd_model_checkpoint);
            }
            else
            {
                return new ImageResponse { ErrorMsg = "Track playoad or item payload object not valid", Success = false };
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        public async Task<string> Initialize(object settings)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var settingsSerialized = JsonHelper.DeepCopy<ConnectionSettings>(settings);
            if (settingsSerialized is ConnectionSettings s)
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
            _wrapper.CloseConnection();
        }

        public double HeigthForEditor(string propertyName)
        {
            return -1; // TODO Promptit isompina
        }

        public async Task<string[]> SelectionOptionsForProperty(string propertyName)
        {
            if (_connectionSettings == null)
            {
                return Array.Empty<string>();
            }

            return await _wrapper.GetSelectionForProperty(propertyName, _connectionSettings);
        }

        public object CopyPayloadForImageTrack(object obj)
        {
            if (JsonHelper.DeepCopy<TrackPayload>(obj) is TrackPayload set)
            {
                return set;
            }
            return DefaultPayloadForImageTrack();
        }

        public object CopyPayloadForImageItem(object obj)
        {
            if (JsonHelper.DeepCopy<ItemPayload>(obj) is ItemPayload set)
            {
                return set;
            }
            return DefaultPayloadForImageItem();
        }

        public object DeserializePayload(string fileName)
        {
            return JsonHelper.Deserialize<TrackPayload>(fileName) ?? new TrackPayload();
        }

        public IPluginBase CreateNewInstance()
        {
            return new ImgToImgPlugin();
        }

        public async Task<string> TestInitialization()
        {
            try
            {
                var res = await _wrapper.PingConnection(_connectionSettings);

                if (res)
                {
                    return "";
                }
                else
                {
                    return "Initialization failed";
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public (bool payloadOk, string reasonIfNot) ValidateImagePayload(object payload)
        {
            if (payload is ItemPayload ip && !File.Exists(ip.PathToImage))
            {
                return (false, "Input image is missing");
            }
            return (true, "");
        }

        public object ItemPayloadFromImageSource(string imgSource)
        {
            return new ItemPayload() { PathToImage = imgSource };
        }

        public async Task<VideoResponse> GetVideo(object trackPayload, object itemsPayload, string folderToSaveVideo)
        {
            if (_connectionSettings == null)
            {
                return new VideoResponse { ErrorMsg = "Connection settings was null, this was not expected", Success = false };
            }

            var newTp = JsonHelper.DeepCopy<TrackPayload>(trackPayload);
            var newIp = JsonHelper.DeepCopy<VideoItemPayload>(itemsPayload);

            if (newTp != null && newIp != null)
            {
#pragma warning disable CS8602
                newTp.Settings.Prompt = $"{newIp.PositivePrompt} {newTp.Settings.Prompt}"; // TODO: Asetukseen tämä, track levelille? Mutta sinne en oikein kivasti saa custom asioita, vielä, täytyy tehdä wrapperi
                newTp.Settings.Negative_prompt = $"{newIp.NegativePrompt} {newTp.Settings.Negative_prompt}";
                newTp.Settings.Prompt = newTp.Settings.Prompt.Trim();
                newTp.Settings.Negative_prompt = newTp.Settings.Negative_prompt.Trim();

                var inputImages = new List<object>();
                newTp.Settings.Init_images = inputImages;
                var frameIndex = 1;
                ImageResponse lastResp = new ImageResponse();
                foreach (var f in newIp.Frames)
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        return new VideoResponse { ErrorMsg = "Operation cancelled by user", Success = false };
                    }
                    if (newTp.Upscale)
                    {
                        inputImages.Clear();
                        newTp.Settings.Script_name = "SD upscale";
                        byte[] imageArray = File.ReadAllBytes(f);
                        inputImages.Add(Convert.ToBase64String(imageArray));
                        var parameterList = new List<object>();
                        parameterList.Add(null);
                        parameterList.Add(newTp.TileOverlap);
                        parameterList.Add(newTp.Upscaler);
                        parameterList.Add(newTp.ScaleFactor);
                        newTp.Settings.Script_args = parameterList;
                    }

                    if (newIp.Seed != 0)
                    {
                        newTp.Settings.Seed = newIp.Seed;
                    }

                    var res = await _wrapper.GetImgToImg(newTp.Settings, _connectionSettings, newTp.Sd_model_checkpoint);
                    lastResp = res;
                    var targetFilename = frameIndex.ToString(CommonConstants.DefaultIntToStringFormat) + $".{res.ImageFormat}";
                    var finalPath = Path.Combine(folderToSaveVideo, targetFilename);
                    File.WriteAllBytes(finalPath, Convert.FromBase64String(res.Image));
                    frameIndex++;

                    progressAction?.Invoke((frameIndex, newIp.Frames.Count));
#pragma warning restore CS8602
                }

                return new VideoResponse { Success = true, ImageFormat = lastResp.ImageFormat };
            }
            else
            {
                return new VideoResponse { ErrorMsg = "Track playoad or item payload object not valid", Success = false };
            }
        }

        public object ItemPayloadFromImageSequence(List<string> frames)
        {
            return new VideoItemPayload() { Frames = frames };
        }

        public object DefaultPayloadForVideoTrack()
        {
            return new TrackPayload();
        }

        public object DefaultPayloadForVideoItem()
        {
            return new VideoItemPayload();
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
            if (JsonHelper.DeepCopy<VideoItemPayload>(obj) is VideoItemPayload set)
            {
                return set;
            }

            return DefaultPayloadForVideoItem();
        }

        public (bool payloadOk, string reasonIfNot) ValidateVideoPayload(object payload)
        {
            if (payload is VideoItemPayload ip)
            {
                if (ip.Frames.Count == 0)
                {
                    return (false, "Input frames missing");
                }
                else if (ip.Frames.Any(f => !File.Exists(f)))
                {
                    return (false, "Some of the input frames missing");
                }
            }
            return (true, "");
        }

        private CancellationToken cancelToken;

        public void SetCancallationToken(CancellationToken cancellationToken)
        {
            cancelToken = cancellationToken;
        }

        private Action<(int currentProgress, int maxProgress)> progressAction;

        public void SetProgressCallback(Action<(int currentProgress, int maxProgress)> action)
        {
            progressAction = action;
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
                return ip.PositivePrompt;
            }
            return "";
        }
    }
}