using PluginBase;

namespace MuApiPlugin.Models.MidjourneyV8
{
    internal class MidjourneyV8ImageHandler
    {
        public static async Task<ImageResponse> GetImage(ConnectionSettings connectionSettings, object trackPayload, object itemsPayload, string model)
        {
            if (connectionSettings == null || string.IsNullOrWhiteSpace(connectionSettings.AccessToken))
            {
                return new ImageResponse() { Success = false, ErrorMsg = "Uninitialized" };
            }

            if (JsonHelper.DeepCopy<MidjourneyV8TrackPayload>(trackPayload) is not MidjourneyV8TrackPayload typedTrackPayload ||
                JsonHelper.DeepCopy<MidjourneyV8ItemPayload>(itemsPayload) is not MidjourneyV8ItemPayload typedItemPayload)
            {
                return new ImageResponse() { Success = false, ErrorMsg = "Track payload or item payload object not valid" };
            }

            var request = new ImageGenerationRequest()
            {
                prompt = $"{typedTrackPayload.Prompt} {typedItemPayload.Prompt}".Trim(),
                negative_prompt = typedItemPayload.NegativePrompt,
                aspect_ratio = typedTrackPayload.AspectRatio,
                stylize = typedTrackPayload.Stylize,
                chaos = typedTrackPayload.Chaos,
                weird = typedTrackPayload.Weird,
                seed = typedTrackPayload.Seed > 0 ? typedTrackPayload.Seed : null
            };

            if (string.IsNullOrWhiteSpace(request.prompt))
            {
                return new ImageResponse() { Success = false, ErrorMsg = "Prompt missing" };
            }

            var imageFiles = typedTrackPayload.ImageReferences.ImageSources.Select(i => i.ImageFile)
                .Concat(typedItemPayload.ImageReferences.ImageSources.Select(i => i.ImageFile))
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .ToList();

            if (imageFiles.Count > 1)
            {
                return new ImageResponse() { Success = false, ErrorMsg = "Midjourney V8 supports a single reference image" };
            }

            var client = new Client();

            try
            {
                if (imageFiles.Count == 1)
                {
                    request.image_url = await client.UploadFile(imageFiles[0], connectionSettings, MuApiVideoPlugin._cancellationToken);
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

            return await client.GetImage(request, model, connectionSettings, itemsPayload as MidjourneyV8ItemPayload,
                MuApiVideoPlugin._saveAndRefreshCallback, MuApiVideoPlugin._textualProgressAction, MuApiVideoPlugin._cancellationToken);
        }
    }
}
