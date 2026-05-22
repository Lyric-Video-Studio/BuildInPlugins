using MuApiPlugin.Models.Seedance2;
using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin.Models.GptImage2
{
    public class GptImage2ItemPayload
    {
        [Description("Item-level prompt suffix for GPT Image 2 text-to-image.")]
        [EditorWidth(360)]
        public string Prompt { get; set; }

        public ImageReferenceContainer ImageReferences { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {
            if (model == GptImage2TrackPayload.ModelTxtToImg && (propertyName == nameof(ImageReferences) || ImageReferenceContainer.IsImageRefName(propertyName)))
            {
                return false;
            }

            return true;
        }
    }
}
