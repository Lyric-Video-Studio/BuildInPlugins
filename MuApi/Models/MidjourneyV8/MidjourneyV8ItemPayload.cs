using MuApiPlugin.Models.Seedance2;
using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin.Models.MidjourneyV8
{
    public class MidjourneyV8ItemPayload : IMuApiPollingPayload
    {
        [Description("Generation id saved after submit. Leave it in place if you want the plugin to resume polling instead of creating a new request.")]
        public string PollingId { get; set; }

        [Description("Item-level prompt suffix for Midjourney V8.")]
        [EditorWidth(360)]
        public string Prompt { get; set; }

        [Description("Things to exclude from the image, e.g. text, watermark.")]
        [EditorWidth(360)]
        public string NegativePrompt { get; set; }

        public ImageReferenceContainer ImageReferences { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {
            return true;
        }
    }
}
