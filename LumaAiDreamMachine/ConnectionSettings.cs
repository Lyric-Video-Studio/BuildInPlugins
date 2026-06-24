using System.ComponentModel;
using System.Text.Json.Serialization;
using PluginBase;

namespace LumaAiDreamMachinePlugin
{
    public class ConnectionSettings : IJsonOnDeserialized, IJsonOnSerialized, IJsonOnSerializing, IAllowMcpGeneration
    {
        private static string accessTokenKey = "LumaAiDreamMachineImgToVidPlugin.accessKey";
        private string url = "https://api.lumalabs.ai";
        private string agentsUrl = "https://agents.lumalabs.ai";
        private string accessToken;
        private string accessTokenUni;

        [Description("Url to luma ai API")]
        public string Url { get => url; set => url = value; }

        [Description("Url to the Luma Agents API used by uni-1 image models and ray-3.2 video")]
        public string AgentsUrl { get => agentsUrl; set => agentsUrl = value; }

        [Description("Access token. Each video creation uses credits. Access token is found from https://lumalabs.ai/dream-machine/api/keys " +
            "This application is not resposible for possible usage of credits and will not in any way refund any used credits!!!")]
        [EditorWidth(300)]
        [MaskInput]
        public string AccessToken { get => accessToken; set => accessToken = value; }

        [Description("Access token for the Luma Agents API, including uni image and ray-3.2 video. Uses credits. Access token is found from https://platform.lumalabs.ai/keys " +
            "This application is not resposible for possible usage of credits and will not in any way refund any used credits!!!")]
        [EditorWidth(300)]
        [MaskInput]
        [CustomName("Access token agents / platform")]
        public string AccessTokenUni { get => accessTokenUni; set => accessTokenUni = value; }

        public bool AllowMcpAccess { get; set; } = false;

        public void OnDeserialized()
        {
            try
            {
                AccessToken = SecureStorageWrapper.SecStorage.GetKey(accessTokenKey);
                AccessTokenUni = SecureStorageWrapper.SecStorage.GetKey(accessTokenKey + ".uni");
            }
            catch (Exception)
            {
                AccessToken = "";
                AccessTokenUni = "";
            }
        }

        public void OnSerialized()
        {
            // Need to change the token back
            OnDeserialized();
        }

        public void OnSerializing()
        {
            if (!string.IsNullOrEmpty(AccessToken))
            {
                SecureStorageWrapper.SecStorage.SetKey(accessTokenKey, AccessToken);
            }

            if (!string.IsNullOrEmpty(AccessTokenUni))
            {
                SecureStorageWrapper.SecStorage.SetKey(accessTokenKey + ".uni", AccessTokenUni);
            }
        }

        internal void DeleteTokens()
        {
            SecureStorageWrapper.SecStorage.DeleteKey(accessTokenKey);
            SecureStorageWrapper.SecStorage.DeleteKey(accessTokenKey + ".uni");
        }
    }
}
