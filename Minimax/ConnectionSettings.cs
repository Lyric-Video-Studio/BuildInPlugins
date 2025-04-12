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
        private string groupId;

        [Description("Url to Minimax API")]
        public string Url { get => url; set => url = value; }

        [Description("Access token. Each video creation uses credits. Access token is found from https://www.minimax.io/platform/user-center/basic-information/interface-key " +
            "This application is not resposible for possible usage of credits and will not in any way refund any used credits!!!")]
        [EditorWidth(300)]
        public string AccessToken { get => accessToken; set => accessToken = value; }

        [Description("Group id, you can find it from your minimax account, under 'Your profile': https://www.minimax.io/platform/user-center/basic-information")]
        public string GroupId { get => groupId; set => groupId = value; }

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