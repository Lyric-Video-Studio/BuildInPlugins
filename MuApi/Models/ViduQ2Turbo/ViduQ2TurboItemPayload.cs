using MuApiPlugin.Models.Seedance2;
using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin.Models.ViduQ2Turbo
{
    public class ViduQ2TurboItemPayload : IMuApiPollingPayload
    {
        [Description("Generation id saved after submit. Leave it in place if you want the plugin to resume polling instead of creating a new request.")]
        public string PollingId { get; set; }

        [Description("Item-level prompt suffix for Vidu Q2 Turbo video generation.")]
        [EditorWidth(360)]
        public string Prompt { get; set; }

        [PropertyComboOptions(["4", "5", "6", "7", "8"])]
        public int Duration { get; set; } = 4;

        [EnableFileDrop]
        public string StartImage { get; set; }

        [EnableFileDrop]
        public string EndImage { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {
            if (!ViduQ2TurboTrackPayload.IsViduQ2TurboModel(model))
            {
                return false;
            }

            if (model == ViduQ2TurboTrackPayload.ModelT2V &&
                propertyName is nameof(StartImage) or nameof(EndImage))
            {
                return false;
            }

            if (model == ViduQ2TurboTrackPayload.ModelI2V &&
                propertyName == nameof(EndImage))
            {
                return false;
            }

            return true;
        }
    }
}
