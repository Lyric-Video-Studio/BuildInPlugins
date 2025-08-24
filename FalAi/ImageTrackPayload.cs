using PluginBase;
using System.Collections.ObjectModel;

namespace FalAiPlugin
{
    public class ImageTrackPayload : IPayloadPropertyVisibility
    {
        public string Model { get; set; }
        public string Prompt { get; set; }
        public string NegativePrompt { get; set; }
        public int Seed { get; set; }
        //public ObservableCollection<ImagePayloadReference> ReferenceImages { get; set; } = new();

        [CustomName("Size")]
        public string SizeQwen { get; set; } = "landscape_16_9";

        [CustomName("Size")]
        public string SizeImagen4 { get; set; } = "16:9";

        public ImageTrackPayload()
        {
            /*ImagePayloadReference.RemoveReference += (s, e) =>
            {
                if (s is ImagePayloadReference r)
                {
                    ReferenceImages.Remove(r);
                }
            };*/
        }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is ImageTrackPayload ip)
            {
                if (propertyName == nameof(SizeQwen))
                {
                    return Model == "qwen-image" || Model == "wan/v2.2-a14b/text-to-image" || Model == "hidream-i1-full";
                }

                if (propertyName == nameof(SizeImagen4))
                {
                    return Model == "imagen4/preview";
                }
            }
            return true;
        }

        /*[CustomAction("Add reference")]
        public void AddReference()
        {
            ReferenceImages.Add(new ImagePayloadReference() { });
        }*/
    }
}