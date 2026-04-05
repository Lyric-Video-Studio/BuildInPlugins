using Google.GenAI;
using Google.GenAI.Types;
using PluginBase;
using System.Reflection;
using System.Text.Json.Nodes;

namespace GooglePlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class GooglePlugin : IImagePlugin, ICancellableGeneration, IVideoPlugin, IImportFromImage
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
                var effectiveImage = !string.IsNullOrEmpty(ip.ImageSource) ? ip.ImageSource : tp.ImageSource;
                var prompt = (tp.Prompt + " " + ip.Prompt).Trim();
                var getCOnfig = new GenerateContentConfig
                {
                    ThinkingConfig = new ThinkingConfig
                    {
                        ThinkingLevel = "MINIMAL"
                    },
                    ImageConfig = new ImageConfig
                    {
                        ImageSize = tp.Size                    
                    },
                    ResponseModalities = new List<string>
                    {
                        "IMAGE",
                        "TEXT"
                    }
                };   

                var contents = new List<Content>
                {
                    new Content
                    {
                        Role = "user",
                        Parts = new List<Part>
                        {
                            new Part { Text = prompt },
                        }
                    },
                };

                if (!string.IsNullOrEmpty(effectiveImage))
                {
                    var imageBytes = await System.IO.File.ReadAllBytesAsync(effectiveImage);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    contents.First().Parts.Add(new Part()
                    {
                        InlineData = new Blob
                        {
                            MimeType = CommonConstants.GetMimeType(Path.GetExtension(effectiveImage)),
                            Data = imageBytes
                        }
                    });
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                }

                await foreach (var chunk in _googleAi.Models.GenerateContentStreamAsync(tp.Model, contents, getCOnfig))
                {
                    if (chunk.Candidates == null || chunk.Candidates.Count == 0 ||
                        chunk.Candidates[0].Content?.Parts == null)
                    {
                        continue;
                    }
                    var part = chunk.Candidates[0].Content.Parts[0];
                    if (part.InlineData?.Data != null)
                    {
                        var inlineData = part.InlineData;
                        var dataBuffer = inlineData.Data;
                        var fileExtension = GetFileExtension(inlineData.MimeType);
                        return new ImageResponse() { Success = dataBuffer.Length > 0, Image = Convert.ToBase64String(dataBuffer), ImageFormat = $"{fileExtension}" };
                    }
                    else
                    {
                        Console.WriteLine(chunk);
                    }
                }
            }
            throw new Exception("Internal error");
        }

        static string GetFileExtension(string mimeType)
        {
            return mimeType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "audio/wav" => ".wav",
                "audio/mpeg" => ".mp3",
                _ => ".bin"
            };
        }

        public async Task<VideoResponse> GetVideo(object trackPayload, object itemsPayload, string folderToSaveVideo)
        {

            if (_connectionSettings == null || string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return new VideoResponse { Success = false, ErrorMsg = "Uninitialized" };
            }

            if (trackPayload is VideoTrackPayload tp && itemsPayload is VideoItemPayload ip)
            {
                var effectiveImage = !string.IsNullOrEmpty(ip.ImageSource) ? ip.ImageSource : tp.ImageSource;
                var prompt = (tp.Prompt + " " + ip.Prompt).Trim();
                var getCOnfig = new GenerateVideosConfig
                {
                    PersonGeneration = tp.Model.Contains("lite") ? null : "allow_adult", 
                    AspectRatio = tp.AspectRatio, 
                    DurationSeconds = int.Parse(ip.Duration), 
                    Resolution = tp.Resolution
                };

                var source = new GenerateVideosSource
                {
                    Prompt = prompt
                };

                if (!string.IsNullOrEmpty(effectiveImage))
                {
                    source.Image = Image.FromFile(effectiveImage);
                }

                var res = await _googleAi.Models.GenerateVideosAsync(tp.Model, source, getCOnfig);

                while (res.Done != true)
                {
                    try
                    {
                        await Task.Delay(5000);
                        res = await _googleAi.Operations.GetAsync(res, null);
                    }
                    catch (TaskCanceledException)
                    {
                        Console.WriteLine("Task was cancelled while waiting.");
                        break;
                    }
                }

                if (res.Response?.GeneratedVideos?.Count == 0 || res.Response?.GeneratedVideos == null)
                {
                    return new VideoResponse { Success = false, ErrorMsg = "failed to generate video" };
                }

                // Download the video file.
                for (var i = 0; i < res.Response.GeneratedVideos.Count; i++)
                {
                    var targetFile = Path.Combine(folderToSaveVideo, $"{Guid.NewGuid()}.mp4");
                    await _googleAi.Files.DownloadToFileAsync(
                        generatedVideo: res.Response.GeneratedVideos[i],
                        outputPath: targetFile
                    );
                    // Current fps is not selectable
                    return new VideoResponse() { Success = true, Fps = 24, VideoFile = targetFile };
                }

            }
            throw new Exception("Internal error");
            
        }

        /*private (string img, string format) ExtractImageDataBase64(GenerateContentResponse response)
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
        }*/

        private Client _googleAi;

        public async Task<string> Initialize(object settings)
        {
            if (JsonHelper.DeepCopy<ConnectionSettings>(settings) is ConnectionSettings s)
            {
                _connectionSettings = s;
                _isInitialized = !string.IsNullOrEmpty(s.AccessToken);
                _googleAi = new Client(
                    apiKey: _connectionSettings.AccessToken
                );

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

        public object DeserializePayload(string fileName)
        {
            switch (CurrentTrackType)
            {
                case IPluginBase.TrackType.Image:
                    return JsonHelper.Deserialize<ImageTrackPayload>(fileName);

                case IPluginBase.TrackType.Video:
                    return JsonHelper.Deserialize<VideoTrackPayload>(fileName);

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
                    return new VideoItemPayload() { Prompt = text };

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
                    return JsonHelper.ToExactType<VideoItemPayload>(obj);

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
                    return JsonHelper.ToExactType<VideoTrackPayload>(obj);

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

            if (itemPayload is VideoItemPayload vi)
            {
                return vi.Prompt;
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
                    return new VideoTrackPayload();

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
                    return new VideoItemPayload(); ;

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
                    return JsonHelper.DeepCopy<VideoTrackPayload>(obj); ;

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
                    return JsonHelper.DeepCopy<VideoItemPayload>(obj);

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
                    if (payload is VideoItemPayload vi && string.IsNullOrEmpty(vi.Prompt))
                    {
                        return (false, "Prompt empty");
                    }
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

            if (trackPayload is VideoTrackPayload vi && itemPayload is VideoItemPayload vi2)
            {
                return new List<string> { vi.ImageSource, vi2.ImageSource };
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

            if (trackPayload is ImageTrackPayload vi && itemPayload is ImageItemPayload vi2)
            {
                for (int i = 0; i < originalPath.Count; i++)
                {
                    if (originalPath[i] == vi.ImageSource)
                    {
                        vi.ImageSource = newPath[i];
                    }

                    if (originalPath[i] == vi2.ImageSource)
                    {
                        vi2.ImageSource = newPath[i];
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

            if (payload is VideoItemPayload vi)
            {
                vi.Prompt = text;
            }
        }

        public void UserDataDeleteRequested()
        {
            if (_connectionSettings != null)
            {
                _connectionSettings.DeleteTokens();
            }
        }

        public object ItemPayloadFromImageSource(string imgSource)
        {
            if (CurrentTrackType == IPluginBase.TrackType.Image)
            {
                return new ImageItemPayload() { ImageSource = imgSource };
            }

            if (CurrentTrackType == IPluginBase.TrackType.Video)
            {
                return new VideoItemPayload() { ImageSource = imgSource };
            }

            return null;
        }
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}