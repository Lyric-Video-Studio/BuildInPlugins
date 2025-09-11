using System.ComponentModel;
using System.Text.Json.Serialization;
using PluginBase;

namespace RunwayMlPlugin
{
    public class ConnectionSettings : IJsonOnDeserialized, IJsonOnSerialized, IJsonOnSerializing
    {
        private static string accessTokenKey = "RunwayMlImgToVidPlugin.accessKey";
        private string url = "https://api.dev.runwayml.com/";
        private string accessToken;

        [Description("Url to Runway ML api, do not change this if unsure")]
        public string Url { get => url; set => url = value; }

        [Description("Access token. Each video creation uses credits. Access token is found from https://dev.runwayml.com " +
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