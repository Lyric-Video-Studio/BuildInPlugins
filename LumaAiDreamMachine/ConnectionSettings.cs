using System.ComponentModel;
using System.Runtime.Serialization;
using PluginBase;

namespace LumaAiDreamMachinePlugin
{
    public class ConnectionSettings
    {
        private static string accessTokenKey = "LumaAiDreamMachineImgToVidPlugin.accessKey";
        private string url = "https://api.lumalabs.ai";
        private string accessToken;

        [Description("Url to luma ai API")]
        public string Url { get => url; set => url = value; }

        [Description("Access token. Each video creation uses credits. Access token is found from https://lumalabs.ai/dream-machine/api/keys " +
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
            // Need to chnage the token back
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