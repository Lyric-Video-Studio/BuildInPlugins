using PluginBase;

namespace LumaAiDreamMachinePlugin
{
    public class LumaAiDreamMachineWrapper
    {
        // TODO: Prepare this for intanciating, so less static thinngs
        // Keep process static. Rememebr that the uri can be to server / internets, so process might not be needed

        private Client client;

        public void InitializeClient()
        {
            client = new Client();
        }

        public async Task<VideoResponse> GetImgToVid(Request payload, string pathForSaving, ConnectionSettings connectionSettings, ItemPayload refItemPlayload, Action saveAndRefreshCallback)
        {
            if (client == null)
            {
                return new VideoResponse { Success = false, ErrorMsg = "Uninitialized" };
            }
            try
            {
                return await client.GetImgToVid(payload, pathForSaving, connectionSettings, refItemPlayload, saveAndRefreshCallback);
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

        public async Task<string[]> GetSelectionForProperty(string property, ConnectionSettings settings)
        {
            if (property == nameof(Request.aspect_ratio))
            {
                return ["16:9", "1:1", "9:16", "4:3", "3:4", "21:9", "9:21"];
            }
            return Array.Empty<string>();
        }
    }
}