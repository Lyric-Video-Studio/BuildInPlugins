using System.ComponentModel;
using System.Text.Json.Serialization;
using PluginBase;

namespace StabilityAiTxtToImgPlugin
{
    public class ConnectionSettings : IJsonOnDeserialized, IJsonOnSerialized, IJsonOnSerializing
    {
        private static string accessTokenKey = "StabilityAiTxtToImgPlugin.accessKey";
        private string url = "https://api.stability.ai";
        private string accessToken;

        [Description("Url to stability API")]
        public string Url { get => url; set => url = value; }

        [Description("Access token. Each image creation uses credits. Access token is found from stability.ai/account/keys. " +
            "This application is not responsible for possible usage of credits and will not in any way refund any used credits!!!")]
        [EditorWidth(300)]
        public string AccessToken { get => accessToken; set => accessToken = value; }

        public void OnDeserialized()
        {
            try
            {
                SecureStorageWrapper.Get(accessTokenKey).ContinueWith(t =>
                {
                    AccessToken = t.Result;
                });
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
                SecureStorageWrapper.Set(accessTokenKey, AccessToken);
                AccessToken = "";
            }
        }
    }
}