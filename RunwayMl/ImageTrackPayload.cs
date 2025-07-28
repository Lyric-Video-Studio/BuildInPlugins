using PluginBase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace RunwayMlPlugin
{
    public class ImageTrackPayload
    {
        public static Subject<bool> Refresh { get; } = new Subject<bool>();

        public string Prompt { get; set; }
        public int Seed { get; set; }
        public List<ImagePayloadReference> ReferenceImages { get; set; } = new List<ImagePayloadReference>();

        public string Ratio { get; set; }

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
}