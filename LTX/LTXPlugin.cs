using PluginBase;
using System.Text.Json.Nodes;

namespace LTXPlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class LtxPlugin : IVideoPlugin, IImportFromImage, IRequestContentUploader, IValidateBothPayloads, ICancellableGeneration
    {
        public string UniqueName { get => "LTXBuildIn"; }
        public string DisplayName { get => "LTX"; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "LTX. You need to have your authorization token";

        public bool AsynchronousGeneration { get; } = true;

        public string[] SettingsLinks => new[] { "https://ltx.io/", "https://console.ltx.video/api-keys/" };

        private ConnectionSettings _connectionSettings = new ConnectionSettings();
        private IContentUploader _contentUploader;

        public IPluginBase.TrackType CurrentTrackType { get; set; }

        public async Task<VideoResponse> GetVideo(object trackPayload, object itemsPayload, string folderToSaveVideo)
        {
            if (_connectionSettings == null || string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return new VideoResponse { Success = false, ErrorMsg = "Uninitialized" };
            }

            if (trackPayload is TrackPayload newTp && itemsPayload is ItemPayload newIp)
            {
                // combine prompts

                // Also, when img2Vid

                var actAudioSource = await Upload(string.IsNullOrEmpty(newIp.AudioSource) ? newTp.AudioSource : newIp.AudioSource);
                var actImageSource = await Upload(string.IsNullOrEmpty(newIp.ImageSource) ? newTp.ImageSource : newIp.ImageSource);

                string actCameraMotio = null;
                try
                {
                    if (newIp.CameraMotion > 0)
                    {
                        actCameraMotio = ItemPayload.MotionTypes[newIp.CameraMotion];
                    }
                }
                catch (Exception)
                {

                }

                var dur = newIp.ShouldPropertyBeVisible(nameof(ItemPayload.DurationFast25), newTp, newIp) ? newIp.DurationFast25 : newIp.Duration;

                var reg = new Request()
                {
                    fps = newTp.Fps, 
                    generate_audio = newIp.GenerateAudio, 
                    model = newTp.Model, 
                    prompt = (newIp.Prompt + " " + newTp.Prompt).Trim(),
                    duration = int.Parse(ItemPayload.DurationFastTypes[dur]), // This is safe for now: fast one has more
                    camera_motion = actCameraMotio,
                    resolution = newTp.Resolution, 
                    image_uri = actImageSource, 
                    audio_uri = actAudioSource
                };

                return await new Client().GetVideo(reg, folderToSaveVideo, _connectionSettings, ct);

            }
            else
            {
                return new VideoResponse { ErrorMsg = "Track playoad or item payload object not valid", Success = false };
            }
        }

        private async Task<string> Upload(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var fileUpload = await _contentUploader.RequestContentUpload(path);
            if (fileUpload.isLocalFile)
            {
                throw new Exception("File must be public url or you must apply your content delivery credentials in Settings-view");
            }
            else if (fileUpload.responseCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"Error uploading to content delivery: {fileUpload.responseCode}");
            }
            else
            {
                return fileUpload.uploadedUrl;
            }
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
            if (propertyName == nameof(ItemPayload.CameraMotion))
            {
                return ItemPayload.MotionTypes;
            }

            if (propertyName == nameof(ItemPayload.DurationFast25))
            {
                return ItemPayload.DurationFastTypes;
            }

            if (propertyName == nameof(ItemPayload.Duration))
            {
                return ItemPayload.DurationTypes;
            }
            return Array.Empty<string>();
        }

        public object DeserializePayload(string fileName)
        {
            return JsonHelper.Deserialize<TrackPayload>(fileName);
        }

        public IPluginBase CreateNewInstance()
        {
            var plug = new LtxPlugin();
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


            return (true, "");
        }

        public object ItemPayloadFromLyrics(string text)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                return new ItemPayload() { Prompt = text };
            }

            return null;
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
            return null;
        }

        public object ObjectToTrackPayload(JsonObject obj)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                return JsonHelper.ToExactType<TrackPayload>(obj);
            }
            return null;
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
                    return new TrackPayload();

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

                default:
                    break;
            }
            return (true, "");
        }

        public List<string> FilePathsOnPayloads(object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp && itemPayload is ItemPayload ip)
            {
                return new List<string>() { ip.ImageSource, ip.AudioSource, tp.AudioSource, tp.ImageSource };
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

                    if (originalPath[i] == tp.ImageSource)
                    {
                        tp.ImageSource = newPath[i];
                    }
                    if (originalPath[i] == tp.AudioSource)
                    {
                        tp.AudioSource = newPath[i];
                    }
                }
            }
        }

        public (bool payloadOk, string reasonIfNot) ValidatePayloads(object trackPaylod, object itemPayload)
        {

            if (trackPaylod is TrackPayload tp && itemPayload is ItemPayload ip)
            {
                if (string.IsNullOrEmpty(tp.Prompt + ip.Prompt))
                {
                    return (false, "Prompt missing");
                }

                if (tp.Model == TrackPayload.FastModel && ip.DurationFast25 >= 12 && tp.Resolution != TrackPayload.ResHd)
                {
                    return (false, "Durations over 10 are not supported for this resolution");
                }

                if (!string.IsNullOrEmpty(tp.AudioSource) || !string.IsNullOrEmpty(ip.AudioSource) && tp.Resolution != TrackPayload.ResHd)
                {
                    return (false, $"Only {TrackPayload.ResHd} is supported with audio source");
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

        private CancellationToken ct;

        public void SetCancallationToken(CancellationToken cancellationToken)
        {
            ct = cancellationToken;
        }
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}