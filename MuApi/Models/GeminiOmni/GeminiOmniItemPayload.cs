using MuApiPlugin.Models.Seedance2;
using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin.Models.GeminiOmni
{
    public class GeminiOmniItemPayload
    {
        [Description("Item-level prompt suffix for Gemini Omni video generation.")]
        [EditorWidth(360)]
        public string Prompt { get; set; }

        [PropertyComboOptions(["4", "6", "8", "10"])]
        public int Duration { get; set; } = 8;

        public ImageReferenceContainer ImageReferences { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {
            if (!GeminiOmniTrackPayload.IsGeminiOmniVideoModel(model))
            {
                return false;
            }

            if (model == GeminiOmniTrackPayload.ModelT2V &&
                (propertyName == nameof(ImageReferences) || ImageReferenceContainer.IsImageRefName(propertyName)))
            {
                return false;
            }

            return true;
        }
    }
}
