using System.ComponentModel;
using System.Text.Json.Serialization;
using PluginBase;

namespace GoogleVeoPlugin
{
    public class ConnectionSettings : IJsonOnDeserialized, IJsonOnSerialized, IJsonOnSerializing
    {
        private static string accessTokenKey = "GoogleVeoImgToVidPlugin.accessKey";

        private string url = "https://generativelanguage.googleapis.com";
        private string accessToken;

        [Description("Url to GoogleVeo API")]
        public string Url { get => url; set => url = value; }

        [Description("Access key for video. Each video creation uses credits. Access token is found from https://aistudio.google.com/apikey " +
            "This application is not resposible for possible usage of credits and will not in any way refund any used credits!!!")]
        [EditorWidth(300)]
        public string AccessToken { get => accessToken; set => accessToken = value; }

        public void OnDeserialized()
        {
            try
            {
                AccessToken = SecureStorageWrapper.Get(accessTokenKey);
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
            SecureStorageWrapper.Set(accessTokenKey, AccessToken);
            AccessToken = "";
        }
    }
}