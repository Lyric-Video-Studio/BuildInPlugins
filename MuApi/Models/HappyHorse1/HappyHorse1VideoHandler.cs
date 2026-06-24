using PluginBase;

namespace MuApiPlugin.Models.HappyHorse1
{
    internal class HappyHorse1VideoHandler
    {
        public static async Task<VideoResponse> GetVideo(ConnectionSettings connectionSettings, object trackPayload, object itemsPayload, string folderToSaveVideo, string model, IApiPollingPayload pollingId)
        {
            if (connectionSettings == null || string.IsNullOrWhiteSpace(connectionSettings.AccessToken))
            {
                return new VideoResponse() { Success = false, ErrorMsg = "Uninitialized" };
            }

            if (JsonHelper.DeepCopy<HappyHorse1TrackPayload>(trackPayload) is not HappyHorse1TrackPayload typedTrackPayload ||
                JsonHelper.DeepCopy<HappyHorse1ItemPayload>(itemsPayload) is not HappyHorse1ItemPayload typedItemPayload)
            {
                return new VideoResponse() { Success = false, ErrorMsg = "Track payload or item payload object not valid" };
            }

            var prompt = $"{typedTrackPayload.Prompt} {typedItemPayload.Prompt}".Trim();
            var client = new Client();
            var request = new GenerationRequest()
            {
                prompt = prompt,
                aspect_ratio = string.IsNullOrWhiteSpace(typedTrackPayload.AspectRatio) ? null : typedTrackPayload.AspectRatio,
                duration = typedItemPayload.Duration <= 0 ? null : typedItemPayload.Duration,
                seed = HappyHorse1TrackPayload.SupportsSeed(model) && typedTrackPayload.Seed > 0 ? typedTrackPayload.Seed : null
            };

            var imageFiles = typedTrackPayload.ImageReferences.ImageSources.Select(i => i.ImageFile)
                .Concat(typedItemPayload.ImageReferences.ImageSources.Select(i => i.ImageFile))
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .ToList();

            if (HappyHorse1TrackPayload.IsImageToVideoModel(model))
            {
                if (imageFiles.Count == 0)
                {
                    return new VideoResponse() { Success = false, ErrorMsg = "Happy Horse 1.1 image-to-video requires an input image" };
                }

                if (imageFiles.Count > 1)
                {
                    return new VideoResponse() { Success = false, ErrorMsg = "Happy Horse 1.1 image-to-video supports a single input image" };
                }

                try
                {
                    var uploadedImage = await client.UploadFile(imageFiles[0], connectionSettings, MuApiVideoPlugin._cancellationToken);
                    request.images_list = [uploadedImage];
                }
                catch (OperationCanceledException)
                {
                    return new VideoResponse() { Success = false, ErrorMsg = "User cancelled" };
                }
                catch (Exception ex)
                {
                    return new VideoResponse() { Success = false, ErrorMsg = ex.Message };
                }
            }
            else if (HappyHorse1TrackPayload.IsReferenceToVideoModel(model))
            {
                if (imageFiles.Count == 0)
                {
                    return new VideoResponse() { Success = false, ErrorMsg = "Happy Horse 1.1 reference-to-video requires at least one reference image" };
                }

                if (imageFiles.Count > 9)
                {
                    return new VideoResponse() { Success = false, ErrorMsg = "Happy Horse 1.1 reference-to-video supports up to 9 reference images" };
                }

                try
                {
                    var uploadedImages = new List<string>();
                    foreach (var imageFile in imageFiles)
                    {
                        uploadedImages.Add(await client.UploadFile(imageFile, connectionSettings, MuApiVideoPlugin._cancellationToken));
                    }

                    request.images_list = uploadedImages;
                }
                catch (OperationCanceledException)
                {
                    return new VideoResponse() { Success = false, ErrorMsg = "User cancelled" };
                }
                catch (Exception ex)
                {
                    return new VideoResponse() { Success = false, ErrorMsg = ex.Message };
                }
            }

            return await client.GetVideo(request, model, folderToSaveVideo, connectionSettings, pollingId,
                MuApiVideoPlugin._saveAndRefreshCallback, MuApiVideoPlugin._textualProgressAction, MuApiVideoPlugin._cancellationToken);
        }
    }
}
