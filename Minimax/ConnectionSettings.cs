using System.ComponentModel;
using System.Text.Json.Serialization;
using PluginBase;

namespace MinimaxPlugin
{
    public class ConnectionSettings : IJsonOnDeserialized, IJsonOnSerialized, IJsonOnSerializing
    {
        private static string accessTokenKey = "MinimaxImgToVidPlugin.accessKey";
        private string url = "https://api.minimaxi.chat";
        private string accessToken;

        [Description("Url to Minimax API")]
        public string Url { get => url; set => url = value; }

        [Description("Access token. Each video creation uses credits. Access token is found from https://www.minimax.io/platform/user-center/basic-information/interface-key " +
            "This application is not resposible for possible usage of credits and will not in any way refund any used credits!!!")]
        [EditorWidth(300)]
        public string AccessToken { get => accessToken; set => accessToken = value; }

        [IgnoreDynamicEdit]
        public List<string> SpeechVoices { get; set; } = new List<string>();

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

        [CustomAction("Refresh text-to-speech voices")]
        public void RefreshVOices()
        {
            voiceRefresh.Invoke();
        }

        private Action voiceRefresh;

        internal void SetVoiceRefreshCallback(Action voiceRefresh)
        {
            this.voiceRefresh = voiceRefresh;
        }
    }
}