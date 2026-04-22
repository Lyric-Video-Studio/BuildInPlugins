using PluginBase;

namespace MuApiPlugin.Models.GptImage2
{
    internal class GptImage2ImageHandler
    {
        public static async Task<ImageResponse> GetImage(ConnectionSettings connectionSettings, object trackPayload, object itemsPayload, string model)
        {
            if (connectionSettings == null || string.IsNullOrWhiteSpace(connectionSettings.AccessToken))
            {
                return new ImageResponse() { Success = false, ErrorMsg = "Uninitialized" };
            }

            if (JsonHelper.DeepCopy<GptImage2TrackPayload>(trackPayload) is not GptImage2TrackPayload typedTrackPayload ||
                JsonHelper.DeepCopy<GptImage2ItemPayload>(itemsPayload) is not GptImage2ItemPayload typedItemPayload)
            {
                return new ImageResponse() { Success = false, ErrorMsg = "Track payload or item payload object not valid" };
            }

            var request = new ImageGenerationRequest()
            {
                prompt = $"{typedTrackPayload.Prompt} {typedItemPayload.Prompt}".Trim()
            };

            var imageFiles = typedTrackPayload.ImageReferences.ImageSources.Select(i => i.ImageFile)
                .Concat(typedItemPayload.ImageReferences.ImageSources.Select(i => i.ImageFile))
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .ToList();

            if (model == GptImage2TrackPayload.ModelImgToImg)
            {
                if (imageFiles.Count == 0)
                {
                    return new ImageResponse() { Success = false, ErrorMsg = "At least one input image is required" };
                }

                if (imageFiles.Count > 16)
                {
                    return new ImageResponse() { Success = false, ErrorMsg = "MuApi supports up to 16 input images" };
                }
            }

            if (string.IsNullOrWhiteSpace(request.prompt))
            {
                return new ImageResponse() { Success = false, ErrorMsg = "Prompt missing" };
            }

            var client = new Client();
            var uploadedImages = new List<string>();

            try
            {
                foreach (var imageSource in imageFiles)
                {
                    uploadedImages.Add(await client.UploadFile(imageSource, connectionSettings, MuApiVideoPlugin._cancellationToken));
                }
            }
            catch (OperationCanceledException)
            {
                return new ImageResponse() { Success = false, ErrorMsg = "User cancelled" };
            }
            catch (Exception ex)
            {
                return new ImageResponse() { Success = false, ErrorMsg = ex.Message };
            }

            request.images_list = uploadedImages.Count > 0 ? uploadedImages : null;

            return await client.GetImage(request, model, connectionSettings, itemsPayload as GptImage2ItemPayload,
                MuApiVideoPlugin._saveAndRefreshCallback, MuApiVideoPlugin._textualProgressAction, MuApiVideoPlugin._cancellationToken);
        }
    }
}
