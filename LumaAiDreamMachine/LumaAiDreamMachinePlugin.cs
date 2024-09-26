using PluginBase;
using SkiaSharp;

namespace LumaAiDreamMachinePlugin
{
    public class LumaAiDreamMachineImgToVidPlugin : IVideoPlugin, ISaveAndRefresh, IImportFromLyrics /*,IImportFromImage*/
    {
        public string UniqueName { get => "LumaAiDreamMachineImgToVidBuildIn"; }
        public string DisplayName { get => "Luma AI Dream Machine"; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Hosted by Luma AI. You need to have your authorization token";

        public bool AsynchronousGeneration { get; } = true;

        public string[] SettingsLinks => new[] { "https://lumalabs.ai/dream-machine/api/keys" };

        private ConnectionSettings _connectionSettings = new ConnectionSettings();
        private LumaAiDreamMachineWrapper _wrapper = new LumaAiDreamMachineWrapper();

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

                newTp.Settings.prompt = newIp.Prompt + " " + newTp.Settings.prompt;
                newTp.Settings.keyframes = newIp.KeyFrames;

                return await _wrapper.GetImgToVid(newTp.Settings, folderToSaveVideo, _connectionSettings, itemsPayload as ItemPayload, saveAndRefreshCallback);
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
                _wrapper.InitializeClient();
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

        public async Task<string[]> SelectionOptionsForProperty(string propertyName)
        {
            if (_connectionSettings == null)
            {
                return Array.Empty<string>();
            }
            return await _wrapper.GetSelectionForProperty(propertyName, _connectionSettings);
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
            return new LumaAiDreamMachineImgToVidPlugin();
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
                if (string.IsNullOrEmpty(ip.Prompt))
                {
                    return (false, "Prompt empty");
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
            return new ItemPayload() { Prompt = text };
        }
    }
}