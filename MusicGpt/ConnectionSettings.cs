using System.ComponentModel;
using System.Text.Json.Serialization;
using PluginBase;

namespace MusicGptPlugin
{
    public class ConnectionSettings : IJsonOnDeserialized, IJsonOnSerialized, IJsonOnSerializing
    {
        private static string accessTokenKey = "MusicGptPlugin.accessKey";
        private string url = "https://api.musicgpt.com/api/public/v1/";
        private string accessToken;

        [Description("Url to MusicGpt")]
        public string Url { get => url; set => url = value; }

        [Description("Access token. Each audio creation uses credits. Access token is found from https://musicgpt.com/api-dashboard/api-keys " +
            "This application is not resposible for possible usage of credits and will not in any way refund any used credits!!!")]
        [EditorWidth(300)]
        public string AccessToken { get => accessToken; set => accessToken = value; }

        [IgnoreDynamicEdit]
        public string Voices { get; set; }

        private Action refreshAction;

        public void SetVoiceRefreshCallback(Action refresh)
        {
            refreshAction = refresh;
        }

        [CustomAction("Refresh voices")]
        public void RefreshVoices()
        {
            refreshAction.Invoke();
        }

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
            if (!string.IsNullOrEmpty(AccessToken))
            {
                SecureStorageWrapper.Set(accessTokenKey, AccessToken);
                AccessToken = "";
            }
        }
    }
}