using PluginBase;
using System.ComponentModel;

namespace MistralTxtToImgPlugin
{
    public class ItemPayload
    {
        private string prompt = "Progressive metal band from Finland playing in forest";
        public string Prompt { get => prompt; set => prompt = value; }
    }
}