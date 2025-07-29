using PluginBase;
using System.Collections.ObjectModel;
using System.Reactive.Subjects;

namespace RunwayMlPlugin
{
    public class ImageTrackPayload
    {
        public string Prompt { get; set; }
        public int Seed { get; set; }
        public ObservableCollection<ImagePayloadReference> ReferenceImages { get; set; } = new();

        public string Ratio { get; set; } = "1920:1080";

        public ImageTrackPayload()
        {
            ImagePayloadReference.RemoveReference += (s, e) =>
            {
                if (s is ImagePayloadReference r)
                {
                    ReferenceImages.Remove(r);
                }
            };
        }

        [CustomAction("Add reference")]
        public void AddReference()
        {
            ReferenceImages.Add(new ImagePayloadReference() { });
        }
    }
}