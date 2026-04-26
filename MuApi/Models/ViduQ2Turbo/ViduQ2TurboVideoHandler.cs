using PluginBase;

namespace MuApiPlugin.Models.ViduQ2Turbo
{
    internal class ViduQ2TurboVideoHandler
    {
        public static async Task<VideoResponse> GetVideo(ConnectionSettings connectionSettings, object trackPayload, object itemsPayload, string folderToSaveVideo, string model)
        {
            if (connectionSettings == null || string.IsNullOrWhiteSpace(connectionSettings.AccessToken))
            {
                return new VideoResponse() { Success = false, ErrorMsg = "Uninitialized" };
            }

            if (JsonHelper.DeepCopy<ViduQ2TurboTrackPayload>(trackPayload) is not ViduQ2TurboTrackPayload typedTrackPayload ||
                JsonHelper.DeepCopy<ViduQ2TurboItemPayload>(itemsPayload) is not ViduQ2TurboItemPayload typedItemPayload)
            {
                return new VideoResponse() { Success = false, ErrorMsg = "Track payload or item payload object not valid" };
            }

            var request = new GenerationRequest()
            {
                prompt = $"{typedTrackPayload.Prompt} {typedItemPayload.Prompt}".Trim(),
                resolution = string.IsNullOrWhiteSpace(typedTrackPayload.Resolution) ? null : typedTrackPayload.Resolution,
                aspect_ratio = model == ViduQ2TurboTrackPayload.ModelT2V && !string.IsNullOrWhiteSpace(typedTrackPayload.AspectRatio)
                    ? typedTrackPayload.AspectRatio
                    : null,
                duration = typedItemPayload.Duration > 0 ? typedItemPayload.Duration : null,
                bgm = typedTrackPayload.Bgm,
                movement_amplitude = string.IsNullOrWhiteSpace(typedTrackPayload.MovementAmplitude) ? null : typedTrackPayload.MovementAmplitude
            };

            var client = new Client();

            if (model == ViduQ2TurboTrackPayload.ModelStartEnd)
            {
                if (string.IsNullOrWhiteSpace(typedItemPayload.StartImage) || string.IsNullOrWhiteSpace(typedItemPayload.EndImage))
                {
                    return new VideoResponse() { Success = false, ErrorMsg = "Vidu Q2 Turbo start-end video requires both start and end images" };
                }

                try
                {
                    request.image_url = await client.UploadFile(typedItemPayload.StartImage, connectionSettings, MuApiVideoPlugin._cancellationToken);
                    request.last_image = await client.UploadFile(typedItemPayload.EndImage, connectionSettings, MuApiVideoPlugin._cancellationToken);
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
            else if (model == ViduQ2TurboTrackPayload.ModelI2V)
            {
                if (string.IsNullOrWhiteSpace(typedItemPayload.StartImage))
                {
                    return new VideoResponse() { Success = false, ErrorMsg = "Vidu Q2 Turbo image-to-video requires an input image" };
                }

                try
                {
                    request.image_url = await client.UploadFile(typedItemPayload.StartImage, connectionSettings, MuApiVideoPlugin._cancellationToken);
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

            return await client.GetVideo(request, model, folderToSaveVideo, connectionSettings, typedItemPayload,
                MuApiVideoPlugin._saveAndRefreshCallback, MuApiVideoPlugin._textualProgressAction, MuApiVideoPlugin._cancellationToken);
        }
    }
}
