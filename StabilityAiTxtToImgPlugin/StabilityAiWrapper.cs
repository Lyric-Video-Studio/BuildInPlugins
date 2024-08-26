using PluginBase;

namespace StabilityAiTxtToImgPlugin
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

        public async Task<ImageResponse> GetTxtToImg(Request payload, ConnectionSettings connectionSettings)
        {
            if(client == null)
            {
                return new ImageResponse { Success = false, ErrorMsg = "Uninitialized" };
            }
            try
            {
                return await client.GetTxtToImg(payload, connectionSettings);
            }
            catch (Exception ex)
            {
                return new ImageResponse { Success = false, ErrorMsg = ex.Message };
            }
        }

        internal void CloseConnection()
        {
            if(client != null)
            {
                client = null;
            }
        }

        public async Task<string[]> GetSelectionForProperty(string property, ConnectionSettings settings)
        {            
            return Array.Empty<string>();
        }
    }
}
