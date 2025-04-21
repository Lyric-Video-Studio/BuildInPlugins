using Bfl;
using PluginBase;
using System.Linq;
using System.Text.Json.Nodes;

namespace BflTxtToImgPlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class BflTxtToImgPlugin : IImagePlugin, IImportFromLyrics, ICancellableGeneration, ISaveAndRefresh, IImportFromImage
    {
        public string UniqueName { get => "BflTxtToImageBuildIn"; }
        public string DisplayName { get => "Black Forest Labs (FLUX pro)"; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Hosted by blackforestlabs.ai/. You need to have your authorization token to use this API";

        public string[] SettingsLinks => ["https://api.bfl.ml/auth/login", "https://blackforestlabs.ai/"];

        public bool AsynchronousGeneration { get; } = true;

        private ConnectionSettings _connectionSettings = new ConnectionSettings();

        private HttpClient httpClient;
        private ResultClient client;
        private Client imgClient;

        public IPluginBase.TrackType CurrentTrackType { get; set; }

        public object DefaultPayloadForImageItem()
        {
            return new ItemPayload();
        }

        public object DefaultPayloadForImageTrack()
        {
            return new TrackPayload();
        }

        private void EnsureClients()
        {
            if (client == null || httpClient == null)
            {
                httpClient?.Dispose();
                httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("x-key", _connectionSettings.AccessToken);
                client = new ResultClient("https://api.bfl.ml", httpClient);
                imgClient = new Client("https://api.bfl.ml", httpClient);
            }
        }

        public async Task<ImageResponse> GetImage(object trackPayload, object itemsPayload)
        {
            if (_connectionSettings == null || string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return new ImageResponse { Success = false, ErrorMsg = "Uninitialized" };
            }

            EnsureClients();

            if (JsonHelper.DeepCopy<TrackPayload>(trackPayload) is TrackPayload newTp && JsonHelper.DeepCopy<ItemPayload>(itemsPayload) is ItemPayload newIp)
            {
                if (!string.IsNullOrEmpty(newIp.PollingId))
                {
                    return await PollImage(newIp.PollingId);
                }

                newTp.Settings.Prompt = $"{newIp.Prompt} {newTp.Settings.Prompt}";

                newTp.Settings.Prompt = newTp.Settings.Prompt.Trim();

                if (!string.IsNullOrEmpty(newIp.ImageSource) && File.Exists(newIp.ImageSource))
                {
                    newTp.Settings.Image_prompt = Convert.ToBase64String(File.ReadAllBytes(newIp.ImageSource));
                }

                try
                {
                    var imageRequest = await imgClient.PostAsync(newTp.Settings);

                    if (!string.IsNullOrEmpty(imageRequest.Id))
                    {
                        ((ItemPayload)itemsPayload).PollingId = imageRequest.Id;
                        saveCallback?.Invoke();
                        return await PollImage(imageRequest.Id);
                    }
                }
                catch (ApiException<HTTPValidationError> validation)
                {
                    return new ImageResponse { ErrorMsg = string.Join(", ", validation.Result.Detail.Select(d => d.Msg)), Success = false };
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                    return new ImageResponse { ErrorMsg = ex.Message, Success = false };
                }

                return new ImageResponse();
            }
            else
            {
                return new ImageResponse { ErrorMsg = "Track playoad or item payload object not valid", Success = false };
            }
        }

        private async Task<ImageResponse> PollImage(string pollingId)
        {
            try
            {
                ResultResponse resp = null;
                while (resp == null || resp.Status == StatusResponse.Pending)
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        return new ImageResponse() { Success = false, ErrorMsg = "Cancelled by user" };
                    }

                    if (cancelToken.IsCancellationRequested)
                    {
                        return new ImageResponse() { Success = false, ErrorMsg = "Cancelled by user" };
                    }

                    try
                    {
                        resp = await client.GetAsync(pollingId, cancelToken);

                        if (resp.Status == StatusResponse.Ready)
                        {
                            var url = resp.Result.AdditionalProperties["sample"] as string; // This is naughty

                            try
                            {
                                using var client = new HttpClient();

                                var response = await client.GetByteArrayAsync(url);
                                var fileFormat = Path.GetExtension(url);
                                var imageBase64 = Convert.ToBase64String(response);
                                return new ImageResponse { Success = true, ImageFormat = fileFormat, Image = imageBase64 };
                            }
                            catch (Exception)
                            {
                                return new ImageResponse { ErrorMsg = "Downloading image failed, please try again", Success = false };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex);
                    }

                    if (resp == null)
                    {
                        await Task.Delay(5000, cancelToken);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return new ImageResponse() { Success = false, ErrorMsg = ex.Message };
            }

            return new ImageResponse { Success = false, ErrorMsg = "Unknown error" };
        }

        public async Task<string> Initialize(object settings)
        {
            if (JsonHelper.DeepCopy<ConnectionSettings>(settings) is ConnectionSettings s)
            {
                client = null;
                httpClient = null;
                _connectionSettings = s;
                _isInitialized = string.IsNullOrEmpty(s.AccessToken);
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
            return JsonHelper.Deserialize<TrackPayload>(fileName);
        }

        public IPluginBase CreateNewInstance()
        {
            return new BflTxtToImgPlugin();
        }

        public object ItemPayloadFromLyrics(string lyric)
        {
            return new ItemPayload() { Prompt = lyric };
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

        public (bool payloadOk, string reasonIfNot) ValidateImagePayload(object payload)
        {
            if (string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return (false, "Auth token missing");
            }

            if (payload is ItemPayload ip && string.IsNullOrEmpty(ip.Prompt))
            {
                return (false, "Prompt missing");
            }
            return (true, "");
        }

        private CancellationToken cancelToken;

        public void SetCancallationToken(CancellationToken cancellationToken)
        {
            cancelToken = cancellationToken;
        }

        private Action saveCallback;

        public void SetSaveAndRefreshCallback(Action saveAndRefreshCallback)
        {
            saveCallback = saveAndRefreshCallback;
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

        public object ItemPayloadFromImageSource(string imgSource)
        {
            return new ItemPayload() { ImageSource = imgSource };
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
                case IPluginBase.TrackType.Image:
                    return DefaultPayloadForImageTrack();

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

                case IPluginBase.TrackType.Audio:
                    return (true, "");

                default:
                    break;
            }
            return (true, "");
        }

        public List<string> FilePathsOnPayloads(object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp && itemPayload is ItemPayload ip)
            {
                return new List<string>() { ip.ImageSource };
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
                }
            }
        }
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}