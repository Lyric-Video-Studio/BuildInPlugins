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
        public string DisplayName { get => "Luma labs"; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Hosted by Luma AI. You need to have your authorization token or Luma Agents token";

        public bool AsynchronousGeneration { get; } = true;

        public string[] SettingsLinks => new[] { "https://lumalabs.ai/dream-machine/api/keys", "https://platform.lumalabs.ai/keys/" };

        public IPluginBase.TrackType CurrentTrackType { get; set; }

        private ConnectionSettings _connectionSettings = new ConnectionSettings();
        private Client _wrapper = new Client();

        public static int CurrentTasks = 0;

        public object DefaultPayloadForVideoItem()
        {
            return SetVideoSubs(new ItemPayload());
        }

        public object DefaultPayloadForVideoTrack()
        {
            return new TrackPayload();
        }

        public async Task<VideoResponse> GetVideo(object trackPayload, object itemsPayload, string folderToSaveVideo)
        {
            var needsAgentsToken = trackPayload is TrackPayload tokenTp && IsRayVideoModel(tokenTp);
            var tokenToUse = needsAgentsToken ? _connectionSettings?.AccessTokenUni : _connectionSettings?.AccessToken;

            if (_connectionSettings == null || string.IsNullOrEmpty(tokenToUse))
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
                    newTp.Settings.prompt = (newTp.Settings.prompt + " " + newIp.Prompt).Trim();

                    if (IsRayVideoModel(newTp))
                    {
                        var rayRequest = await BuildRayVideoRequestAsync(newTp, newIp);
                        return await _wrapper.GetRayVideo(rayRequest, folderToSaveVideo, _connectionSettings, itemsPayload as ItemPayload, saveAndRefreshCallback, textualProgressAction);
                    }

                    if (!string.IsNullOrEmpty(newIp.VideoFile))
                    {
                        return await ModifyVideo(newTp, newIp, folderToSaveVideo, _connectionSettings, itemsPayload as ItemPayload, saveAndRefreshCallback, textualProgressAction);
                    }

                    newTp.Settings.keyframes = newIp.KeyFrames;

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
                        newTp.Settings.duration = null;
                        newTp.Settings.resolution = null;
                    }

                    return await _wrapper.GetImgToVid(newTp.Settings, folderToSaveVideo, _connectionSettings, itemsPayload as ItemPayload, saveAndRefreshCallback, textualProgressAction);
                }

                return new VideoResponse { ErrorMsg = "Track playoad or item payload object not valid", Success = false };
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

                throw new Exception($"Failed to upload image to cloud, {resp.responseCode}");
            }

            modifyRequest.media.url = await UploadedPathAsync(newIp.VideoFile);

            if (!string.IsNullOrEmpty(newIp.FirstFrame))
            {
                modifyRequest.first_frame.url = await UploadedPathAsync(newIp.FirstFrame);
            }
            else if (!string.IsNullOrEmpty(newTp.FirstFrame))
            {
                modifyRequest.first_frame.url = await UploadedPathAsync(newTp.FirstFrame);
            }
            else
            {
                modifyRequest.first_frame = null;
            }

            return await _wrapper.GetImgToVid(modifyRequest, folderToSaveVideo, connectionSettings, itemPayload, saveAndRefreshCallback, textualProgressAction);
        }

        public async Task<ImageResponse> GetImage(object trackPayload, object itemsPayload)
        {
            if (_connectionSettings == null || (string.IsNullOrEmpty(_connectionSettings.AccessToken) && string.IsNullOrEmpty(_connectionSettings.AccessTokenUni)))
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
                    newTp.Settings.prompt = (newTp.Settings.prompt + " " + newIp.Prompt).Trim();

                    if (IsUniImageModel(newTp.Settings.model))
                    {
                        var uniRequest = await BuildUniImageRequestAsync(newTp, newIp);
                        return await _wrapper.GetUniImage(uniRequest, _connectionSettings, itemsPayload as ImageItemPayload, saveAndRefreshCallback);
                    }

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
                        newIp.CharacterRefs[i].CharacterSourceFile = newIp.CharacterRefs[i].CharacterSourceFile.Replace("\"", "");
                    }

                    var charRefs = newIp.CharacterRefs.Where(s => File.Exists(s.CharacterSourceFile)).ToList();
                    if (charRefs.Count > 0)
                    {
                        newTp.Settings.character_ref = new ImageRequestRefCharacter();
                        newTp.Settings.character_ref.identity0.images = new string[charRefs.Count];

                        for (int i = 0; i < charRefs.Count; i++)
                        {
                            var resp = await _uploader.RequestContentUpload(charRefs[i].CharacterSourceFile);

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

                return new ImageResponse { ErrorMsg = "Track playoad or item payload object not valid", Success = false };
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
                _isInitialized = !string.IsNullOrEmpty(s.AccessToken) || !string.IsNullOrEmpty(s.AccessTokenUni);
                return "";
            }

            return "Connection settings object not valid";
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

            if (propertyName == nameof(Request.aspect_ratio))
            {
                return ["16:9", "1:1", "9:16", "4:3", "3:4", "21:9", "9:21"];
            }

            if (propertyName == nameof(ImageRequest.aspect_ratio))
            {
                return ["3:1", "2:1", "16:9", "3:2", "1:1", "2:3", "9:16", "1:2", "1:3", "4:3", "3:4", "21:9", "9:21"];
            }

            if (propertyName == nameof(Request.resolution))
            {
                return resolutions.ToArray();
            }

            if (propertyName == nameof(Request.duration))
            {
                return ["5s", "9s", "10s"];
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
                        return ["uni-1", "uni-1-max", "photon-1", "photon-flash-1"];

                    case IPluginBase.TrackType.Video:
                        return ["ray-3.2", "ray-2", "ray-flash-2", "ray-1-6"];
                }
            }

            return Array.Empty<string>();
        }

        private static List<string> resolutions = ["360p", "540p", "720p", "1080p", "4k"];

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
                return SetVideoSubs(set);
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
                if (string.IsNullOrEmpty(ip.Prompt))
                {
                    return (false, "Prompt empty");
                }
            }

            if (payload is TrackPayload tp)
            {
                if (IsRayVideoModel(tp))
                {
                    if (string.IsNullOrEmpty(_connectionSettings.AccessTokenUni))
                    {
                        return (false, "Luma Agents auth token empty!!!");
                    }

                    var validAspectRatios = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "9:16", "3:4", "1:1", "4:3", "16:9", "21:9" };
                    if (!string.IsNullOrWhiteSpace(tp.Settings?.aspect_ratio) && !validAspectRatios.Contains(tp.Settings.aspect_ratio))
                    {
                        return (false, "ray-3.2 supports aspect ratios 9:16, 3:4, 1:1, 4:3, 16:9 and 21:9");
                    }

                    var validResolutions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "360p", "540p", "720p", "1080p" };
                    if (!string.IsNullOrWhiteSpace(tp.Settings?.resolution) && !validResolutions.Contains(tp.Settings.resolution))
                    {
                        return (false, "ray-3.2 supports resolutions 360p, 540p, 720p and 1080p");
                    }

                    var validDurations = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "5s", "10s" };
                    if (!string.IsNullOrWhiteSpace(tp.Settings?.duration) && !validDurations.Contains(tp.Settings.duration))
                    {
                        return (false, "ray-3.2 supports durations 5s and 10s");
                    }

                    if (tp.ExrExport && !tp.Hdr)
                    {
                        return (false, "EXR export requires HDR");
                    }

                    if (tp.Hdr && tp.Settings?.resolution is "360p" or "540p")
                    {
                        return (false, "HDR requires 720p or 1080p");
                    }
                }
                else if (string.IsNullOrEmpty(_connectionSettings.AccessToken))
                {
                    return (false, "Auth token empty!!!");
                }

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

            return new ImageItemPayload() { Prompt = text };
        }

        public object ItemPayloadFromImageSource(string imgSource)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                var output = new ItemPayload();
                output.KeyFrames.frame0.url = imgSource;
                return output;
            }

            return new ImageItemPayload() { UniImageToModify = imgSource };
        }

        public void ContentUploaderProvided(IContentUploader uploader)
        {
            _uploader = uploader;
        }

        private ItemPayload SetVideoSubs(ItemPayload pl)
        {
            pl.OnDeserialized();
            pl.ImageModeChanged += (_, __) => saveAndRefreshCallback?.Invoke(false);
            return pl;
        }

        public object ObjectToItemPayload(JsonObject obj)
        {
            if (obj[nameof(ItemPayload.IsVideo)].AsValue().TryGetValue<bool>(out var isVid) && isVid)
            {
                return SetVideoSubs(JsonHelper.ToExactType<ItemPayload>(obj));
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

            return SetSubs(JsonHelper.ToExactType<ImageTrackPayload>(obj));
        }

        public object ObjectToGeneralSettings(JsonObject obj)
        {
            return JsonHelper.ToExactType<ConnectionSettings>(obj);
        }

        public object DefaultPayloadForImageTrack()
        {
            return SetSubs(new ImageTrackPayload());
        }

        public object DefaultPayloadForImageItem()
        {
            return new ImageItemPayload();
        }

        private ImageTrackPayload SetSubs(ImageTrackPayload pl)
        {
            pl.Settings.ModelChanged += (_, __) => saveAndRefreshCallback.Invoke(false);
            return pl;
        }

        public object CopyPayloadForImageTrack(object obj)
        {
            if (obj is ImageTrackPayload ip)
            {
                return SetSubs(JsonHelper.DeepCopy<ImageTrackPayload>(ip));
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
                if (string.IsNullOrEmpty(_connectionSettings.AccessToken) && string.IsNullOrEmpty(_connectionSettings.AccessTokenUni))
                {
                    return (false, "Both Auth tokens empty");
                } 

                if (string.IsNullOrEmpty(ip.Prompt))
                {
                    return (false, "Prompt empty");
                }
            }

            if (payload is ImageTrackPayload tp && IsUniImageModel(tp.Settings?.model))
            {
                var validAspectRatios = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "3:1", "2:1", "16:9", "3:2", "1:1", "2:3", "9:16", "1:2", "1:3" };
                if (!string.IsNullOrWhiteSpace(tp.Settings?.aspect_ratio) && !validAspectRatios.Contains(tp.Settings.aspect_ratio))
                {
                    return (false, "uni-1 models support aspect ratios 3:1, 2:1, 16:9, 3:2, 1:1, 2:3, 9:16, 1:2 and 1:3");
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
                var output = new List<string>() { ip.KeyFrames.frame0.url, ip.KeyFrames.frame1.url, ip.VideoFile, ip.FirstFrame, tp.FirstFrame };
                output.AddRange(ip.MultiKeyFrames.Select(s => s.ImageSource));
                return output
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }

            if (trackPayload is ImageTrackPayload && itemPayload is ImageItemPayload imgIp)
            {
                var output = new List<string> { imgIp.ImageRef?.ImageSource, imgIp.StyleRef?.ImageSource, imgIp.ModifyImage?.ImageSource, imgIp.UniImageToModify };
                output.AddRange(imgIp.CharacterRefs.Select(s => s.CharacterSourceFile));
                output.AddRange(imgIp.UniReferenceImages.Select(s => s.UniSourceFile));
                return output.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            }

            return new List<string>();
        }

        public void ReplaceFilePathsOnPayloads(List<string> originalPath, List<string> newPath, object trackPayload, object itemPayload)
        {
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

                    if (originalPath[i] == ip.VideoFile)
                    {
                        ip.VideoFile = newPath[i];
                    }

                    if (originalPath[i] == ip.FirstFrame)
                    {
                        ip.FirstFrame = newPath[i];
                    }

                    if (originalPath[i] == tp.FirstFrame)
                    {
                        tp.FirstFrame = newPath[i];
                    }

                    foreach (var item in ip.MultiKeyFrames)
                    {
                        if (originalPath[i] == item.ImageSource)
                        {
                            item.ImageSource = newPath[i];
                        }
                    }
                }
            }

            if (trackPayload is ImageTrackPayload && itemPayload is ImageItemPayload imgIp)
            {
                for (int i = 0; i < originalPath.Count; i++)
                {
                    if (originalPath[i] == imgIp.ImageRef?.ImageSource)
                    {
                        imgIp.ImageRef.ImageSource = newPath[i];
                    }

                    if (originalPath[i] == imgIp.StyleRef?.ImageSource)
                    {
                        imgIp.StyleRef.ImageSource = newPath[i];
                    }

                    if (originalPath[i] == imgIp.ModifyImage?.ImageSource)
                    {
                        imgIp.ModifyImage.ImageSource = newPath[i];
                    }

                    if (originalPath[i] == imgIp.UniImageToModify)
                    {
                        imgIp.UniImageToModify = newPath[i];
                    }

                    foreach (var item in imgIp.CharacterRefs)
                    {
                        if (originalPath[i] == item.CharacterSourceFile)
                        {
                            item.CharacterSourceFile = newPath[i];
                        }
                    }

                    foreach (var item in imgIp.UniReferenceImages)
                    {
                        if (originalPath[i] == item.UniSourceFile)
                        {
                            item.UniSourceFile = newPath[i];
                        }
                    }
                }
            }
        }

        public (bool payloadOk, string reasonIfNot) ValidatePayloads(object trackPaylod, object itemPayload)
        {
            if (trackPaylod is TrackPayload tp && itemPayload is ItemPayload ip)
            {
                var hasSourceVideo = HasSourceVideo(ip);
                var hasStartFrame = HasFrameReference(ip.KeyFrames?.frame0);
                var hasEndFrame = HasFrameReference(ip.KeyFrames?.frame1);

                if (!string.IsNullOrEmpty(ip.VideoFile) && tp.Settings.model == "ray-1-6")
                {
                    return (false, "ray-1-6 is not supported for video modify");
                }

                if (!string.IsNullOrWhiteSpace(ip.SourceGenerationId) && !IsRayVideoModel(tp))
                {
                    return (false, "Source generation id is only supported by ray-3.2");
                }

                if (hasSourceVideo && tp.SourceVideoMode == TrackPayload.SourceVideoModeReframe && !IsRayVideoModel(tp))
                {
                    return (false, "Video reframing is only supported by ray-3.2");
                }

                if (IsRayVideoModel(tp))
                {
                    if (!hasSourceVideo && ip.ImageMode == ItemPayload.ImageModeMultiFrame)
                    {
                        if (ip.MultiKeyFrames.Count == 0)
                        {
                            return (false, "Add at least one multiframe keyframe");
                        }

                        if (ip.MultiKeyFrames.Count > 64)
                        {
                            return (false, "ray-3.2 multiframe mode supports up to 64 keyframes");
                        }

                        if (ip.MultiKeyFrames.Any(s => string.IsNullOrWhiteSpace(s.ImageSource) && string.IsNullOrWhiteSpace(s.GenerationId)))
                        {
                            return (false, "Each multiframe keyframe needs either image source or generation id");
                        }

                        if (ip.MultiKeyFrames.Select(s => s.FrameIndex).Distinct().Count() != ip.MultiKeyFrames.Count)
                        {
                            return (false, "Multiframe keyframe indexes must be unique");
                        }

                        if (ip.MultiKeyFrames.Any(s => s.FrameIndex < 0))
                        {
                            return (false, "Multiframe keyframe indexes must be non-negative");
                        }

                        var maxIndex = tp.Settings.duration == "10s" ? 240 : 120;
                        if (ip.MultiKeyFrames.Any(s => s.FrameIndex > maxIndex))
                        {
                            return (false, $"Multiframe keyframe indexes must be within 0-{maxIndex} for {tp.Settings.duration ?? "5s"} duration");
                        }

                        if (tp.Settings.loop)
                        {
                            return (false, "ray-3.2 does not support loop together with multiframe mode");
                        }
                    }

                    if (hasSourceVideo && tp.SourceVideoMode == TrackPayload.SourceVideoModeReframe)
                    {
                        if (tp.Hdr || tp.ExrExport)
                        {
                            return (false, "ray-3.2 video reframe does not support HDR or EXR");
                        }
                    }

                    if (!hasSourceVideo)
                    {
                        if (tp.Settings.duration == "10s" && (hasStartFrame || hasEndFrame))
                        {
                            return (false, "ray-3.2 does not support start or end frames with 10s duration");
                        }

                        if (tp.Settings.duration == "10s" && tp.Hdr)
                        {
                            return (false, "ray-3.2 does not support HDR with 10s duration");
                        }

                        if (tp.Settings.duration == "10s" && tp.Settings.loop)
                        {
                            return (false, "ray-3.2 does not support looping with 10s duration");
                        }

                        if (tp.Settings.loop && hasEndFrame)
                        {
                            return (false, "ray-3.2 does not support loop together with an end frame");
                        }

                        if (tp.Settings.loop && tp.Hdr)
                        {
                            return (false, "ray-3.2 does not support loop together with HDR");
                        }
                    }
                }
            }

            if (trackPaylod is ImageTrackPayload imgTp && itemPayload is ImageItemPayload imgIp && IsUniImageModel(imgTp.Settings?.model))
            {
                var isEdit = imgIp.ModifyImage != null && !string.IsNullOrWhiteSpace(imgIp.ModifyImage.ImageSource);
                if (!isEdit && imgTp.Style == "manga" && imgTp.Settings?.aspect_ratio is "3:1" or "2:1" or "16:9" or "3:2" or "1:1")
                {
                    return (false, "manga style only supports portrait ratios for uni text-to-image");
                }
            }

            return (true, "");
        }

        public void AppendToPayloadFromLyrics(string text, object payload)
        {
            if (payload is ItemPayload ip)
            {
                ip.Prompt = text;
            }

            if (payload is ImageItemPayload imgIp)
            {
                imgIp.Prompt = text;
            }
        }

        public void UserDataDeleteRequested()
        {
            if (_connectionSettings != null)
            {
                _connectionSettings.DeleteTokens();
            }
        }

        private static bool IsUniImageModel(string model)
        {
            return model == "uni-1" || model == "uni-1-max";
        }

        private static bool IsRayVideoModel(TrackPayload payload)
        {
            return payload?.Settings?.model == "ray-3.2";
        }

        private static bool HasSourceVideo(ItemPayload payload)
        {
            return !string.IsNullOrWhiteSpace(payload?.VideoFile) || !string.IsNullOrWhiteSpace(payload?.SourceGenerationId);
        }

        private static bool HasFrameReference(KeyFrame keyFrame)
        {
            return keyFrame != null && (!string.IsNullOrWhiteSpace(keyFrame.url) || !string.IsNullOrWhiteSpace(keyFrame.id));
        }

        private async Task<LumaAgentsVideoRequest> BuildRayVideoRequestAsync(TrackPayload trackPayload, ItemPayload itemPayload)
        {
            var request = new LumaAgentsVideoRequest
            {
                model = "ray-3.2",
                prompt = trackPayload.Settings.prompt,
                video = new LumaAgentsVideoOptions
                {
                    resolution = string.IsNullOrWhiteSpace(trackPayload.Settings.resolution) ? null : trackPayload.Settings.resolution
                }
            };

            if (HasSourceVideo(itemPayload))
            {
                request.source = await BuildRayVideoSourceAsync(itemPayload);

                if (trackPayload.SourceVideoMode == TrackPayload.SourceVideoModeReframe)
                {
                    request.type = "video_reframe";
                    request.aspect_ratio = string.IsNullOrWhiteSpace(trackPayload.Settings.aspect_ratio) ? null : trackPayload.Settings.aspect_ratio;

                    if (trackPayload.UseCustomSourcePosition)
                    {
                        request.video.source_position = new LumaAgentsVideoSourcePosition
                        {
                            x_norm = trackPayload.SourcePositionXNorm,
                            y_norm = trackPayload.SourcePositionYNorm,
                            w_norm = trackPayload.SourcePositionWNorm,
                            h_norm = trackPayload.SourcePositionHNorm
                        };
                    }
                }
                else
                {
                    request.type = "video_edit";
                    request.video.edit = string.IsNullOrWhiteSpace(trackPayload.VideoEditMode)
                        ? new LumaAgentsVideoEditOptions { auto_controls = true }
                        : new LumaAgentsVideoEditOptions { strength = trackPayload.VideoEditMode };

                    if (trackPayload.Hdr)
                    {
                        request.video.hdr = true;
                    }

                    if (trackPayload.ExrExport)
                    {
                        request.video.exr_export = true;
                    }

                    var guideFramePath = !string.IsNullOrWhiteSpace(itemPayload.FirstFrame) ? itemPayload.FirstFrame : trackPayload.FirstFrame;
                    request.video.start_frame = await BuildRayImageReferenceAsync(guideFramePath, null);
                }

                return request;
            }

            request.type = "video";
            request.aspect_ratio = string.IsNullOrWhiteSpace(trackPayload.Settings.aspect_ratio) ? null : trackPayload.Settings.aspect_ratio;
            request.video.duration = string.IsNullOrWhiteSpace(trackPayload.Settings.duration) ? null : trackPayload.Settings.duration;

            if (trackPayload.Settings.loop)
            {
                request.video.loop = true;
            }

            if (trackPayload.Hdr)
            {
                request.video.hdr = true;
            }

            if (trackPayload.ExrExport)
            {
                request.video.exr_export = true;
            }

            if (itemPayload.ImageMode == ItemPayload.ImageModeMultiFrame)
            {
                var keyframeReferences = new List<LumaAgentsMediaReference>();
                var keyframeIndexes = new List<int>();

                foreach (var keyframe in itemPayload.MultiKeyFrames)
                {
                    keyframeReferences.Add(await BuildRayImageReferenceAsync(keyframe.ImageSource, keyframe.GenerationId));
                    keyframeIndexes.Add(keyframe.FrameIndex);
                }

                request.video.keyframes = keyframeReferences.ToArray();
                request.video.keyframe_indexes = keyframeIndexes.ToArray();
            }
            else
            {
                request.video.start_frame = await BuildRayImageReferenceAsync(itemPayload.KeyFrames?.frame0?.url, itemPayload.KeyFrames?.frame0?.id);
                request.video.end_frame = await BuildRayImageReferenceAsync(itemPayload.KeyFrames?.frame1?.url, itemPayload.KeyFrames?.frame1?.id);
            }

            return request;
        }

        private async Task<LumaAgentsMediaReference> BuildRayImageReferenceAsync(string sourcePath, string generationId)
        {
            if (!string.IsNullOrWhiteSpace(generationId))
            {
                return new LumaAgentsMediaReference { generation_id = generationId.Trim() };
            }

            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return null;
            }

            var resp = await _uploader.RequestContentUpload(sourcePath);
            if (resp.responseCode == System.Net.HttpStatusCode.OK && !resp.isLocalFile)
            {
                return new LumaAgentsMediaReference { url = resp.uploadedUrl };
            }

            throw new Exception($"Failed to upload image to cloud, {resp.responseCode}");
        }

        private async Task<LumaAgentsMediaReference> BuildRayVideoSourceAsync(ItemPayload itemPayload)
        {
            if (!string.IsNullOrWhiteSpace(itemPayload.SourceGenerationId))
            {
                return new LumaAgentsMediaReference { generation_id = itemPayload.SourceGenerationId.Trim() };
            }

            var resp = await _uploader.RequestContentUpload(itemPayload.VideoFile);
            if (resp.responseCode == System.Net.HttpStatusCode.OK && !resp.isLocalFile)
            {
                return new LumaAgentsMediaReference
                {
                    url = resp.uploadedUrl,
                    media_type = GetVideoMediaType(itemPayload.VideoFile)
                };
            }

            throw new Exception($"Failed to upload video to cloud, {resp.responseCode}");
        }

        private static string GetVideoMediaType(string sourcePath)
        {
            return Path.GetExtension(sourcePath)?.ToLowerInvariant() switch
            {
                ".mov" => "video/quicktime",
                ".webm" => "video/webm",
                ".mkv" => "video/x-matroska",
                ".avi" => "video/x-msvideo",
                _ => "video/mp4"
            };
        }

        private async Task<LumaAgentsImageRequest> BuildUniImageRequestAsync(ImageTrackPayload trackPayload, ImageItemPayload itemPayload)
        {
            var isEdit = !string.IsNullOrWhiteSpace(itemPayload.UniImageToModify);
            var request = new LumaAgentsImageRequest
            {
                model = trackPayload.Settings.model,
                prompt = trackPayload.Settings.prompt,
                type = isEdit ? "image_edit" : "image",
                style = string.IsNullOrWhiteSpace(trackPayload.Style) ? "auto" : trackPayload.Style,
                output_format = trackPayload.OutputFormat == "auto" ? null : trackPayload.OutputFormat,
                web_search = trackPayload.WebSearch
            };

            if (!isEdit)
            {
                request.aspect_ratio = string.IsNullOrWhiteSpace(trackPayload.Settings.aspect_ratio) ? null : trackPayload.Settings.aspect_ratio;
            }

            var refs = new List<LumaAgentsImageReference>();

            async Task AddReferenceAsync(string source)
            {
                if (string.IsNullOrWhiteSpace(source))
                {
                    return;
                }

                var resp = await _uploader.RequestContentUpload(source);
                if (resp.responseCode == System.Net.HttpStatusCode.OK && !resp.isLocalFile)
                {
                    refs.Add(new LumaAgentsImageReference { url = resp.uploadedUrl });
                    return;
                }

                throw new Exception($"Failed to upload image to cloud, {resp.responseCode}");
            }

            if (isEdit)
            {
                var resp = await _uploader.RequestContentUpload(itemPayload.UniImageToModify);
                if (resp.responseCode == System.Net.HttpStatusCode.OK && !resp.isLocalFile)
                {
                    request.source = new LumaAgentsImageReference { url = resp.uploadedUrl };
                }
                else
                {
                    throw new Exception($"Failed to upload image to cloud, {resp.responseCode}");
                }
            }

            foreach (var item in itemPayload.UniReferenceImages)
            {
                if (refs.Count >= (isEdit ? 8 : 9))
                {
                    break;
                }

                await AddReferenceAsync(item.UniSourceFile?.Replace("\"", ""));
            }

            if (refs.Count > 0)
            {
                request.image_ref = refs.Take(isEdit ? 8 : 9).ToArray();
            }

            return request;
        }
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
