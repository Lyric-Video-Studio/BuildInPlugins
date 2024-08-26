
using PluginBase;

namespace OpenAiTxtToImgPlugin
{
    public class ItemPayload
    {
        private string prompt = "Progressive metal band from Finland playing in forest";

        [EditorWidth(600)]
        public string Prompt { get => prompt; set => prompt = value; }
    }
}
