using PluginBase;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json.Nodes;

namespace MinimaxPlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class MinimaxImgToVidPlugin : IVideoPlugin, ISaveAndRefresh, IImportFromImage, IRequestContentUploader, IImagePlugin, ITextualProgressIndication
    {
        public const string PluginName = "MinimaxImgToVidBuildIn";
        public string UniqueName { get => PluginName; }
        public string DisplayName { get => "Minimax"; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Hosted by Minimax. You need to have your authorization token";

        public bool AsynchronousGeneration { get; } = true;

        public string[] SettingsLinks => new[] { "https://www.minimax.io/platform/user-center/basic-information/interface-key", "https://www.minimax.io/platform/user-center/basic-information" };

        public IPluginBase.TrackType CurrentTrackType { get; set; }

        private ConnectionSettings _connectionSettings = new ConnectionSettings();
        private Client _wrapper = new Client();

        public static int CurrentTasks = 0;

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

            while (CurrentTasks > 19)
            {
                await Task.Delay(1000);
            }

            CurrentTasks++;

            try
            {
                if (JsonHelper.DeepCopy<TrackPayload>(trackPayload) is TrackPayload newTp && JsonHelper.DeepCopy<ItemPayload>(itemsPayload) is ItemPayload newIp)
                {
                    // combine prompts

                    // Also, when img2Vid

                    newTp.Settings.prompt = newIp.Prompt + " " + newTp.Settings.prompt;

                    if (!string.IsNullOrEmpty(newIp.ImagePath))
                    {
                        newTp.Settings.first_frame_image = newIp.ImagePath;
                    }

                    if (!string.IsNullOrEmpty(newTp.Settings.first_frame_image))
                    {
                        var newUrl = await _uploader.RequestContentUpload(newTp.Settings.first_frame_image);

                        if (newUrl.responseCode != System.Net.HttpStatusCode.OK)
                        {
                            return new VideoResponse { ErrorMsg = $"Failed to upload image, response code: {newUrl.responseCode}", Success = false };
                        }

                        newTp.Settings.first_frame_image = newUrl.uploadedUrl;
                    }

                    if (newIp.SubjectReferences.SubjectReferences.Count > 0)
                    {
                        foreach (var item in newIp.SubjectReferences.SubjectReferences)
                        {
                            newTp.SubjectReferences.SubjectReferences.Add(item);
                        }
                    }

                    if (newTp.SubjectReferences.SubjectReferences.Count > 0)
                    {
                        newTp.Settings.subject_reference = new KeyFrame[1];
                        newTp.Settings.subject_reference[0] = new KeyFrame() { image = new string[newTp.SubjectReferences.SubjectReferences.Count(s => File.Exists(s.Path))] };
                    }
                    var actualIndex = 0;
                    for (int i = 0; i < newTp.SubjectReferences.SubjectReferences.Count; i++)
                    {
                        if (File.Exists(newTp.SubjectReferences.SubjectReferences[i].Path))
                        {
                            var newUrl = await _uploader.RequestContentUpload(newTp.SubjectReferences.SubjectReferences[i].Path);

                            if (newUrl.responseCode != System.Net.HttpStatusCode.OK)
                            {
                                return new VideoResponse { ErrorMsg = $"Failed to upload image, response code: {newUrl.responseCode}", Success = false };
                            }

                            newTp.Settings.subject_reference[0].image[actualIndex] = newUrl.uploadedUrl;
                            actualIndex++;
                        }
                    }

                    if (newTp.Settings.model == "MiniMax-Hailuo-2.3")
                    {
                        newTp.Settings.resolution = "1080P";
                    }

                    return await _wrapper.GetImgToVid(newTp.Settings, folderToSaveVideo, _connectionSettings, itemsPayload as ItemPayload, saveAndRefreshCallback, textualProgressAction);
                }
                else
                {
                    return new VideoResponse { ErrorMsg = "Track playoad or item payload object not valid", Success = false };
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                CurrentTasks--;
            }
        }

        private Random rnd = new Random();

        public async Task<ImageResponse> GetImage(object trackPayload, object itemsPayload)
        {
            if (_connectionSettings == null || string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return new ImageResponse { Success = false, ErrorMsg = "Uninitialized" };
            }

            while (CurrentTasks > 19)
            {
                await Task.Delay(1000);
            }

            CurrentTasks++;

            try
            {
                if (JsonHelper.DeepCopy<ImageTrackPayload>(trackPayload) is ImageTrackPayload newTp && JsonHelper.DeepCopy<ImageItemPayload>(itemsPayload) is ImageItemPayload newIp)
                {
                    // combine prompts

                    // Also, when img2Vid

                    newTp.Settings.prompt = newTp.Settings.prompt + " " + newIp.Prompt;

                    newTp.Settings.prompt = newTp.Settings.prompt.Trim();

                    // Upload to cloud first
                    if (!string.IsNullOrEmpty(newIp.CharacterRef))
                    {
                        newTp.CharacterReference = newIp.CharacterRef;
                    }

                    if (!string.IsNullOrEmpty(newTp.CharacterReference))
                    {
                        newTp.CharacterReference = newTp.CharacterReference.Replace("\"", "");

                        if (File.Exists(newTp.CharacterReference))
                        {
                            newTp.Settings.subject_reference = new KeyFrameImage[1];
                            newTp.Settings.subject_reference[0] = new KeyFrameImage();

                            var resp = await _uploader.RequestContentUpload(newTp.CharacterReference);

                            if (resp.responseCode == System.Net.HttpStatusCode.OK && !resp.isLocalFile)
                            {
                                newTp.Settings.subject_reference[0].image_file = resp.uploadedUrl;
                            }
                            else
                            {
                                return new ImageResponse { ErrorMsg = $"Failed to upload image to cloud, {resp.responseCode}", Success = false };
                            }
                        }
                    }

                    if (newIp.Seed > 0)
                    {
                        newTp.Settings.seed = newIp.Seed;
                    }

                    if (newTp.Settings.seed <= 0)
                    {
                        newTp.Settings.seed = rnd.NextInt64();
                        (itemsPayload as ImageItemPayload).Seed = newTp.Settings.seed;
                        saveAndRefreshCallback.Invoke(true);
                    }

                    return await _wrapper.GetImg(newTp.Settings, _connectionSettings);
                }
                else
                {
                    return new ImageResponse { ErrorMsg = "Track playoad or item payload object not valid", Success = false };
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                CurrentTasks--;
            }
        }

        public async Task<string> Initialize(object settings)
        {
            if (JsonHelper.DeepCopy<ConnectionSettings>(settings) is ConnectionSettings s)
            {
                _connectionSettings = s;
                _isInitialized = !string.IsNullOrEmpty(s.AccessToken);
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
            if (_connectionSettings == null)
            {
                return Array.Empty<string>();
            }

            if (propertyName == nameof(Request.model))
            {
                switch (CurrentTrackType)
                {
                    case IPluginBase.TrackType.Video:
                        return ["MiniMax-Hailuo-2.3", "MiniMax-Hailuo-02", "S2V-01", "T2V-01", "T2V-01-Director", "I2V-01", "I2V-01-Director", "I2V-01-live"];

                    default:
                        break;
                }
            }

            if (propertyName == nameof(ImageRequest.aspect_ratio))
            {
                return ["16:9", "1:1", "4:3", "2:3", "3:4", "9:16", "21:9"];
            }
            return Array.Empty<string>();
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
            return new MinimaxImgToVidPlugin();
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
            }

            return (true, "");
        }

        private Action<bool> saveAndRefreshCallback;
        private IContentUploader _uploader;

        public void SetSaveAndRefreshCallback(Action<bool> saveAndRefreshCallback)
        {
            this.saveAndRefreshCallback = saveAndRefreshCallback;
        }

        public object ItemPayloadFromLyrics(string text)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Audio)
            {
                return null; // Not supported, should we somehow differentiate the iporting stuff between video, image and audio?
            }
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                return new ItemPayload() { Prompt = text };
            }
            else
            {
                return new ImageItemPayload() { Prompt = text };
            }
        }

        public object ItemPayloadFromImageSource(string imgSource)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Audio)
            {
                return null; // Not supported, should we somehow differentiate the iporting stuff between video, image and audio?
            }
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                var output = new ItemPayload();
                output.ImagePath = imgSource;
                return output;
            }
            else
            {
                return new ImageItemPayload() { CharacterRef = imgSource };
            }
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
            _uploader = uploader;
        }

        public object ObjectToItemPayload(JsonObject obj)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                var resp = JsonHelper.ToExactType<ItemPayload>(obj);
                return resp;
            }

            return JsonHelper.ToExactType<ImageItemPayload>(obj);
        }

        public object ObjectToTrackPayload(JsonObject obj)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                var resp = JsonHelper.ToExactType<TrackPayload>(obj);
                return resp;
            }

            return JsonHelper.ToExactType<ImageTrackPayload>(obj);
        }

        public object ObjectToGeneralSettings(JsonObject obj)
        {
            return JsonHelper.ToExactType<ConnectionSettings>(obj);
        }

        // Image stuffs

        public object DefaultPayloadForImageTrack()
        {
            return new ImageTrackPayload();
        }

        public object DefaultPayloadForImageItem()
        {
            return new ImageItemPayload();
        }

        public object CopyPayloadForImageTrack(object obj)
        {
            if (obj is ImageTrackPayload ip)
            {
                return JsonHelper.DeepCopy<ImageTrackPayload>(ip);
            }
            return null;
        }

        public object CopyPayloadForImageItem(object obj)
        {
            if (obj is ImageItemPayload ip)
            {
                return JsonHelper.DeepCopy<ImageItemPayload>(ip);
            }
            return null;
        }

        public (bool payloadOk, string reasonIfNot) ValidateImagePayload(object payload)
        {
            if (payload is ImageItemPayload ip)
            {
                if (string.IsNullOrEmpty(_connectionSettings.AccessToken))
                {
                    return (false, "Auth token empty!!!");
                }

                if (string.IsNullOrEmpty(ip.Prompt))
                {
                    return (false, "Prompt empty");
                }
            }

            return (true, "");
        }

        public string TextualRepresentation(object itemPayload)
        {
            if (itemPayload is ItemPayload ip)
            {
                return ip.Prompt;
            }

            if (itemPayload is ImageItemPayload imgIp)
            {
                return imgIp.Prompt;
            }
            return "";
        }

        public object DefaultPayloadForTrack()
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Image:
                    return DefaultPayloadForImageTrack();

                case IPluginBase.TrackType.Video:
                    return DefaultPayloadForVideoTrack();

                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public object DefaultPayloadForItem()
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Image:
                    return DefaultPayloadForImageItem();

                case IPluginBase.TrackType.Video:
                    return DefaultPayloadForVideoItem();

                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public object CopyPayloadForTrack(object obj)
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Image:
                    return CopyPayloadForImageTrack(obj);

                case IPluginBase.TrackType.Video:
                    return CopyPayloadForVideoTrack(obj);

                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public object CopyPayloadForItem(object obj)
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Image:
                    return CopyPayloadForImageItem(obj);

                case IPluginBase.TrackType.Video:
                    return CopyPayloadForVideoItem(obj);

                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public (bool payloadOk, string reasonIfNot) ValidatePayload(object payload)
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Image:
                    return ValidateImagePayload(payload);

                case IPluginBase.TrackType.Video:
                    return ValidateVideoPayload(payload);

                case IPluginBase.TrackType.Audio:
                    return (true, "");

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
                return (new List<string>() { ip.ImagePath, tp.Settings.first_frame_image }.Concat(tp.SubjectReferences.SubjectReferences.Select(s => s.Path))
                    .Concat(ip.SubjectReferences.SubjectReferences.Select(s => s.Path))).ToList();
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
                    if (originalPath[i] == ip.ImagePath)
                    {
                        ip.ImagePath = newPath[i];
                    }

                    if (originalPath[i] == tp.Settings.first_frame_image)
                    {
                        tp.Settings.first_frame_image = newPath[i];
                    }

                    foreach (var item in tp.SubjectReferences.SubjectReferences)
                    {
                        if (originalPath[i] == item.Path)
                        {
                            item.Path = newPath[i];
                        }
                    }

                    foreach (var item in ip.SubjectReferences.SubjectReferences)
                    {
                        if (originalPath[i] == item.Path)
                        {
                            item.Path = newPath[i];
                        }
                    }
                }
            }
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