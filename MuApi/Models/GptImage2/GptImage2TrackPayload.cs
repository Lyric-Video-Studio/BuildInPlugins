using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin.Models.GptImage2
{
    public class GptImage2TrackPayload
    {
        public const string ModelTxtToImg = "gpt-image-2-text-to-image";
        public const string ModelImgToImg = "gpt-image-2-image-to-image";

        [Description("Track-level prompt prefix for GPT Image 2 text-to-image.")]
        [EditorWidth(360)]
        public string Prompt { get; set; }

        public ImageReferenceContainer ImageReferences { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {
            if (model == ModelTxtToImg && (propertyName == nameof(ImageReferences) || ImageReferenceContainer.IsImageRefName(propertyName)))
            {
                return false;
            }

            return true;
        }
    }
}
