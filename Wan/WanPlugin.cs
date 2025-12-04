using PluginBase;
using System.Text.Json.Nodes;

namespace WanPlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class WanVideoPlugin : IVideoPlugin, ISaveAndRefresh, IImportFromImage, IRequestContentUploader, ITextualProgressIndication
    {
        public string UniqueName { get => "WanVideoBuildIn"; }
        public string DisplayName { get => "WAN API"; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Powered by Alibaba. You need to have your authorization token";

        public bool AsynchronousGeneration { get; } = true;

        public string[] SettingsLinks => new[] { "https://modelstudio.console.alibabacloud.com/?tab=globalset#/efm/api_key" };

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

                newTp.Request.input.prompt = (newIp.Prompt + " " + newTp.Request.input.prompt).Trim();
                newTp.Request.input.negative_prompt = (newIp.NegativePrompt + " " + newTp.Request.input.negative_prompt).Trim();

                if (newIp.Seed != 0)
                {
                    newTp.Request.parameters.seed = newIp.Seed;
                }
                else if (itemsPayload is ItemPayload ipOld)
                {
                    ipOld.Seed = new Random().Next(1, int.MaxValue);
                    saveAndRefreshCallback.Invoke(true);
                    newTp.Request.parameters.seed = ipOld.Seed;
                }

                if (!string.IsNullOrEmpty(newIp.FirstFrame))
                {
                    newTp.Request.model = "wan2.2-i2v-plus";
                }

                if (!string.IsNullOrEmpty(newIp.FirstFrame) && string.IsNullOrEmpty(newIp.LastFrame))
                {
                    var fileUpload = await _contentUploader.RequestContentUpload(newIp.FirstFrame);

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
                        newTp.Request.input.img_url = fileUpload.uploadedUrl;
                    }
                }
                else if (!string.IsNullOrEmpty(newIp.FirstFrame) && !string.IsNullOrEmpty(newIp.LastFrame))
                {
                    var fileUpload = await _contentUploader.RequestContentUpload(newIp.FirstFrame);

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
                        newTp.Request.input.first_frame = fileUpload.uploadedUrl;
                    }

                    fileUpload = await _contentUploader.RequestContentUpload(newIp.LastFrame);

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
                        newTp.Request.input.last_frame = fileUpload.uploadedUrl;
                    }
                }

                var videoResp = await new Client().GetVideo(newTp.Request, folderToSaveVideo, _connectionSettings, itemsPayload as ItemPayload, saveAndRefreshCallback, textualProgressAction);
                return videoResp;
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

        private static string[] ratios = ["1920*1080", "1080*1920", "1440*1440", "1632*1248", "1248*1632", "832*480", "480*832", "624*624"];

        public async Task<string[]> SelectionOptionsForProperty(string propertyName)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                switch (propertyName)
                {
                    case nameof(Parameters.size):
                        return ratios;

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

        public IPluginBase CreateNewInstance()
        {
            var plug = new WanVideoPlugin();
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
            if (string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return (false, "Auth token empty!!!");
            }

            if (payload is ItemPayload ip)
            {
                return (!string.IsNullOrEmpty(ip.Prompt), "Prompt missing");
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

            throw new NotImplementedException();
        }

        public object ItemPayloadFromImageSource(string imgSource)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                return new ItemPayload() { FirstFrame = imgSource };
            }

            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public object ObjectToTrackPayload(JsonObject obj)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                return JsonHelper.ToExactType<TrackPayload>(obj);
            }
            throw new NotImplementedException();
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

                case IPluginBase.TrackType.Image:
                    throw new NotImplementedException();

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
                    throw new NotImplementedException();

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
                    throw new NotImplementedException();

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
                    throw new NotImplementedException();

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
                    throw new NotImplementedException();

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
                return new List<string>() { ip.FirstFrame, ip.LastFrame };
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
                    if (originalPath[i] == ip.FirstFrame)
                    {
                        ip.FirstFrame = newPath[i];
                    }
                    if (originalPath[i] == ip.LastFrame)
                    {
                        ip.LastFrame = newPath[i];
                    }
                }
            }
        }
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}