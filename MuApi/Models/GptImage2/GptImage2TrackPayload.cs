using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin.Models.GptImage2
{
    public class GptImage2TrackPayload
    {
        public const string ModelTxtToImg = "gpt-image-2-text-to-image";

        [Description("Track-level prompt prefix for GPT Image 2 text-to-image.")]
        [EditorWidth(360)]
        public string Prompt { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {
            return true;
        }
    }
}
