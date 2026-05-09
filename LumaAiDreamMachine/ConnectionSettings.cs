using System.ComponentModel;
using System.Text.Json.Serialization;
using PluginBase;

namespace LumaAiDreamMachinePlugin
{
    public class ConnectionSettings : IJsonOnDeserialized, IJsonOnSerialized, IJsonOnSerializing
    {
        private static string accessTokenKey = "LumaAiDreamMachineImgToVidPlugin.accessKey";
        private string url = "https://api.lumalabs.ai";
        private string agentsUrl = "https://agents.lumalabs.ai";
        private string accessToken;

        [Description("Url to luma ai API")]
        public string Url { get => url; set => url = value; }

        [Description("Url to the Luma Agents image API used by uni-1 and uni-1-max")]
        public string AgentsUrl { get => agentsUrl; set => agentsUrl = value; }

        [Description("Access token. Each video creation uses credits. Access token is found from https://lumalabs.ai/dream-machine/api/keys " +
            "This application is not resposible for possible usage of credits and will not in any way refund any used credits!!!")]
        [EditorWidth(300)]
        [MaskInput]
        public string AccessToken { get => accessToken; set => accessToken = value; }

        public void OnDeserialized()
        {
            try
            {
                AccessToken = SecureStorageWrapper.SecStorage.GetKey(accessTokenKey);
            }
            catch (Exception)
            {
                AccessToken = "";
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
        }

        internal void DeleteTokens()
        {
            SecureStorageWrapper.SecStorage.DeleteKey(accessTokenKey);
        }
    }
}
