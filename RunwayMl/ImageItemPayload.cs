using PluginBase;
using System.ComponentModel;
using System.Reactive.Subjects;

namespace RunwayMlPlugin
{
    public class ImageItemPayload
    {
        public static Subject<bool> Refresh { get; } = new Subject<bool>();

        public string PollingId { get; set; }

        public string Prompt { get; set; }
        public int Seed { get; set; }
        public List<ImagePayloadReference> ReferenceImages { get; set; } = new List<ImagePayloadReference>();

        [CustomAction("Remove last reference")]
        public void RemoveLastReference()
        {
            ReferenceImages.RemoveAt(ReferenceImages.Count - 1);
            Refresh.OnNext(true);
        }

        [CustomAction("Add reference")]
        public void AddReference()
        {
            ReferenceImages.Add(new ImagePayloadReference() { });
            Refresh.OnNext(true);
        }
    }

    public class ImagePayloadReference
    {
        public string FilePath { get; set; }

        [Description("Use tags in prompt, like @tag")]
        public string Tag { get; set; }
    }
}