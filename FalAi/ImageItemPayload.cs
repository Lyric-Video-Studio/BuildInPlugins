using PluginBase;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace FalAiPlugin
{
    public class ImageItemPayload
    {
        public string PollingId { get; set; }

        public string Prompt { get; set; }
        public string NegativePrompt { get; set; }
        public int Seed { get; set; }
        //public ObservableCollection<ImagePayloadReference> ReferenceImages { get; set; } = new();

        public ImageItemPayload()
        {
            /*ImagePayloadReference.RemoveReference += (s, e) =>
            {
                if (s is ImagePayloadReference r)
                {
                    ReferenceImages.Remove(r);
                }
            };*/
        }

        /*[CustomAction("Add reference")]
        public void AddReference()
        {
            ReferenceImages.Add(new ImagePayloadReference() { });
        }*/
    }

    /*public class ImagePayloadReference
    {
        public static event EventHandler RemoveReference;

        [EnableFileDrop]
        public string FilePath { get; set; }

        [Description("Use tags in prompt, like @tag")]
        public string Tag { get; set; }

        [CustomAction("Remove")]
        public void Remove()
        {
            RemoveReference.Invoke(this, null);
        }
    }*/
}