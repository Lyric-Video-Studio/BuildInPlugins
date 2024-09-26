using System.ComponentModel;
using System.Runtime.Serialization;
using PluginBase;

namespace OpenAiTxtToImgPlugin
{
    public class ConnectionSettings
    {
        private static string accessTokenKey = "OpenAiTxtToImgPlugin.accessKey";
        private string accessToken;

        [Description("Access token. Each image creation uses credits. Access token is found from Open.ai/account/keys. " +
            "This application is not resposible for possible usage of credits and will not in any way refund any used credits!!!")]
        [EditorWidth(300)]
        public string AccessToken { get => accessToken; set => accessToken = value; }

        [OnSerializing]
        internal void OnSerializingMethod(StreamingContext context)
        {
            if (!string.IsNullOrEmpty(AccessToken))
            {
                SecureStorageWrapper.Set(accessTokenKey, AccessToken);
                AccessToken = "";
            }
        }

        [OnSerialized]
        internal void OnSerializedMethod(StreamingContext context)
        {
            // Need to change the token back
            OnDeserializedMethod(new StreamingContext());
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
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
    }
}