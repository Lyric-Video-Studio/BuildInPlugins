using Bfl;
using PluginBase;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using static System.Net.Mime.MediaTypeNames;

namespace BflTxtToImgPlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class BflTxtToImgPlugin : IImagePlugin, ICancellableGeneration, ISaveAndRefresh, IImportFromImage, IGenerationCost, ITextualProgressIndication, IValidateBothPayloads
    {
        public string UniqueName { get => "BflTxtToImageBuildIn"; }
        public string DisplayName { get => "Black Forest Labs"; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Hosted by blackforestlabs.ai/. You need to have your authorization token to use this API";

        public string[] SettingsLinks => ["https://api.bfl.ml/auth/login", "https://blackforestlabs.ai/"];

        public bool AsynchronousGeneration { get; } = true;

        private ConnectionSettings _connectionSettings = new ConnectionSettings();

        private HttpClient httpClient;
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
            if (httpClient == null)
            {
                httpClient?.Dispose();
                httpClient = new HttpClient()
                {
                    Timeout = TimeSpan.FromMinutes(10),
                };
                httpClient.DefaultRequestHeaders.Add("x-key", _connectionSettings.AccessToken);
                imgClient = new Client(httpClient);
            }
        }

        private Random rnd = new Random();

        public async Task<ImageResponse> GetImage(object trackPayload, object itemsPayload)
        {
            if (_connectionSettings == null || string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return new ImageResponse { Success = false, ErrorMsg = "Uninitialized" };
            }

            EnsureClients();

            if (JsonHelper.DeepCopy<TrackPayload>(trackPayload) is TrackPayload newTp && JsonHelper.DeepCopy<ItemPayload>(itemsPayload) is ItemPayload newIp && itemsPayload is ItemPayload oldPl)
            {
                if (!string.IsNullOrEmpty(newIp.PollingUrl))
                {
                    return await PollImage(newIp.PollingUrl);
                }

                try
                {
                    AsyncResponse imageRequest;

                    var inputImages = GetInputImagePaths(newTp, newIp);

                    if (newTp.Mode == TrackPayload.ModeOutpaint)
                    {
                        if (inputImages.Count == 0)
                        {
                            return new ImageResponse { Success = false, ErrorMsg = "Input image missing" };
                        }

                        if (inputImages.Count > 1)
                        {
                            return new ImageResponse { Success = false, ErrorMsg = "Outpaint supports a single input image" };
                        }

                        imageRequest = await SubmitOutpaintRequestAsync(newTp.OutpaintSettings, inputImages.First());
                    }
                    else
                    {
                        if (newIp.Seed == 0)
                        {
                            newIp.Seed = rnd.Next();
                            oldPl.Seed = newIp.Seed;
                        }

                        newTp.SettingsNew.Prompt = $"{newIp.Prompt} {newTp.SettingsNew.Prompt}";
                        newTp.SettingsNew.Prompt = newTp.SettingsNew.Prompt.Trim();

                        ApplyFlux2InputImages(newTp.SettingsNew, inputImages);
                        imageRequest = await imgClient.Generate_flux_2_pro_v1_flux_2_pro_postAsync(newTp.SettingsNew);
                    }

                    if (imageRequest.Cost > 0)
                    {
                        costAction?.Invoke((imageRequest.Cost / 100).ToString() + "€");
                    }

                    if (!string.IsNullOrEmpty(imageRequest.Id))
                    {
                        ((ItemPayload)itemsPayload).PollingUrl = imageRequest.Polling_url;
                        saveCallback?.Invoke(true);
                        return await PollImage(imageRequest.Polling_url);
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

        private async Task<ImageResponse> PollImage(string pollingUrl)
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
                        using var fetchImgClient = new HttpClient() { Timeout = TimeSpan.FromMinutes(10), BaseAddress = new Uri(pollingUrl) };
                        fetchImgClient.DefaultRequestHeaders.Add("x-key", _connectionSettings.AccessToken);
                        fetchImgClient.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

                        var rawRes = await fetchImgClient.GetAsync("");

                        var stringResp = await rawRes.Content.ReadAsStringAsync();

                        resp = JsonHelper.DeserializeString<ResultResponse>(stringResp);

                        progressAction?.Invoke(resp.Status.ToString());

                        if (resp.Status == StatusResponse.Ready)
                        {
                            var url = resp.Result.AdditionalProperties["sample"].ToString(); // This is naughty

                            try
                            {
                                using var client = new HttpClient() { Timeout = TimeSpan.FromMinutes(10), BaseAddress = new Uri(url as string) };

                                var response = await client.GetByteArrayAsync("");
                                var fileFormat = (url as string).Contains(".png") ? "png" : "jpg";
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
                imgClient = null;
                httpClient = null;
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

            if (propertyName == nameof(FluxKontextProInputs.Output_format))
            {
                return ["png", "jpeg"];
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

            return (true, "");
        }

        public (bool payloadOk, string reasonIfNot) ValidatePayloads(object trackPaylod, object itemPayload)
        {
            if (string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return (false, "Auth token missing");
            }

            if (trackPaylod is not TrackPayload tp || itemPayload is not ItemPayload ip)
            {
                return (false, "Track playoad or item payload object not valid");
            }

            if (tp.Mode == TrackPayload.ModeOutpaint)
            {
                if (tp.OutpaintSettings.Width < 64 || tp.OutpaintSettings.Height < 64)
                {
                    return (false, "Width and height must be at least 64");
                }

                var inputImages = GetInputImagePaths(tp, ip);
                if (inputImages.Count == 0)
                {
                    return (false, "Input image missing");
                }

                if (inputImages.Count > 1)
                {
                    return (false, "Outpaint supports a single input image");
                }

                return (true, "");
            }

            var prompt = $"{ip.Prompt} {tp.SettingsNew.Prompt}".Trim();
            if (string.IsNullOrEmpty(prompt))
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

        private Action<bool> saveCallback;

        public void SetSaveAndRefreshCallback(Action<bool> saveAndRefreshCallback)
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
            return new ItemPayload() { InputImage = imgSource };
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
                return new List<string>() { ip.InputImage, ip.InputImage2, ip.InputImage3, ip.InputImage4, ip.InputImage5, ip.InputImage6, ip.InputImage7, ip.InputImage8 };
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
                    if (originalPath[i] == ip.InputImage)
                    {
                        ip.InputImage = newPath[i];
                    }

                    if (originalPath[i] == ip.InputImage2)
                    {
                        ip.InputImage2 = newPath[i];
                    }

                    if (originalPath[i] == ip.InputImage3)
                    {
                        ip.InputImage3 = newPath[i];
                    }

                    if (originalPath[i] == ip.InputImage4)
                    {
                        ip.InputImage4 = newPath[i];
                    }

                    if (originalPath[i] == ip.InputImage5)
                    {
                        ip.InputImage5 = newPath[i];
                    }

                    if (originalPath[i] == ip.InputImage6)
                    {
                        ip.InputImage6 = newPath[i];
                    }

                    if (originalPath[i] == ip.InputImage7)
                    {
                        ip.InputImage7 = newPath[i];
                    }

                    if (originalPath[i] == ip.InputImage8)
                    {
                        ip.InputImage8 = newPath[i];
                    }
                }
            }
        }

        public void AppendToPayloadFromLyrics(string text, object payload)
        {
            if (payload is ItemPayload ip)
            {
                ip.Prompt = text;
            }
        }

        private Action<string> costAction;

        public void SetShowCostAction(Action<string> cost)
        {
            costAction = cost;
        }

        private Action<string> progressAction;

        public void SetTextProgressCallback(Action<string> action)
        {
            progressAction = action;
        }

        private static List<string> GetInputImagePaths(TrackPayload trackPayload, ItemPayload itemPayload)
        {
            return new string[] { trackPayload.InputImage, trackPayload.InputImage2, trackPayload.InputImage3, trackPayload.InputImage4, trackPayload.InputImage5,
                    trackPayload.InputImage6, trackPayload.InputImage7, trackPayload.InputImage8,
                    itemPayload.InputImage, itemPayload.InputImage2, itemPayload.InputImage3, itemPayload.InputImage4, itemPayload.InputImage5,
                    itemPayload.InputImage6, itemPayload.InputImage7, itemPayload.InputImage8 }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(path => WorkspaceSettings.GetAbsolutePath(path))
                .Where(File.Exists)
                .ToList();
        }

        private static void ApplyFlux2InputImages(Flux2Inputs settings, List<string> inputImages)
        {
            settings.Input_image = null;
            settings.Input_image_2 = null;
            settings.Input_image_3 = null;
            settings.Input_image_4 = null;
            settings.Input_image_5 = null;
            settings.Input_image_6 = null;
            settings.Input_image_7 = null;
            settings.Input_image_8 = null;

            for (int i = 0; i < inputImages.Count && i < 8; i++)
            {
                var b64 = Convert.ToBase64String(File.ReadAllBytes(inputImages[i]));

                switch (i)
                {
                    case 0:
                        settings.Input_image = b64;
                        break;
                    case 1:
                        settings.Input_image_2 = b64;
                        break;
                    case 2:
                        settings.Input_image_3 = b64;
                        break;
                    case 3:
                        settings.Input_image_4 = b64;
                        break;
                    case 4:
                        settings.Input_image_5 = b64;
                        break;
                    case 5:
                        settings.Input_image_6 = b64;
                        break;
                    case 6:
                        settings.Input_image_7 = b64;
                        break;
                    case 7:
                        settings.Input_image_8 = b64;
                        break;
                }
            }
        }

        private async Task<AsyncResponse> SubmitOutpaintRequestAsync(FluxOutpaintSettings settings, string inputImagePath)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.bfl.ai/v1/flux-tools/outpainting-v1");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

            var payload = new Dictionary<string, object>
            {
                ["input_image"] = Convert.ToBase64String(File.ReadAllBytes(inputImagePath)),
                ["width"] = settings.Width,
                ["height"] = settings.Height,
                ["output_format"] = settings.OutputFormat
            };

            if (settings.ReferenceOffsetX.HasValue)
            {
                payload["reference_offset_x"] = settings.ReferenceOffsetX.Value;
            }

            if (settings.ReferenceOffsetY.HasValue)
            {
                payload["reference_offset_y"] = settings.ReferenceOffsetY.Value;
            }

            if (settings.AutoCrop)
            {
                payload["auto_crop"] = true;
            }

            var json = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(payload);
            request.Content = new ByteArrayContent(json);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            using var response = await httpClient.SendAsync(request, cancelToken);
            var responseText = await response.Content.ReadAsStringAsync(cancelToken);

            if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
            {
                var validation = System.Text.Json.JsonSerializer.Deserialize<HTTPValidationError>(responseText);
                var validationMessage = validation?.Detail != null
                    ? string.Join(", ", validation.Detail.Select(d => d.Msg))
                    : responseText;
                throw new InvalidOperationException(validationMessage);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"BFL outpaint request failed ({(int)response.StatusCode}): {responseText}");
            }

            var result = System.Text.Json.JsonSerializer.Deserialize<AsyncResponse>(responseText);
            if (result == null)
            {
                throw new InvalidOperationException("BFL outpaint request did not return a valid response");
            }

            return result;
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
