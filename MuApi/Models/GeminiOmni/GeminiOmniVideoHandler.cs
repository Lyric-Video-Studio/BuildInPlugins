using PluginBase;

namespace MuApiPlugin.Models.GeminiOmni
{
    internal class GeminiOmniVideoHandler
    {
        private const int MaxImageCount = 5;
        private const int MaxAudioIdCount = 3;
        private const int MaxCharacterIdCount = 3;

        public static async Task<VideoResponse> GetVideo(ConnectionSettings connectionSettings, object trackPayload, object itemsPayload, string folderToSaveVideo, string model, IApiPollingPayload pollingId)
        {
            if (connectionSettings == null || string.IsNullOrWhiteSpace(connectionSettings.AccessToken))
            {
                return new VideoResponse() { Success = false, ErrorMsg = "Uninitialized" };
            }

            if (JsonHelper.DeepCopy<GeminiOmniTrackPayload>(trackPayload) is not GeminiOmniTrackPayload typedTrackPayload ||
                JsonHelper.DeepCopy<GeminiOmniItemPayload>(itemsPayload) is not GeminiOmniItemPayload typedItemPayload)
            {
                return new VideoResponse() { Success = false, ErrorMsg = "Track payload or item payload object not valid" };
            }

            var prompt = $"{typedTrackPayload.Prompt} {typedItemPayload.Prompt}".Trim();
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return new VideoResponse() { Success = false, ErrorMsg = "Prompt missing" };
            }

            var audioIds = CollectIds(typedTrackPayload.AudioIds.AudioIds.Select(i => i.AudioId), MaxAudioIdCount, "audio IDs");
            if (!audioIds.Success)
            {
                return new VideoResponse() { Success = false, ErrorMsg = audioIds.Error };
            }

            var characterIds = CollectIds(typedTrackPayload.CharacterIds.CharacterIds.Select(i => i.CharacterId), MaxCharacterIdCount, "character IDs");
            if (!characterIds.Success)
            {
                return new VideoResponse() { Success = false, ErrorMsg = characterIds.Error };
            }

            var request = new GenerationRequest()
            {
                prompt = prompt,
                duration = typedItemPayload.Duration > 0 ? typedItemPayload.Duration : null,
                resolution = string.IsNullOrWhiteSpace(typedTrackPayload.Resolution) ? null : typedTrackPayload.Resolution,
                aspect_ratio = string.IsNullOrWhiteSpace(typedTrackPayload.AspectRatio) ? null : typedTrackPayload.AspectRatio,
                audio_ids = audioIds.Values.Count > 0 ? audioIds.Values : null,
                seed = typedTrackPayload.Seed > 0 ? typedTrackPayload.Seed : null,
                character_ids = characterIds.Values.Count > 0 ? characterIds.Values : null,
            };

            if (model == GeminiOmniTrackPayload.ModelI2V)
            {
                var imageFiles = typedTrackPayload.ImageReferences.ImageSources.Select(i => i.ImageFile)
                    .Concat(typedItemPayload.ImageReferences.ImageSources.Select(i => i.ImageFile))
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .ToList();

                if (imageFiles.Count == 0)
                {
                    return new VideoResponse() { Success = false, ErrorMsg = "Gemini Omni image-to-video requires at least one input image" };
                }

                if (imageFiles.Count > MaxImageCount)
                {
                    return new VideoResponse() { Success = false, ErrorMsg = $"Gemini Omni image-to-video supports up to {MaxImageCount} input images" };
                }

                var client = new Client();
                var uploadedImages = new List<string>();

                try
                {
                    foreach (var imageFile in imageFiles)
                    {
                        uploadedImages.Add(await client.UploadFile(imageFile, connectionSettings, MuApiVideoPlugin._cancellationToken));
                    }
                }
                catch (OperationCanceledException)
                {
                    return new VideoResponse() { Success = false, ErrorMsg = "User cancelled" };
                }
                catch (Exception ex)
                {
                    return new VideoResponse() { Success = false, ErrorMsg = ex.Message };
                }

                request.image_urls = uploadedImages;
            }

            return await new Client().GetVideo(request, model, folderToSaveVideo, connectionSettings, pollingId,
                MuApiVideoPlugin._saveAndRefreshCallback, MuApiVideoPlugin._textualProgressAction, MuApiVideoPlugin._cancellationToken);
        }

        private static (bool Success, string Error, List<string> Values) CollectIds(IEnumerable<string> ids, int maxCount, string idTypeName)
        {
            var values = ids
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (values.Count > maxCount)
            {
                return (false, $"Gemini Omni supports up to {maxCount} {idTypeName}", []);
            }

            return (true, "", values);
        }
    }
}
