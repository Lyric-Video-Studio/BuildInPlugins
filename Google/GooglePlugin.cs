using Google.GenAI;
using Google.GenAI.Types;
using PluginBase;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;

namespace GooglePlugin
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class GooglePlugin : IImagePlugin, IAudioPlugin, ICancellableGeneration, IVideoPlugin, IImportFromImage
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
                    var part = chunk.Candidates[0].Content!.Parts![0];
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

        public async Task<AudioResponse> GetAudio(object trackPayload, object itemsPayload, string folderToSaveAudio)
        {
            if (_connectionSettings == null || string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return new AudioResponse { Success = false, ErrorMsg = "Uninitialized" };
            }

            if (trackPayload is GoogleAudioTrackPayload tp && itemsPayload is GoogleAudioItemPayload ip)
            {
                if (GoogleAudioTrackPayload.IsLyriaModel(tp.Model))
                {
                    return await GetMusicAudio(tp, ip, folderToSaveAudio);
                }

                var prompt = (tp.Prompt + "\n" + ip.Prompt).Trim();
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

                var config = new GenerateContentConfig
                {
                    Temperature = tp.Temperature,
                    ResponseModalities = new List<string> { "audio" },
                    SpeechConfig = new SpeechConfig()
                };

                if (tp.MultiSpeaker)
                {
                    config.SpeechConfig.MultiSpeakerVoiceConfig = new MultiSpeakerVoiceConfig
                    {
                        SpeakerVoiceConfigs = new List<SpeakerVoiceConfig>
                        {
                            new SpeakerVoiceConfig
                            {
                                Speaker = string.IsNullOrWhiteSpace(tp.Speaker1Name) ? "Speaker 1" : tp.Speaker1Name,
                                VoiceConfig = new VoiceConfig
                                {
                                    PrebuiltVoiceConfig = new PrebuiltVoiceConfig
                                    {
                                        VoiceName = tp.Speaker1Voice
                                    }
                                }
                            },
                            new SpeakerVoiceConfig
                            {
                                Speaker = string.IsNullOrWhiteSpace(tp.Speaker2Name) ? "Speaker 2" : tp.Speaker2Name,
                                VoiceConfig = new VoiceConfig
                                {
                                    PrebuiltVoiceConfig = new PrebuiltVoiceConfig
                                    {
                                        VoiceName = tp.Speaker2Voice
                                    }
                                }
                            },
                        }
                    };
                }
                else
                {
                    config.SpeechConfig.VoiceConfig = new VoiceConfig
                    {
                        PrebuiltVoiceConfig = new PrebuiltVoiceConfig
                        {
                            VoiceName = tp.Speaker1Voice
                        }
                    };
                }

                AudioFormatData? formatInfo = null;

                try
                {
                    var response = await _googleAi.Models.GenerateContentAsync(tp.Model, contents, config, ct);
                    if (response.Candidates == null || response.Candidates.Count == 0)
                    {
                        return new AudioResponse { Success = false, ErrorMsg = "No candidates were returned" };
                    }

                    var textResponse = new StringBuilder();
                    var audioParts = new List<(byte[] Data, string MimeType)>();

                    foreach (var candidate in response.Candidates)
                    {
                        var parts = candidate.Content?.Parts;
                        if (parts == null)
                        {
                            continue;
                        }

                        foreach (var part in parts)
                        {
                            if (part.InlineData?.Data != null && part.InlineData.Data.Length > 0)
                            {
                                audioParts.Add((part.InlineData.Data, part.InlineData.MimeType ?? ""));
                            }
                            else if (!string.IsNullOrWhiteSpace(part.Text))
                            {
                                textResponse.AppendLine(part.Text);
                            }
                        }
                    }

                    if (audioParts.Count == 0)
                    {
                        var errorMsg = textResponse.Length > 0 ? textResponse.ToString().Trim() : "No audio data was returned";
                        return new AudioResponse { Success = false, ErrorMsg = errorMsg };
                    }

                    var primaryAudio = audioParts[0];
                    var mimeType = primaryAudio.MimeType;

                    if (mimeType.StartsWith("audio/l", StringComparison.OrdinalIgnoreCase))
                    {
                        var bitsPerSample = 16;
                        var sampleRate = 24000;
                        var channels = 1;
                        var split = mimeType.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        foreach (var item in split)
                        {
                            if (item.StartsWith("audio/l", StringComparison.OrdinalIgnoreCase))
                            {
                                if (int.TryParse(item["audio/l".Length..], out var parsedBits))
                                {
                                    bitsPerSample = parsedBits;
                                }
                                continue;
                            }

                            var chOrRate = item.Split('=', 2, StringSplitOptions.TrimEntries);
                            if (chOrRate.Length != 2)
                            {
                                continue;
                            }

                            switch (chOrRate[0])
                            {
                                case "rate":
                                    if (int.TryParse(chOrRate[1], out var parsedRate))
                                    {
                                        sampleRate = parsedRate;
                                    }
                                    break;
                                case "channels":
                                    if (int.TryParse(chOrRate[1], out var parsedChannels))
                                    {
                                        channels = parsedChannels;
                                    }
                                    break;
                            }
                        }

                        formatInfo = new AudioFormatData
                        {
                            Bitrate = bitsPerSample,
                            AudioSampleRate = sampleRate,
                            Channels = channels
                        };
                    }

                    var fileExtension = GetFileExtension(mimeType);
                    var targetFile = Path.Combine(folderToSaveAudio, $"{Guid.NewGuid()}{fileExtension}");
                    await System.IO.File.WriteAllBytesAsync(targetFile, primaryAudio.Data, ct);

                    var result = new AudioResponse
                    {
                        Success = true,
                        AudioFile = targetFile,
                        AudioFormat = fileExtension.TrimStart('.'),
                        FormatInfo = formatInfo
                    };

                    if (audioParts.Count > 1)
                    {
                        var alternativeAudio = audioParts[1];
                        var alternativeExtension = GetFileExtension(alternativeAudio.MimeType);
                        var alternativeFile = Path.Combine(folderToSaveAudio, $"{Guid.NewGuid()}{alternativeExtension}");
                        await System.IO.File.WriteAllBytesAsync(alternativeFile, alternativeAudio.Data, ct);
                        result.AlternativeAudioFile = alternativeFile;
                    }

                    return result;
                }
                catch (OperationCanceledException)
                {
                    return new AudioResponse { Success = false, ErrorMsg = "Cancelled" };
                }
            }

            return new AudioResponse { Success = false, ErrorMsg = "Track playoad or item payload object not valid" };
        }

        private async Task<AudioResponse> GetMusicAudio(GoogleAudioTrackPayload tp, GoogleAudioItemPayload ip, string folderToSaveAudio)
        {
            var prompt = (tp.Prompt + "\n" + ip.Prompt).Trim();
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

            var config = new GenerateContentConfig
            {
                ResponseModalities = new List<string> { "AUDIO", "TEXT" }
            };

            if (GoogleAudioTrackPayload.IsLyriaPro(tp.Model) && string.Equals(tp.MusicFormat, "wav", StringComparison.OrdinalIgnoreCase))
            {
                config.ResponseMimeType = "audio/wav";
            }

            try
            {
                var response = await _googleAi.Models.GenerateContentAsync(tp.Model, contents, config, ct);
                if (response.Candidates == null || response.Candidates.Count == 0)
                {
                    return new AudioResponse { Success = false, ErrorMsg = "No candidates were returned" };
                }

                var textResponse = new StringBuilder();
                (byte[] Data, string MimeType)? firstAudio = null;

                foreach (var candidate in response.Candidates)
                {
                    var parts = candidate.Content?.Parts;
                    if (parts == null)
                    {
                        continue;
                    }

                    foreach (var part in parts)
                    {
                        if (part.InlineData?.Data != null && part.InlineData.Data.Length > 0)
                        {
                            firstAudio ??= (part.InlineData.Data, part.InlineData.MimeType ?? "");
                        }
                        else if (!string.IsNullOrWhiteSpace(part.Text))
                        {
                            textResponse.AppendLine(part.Text);
                        }
                    }
                }

                if (!firstAudio.HasValue)
                {
                    var errorMsg = textResponse.Length > 0 ? textResponse.ToString().Trim() : "No audio data was returned";
                    return new AudioResponse { Success = false, ErrorMsg = errorMsg };
                }

                var mimeType = firstAudio.Value.MimeType;
                var fileExtension = GetFileExtension(mimeType);
                var targetFile = Path.Combine(folderToSaveAudio, $"{Guid.NewGuid()}{fileExtension}");
                await System.IO.File.WriteAllBytesAsync(targetFile, firstAudio.Value.Data, ct);

                return new AudioResponse
                {
                    Success = true,
                    AudioFile = targetFile,
                    AudioFormat = fileExtension.TrimStart('.')
                };
            }
            catch (OperationCanceledException)
            {
                return new AudioResponse { Success = false, ErrorMsg = "Cancelled" };
            }
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
                    return JsonHelper.Deserialize<GoogleAudioTrackPayload>(fileName);

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
                    return new GoogleAudioItemPayload() { Prompt = text };

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
                    return JsonHelper.ToExactType<GoogleAudioItemPayload>(obj);

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
                    return JsonHelper.ToExactType<GoogleAudioTrackPayload>(obj);

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

            if (itemPayload is GoogleAudioItemPayload ai)
            {
                return ai.Prompt;
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

                case IPluginBase.TrackType.Audio:
                    return new GoogleAudioTrackPayload();

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

                case IPluginBase.TrackType.Audio:
                    return new GoogleAudioItemPayload();

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

                case IPluginBase.TrackType.Audio:
                    return JsonHelper.DeepCopy<GoogleAudioTrackPayload>(obj);

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

                case IPluginBase.TrackType.Audio:
                    return JsonHelper.DeepCopy<GoogleAudioItemPayload>(obj);

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
                return (false, "Access key missing...");
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
                    if (payload is GoogleAudioItemPayload ai && string.IsNullOrWhiteSpace(ai.Prompt))
                    {
                        return (false, "Prompt empty");
                    }
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

            if (payload is GoogleAudioItemPayload ai)
            {
                ai.Prompt = text;
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
