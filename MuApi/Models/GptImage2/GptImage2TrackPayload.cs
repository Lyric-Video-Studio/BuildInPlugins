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

        [Description("Output image aspect ratio. MuApi requires resolution 1K when using auto.")]
        [PropertyComboOptions(["auto", "1:1", "16:9", "9:16", "4:3", "3:4", "2:3", "3:2"])]
        public string AspectRatio { get; set; } = "auto";

        [Description("Output image resolution. MuApi only allows 1K with auto aspect ratio, and 1:1 cannot use 4K.")]
        [PropertyComboOptions(["1K", "2K", "4K"])]
        public string Resolution { get; set; } = "1K";

        [Description("Generation quality. Low and medium use a faster backend, high uses the higher-fidelity backend.")]
        [PropertyComboOptions(["low", "medium", "high"])]
        public string Quality { get; set; } = "high";

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
