using System.ComponentModel;
using System.Text.Json.Serialization;
using PluginBase;

namespace MuApiPlugin
{
    public class ConnectionSettings : IJsonOnDeserialized, IJsonOnSerialized, IJsonOnSerializing
    {
        private const string AccessTokenKey = "MuApiPlugin.accessKey";
        private string url = "https://api.muapi.ai/api/v1/";
        private string accessToken;

        [Description("MuApi API base url. Leave this as default unless MuApi changes their API host.")]
        public string Url { get => url; set => url = value; }

        [Description("MuApi API key. Uploads are free, but MuApi still requires a valid credit-enabled key to use upload and generation endpoints.")]
        [EditorWidth(320)]
        [MaskInput]
        public string AccessToken { get => accessToken; set => accessToken = value; }

        public void OnDeserialized()
        {
            try
            {
                AccessToken = SecureStorageWrapper.SecStorage.Get(AccessTokenKey);
            }
            catch (Exception)
            {
                AccessToken = "";
            }
        }

        public void OnSerialized()
        {
            OnDeserialized();
        }

        public void OnSerializing()
        {
            if (!string.IsNullOrEmpty(AccessToken))
            {
                SecureStorageWrapper.SecStorage.Set(AccessTokenKey, AccessToken);
            }
        }

        internal void DeleteTokens()
        {
            SecureStorageWrapper.SecStorage.Delete(AccessTokenKey);
        }
    }
}
