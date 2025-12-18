using PluginBase;
using System.Text.Json.Nodes;

namespace LumaAiDreamMachinePlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class LumaAiDreamMachineImgToVidPlugin : IVideoPlugin, ISaveAndRefresh, IImportFromImage, IRequestContentUploader,
        IImagePlugin, IContentId, ITextualProgressIndication, IValidateBothPayloads
    {
        public const string PluginName = "LumaAiDreamMachineImgToVidBuildIn";
        public string UniqueName { get => PluginName; }
        public string DisplayName { get => "Dream Machine"; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Hosted by Luma AI. You need to have your authorization token";

        public bool AsynchronousGeneration { get; } = true;

        public string[] SettingsLinks => new[] { "https://lumalabs.ai/dream-machine/api/keys" };

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

                    if (!string.IsNullOrEmpty(newIp.VideoFile))
                    {
                        return await ModifyVideo(newTp, newIp, folderToSaveVideo, _connectionSettings, itemsPayload as ItemPayload, saveAndRefreshCallback, textualProgressAction);
                    }

                    // Also, when img2Vid

                    newTp.Settings.prompt = (newTp.Settings.prompt + " " + newIp.Prompt).Trim();
                    newTp.Settings.keyframes = newIp.KeyFrames;

                    // Upload to cloud first
                    if (!string.IsNullOrEmpty(newTp.Settings.keyframes.frame0.url))
                    {
                        var resp = await _uploader.RequestContentUpload(newTp.Settings.keyframes.frame0.url);

                        if (resp.responseCode == System.Net.HttpStatusCode.OK && !resp.isLocalFile)
                        {
                            newTp.Settings.keyframes.frame0.url = resp.uploadedUrl;
                        }
                        else
                        {
                            return new VideoResponse { ErrorMsg = $"Failed to upload image to cloud, {resp.responseCode}", Success = false };
                        }
                    }

                    if (!string.IsNullOrEmpty(newTp.Settings.keyframes.frame1.url))
                    {
                        var resp = await _uploader.RequestContentUpload(newTp.Settings.keyframes.frame1.url);

                        if (resp.responseCode == System.Net.HttpStatusCode.OK && !resp.isLocalFile)
                        {
                            newTp.Settings.keyframes.frame1.url = resp.uploadedUrl;
                        }
                        else
                        {
                            return new VideoResponse { ErrorMsg = $"Failed to upload image to cloud, {resp.responseCode}", Success = false };
                        }
                    }

                    if (newTp.Settings.model == "ray-1-6")
                    {
                        // TODO: quick hack, remember to do thatdunamic thingies as well
                        newTp.Settings.duration = null;
                        newTp.Settings.resolution = null;
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

        private async Task<VideoResponse> ModifyVideo(TrackPayload newTp, ItemPayload newIp, string folderToSaveVideo, ConnectionSettings connectionSettings,
            ItemPayload itemPayload, Action<bool> saveAndRefreshCallback, Action<string> textualProgressAction)
        {
            var modifyRequest = new ModifyRequest() { mode = newTp.VideoEditMode, model = newTp.Settings.model };
            modifyRequest.prompt = (newTp.Settings.prompt + " " + newIp.Prompt).Trim();

            async Task<string> UploadedPathAsync(string input)
            {
                var resp = await _uploader.RequestContentUpload(input);

                if (resp.responseCode == System.Net.HttpStatusCode.OK && !resp.isLocalFile)
                {
                    return resp.uploadedUrl;
                }
                else
                {
                    throw new Exception($"Failed to upload image to cloud, {resp.responseCode}");
                }
            }
            modifyRequest.media.url = await UploadedPathAsync(newIp.VideoFile);

            if (!string.IsNullOrEmpty(newIp.FirstFrame))
            {
                modifyRequest.first_frame.url = await UploadedPathAsync(newIp.FirstFrame);
            }
            else if (!string.IsNullOrEmpty(newTp.FirstFrame))
            {
                modifyRequest.first_frame.url = await UploadedPathAsync(newIp.FirstFrame);
            }
            else
            {
                modifyRequest.first_frame = null;
            }

            return await _wrapper.GetImgToVid(modifyRequest, folderToSaveVideo, connectionSettings, itemPayload, saveAndRefreshCallback, textualProgressAction);
        }

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

                    newTp.Settings.prompt = (newTp.Settings.prompt + " " + newIp.Prompt).Trim();

                    // Upload to cloud first
                    if (newIp.ImageRef != null && !string.IsNullOrEmpty(newIp.ImageRef.ImageSource))
                    {
                        newTp.Settings.image_ref = [new ImageRequestRefImage()];

                        var resp = await _uploader.RequestContentUpload(newIp.ImageRef.ImageSource);

                        if (resp.responseCode == System.Net.HttpStatusCode.OK && !resp.isLocalFile)
                        {
                            newTp.Settings.image_ref[0].url = resp.uploadedUrl;
                        }
                        else
                        {
                            return new ImageResponse { ErrorMsg = $"Failed to upload image to cloud, {resp.responseCode}", Success = false };
                        }

                        newTp.Settings.image_ref[0].weight = newIp.ImageRef.weight;
                    }

                    if (newIp.StyleRef != null && !string.IsNullOrEmpty(newIp.StyleRef.ImageSource))
                    {
                        newTp.Settings.style_ref = [new ImageRequestRefImage()];

                        var resp = await _uploader.RequestContentUpload(newIp.StyleRef.ImageSource);

                        if (resp.responseCode == System.Net.HttpStatusCode.OK && !resp.isLocalFile)
                        {
                            newTp.Settings.style_ref[0].url = resp.uploadedUrl;
                        }
                        else
                        {
                            return new ImageResponse { ErrorMsg = $"Failed to upload image to cloud, {resp.responseCode}", Success = false };
                        }

                        newTp.Settings.style_ref[0].weight = newIp.StyleRef.weight;
                    }

                    if (newIp.ModifyImage != null && !string.IsNullOrEmpty(newIp.ModifyImage.ImageSource))
                    {
                        newTp.Settings.modify_image_ref = new ImageRequestRefImage();

                        var resp = await _uploader.RequestContentUpload(newIp.ModifyImage.ImageSource);

                        if (resp.responseCode == System.Net.HttpStatusCode.OK && !resp.isLocalFile)
                        {
                            newTp.Settings.modify_image_ref.url = resp.uploadedUrl;
                        }
                        else
                        {
                            return new ImageResponse { ErrorMsg = $"Failed to upload image to cloud, {resp.responseCode}", Success = false };
                        }

                        newTp.Settings.modify_image_ref.weight = newIp.ModifyImage.weight;
                    }

                    for (int i = 0; i < newIp.CharacterRefs.Count; i++)
                    {
                        newIp.CharacterRefs[i].SourceFile = newIp.CharacterRefs[i].SourceFile.Replace("\"", ""); // Because windows source copy gives quotes...
                    }

                    var charRefs = newIp.CharacterRefs.Where(s => File.Exists(s.SourceFile)).ToList(); ;
                    if (charRefs.Count > 0)
                    {
                        newTp.Settings.character_ref = new ImageRequestRefCharacter();
                        newTp.Settings.character_ref.identity0.images = new string[charRefs.Count];

                        for (int i = 0; i < charRefs.Count; i++)
                        {
                            var resp = await _uploader.RequestContentUpload(charRefs[i].SourceFile);

                            if (resp.responseCode == System.Net.HttpStatusCode.OK && !resp.isLocalFile)
                            {
                                newTp.Settings.character_ref.identity0.images[i] = resp.uploadedUrl;
                            }
                            else
                            {
                                return new ImageResponse { ErrorMsg = $"Failed to upload image to cloud, {resp.responseCode}", Success = false };
                            }
                        }
                    }

                    return await _wrapper.GetImg(newTp.Settings, _connectionSettings, itemsPayload as ImageItemPayload, saveAndRefreshCallback);
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
            // TODO: Mutta miten jos tää onkin image? Samassa siis
            if (propertyName == nameof(Request.aspect_ratio))
            {
                return ["16:9", "1:1", "9:16", "4:3", "3:4", "21:9", "9:21"];
            }

            if (propertyName == nameof(Request.resolution))
            {
                return resolutions.ToArray();
            }

            if (propertyName == nameof(Request.duration))
            {
                return ["5s", "9s"];
            }

            if (propertyName == nameof(TrackPayload.VideoEditMode))
            {
                return ["adhere_1", "adhere_2", "adhere_3", "flex_1", "flex_2", "flex_3", "reimagine_1", "reimagine_2", "reimagine_3"];
            }

            if (propertyName == nameof(Request.model))
            {
                switch (CurrentTrackType)
                {
                    case IPluginBase.TrackType.Image:
                        return ["photon-1", "photon-flash-1"];

                    case IPluginBase.TrackType.Video:
                        return ["ray-2", "ray-flash-2", "ray-1-6"];

                    default:
                        break;
                }
            }
            return Array.Empty<string>();
        }

        private static List<string> resolutions = ["720p", "1080p", "4k", "540p"];

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
            }

            if (payload is TrackPayload tp)
            {
                if (tp.Settings.model == "ray-1-6" && resolutions.IndexOf(tp.Settings.resolution) > 0)
                {
                    return (false, "Ray-2 model supports only 720p");
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
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                var output = new ItemPayload();
                output.KeyFrames.frame0.url = imgSource;
                return output;
            }
            else
            {
                return new ImageItemPayload() { ImageRef = new ImageRef() { ImageSource = imgSource } };
            }
        }

        public void ContentUploaderProvided(IContentUploader uploader)
        {
            _uploader = uploader;
        }

        public object ObjectToItemPayload(JsonObject obj)
        {
            if (obj[nameof(ItemPayload.IsVideo)].AsValue().TryGetValue<bool>(out var isVid) && isVid)
            {
                var resp = JsonHelper.ToExactType<ItemPayload>(obj);
                return resp;
            }

            return JsonHelper.ToExactType<ImageItemPayload>(obj);
        }

        public object ObjectToTrackPayload(JsonObject obj)
        {
            if (obj[nameof(TrackPayload.IsVideo)].AsValue().TryGetValue<bool>(out var isVid) && isVid)
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

        public string GetContentFromPayloadId(object payload)
        {
            if (payload is ItemPayload ip)
            {
                return ip.PollingId;
            }

            if (payload is ImageItemPayload imgIp)
            {
                return imgIp.PollingId;
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
                return new List<string>() { ip.KeyFrames.frame0.url, ip.KeyFrames.frame1.url };
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
                    if (originalPath[i] == ip.KeyFrames.frame0.url)
                    {
                        ip.KeyFrames.frame0.url = newPath[i];
                    }

                    if (originalPath[i] == ip.KeyFrames.frame1.url)
                    {
                        ip.KeyFrames.frame1.url = newPath[i];
                    }
                }
            }
        }

        public (bool payloadOk, string reasonIfNot) ValidatePayloads(object trackPaylod, object itemPayload)
        {
            if (trackPaylod is TrackPayload tp && itemPayload is ItemPayload ip)
            {
                if (!string.IsNullOrEmpty(ip.VideoFile) && tp.Settings.model == "ray-1-6")
                {
                    return (false, "ray-1-6 is not supported for video modify");
                }
            }

            return (true, "");
        }

        public void AppendToPayloadFromLyrics(string text, object payload)
        {
            if (payload is ItemPayload ip)
            {
                ip.Prompt += text;
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