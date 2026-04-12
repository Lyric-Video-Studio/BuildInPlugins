using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin.Models.Seedance2
{
    public class Seedance2ItemPayload : IMuApiPollingPayload
    {
        [Description("Generation id saved after submit. Leave it in place if you want the plugin to resume polling instead of creating a new request.")]
        public string PollingId { get; set; }

        [Description("Item-level prompt suffix. Use @image1, @image2, @audio1 and similar tags to refer to the references below.")]
        [EditorWidth(360)]
        public string Prompt { get; set; }

        [PropertyComboOptions(["5", "10", "15"])]
        public int Duration { get; set; } = 5;

        public ImageReferenceContainer ImageReferences { get; set; } = new();

        public AudioReferenceContainer AudioReferences { get; set; } = new();

        public VideoReferenceContainer VideoReferences { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {

            if (model == Seedance2TrackPayload.ModelI2V  && (AudioReferenceContainer.IsAudioRefName(propertyName) || VideoReferenceContainer.IsVideoRefName(propertyName)))
            {
                return false;
            }

            if (model == Seedance2TrackPayload.ModelT2V && (AudioReferenceContainer.IsAudioRefName(propertyName) || ImageReferenceContainer.IsImageRefName(propertyName) || VideoReferenceContainer.IsVideoRefName(propertyName)))
            {
                return false;
            }
            return true;
        }
    }
}
