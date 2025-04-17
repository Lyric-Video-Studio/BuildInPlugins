using Newtonsoft.Json.Linq;
using PluginBase;
using System.Diagnostics;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json.Nodes;

namespace GoogleVeoPlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class GoogleVeoImgToVidPlugin /*: IVideoPlugin, ISaveAndRefresh, IImportFromLyrics, IRequestContentUploader, ICancellableGeneration*/
    {
        public const string PluginName = "GoogleVeoTxtToVidBuildIn";
        public string UniqueName { get => PluginName; }
        public string DisplayName { get => "GoogleVeo"; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Hosted by GoogleVeo. You need to have your authorization token";

        public bool AsynchronousGeneration { get; } = true;

        public string[] SettingsLinks => new[] { "https://aistudio.google.com/apikey" };

        public IPluginBase.TrackType CurrentTrackType { get; set; }

        private ConnectionSettings _connectionSettings = new ConnectionSettings();
        private Veo2ApiClient _wrapper;

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

            if (_wrapper == null)
            {
                _wrapper = new Veo2ApiClient(_connectionSettings.Url, _connectionSettings.AccessToken);
            }

            while (CurrentTasks > 3)
            {
                await Task.Delay(1000);
            }

            CurrentTasks++;

            try
            {
                if (JsonHelper.DeepCopy<TrackPayload>(trackPayload) is TrackPayload newTp && JsonHelper.DeepCopy<ItemPayload>(itemsPayload) is ItemPayload newIp && itemsPayload is ItemPayload origPayload)
                {
                    // combine prompts

                    // Also, when img2Vid

                    newTp.Settings.Prompt = (newIp.Prompt + " " + newTp.Settings.Prompt).Trim();
                    newTp.Settings.NegativePrompt = (newIp.NegativePrompt + " " + newTp.Settings.NegativePrompt).Trim();

                    /*if (!string.IsNullOrEmpty(newTp.Settings.EndFramePath))
                    {
                        var newUrl = await _uploader.RequestContentUpload(newTp.Settings.EndFramePath);

                        if (newUrl.responseCode != System.Net.HttpStatusCode.OK)
                        {
                            return new VideoResponse { ErrorMsg = $"Failed to upload image, response code: {newUrl.responseCode}", Success = false };
                        }

                        newTp.Settings.EndFramePath = newUrl.uploadedUrl;
                    }*/

                    //return await _wrapper.GetImgToVid(newTp.Settings, folderToSaveVideo, _connectionSettings, itemsPayload as ItemPayload, saveAndRefreshCallback);

                    if (!string.IsNullOrEmpty(newIp.PollingId))
                    {
                        return await DownloadVideo(newIp.PollingId, folderToSaveVideo);
                    }

                    try
                    {
                        var res = await _wrapper.InitiateVideoGenerationAsync(newTp.Settings, _connectionSettings.AccessToken, _cancelToken);

                        if (res?.JobId == null)
                        {
                            return new VideoResponse { ErrorMsg = $"Video generating failed, {res?.Status}", Success = false };
                        }
                        else
                        {
                            origPayload.PollingId = res.JobId;
                            saveAndRefreshCallback.Invoke();
                            return await DownloadVideo(newIp.PollingId, folderToSaveVideo);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
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

        private async Task<VideoResponse> DownloadVideo(string pollingId, string folderToSave)
        {
            var status = await _wrapper.PollUntilCompletionAsync(
                pollingId,
                null,
                pollInterval: TimeSpan.FromSeconds(8), // Check every 8 seconds
                timeout: TimeSpan.FromMinutes(10),    // Max wait 10 mins
                _cancelToken
            );

            if (status?.Status == "COMPLETED" && !string.IsNullOrEmpty(status.VideoUrl))
            {
                var file = Path.GetFileName(status.VideoUrl);

                using var downloadClient = new HttpClient { BaseAddress = new Uri(status.VideoUrl.Replace(file, "")) };

                downloadClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "video/*");

                // Store video request token to disk, in case connection is broken or something

                var videoResp = await downloadClient.GetAsync(file);

                while (videoResp.StatusCode != HttpStatusCode.OK)
                {
                    await Task.Delay(1000);
                    videoResp = await downloadClient.GetAsync(file);
                }

                if (videoResp.StatusCode == HttpStatusCode.OK)
                {
                    var respBytes = await videoResp.Content.ReadAsByteArrayAsync();
                    var pathToVideo = Path.Combine(folderToSave, $"{pollingId}.mp4");
                    await File.WriteAllBytesAsync(pathToVideo, respBytes);
                    return new VideoResponse() { Success = true, VideoFile = pathToVideo };
                }
                else
                {
                    return new VideoResponse() { ErrorMsg = $"Error: {videoResp.StatusCode}, details: {await videoResp.Content.ReadAsStringAsync()}", Success = false };
                }
            }
            else
            {
                return new VideoResponse { ErrorMsg = "Track playoad or item payload object not valid", Success = false };
            }
        }

        /*public async Task<ImageResponse> GetImage(object trackPayload, object itemsPayload)
        {
            if (_connectionSettings == null || string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return new ImageResponse { Success = false, ErrorMsg = "Uninitialized" };
            }

            while (CurrentTasks > 3)
            {
                await Task.Delay(1000);
            }
            CurrentTasks++;

            try
            {
                if (JsonHelper.DeepCopy<ImageTrackPayload>(trackPayload) is ImageTrackPayload newTp && JsonHelper.DeepCopy<ImageItemPayload>(itemsPayload) is ImageItemPayload newIp && itemsPayload is ImageItemPayload origIp)
                {
                    // combine prompts

                    // Also, when img2Vid

                    newTp.Settings.Prompt = (newTp.Settings.Prompt + " " + newIp.Prompt).Trim();
                    newTp.Settings.NegativePrompt = (newTp.Settings.NegativePrompt + " " + newIp.NegativePrompt).Trim();

                    if (!string.IsNullOrEmpty(newIp.CharacterRef))
                    {
                        newTp.Settings.ImageReferencePath = newIp.CharacterRef;
                    }

                    // Upload to cloud first
                    if (!string.IsNullOrEmpty(newTp.Settings.ImageReferencePath))
                    {
                        newTp.Settings.ImageReferencePath = newTp.Settings.ImageReferencePath.Replace("\"", "");

                        if (File.Exists(newTp.Settings.ImageReferencePath))
                        {
                            var resp = await _uploader.RequestContentUpload(newTp.Settings.ImageReferencePath);

                            if (resp.responseCode == System.Net.HttpStatusCode.OK && !resp.isLocalFile)
                            {
                                newTp.Settings.ImageReferencePath = resp.uploadedUrl;
                            }
                            else
                            {
                                return new ImageResponse { ErrorMsg = $"Failed to image upload to cloud, {resp.responseCode}", Success = false };
                            }
                        }
                    }

                    return await _wrapper.GetImg(newTp.Settings, _connectionSettings, origIp, saveAndRefreshCallback);
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
        }*/

        public async Task<string> Initialize(object settings)
        {
            if (JsonHelper.DeepCopy<ConnectionSettings>(settings) is ConnectionSettings s)
            {
                _connectionSettings = s;
                _isInitialized = !string.IsNullOrEmpty(s.AccessToken);
                _wrapper?.Dispose();
                _wrapper = null;
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
            throw new NotImplementedException("Google sucks ass with their API 'documentation' what zilloin different platforms where this api is not available. And it's costly too!!!")
            //return new GoogleVeoImgToVidPlugin();
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

        private Action saveAndRefreshCallback;
        private IContentUploader _uploader;

        public void SetSaveAndRefreshCallback(Action saveAndRefreshCallback)
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
                throw new NotImplementedException();
                //return new ImageItemPayload() { Prompt = text };
            }
        }

        /*
        public object ItemPayloadFromImageSource(string imgSource)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Audio)
            {
                return null; // Not supported, should we somehow differentiate the iporting stuff between video, image and audio?
            }
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                var output = new ItemPayload();
                output.StartFramePath = imgSource;
                return output;
            }
            else
            {
                return new ImageItemPayload() { CharacterRef = imgSource };
            }
        }*/

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
            throw new NotImplementedException();
            //return JsonHelper.ToExactType<ImageItemPayload>(obj);
        }

        public object ObjectToTrackPayload(JsonObject obj)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                var resp = JsonHelper.ToExactType<TrackPayload>(obj);
                return resp;
            }
            throw new NotImplementedException();
            //return JsonHelper.ToExactType<ImageTrackPayload>(obj);
        }

        public object ObjectToGeneralSettings(JsonObject obj)
        {
            return JsonHelper.ToExactType<ConnectionSettings>(obj);
        }

        // Image stuffs

        /*public object DefaultPayloadForImageTrack()
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
        }*/

        public string TextualRepresentation(object itemPayload)
        {
            if (itemPayload is ItemPayload ip)
            {
                return ip.Prompt;
            }

            /*if (itemPayload is ImageItemPayload imgIp)
            {
                return imgIp.Prompt;
            }*/
            return "";
        }

        public object DefaultPayloadForTrack()
        {
            switch (CurrentTrackType)
            {
                /*case IPluginBase.TrackType.Image:
                    return DefaultPayloadForImageTrack();*/

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
                /*case IPluginBase.TrackType.Image:
                    return DefaultPayloadForImageItem();*/

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
                /*case IPluginBase.TrackType.Image:
                    return CopyPayloadForImageTrack(obj);*/

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
                /*case IPluginBase.TrackType.Image:
                    return CopyPayloadForImageItem(obj);*/

                case IPluginBase.TrackType.Video:
                    return CopyPayloadForVideoItem(obj);

                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public (bool payloadOk, string reasonIfNot) ValidatePayload(object payload)
        {
            if (_connectionSettings == null)
            {
                return (false, "Uninitized");
            }

            if (string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return (false, "Access key");
            }

            switch (CurrentTrackType)
            {
                /*case IPluginBase.TrackType.Image:
                    return ValidateImagePayload(payload);*/

                case IPluginBase.TrackType.Video:
                    return ValidateVideoPayload(payload);

                case IPluginBase.TrackType.Audio:
                    return (true, "");

                default:
                    break;
            }
            return (true, "");
        }

        private CancellationToken _cancelToken;

        public void SetCancallationToken(CancellationToken cancellationToken)
        {
            _cancelToken = cancellationToken;
        }
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}