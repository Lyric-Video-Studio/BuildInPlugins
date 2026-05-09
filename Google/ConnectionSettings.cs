using System.ComponentModel;
using System.Text.Json.Serialization;
using PluginBase;

namespace GooglePlugin
{
    public class ConnectionSettings : IJsonOnDeserialized, IJsonOnSerialized, IJsonOnSerializing
    {
        private static string accessTokenKey = "GoogleVeoImgToVidPlugin.accessKey";

        private string accessToken;

        [Description("Access key for video. Each video creation uses credits. Access token is found from https://aistudio.google.com/apikey " +
            "This application is not resposible for possible usage of credits and will not in any way refund any used credits!!!")]
        [EditorWidth(300)]
        [MaskInput]
        public string AccessToken { get => accessToken; set => accessToken = value; }

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

        [Description("Requests per minute limit, adjust this if your Google quota is higher")]
        public int VideRpmLimit { get; set; } = 2;

        [Description("Requests per minute limit, adjust this if your Google quota is higher")]
        public int AudioRpmLimit { get; set; } = 10;

        [Description("Requests per minute limit, adjust this if your Google quota is higher")]
        public int ImageRpmLimit { get; set; } = 20;

        public void OnSerialized()
        {
            // Need to change the token back
            OnDeserialized();
        }

        public void OnSerializing()
        {
            SecureStorageWrapper.SecStorage.SetKey(accessTokenKey, AccessToken);
        }

        internal void DeleteTokens()
        {
            SecureStorageWrapper.SecStorage.DeleteKey(accessTokenKey);
        }
    }
}
