using MuApiPlugin.Models.Seedance2;
using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin.Models.GptImage2
{
    public class GptImage2ItemPayload : IMuApiPollingPayload
    {
        [Description("Generation id saved after submit. Leave it in place if you want the plugin to resume polling instead of creating a new request.")]
        public string PollingId { get; set; }

        [Description("Item-level prompt suffix for GPT Image 2 text-to-image.")]
        [EditorWidth(360)]
        public string Prompt { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {
            return true;
        }
    }
}
