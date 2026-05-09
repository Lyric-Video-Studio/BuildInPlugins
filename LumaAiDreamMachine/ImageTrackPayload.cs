using PluginBase;
using System.ComponentModel;

namespace LumaAiDreamMachinePlugin
{
    public class ImageTrackPayload : IPayloadPropertyVisibility
    {
        [IgnoreDynamicEdit]
        public bool IsVideo { get; set; } = false;

        private ImageRequest txtToImgPayload = new ImageRequest();

        [Description("Image settings")]
        [IgnorePropertyName]
        public ImageRequest Settings { get => txtToImgPayload; set => txtToImgPayload = value; }

        [TriggerReload]
        [PropertyComboOptions(["auto", "manga"])]
        public string Style { get; set; } = "auto";

        [TriggerReload]
        [PropertyComboOptions(["auto", "png", "jpeg"])]
        [CustomName("Output format")]
        public string OutputFormat { get; set; } = "auto";

        [Description("Only supported by uni-1 and uni-1-max. Helps with real-world landmarks and products.")]
        public bool WebSearch { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            var useUniModel = Settings?.model == "uni-1" || Settings?.model == "uni-1-max";
            var isEdit = itemPayload is ImageItemPayload ip && !string.IsNullOrWhiteSpace(ip.UniImageToModify);

            if (propertyName == nameof(Style) || propertyName == nameof(OutputFormat) || propertyName == nameof(WebSearch))
            {
                return useUniModel;
            }

            if (propertyName == nameof(ImageRequest.aspect_ratio))
            {
                return !isEdit;
            }

            return true;
        }
    }
}
