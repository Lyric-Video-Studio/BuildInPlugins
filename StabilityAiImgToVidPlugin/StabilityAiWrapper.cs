using PluginBase;

namespace StabilityAiImgToVidPlugin
{
    public class StabilityAiWrapper
    {
        // TODO: Prepare this for intanciating, so less static thinngs
        // Keep process static. Rememebr that the uri can be to server / internets, so process might not be needed

        private Client client;

        public void InitializeClient()
        {
            client = new Client();
        }

        public async Task<VideoResponse> GetImgToVid(Request payload, string pathToSourceImage, string pathForSaving, ConnectionSettings connectionSettings, ItemPayload refItemPlayload, Action<bool> saveAndRefreshCallback)
        {
            if (client == null)
            {
                return new VideoResponse { Success = false, ErrorMsg = "Uninitialized" };
            }
            try
            {
                return await client.GetImgToVid(payload, pathToSourceImage, pathForSaving, connectionSettings, refItemPlayload, saveAndRefreshCallback);
            }
            catch (Exception ex)
            {
                return new VideoResponse { Success = false, ErrorMsg = ex.Message };
            }
        }

        internal void CloseConnection()
        {
            if (client != null)
            {
                client = null;
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        public async Task<string[]> GetSelectionForProperty(string property, ConnectionSettings settings)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            return Array.Empty<string>();
        }
    }
}