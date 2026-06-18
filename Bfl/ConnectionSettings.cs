using System.ComponentModel;
using System.Text.Json.Serialization;
using PluginBase;

namespace BflTxtToImgPlugin
{
    public class ConnectionSettings : IJsonOnDeserialized, IJsonOnSerialized, IJsonOnSerializing, IAllowMcpGeneration
    {
        private static string accessTokenKey = "BflTxtToImgPlugin.accessKey";
        private string accessToken;

        [Description("Access token. Each image creation uses credits. Access token is found from https://api.bfl.ml/auth/login. " +
            "This application is not resposible for possible usage of credits and will not in any way refund any used credits!!!")]
        [EditorWidth(300)]
        [MaskInput]
        public string AccessToken { get => accessToken; set => accessToken = value; }

        public bool AllowMcpAccess { get; set; } = false;

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