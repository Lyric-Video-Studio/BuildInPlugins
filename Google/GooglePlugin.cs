using Google.GenAI;
using Google.GenAI.Types;
using Newtonsoft.Json.Bson;
using PluginBase;
using System.Reflection;
using System.Text;
using System.Text.Json;
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

        private static readonly TimeSpan RequestWindow = TimeSpan.FromMinutes(1);
        private readonly Queue<DateTimeOffset> _videoRequestTimes = new();
        private readonly Queue<DateTimeOffset> _audioRequestTimes = new();
        private readonly Queue<DateTimeOffset> _imageRequestTimes = new();
        private readonly object _videoRequestLock = new();
        private readonly object _audioRequestLock = new();
        private readonly object _imageRequestLock = new();

        public async Task<ImageResponse> GetImage(object trackPayload, object itemsPayload)
        {
            if (_connectionSettings == null || string.IsNullOrEmpty(_connectionSettings.AccessToken))
            {
                return new ImageResponse { Success = false, ErrorMsg = "Uninitialized" };
            }

            await WaitForRequestSlotAsync(_connectionSettings.ImageRpmLimit, _imageRequestTimes, _imageRequestLock, ct);

            if (trackPayload is ImageTrackPayload tp && itemsPayload is ImageItemPayload ip)
            {
                var effectiveImages = new List<string>();
                var img = !string.IsNullOrEmpty(ip.ImageSource) ? ip.ImageSource : tp.ImageSource;

                if (!string.IsNullOrEmpty(img))
                {
                    effectiveImages.Add(img);
                }

                img = !string.IsNullOrEmpty(ip.ImageSource2) ? ip.ImageSource2 : tp.ImageSource2;

                if (!string.IsNullOrEmpty(img))
                {
                    effectiveImages.Add(img);
                }

                img = !string.IsNullOrEmpty(ip.ImageSource3) ? ip.ImageSource3 : tp.ImageSource3;

                if (!string.IsNullOrEmpty(img))
                {
                    effectiveImages.Add(img);
                }

                img = !string.IsNullOrEmpty(ip.ImageSource4) ? ip.ImageSource4 : tp.ImageSource4;

                if (!string.IsNullOrEmpty(img))
                {
                    effectiveImages.Add(img);
                }

                var prompt = (tp.Prompt + " " + ip.Prompt).Trim();
                var getCOnfig = new GenerateContentConfig
                {
                    ThinkingConfig = tp.Model is "gemini-2.5-flash-image" ? null : new ThinkingConfig
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

                foreach (var effectiveImage in effectiveImages)
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
                        if (chunk?.Candidates?.Count > 0 && chunk.Candidates[0].FinishReason == FinishReason.ImageOther)
                        {
                            throw new Exception("Google API reported unknown image generation error");
                        }
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

            await WaitForRequestSlotAsync(_connectionSettings.AudioRpmLimit, _audioRequestTimes, _audioRequestLock, ct);

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

            await WaitForRequestSlotAsync(_connectionSettings.VideRpmLimit, _videoRequestTimes, _videoRequestLock, ct);

            if (trackPayload is VideoTrackPayload tp && itemsPayload is VideoItemPayload ip)
            {
                var effectiveImages = CollectEffectiveVideoImages(tp, ip);
                var prompt = (tp.Prompt + " " + ip.Prompt).Trim();

                if (IsOmniVideoModel(tp.Model))
                {
                    return await GetOmniVideo(tp, ip, prompt, effectiveImages, folderToSaveVideo);
                }

                var getCOnfig = new GenerateVideosConfig
                {
                    PersonGeneration =  null,
                    AspectRatio = tp.AspectRatio == "Auto" ? null : tp.AspectRatio,
                    DurationSeconds = int.Parse(ip.Duration),
                    Resolution = tp.Resolution
                };

                var source = new GenerateVideosSource
                {
                    Prompt = prompt
                };

                var veoImages = effectiveImages.Take(3).ToList();
                if (veoImages.Count == 1)
                {
                    source.Image = Image.FromFile(veoImages.FirstOrDefault());
                }
                else if (veoImages.Count > 1)
                {
                    getCOnfig.ReferenceImages = new List<VideoGenerationReferenceImage>();
                    foreach(var refImg in veoImages)
                    {
                        var gRefImg = new VideoGenerationReferenceImage();
                        gRefImg.Image = Image.FromFile(refImg);
                        getCOnfig.ReferenceImages.Add(gRefImg);
                    }
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
                    return new VideoResponse { Success = false, ErrorMsg = res?.Error != null ? string.Join(", ", res?.Error?.Values.Select(s => s.ToString())): "Unknown server errror (google API's)" };
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

        private static bool IsOmniVideoModel(string model)
        {
            return string.Equals(model, VideoTrackPayload.ModelGeminiOmniFlashPreview, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<VideoResponse> GetOmniVideo(VideoTrackPayload tp, VideoItemPayload ip, string prompt, List<string> effectiveImages, string folderToSaveVideo)
        {
            using var httpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
            var request = await BuildOmniVideoRequest(tp, ip, prompt, effectiveImages);
            var url = $"https://generativelanguage.googleapis.com/v1beta/interactions?key={Uri.EscapeDataString(_connectionSettings.AccessToken)}";
            using var content = new StringContent(request.ToJsonString(), Encoding.UTF8, "application/json");
            using var response = await httpClient.PostAsync(url, content, ct);
            var responsePayload = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                return new VideoResponse { Success = false, ErrorMsg = GetGoogleJsonError(responsePayload) };
            }

            var responseJson = JsonNode.Parse(responsePayload);
            ip.LastInteractionId = responseJson?["id"]?.ToString() ?? ip.LastInteractionId;
            var outputVideo = FindOmniOutputVideo(responseJson);
            if (outputVideo == null)
            {
                return new VideoResponse { Success = false, ErrorMsg = GetGoogleJsonError(responsePayload) };
            }

            var targetFile = Path.Combine(folderToSaveVideo, $"{Guid.NewGuid()}.mp4");
            var inlineData = outputVideo["data"]?.ToString();
            if (!string.IsNullOrWhiteSpace(inlineData))
            {
                await System.IO.File.WriteAllBytesAsync(targetFile, Convert.FromBase64String(inlineData), ct);
                return new VideoResponse() { Success = true, Fps = 24, VideoFile = targetFile };
            }

            var uri = outputVideo["uri"]?.ToString();
            if (!string.IsNullOrWhiteSpace(uri))
            {
                await DownloadOmniVideo(httpClient, uri, targetFile);
                return new VideoResponse() { Success = true, Fps = 24, VideoFile = targetFile };
            }

            return new VideoResponse { Success = false, ErrorMsg = "Google Omni response did not contain video data" };
        }

        private async Task<JsonObject> BuildOmniVideoRequest(VideoTrackPayload tp, VideoItemPayload ip, string prompt, List<string> effectiveImages)
        {
            var previousInteractionId = ip.PreviousInteractionId?.Trim();
            var videoSource = !string.IsNullOrWhiteSpace(ip.VideoSource) ? ip.VideoSource : tp.VideoSource;
            var hasVideoSource = !string.IsNullOrWhiteSpace(videoSource);
            var hasPreviousInteraction = !string.IsNullOrWhiteSpace(previousInteractionId);
            var task = ResolveOmniTask(tp.VideoTask, effectiveImages.Count, hasVideoSource, hasPreviousInteraction);
            var request = new JsonObject
            {
                ["model"] = tp.Model,
                ["response_format"] = BuildOmniResponseFormat(tp, task == "edit"),
                ["background"] = false,
                ["stream"] = false
            };

            if (hasPreviousInteraction)
            {
                request["previous_interaction_id"] = previousInteractionId;
            }

            var omniPrompt = AddOmniDurationToPrompt(prompt, ip.Duration);
            request["input"] = await BuildOmniInput(omniPrompt, effectiveImages, videoSource);

            if (!string.IsNullOrWhiteSpace(task))
            {
                request["generation_config"] = new JsonObject
                {
                    ["video_config"] = new JsonObject
                    {
                        ["task"] = task
                    }
                };
            }
            return request;
        }

        private static JsonObject BuildOmniResponseFormat(VideoTrackPayload tp, bool omitAspectRatio)
        {
            var responseFormat = new JsonObject
            {
                ["type"] = "video",
                ["delivery"] = "uri"
            };

            if (!omitAspectRatio && !string.IsNullOrWhiteSpace(tp.AspectRatio) && tp.AspectRatio != "Auto")
            {
                responseFormat["aspect_ratio"] = tp.AspectRatio;
            }

            return responseFormat;
        }
        private async Task<JsonNode> BuildOmniInput(string prompt, List<string> effectiveImages, string videoSource)
        {
            if (effectiveImages.Count == 0 && string.IsNullOrWhiteSpace(videoSource))
            {
                return JsonValue.Create(prompt);
            }

            var content = new JsonArray();
            if (!string.IsNullOrWhiteSpace(videoSource))
            {
                content.Add(await BuildOmniMediaPart("video", videoSource));
            }

            foreach (var image in effectiveImages.Take(6))
            {
                content.Add(await BuildOmniMediaPart("image", image));
            }

            content.Add(new JsonObject
            {
                ["type"] = "text",
                ["text"] = prompt
            });

            if (!string.IsNullOrWhiteSpace(videoSource))
            {
                return new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "user_input",
                        ["content"] = content
                    }
                };
            }

            return content;
        }

        private async Task<JsonObject> BuildOmniMediaPart(string type, string path)
        {
            var bytes = await System.IO.File.ReadAllBytesAsync(path, ct);
            return new JsonObject
            {
                ["type"] = type,
                ["data"] = Convert.ToBase64String(bytes),
                ["mime_type"] = CommonConstants.GetMimeType(Path.GetExtension(path))
            };
        }

        private static string AddOmniDurationToPrompt(string prompt, string duration)
        {
            var seconds = GetSelectedVideoDurationSeconds(duration);
            var durationInstruction = $"Create a video that is about {seconds} seconds long.";
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return durationInstruction;
            }

            return prompt.Trim() + " " + durationInstruction;
        }

        private static int GetSelectedVideoDurationSeconds(string duration)
        {
            if (!int.TryParse(duration, out var seconds))
            {
                seconds = 8;
            }

            return Math.Clamp(seconds, 3, 10);
        }

        private static string ResolveOmniTask(string selectedTask, int imageCount, bool hasVideo, bool hasPreviousInteraction)
        {
            return selectedTask switch
            {
                VideoTrackPayload.VideoTaskUnspecified => null,
                VideoTrackPayload.VideoTaskTextToVideo => "text_to_video",
                VideoTrackPayload.VideoTaskImageToVideo => "image_to_video",
                VideoTrackPayload.VideoTaskReferenceToVideo => "reference_to_video",
                VideoTrackPayload.VideoTaskEdit => "edit",
                _ when hasVideo || hasPreviousInteraction => "edit",
                _ when imageCount > 1 => "reference_to_video",
                _ when imageCount == 1 => "image_to_video",
                _ => "text_to_video"
            };
        }

        private static List<string> CollectEffectiveVideoImages(VideoTrackPayload tp, VideoItemPayload ip)
        {
            var effectiveImages = new List<string>();
            AddEffectivePath(effectiveImages, ip.ImageSource, tp.ImageSource);
            AddEffectivePath(effectiveImages, ip.ImageSource2, tp.ImageSource2);
            AddEffectivePath(effectiveImages, ip.ImageSource3, tp.ImageSource3);
            AddEffectivePath(effectiveImages, ip.ImageSource4, tp.ImageSource4);
            AddEffectivePath(effectiveImages, ip.ImageSource5, tp.ImageSource5);
            AddEffectivePath(effectiveImages, ip.ImageSource6, tp.ImageSource6);
            return effectiveImages;
        }

        private static void AddEffectivePath(List<string> paths, string itemPath, string trackPath)
        {
            var path = !string.IsNullOrEmpty(itemPath) ? itemPath : trackPath;
            if (!string.IsNullOrEmpty(path))
            {
                paths.Add(path);
            }
        }

        private static JsonNode FindOmniOutputVideo(JsonNode responseJson)
        {
            var directOutput = responseJson?["output_video"];
            if (directOutput != null)
            {
                return directOutput;
            }

            var steps = responseJson?["steps"]?.AsArray();
            if (steps == null)
            {
                return null;
            }

            foreach (var step in steps)
            {
                var content = step?["content"]?.AsArray();
                if (content == null)
                {
                    continue;
                }

                foreach (var part in content)
                {
                    if (part?["type"]?.ToString() == "video")
                    {
                        return part;
                    }
                }
            }

            return null;
        }

        private async Task DownloadOmniVideo(HttpClient httpClient, string uri, string targetFile)
        {
            var downloadUri = uri.Contains("key=", StringComparison.OrdinalIgnoreCase)
                ? uri
                : uri + (uri.Contains('?') ? "&" : "?") + $"key={Uri.EscapeDataString(_connectionSettings.AccessToken)}";
            using var downloadResponse = await httpClient.GetAsync(downloadUri, ct);
            var bytes = await downloadResponse.Content.ReadAsByteArrayAsync(ct);
            if (!downloadResponse.IsSuccessStatusCode)
            {
                var errorPayload = Encoding.UTF8.GetString(bytes);
                throw new Exception(GetGoogleJsonError(errorPayload));
            }

            await System.IO.File.WriteAllBytesAsync(targetFile, bytes, ct);
        }

        private static string GetGoogleJsonError(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return "Unknown Google API error";
            }

            try
            {
                var node = JsonNode.Parse(payload);
                return node?["error"]?["message"]?.ToString() ??
                    node?["message"]?.ToString() ??
                    node?["status"]?.ToString() ??
                    payload;
            }
            catch
            {
                return payload;
            }
        }
        private async Task WaitForRequestSlotAsync(int requestsPerMinute, Queue<DateTimeOffset> requestTimes, object requestLock, CancellationToken cancellationToken)
        {
            if (requestsPerMinute <= 0)
            {
                return;
            }

            while (true)
            {
                TimeSpan waitTime;

                lock (requestLock)
                {
                    var now = DateTimeOffset.UtcNow;
                    while (requestTimes.Count > 0 && now - requestTimes.Peek() >= RequestWindow)
                    {
                        requestTimes.Dequeue();
                    }

                    if (requestTimes.Count < requestsPerMinute)
                    {
                        requestTimes.Enqueue(now);
                        return;
                    }

                    waitTime = RequestWindow - (now - requestTimes.Peek());
                    if (waitTime < TimeSpan.FromMilliseconds(50))
                    {
                        waitTime = TimeSpan.FromMilliseconds(50);
                    }
                }

                await Task.Delay(waitTime, cancellationToken);
            }
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
                return new List<string> { ip.ImageSource, tp.ImageSource, ip.ImageSource2, tp.ImageSource2, ip.ImageSource3, tp.ImageSource3, ip.ImageSource4, tp.ImageSource4 };
            }

            if (trackPayload is VideoTrackPayload vi && itemPayload is VideoItemPayload vi2)
            {
                return new List<string>
                {
                    vi.ImageSource, vi2.ImageSource,
                    vi.ImageSource2, vi2.ImageSource2,
                    vi.ImageSource3, vi2.ImageSource3,
                    vi.ImageSource4, vi2.ImageSource4,
                    vi.ImageSource5, vi2.ImageSource5,
                    vi.ImageSource6, vi2.ImageSource6,
                    vi.VideoSource, vi2.VideoSource
                };
            }
            return new List<string>();
        }

        public void ReplaceFilePathsOnPayloads(List<string> originalPath, List<string> newPath, object trackPayload, object itemPayload)
        {
            if (trackPayload is VideoTrackPayload ip && itemPayload is VideoItemPayload tp)
            {
                ip.ImageSource = ReplacePayloadPath(originalPath, newPath, ip.ImageSource);
                tp.ImageSource = ReplacePayloadPath(originalPath, newPath, tp.ImageSource);
                ip.ImageSource2 = ReplacePayloadPath(originalPath, newPath, ip.ImageSource2);
                tp.ImageSource2 = ReplacePayloadPath(originalPath, newPath, tp.ImageSource2);
                ip.ImageSource3 = ReplacePayloadPath(originalPath, newPath, ip.ImageSource3);
                tp.ImageSource3 = ReplacePayloadPath(originalPath, newPath, tp.ImageSource3);
                ip.ImageSource4 = ReplacePayloadPath(originalPath, newPath, ip.ImageSource4);
                tp.ImageSource4 = ReplacePayloadPath(originalPath, newPath, tp.ImageSource4);
                ip.ImageSource5 = ReplacePayloadPath(originalPath, newPath, ip.ImageSource5);
                tp.ImageSource5 = ReplacePayloadPath(originalPath, newPath, tp.ImageSource5);
                ip.ImageSource6 = ReplacePayloadPath(originalPath, newPath, ip.ImageSource6);
                tp.ImageSource6 = ReplacePayloadPath(originalPath, newPath, tp.ImageSource6);
                ip.VideoSource = ReplacePayloadPath(originalPath, newPath, ip.VideoSource);
                tp.VideoSource = ReplacePayloadPath(originalPath, newPath, tp.VideoSource);
            }

            if (trackPayload is ImageTrackPayload vi && itemPayload is ImageItemPayload vi2)
            {
                vi.ImageSource = ReplacePayloadPath(originalPath, newPath, vi.ImageSource);
                vi2.ImageSource = ReplacePayloadPath(originalPath, newPath, vi2.ImageSource);
                vi.ImageSource2 = ReplacePayloadPath(originalPath, newPath, vi.ImageSource2);
                vi2.ImageSource2 = ReplacePayloadPath(originalPath, newPath, vi2.ImageSource2);
                vi.ImageSource3 = ReplacePayloadPath(originalPath, newPath, vi.ImageSource3);
                vi2.ImageSource3 = ReplacePayloadPath(originalPath, newPath, vi2.ImageSource3);
                vi.ImageSource4 = ReplacePayloadPath(originalPath, newPath, vi.ImageSource4);
                vi2.ImageSource4 = ReplacePayloadPath(originalPath, newPath, vi2.ImageSource4);
            }
        }

        private static string ReplacePayloadPath(List<string> originalPath, List<string> newPath, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            for (int i = 0; i < originalPath.Count; i++)
            {
                if (originalPath[i] == path)
                {
                    return newPath[i];
                }
            }

            return path;
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













