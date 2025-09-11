using System.ComponentModel;
using System.Text.Json.Serialization;
using PluginBase;

namespace OpenAiTxtToImgPlugin
{
    public class ConnectionSettings : IJsonOnDeserialized, IJsonOnSerialized, IJsonOnSerializing
    {
        private static string accessTokenKey = "OpenAiTxtToImgPlugin.accessKey";
        private string accessToken;

        [Description("Access token. Each image creation uses credits. Access token is found from Open.ai/account/keys. " +
            "This application is not resposible for possible usage of credits and will not in any way refund any used credits!!!")]
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