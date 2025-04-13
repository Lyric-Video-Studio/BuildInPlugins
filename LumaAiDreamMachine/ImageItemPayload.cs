using PluginBase;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LumaAiDreamMachinePlugin
{
    public class ImageItemPayload : IJsonOnDeserialized
    {
        [IgnoreDynamicEdit]
        public bool IsVideo { get; set; } = false;

        private string prompt = "";

        //public KeyFrames KeyFrames { get; set; } = new KeyFrames();

        public string Prompt { get => prompt; set => prompt = value; }

        [ParentName("Image reference")]
        public ImageRef ImageRef { get; set; }

        [ParentName("Style reference")]
        public ImageRef StyleRef { get; set; }

        [ParentName("Modify imagee")]
        public ImageRef ModifyImage { get; set; }

        [Description("Up to 4")]
        public ObservableCollection<CharacterRef> CharacterRefs { get; set; } = new ObservableCollection<CharacterRef>();

        private string pollingId;

        [Description("PollingId (generation id) is filled when you make new request. Unlike in other plugins, this is not cleared when generation is completed, because ths id is needed if you want to extend the video. " +
            "If you need to create new variation, clear this id, otherwise the same result will be fetched from the server")]
        public string PollingId { get => pollingId; set => pollingId = value; }

        [CustomAction("Add image reference")]
        public void AddImageReference()
        {
            ImageRef = new ImageRef();
        }

        [CustomAction("Remove image reference")]
        public void RemoveImageReference()
        {
            ImageRef = null;
        }

        [CustomAction("Add style reference")]
        public void AddStyleReference()
        {
            StyleRef = new ImageRef();
        }

        [CustomAction("Remove style reference")]
        public void RemoveStyleReference()
        {
            StyleRef = null;
        }

        [CustomAction("Add character reference")]
        public void AddCharacterReference()
        {
            if (CharacterRefs.Count < 4)
            {
                var r = new CharacterRef();
                r.AddParent(CharacterRefs);
                CharacterRefs.Add(r);
            }
        }

        [CustomAction("Add modify image")]
        public void AddModImg()
        {
            ModifyImage = new ImageRef();
        }

        [CustomAction("Remove modify image")]
        public void RemoveAddModImg()
        {
            ModifyImage = null;
        }

        public void OnDeserialized()
        {
            foreach (var item in CharacterRefs)
            {
                item.AddParent(CharacterRefs);
            }
        }
    }

    public class ImageRef
    {
        [EnableFileDrop]
        public string ImageSource { get; set; }

        public double weight { get; set; } = 0.85;
    }

    public class CharacterRef
    {
        private ObservableCollection<CharacterRef> parent;

        public void AddParent(ObservableCollection<CharacterRef> list)
        {
            parent = list;
        }

        [EnableFileDrop]
        public string SourceFile { get; set; }

        [CustomAction("Remove character reference")]
        public void RemoveCharacterReference()
        {
            parent.Remove(this);
        }
    }
}