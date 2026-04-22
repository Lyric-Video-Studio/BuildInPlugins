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

            if (string.IsNullOrWhiteSpace(request.prompt))
            {
                return new ImageResponse() { Success = false, ErrorMsg = "Prompt missing" };
            }

            var client = new Client();
            return await client.GetImage(request, model, connectionSettings, itemsPayload as GptImage2ItemPayload,
                MuApiVideoPlugin._saveAndRefreshCallback, MuApiVideoPlugin._textualProgressAction, MuApiVideoPlugin._cancellationToken);
        }
    }
}
