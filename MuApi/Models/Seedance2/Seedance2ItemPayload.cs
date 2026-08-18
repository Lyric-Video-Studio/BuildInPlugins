using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin.Models.Seedance2
{
    public class Seedance2ItemPayload
    {
        [Description("Item-level prompt suffix. Use @image1, @video1, @audio1 and similar tags to refer to the references below.")]
        [EditorWidth(360)]
        public string Prompt { get; set; }

        [PropertyComboOptions(["4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "30"])]
        public int Duration { get; set; } = 5;

        public ImageReferenceContainer ImageReferences { get; set; } = new();

        public AudioReferenceContainer AudioReferences { get; set; } = new();

        public VideoReferenceContainer VideoReferences { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {
            if (!Seedance2TrackPayload.SupportsImageReferences(model) && IsImageReferenceProperty(propertyName))
            {
                return false;
            }

            if (!Seedance2TrackPayload.SupportsAudioReferences(model) && IsAudioReferenceProperty(propertyName))
            {
                return false;
            }

            if (!Seedance2TrackPayload.SupportsVideoReferences(model) && IsVideoReferenceProperty(propertyName))
            {
                return false;
            }

            return true;
        }

        private static bool IsImageReferenceProperty(string propertyName) =>
            propertyName == nameof(ImageReferences) || ImageReferenceContainer.IsImageRefName(propertyName);

        private static bool IsAudioReferenceProperty(string propertyName) =>
            propertyName == nameof(AudioReferences) || AudioReferenceContainer.IsAudioRefName(propertyName);

        private static bool IsVideoReferenceProperty(string propertyName) =>
            propertyName == nameof(VideoReferences) || VideoReferenceContainer.IsVideoRefName(propertyName);
    }
}
