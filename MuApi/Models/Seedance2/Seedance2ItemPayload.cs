using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin.Models.Seedance2
{
    public class Seedance2ItemPayload
    {
        [Description("Item-level prompt suffix. Use @image1, @video1, @audio1 and similar tags to refer to the references below.")]
        [EditorWidth(360)]
        public string Prompt { get; set; }

        [PropertyComboOptions(["5", "10", "15"])]
        public int Duration { get; set; } = 5;

        public ImageReferenceContainer ImageReferences { get; set; } = new();

        public AudioReferenceContainer AudioReferences { get; set; } = new();

        public VideoReferenceContainer VideoReferences { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {
            if (Seedance2TrackPayload.IsImageToVideoModel(model) && (AudioReferenceContainer.IsAudioRefName(propertyName) || VideoReferenceContainer.IsVideoRefName(propertyName)))
            {
                return false;
            }

            if (Seedance2TrackPayload.IsTextToVideoModel(model) && (AudioReferenceContainer.IsAudioRefName(propertyName) || ImageReferenceContainer.IsImageRefName(propertyName) || VideoReferenceContainer.IsVideoRefName(propertyName)))
            {
                return false;
            }
            return true;
        }
    }
}
