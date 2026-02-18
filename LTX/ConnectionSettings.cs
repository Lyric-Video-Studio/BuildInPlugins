using System.ComponentModel;
using System.Text.Json.Serialization;
using PluginBase;

namespace LTXPlugin
{
    public class ConnectionSettings : IJsonOnDeserialized, IJsonOnSerialized, IJsonOnSerializing
    {
        private static string accessTokenKey = "LTXlugin.accessKey";
        private string url = "https://api.ltx.video";
        private string accessToken;

        [Description("Url to LTX api, do not change this if unsure")]
        public string Url { get => url; set => url = value; }

        [Description("Access token. Each video creation uses credits. Access token is found from https://console.ltx.video/api-keys/ " +
            "This application is not resposible for possible usage of credits and will not in any way refund any used credits!!!")]
        [EditorWidth(300)]
        [MaskInput]
        public string AccessToken { get => accessToken; set => accessToken = value; }

        public void OnDeserialized()
        {
            try
            {
                AccessToken = SecureStorageWrapper.SecStorage.Get(accessTokenKey);
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
                SecureStorageWrapper.SecStorage.Set(accessTokenKey, AccessToken);
            }
        }

        internal void DeleteTokens()
        {
            SecureStorageWrapper.SecStorage.Delete(accessTokenKey);
        }
    }
}