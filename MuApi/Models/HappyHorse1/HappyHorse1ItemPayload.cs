using MuApiPlugin.Models.Seedance2;
using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin.Models.HappyHorse1
{
    public class HappyHorse1ItemPayload : IMuApiPollingPayload
    {
        [Description("Generation id saved after submit. Leave it in place if you want the plugin to resume polling instead of creating a new request.")]
        public string PollingId { get; set; }

        [Description("Item-level prompt suffix for Happy Horse 1 video generation.")]
        [EditorWidth(360)]
        public string Prompt { get; set; }

        [PropertyComboOptions(["4", "5", "6", "7", "8", "9", "10", "12", "15"])]
        public int Duration { get; set; } = 5;

        public ImageReferenceContainer ImageReferences { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {
            if (!HappyHorse1TrackPayload.IsHappyHorseModel(model))
            {
                return false;
            }

            if (model is HappyHorse1TrackPayload.ModelT2V1080p or HappyHorse1TrackPayload.ModelT2V720p && (propertyName == nameof(ImageReferences) || ImageReferenceContainer.IsImageRefName(propertyName)))
            {
                return false;
            }

            return true;
        }
    }
}
