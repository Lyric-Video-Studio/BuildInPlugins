using System.ComponentModel;
using System.Text.Json.Serialization;
using PluginBase;

namespace WanPlugin
{
    public class ConnectionSettings : IJsonOnDeserialized, IJsonOnSerialized, IJsonOnSerializing
    {
        private static string accessTokenKey = "WanImgToVidPlugin.accessKey";
        private string url = "https://dashscope-intl.aliyuncs.com/api/v1/";
        private string accessToken;

        [Description("Url to WAN Api, do not change this if unsure")]
        public string Url { get => url; set => url = value; }

        [Description("Access token. Each video creation uses credits. Access token is found from https://modelstudio.console.alibabacloud.com/?tab=globalset#/efm/api_key" +
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