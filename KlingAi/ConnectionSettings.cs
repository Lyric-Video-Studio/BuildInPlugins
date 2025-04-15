using System.ComponentModel;
using System.Text.Json.Serialization;
using PluginBase;

namespace KlingAiPlugin
{
    public class ConnectionSettings : IJsonOnDeserialized, IJsonOnSerialized, IJsonOnSerializing
    {
        private static string accessTokenKey = "KlingAiImgToVidPlugin.accessKey";
        private static string accessSecretKey = "KlingAiImgToVidPlugin.accessSecret";
        private string url = "https://api.klingai.com";
        private string accessToken;
        private string accessSecret;

        [Description("Url to KlingAi API")]
        public string Url { get => url; set => url = value; }

        [Description("Access key. Each video creation uses credits. Access token is found from https://console.klingai.com/console/access-control/accesskey-management " +
            "This application is not resposible for possible usage of credits and will not in any way refund any used credits!!!")]
        [EditorWidth(300)]
        public string AccessToken { get => accessToken; set => accessToken = value; }

        [Description("Access secret. Each video creation uses credits. Access token is found from https://console.klingai.com/console/access-control/accesskey-management " +
            "This application is not resposible for possible usage of credits and will not in any way refund any used credits!!!")]
        public string AccessSecret { get => accessSecret; set => accessSecret = value; }

        public void OnDeserialized()
        {
            try
            {
                AccessToken = SecureStorageWrapper.Get(accessTokenKey);
                AccessSecret = SecureStorageWrapper.Get(accessSecretKey);
            }
            catch (Exception)
            {
                AccessToken = "";
                AccessSecret = "";
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
                SecureStorageWrapper.Set(accessSecretKey, AccessSecret);
                AccessToken = "";
                AccessSecret = "";
            }
        }
    }
}