using Mscc.GenerativeAI;
using PluginBase;
using System.Text.Json.Nodes;

namespace GooglePlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class GooglePlugin : IImagePlugin, ICancellableGeneration
    {
        public const string PluginName = "GooglePluginBuildIn";
        public string UniqueName { get => PluginName; }
        public string DisplayName { get => "Google"; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Hosted by Google. You need to have your authorization token";

        public bool AsynchronousGeneration { get; } = true;

        public string[] SettingsLinks => new[] { "https://aistudio.google.com/apikey" };

        public IPluginBase.TrackType CurrentTrackType { get; set; }

        private ConnectionSettings _connectionSettings = new ConnectionSettings();

        public async Task<ImageResponse> GetImage(object trackPayload, object itemsPayload)
        {
            if (_connectionSettings == null || string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return new ImageResponse { Success = false, ErrorMsg = "Uninitialized" };
            }

            if (trackPayload is ImageTrackPayload tp && itemsPayload is ImageItemPayload ip)
            {
                if (_generativeModel == null)
                {
                    _generativeModel = _googleAi.GenerativeModel(tp.Model);
                }
                var effectiveImage = !string.IsNullOrEmpty(ip.ImageSource) ? ip.ImageSource : tp.ImageSource;
                var prompt = (tp.Prompt + " " + ip.Prompt).Trim();
                GenerationConfig? genConfig = null;
                if (tp.Model == "gemini-3-pro-image-preview")
                {
                    var imgSize = Enum.GetValues<ImageSize>().FirstOrDefault(s => s.ToString().EndsWith(tp.Size, StringComparison.InvariantCultureIgnoreCase));

                    genConfig = new GenerationConfig();
                    genConfig.ImageConfig = new ImageConfig() { ImageSize = imgSize };
                }

                if (string.IsNullOrEmpty(effectiveImage))
                {
                    var request = new GenerateContentRequest(prompt);
                    request.GenerationConfig = genConfig;
                    var response = await _generativeModel.GenerateContent(request, cancellationToken: ct);
                    var imageData = ExtractImageDataBase64(response);
                    return new ImageResponse() { Success = !string.IsNullOrEmpty(imageData.img), Image = imageData.img, ImageFormat = $".{imageData.format}" };
                }
                else
                {
                    var imageBytes = await File.ReadAllBytesAsync(effectiveImage);
                    var mimeType = Path.GetExtension(effectiveImage).ToLower() switch
                    {
                        ".jpg" or ".jpeg" => "image/jpeg",
                        ".png" => "image/png",
                        _ => throw new NotSupportedException("Unsupported image format.")
                    };

                    var parts = new List<IPart>
                    {
                        new TextData { Text = prompt },
                        new InlineData { MimeType = mimeType, Data = Convert.ToBase64String(imageBytes) }
                    };

                    var response = await _generativeModel.GenerateContent(parts, genConfig, cancellationToken: ct);
                    var imageData = ExtractImageDataBase64(response);
                    return new ImageResponse() { Success = !string.IsNullOrEmpty(imageData.img), Image = imageData.img, ImageFormat = $".{imageData.format}" };
                }
            }
            throw new Exception("Internal error");
        }

        private (string img, string format) ExtractImageDataBase64(GenerateContentResponse response)
        {
            var imageStrings = new List<string>();
            if (response.Candidates != null)
            {
                foreach (var candidate in response.Candidates)
                {
                    if (candidate.Content?.Parts != null)
                    {
                        foreach (var part in candidate.Content.Parts)
                        {
                            if (part.InlineData != null && !string.IsNullOrEmpty(part.InlineData.Data) && part.InlineData.MimeType == "image/png")
                            {
                                return (part.InlineData.Data, "png");
                            }

                            if (part.InlineData != null && !string.IsNullOrEmpty(part.InlineData.Data) && part.InlineData.MimeType == "image/jpeg")
                            {
                                return (part.InlineData.Data, "jpg");
                            }
                        }
                    }
                }
            }
            return ("", "");
        }

        private GenerativeModel _generativeModel;
        private GoogleAI _googleAi;

        public async Task<string> Initialize(object settings)
        {
            if (JsonHelper.DeepCopy<ConnectionSettings>(settings) is ConnectionSettings s)
            {
                _connectionSettings = s;
                _isInitialized = !string.IsNullOrEmpty(s.AccessToken);
                _googleAi = new GoogleAI(_connectionSettings.AccessToken);
                _generativeModel?.Dispose();
                _generativeModel = null;
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

            if (propertyName == nameof(ImageTrackPayload.Size))
            {
                return ["1K", "2K", "4K"];
            }

            if (propertyName == nameof(ImageTrackPayload.Model))
            {
                return ["gemini-3-pro-image-preview", "gemini-2.5-flash-image-preview"];
            }

            return Array.Empty<string>();
        }

        public object DeserializePayload(string fileName)
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Image:
                    return JsonHelper.Deserialize<ImageTrackPayload>(fileName);

                case IPluginBase.TrackType.Video:
                    break;

                case IPluginBase.TrackType.Audio:
                    break;

                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public IPluginBase CreateNewInstance()
        {
            return new GooglePlugin();
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

        public object ItemPayloadFromLyrics(string text)
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Image:
                    return new ImageItemPayload() { Prompt = text };

                case IPluginBase.TrackType.Video:
                    break;

                case IPluginBase.TrackType.Audio:
                    break;

                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public object ObjectToItemPayload(JsonObject obj)
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Image:
                    return JsonHelper.ToExactType<ImageItemPayload>(obj);

                case IPluginBase.TrackType.Video:
                    break;

                case IPluginBase.TrackType.Audio:
                    break;

                default:
                    break;
            }

            throw new NotImplementedException();
        }

        public object ObjectToTrackPayload(JsonObject obj)
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Image:
                    return JsonHelper.ToExactType<ImageTrackPayload>(obj);

                case IPluginBase.TrackType.Video:
                    break;

                case IPluginBase.TrackType.Audio:
                    break;

                default:
                    break;
            }

            throw new NotImplementedException();
        }

        public object ObjectToGeneralSettings(JsonObject obj)
        {
            return JsonHelper.ToExactType<ConnectionSettings>(obj);
        }

        public string TextualRepresentation(object itemPayload)
        {
            if (itemPayload is ImageItemPayload ip)
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
                    return new ImageTrackPayload();

                case IPluginBase.TrackType.Video:
                    break;

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
                    return new ImageItemPayload();

                case IPluginBase.TrackType.Video:
                    break;

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
                    return JsonHelper.DeepCopy<ImageTrackPayload>(obj);

                case IPluginBase.TrackType.Video:
                    break;

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
                    return JsonHelper.DeepCopy<ImageItemPayload>(obj);

                case IPluginBase.TrackType.Video:
                    break;

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
                case IPluginBase.TrackType.Image:
                    if (payload is ImageItemPayload ip && string.IsNullOrEmpty(ip.Prompt))
                    {
                        return (false, "Prompt empty");
                    }
                    return (true, "");

                case IPluginBase.TrackType.Video:
                    return (true, "");

                case IPluginBase.TrackType.Audio:
                    return (true, "");

                default:
                    break;
            }
            return (true, "");
        }

        public List<string> FilePathsOnPayloads(object trackPayload, object itemPayload)
        {
            if (trackPayload is ImageTrackPayload ip && itemPayload is ImageItemPayload tp)
            {
                return new List<string> { ip.ImageSource, tp.ImageSource };
            }
            return new List<string>();
        }

        public void ReplaceFilePathsOnPayloads(List<string> originalPath, List<string> newPath, object trackPayload, object itemPayload)
        {
            if (trackPayload is ImageTrackPayload ip && itemPayload is ImageItemPayload tp)
            {
                for (int i = 0; i < originalPath.Count; i++)
                {
                    if (originalPath[i] == ip.ImageSource)
                    {
                        ip.ImageSource = newPath[i];
                    }

                    if (originalPath[i] == tp.ImageSource)
                    {
                        tp.ImageSource = newPath[i];
                    }
                }
            }
        }

        private CancellationToken ct;

        public void SetCancallationToken(CancellationToken cancellationToken)
        {
            ct = cancellationToken;
        }

        public void AppendToPayloadFromLyrics(string text, object payload)
        {
            if (payload is ImageItemPayload ip)
            {
                ip.Prompt = text;
            }
        }
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}