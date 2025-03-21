﻿using PluginBase;

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

        public async Task<ImageResponse> GetImage(ImageRequest payload, ConnectionSettings connectionSettings, ImageItemPayload refItemPlayload, Action saveAndRefreshCallback)
        {
            if (client == null)
            {
                return new ImageResponse { Success = false, ErrorMsg = "Uninitialized" };
            }
            try
            {
                return await client.GetImg(payload, connectionSettings, refItemPlayload, saveAndRefreshCallback);
            }
            catch (Exception ex)
            {
                return new ImageResponse { Success = false, ErrorMsg = ex.Message };
            }
        }

        internal void CloseConnection()
        {
            if (client != null)
            {
                client = null;
            }
        }
    }
}