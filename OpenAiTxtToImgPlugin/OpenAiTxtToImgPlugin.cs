using PluginBase;
using OpenAI;
using OpenAI.Images;
using System.Text.Json.Nodes;

namespace OpenAiTxtToImgPlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class OpenAiTxtToImgPlugin : IImagePlugin
    {
        public string UniqueName { get => "OpenAiTxtToImageBuildIn"; }
        public string DisplayName { get => "Open Ai TxtToImg (dall-e-3)"; }

        public object GeneralDefaultSettings => new ConnectionSettings();

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public string SettingsHelpText => "Hosted by Open.ai. You need to have your authorization token";

        public string[] SettingsLinks => ["https://platform.openai.com/api-keys"];

        public string ImageFormat => "png";

        public bool AsynchronousGeneration { get; } = true;

        private ConnectionSettings _connectionSettings = new ConnectionSettings();

        private OpenAIClient openAIClient;

        public IPluginBase.TrackType CurrentTrackType { get; set; }

        public object DefaultPayloadForImageItem()
        {
            return new ItemPayload();
        }

        private string[] GetFunctionPropertyArray(string prop)
        {
            var prope = typeof(ImageGenerationRequest).GetProperties()
                .Where(p => p.Name == prop)
                .FirstOrDefault();

            var fpa = prope?
                .GetCustomAttributes(false)?
                .OfType<FunctionPropertyAttribute>()?
                .FirstOrDefault();

            return fpa?
                .PossibleValues?
                .OfType<string>()?
                .ToArray();
        }

        public object DefaultPayloadForImageTrack()
        {
            return new TrackPayload("", OpenAI.Models.Model.DallE_3, 1, GetFunctionPropertyArray(nameof(ImageGenerationRequest.Quality))[0], ImageResponseFormat.B64_Json,
                GetFunctionPropertyArray(nameof(ImageGenerationRequest.Size))[0], GetFunctionPropertyArray(nameof(ImageGenerationRequest.Style))[0]);
        }

        public async Task<ImageResponse> GetImage(object trackPayload, object itemsPayload)
        {
            if (_connectionSettings == null || string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return new ImageResponse { Success = false, ErrorMsg = "Uninitialized" };
            }

            if (trackPayload is TrackPayload newTp && itemsPayload is ItemPayload newIp)
            {
                var payload = new ImageGenerationRequest($"{newIp.Prompt} {newTp.Prompt}", newTp.Model, 1, newTp.Quality, ImageResponseFormat.B64_Json, newTp.Size, style: newTp.Style);

                try
                {
                    var res = await openAIClient.ImagesEndPoint.GenerateImageAsync(payload);

                    var resultList = res.ToList();

                    if (resultList.Count > 0)
                    {
                        return new ImageResponse { ImageFormat = "png", Success = true, Image = resultList[0].B64_Json, Params = new List<(string, string)>() { ("Revised prompt", resultList[0].RevisedPrompt) } };
                    }
                    else
                    {
                        return new ImageResponse { ErrorMsg = "No results in openAi response", Success = false };
                    }
                }
                catch (Exception e)
                {
                    return new ImageResponse { ErrorMsg = e.ToString(), Success = false };
                }
            }
            else
            {
                return new ImageResponse { ErrorMsg = "Track playoad or item payload object not valid", Success = false };
            }
        }

        public async Task<string> Initialize(object settings)
        {
            if (JsonHelper.DeepCopy<ConnectionSettings>(settings) is ConnectionSettings s && !string.IsNullOrEmpty(s.AccessToken))
            {
                _connectionSettings = s;
                _isInitialized = true;
                openAIClient = new(s.AccessToken);
                return "";
            }
            else
            {
                openAIClient = null;
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
            var values = GetFunctionPropertyArray(propertyName);
            return values;
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
            return JsonHelper.Deserialize<ItemPayload>(fileName);
        }

        public IPluginBase CreateNewInstance()
        {
            return new OpenAiTxtToImgPlugin();
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
                return (false, "Auth token is missing");
            }
            if (payload is ItemPayload ip && string.IsNullOrEmpty(ip.Prompt))
            {
                return (false, "Prompt missing");
            }
            return (true, "");
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
            return [];
        }

        public void ReplaceFilePathsOnPayloads(List<string> originalPath, List<string> newPath, object trackPayload, object itemPayload)
        {
        }

        public void AppendToPayloadFromLyrics(string text, object payload)
        {
            if (payload is ItemPayload ip)
            {
                ip.Prompt = text;
            }
        }
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}