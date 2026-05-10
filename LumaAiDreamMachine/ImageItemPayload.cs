using PluginBase;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LumaAiDreamMachinePlugin
{
    public class ImageItemPayload : IJsonOnDeserialized, IPayloadPropertyVisibility
    {
        [IgnoreDynamicEdit]
        public bool IsVideo { get; set; } = false;

        private string prompt = "";

        public string Prompt { get => prompt; set => prompt = value; }

        [ParentName("Image reference")]
        public ImageRef ImageRef { get; set; }

        [ParentName("Style reference")]
        public ImageRef StyleRef { get; set; }

        [ParentName("Edit source image")]
        public ImageRef ModifyImage { get; set; }

        [Description("Up to 4")]
        public ObservableCollection<CharacterRef> CharacterRefs { get; set; } = new ObservableCollection<CharacterRef>();

        [EnableFileDrop]
        [CustomName("Uni image to modify")]
        public string UniImageToModify { get; set; }

        [Description("Used by uni-1 and uni-1-max as extra visual references")]
        public ObservableCollection<UniReferenceImage> UniReferenceImages { get; set; } = new ObservableCollection<UniReferenceImage>();

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

        [CustomAction("Add uni reference image")]
        public void AddUniReferenceImage()
        {
            var item = new UniReferenceImage();
            item.AddParent(UniReferenceImages);
            UniReferenceImages.Add(item);
        }

        public void OnDeserialized()
        {
            foreach (var item in CharacterRefs)
            {
                item.AddParent(CharacterRefs);
            }

            foreach (var item in UniReferenceImages)
            {
                item.AddParent(UniReferenceImages);
            }
        }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            var useUniModel = trackPayload is ImageTrackPayload tp && (tp.Settings?.model == "uni-1" || tp.Settings?.model == "uni-1-max");

            if (propertyName is nameof(UniImageToModify) or nameof(UniReferenceImages) or nameof(UniReferenceImage.RemoveUniReferenceImage) or nameof(AddUniReferenceImage) or nameof(UniReferenceImages))
            {
                return useUniModel;
            }

            if (propertyName is nameof(AddImageReference) or nameof(ImageRef) or  nameof(RemoveImageReference) or 
                nameof(AddStyleReference) or nameof(RemoveStyleReference) or nameof(StyleRef) or
                nameof(AddCharacterReference) or nameof(CharacterRefs) or nameof(CharacterRef.RemoveCharacterReference) or
                nameof(AddModImg) or nameof(RemoveAddModImg) or nameof(ModifyImage))
            {
                return !useUniModel;
            }

            return true;
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
        [CustomName("Source file")]
        public string CharacterSourceFile { get; set; }

        [CustomAction("Remove character reference", false, nameof(CharacterSourceFile))]
        public void RemoveCharacterReference()
        {
            parent.Remove(this);
        }
    }

    public class UniReferenceImage
    {
        private ObservableCollection<UniReferenceImage> parent;

        public void AddParent(ObservableCollection<UniReferenceImage> list)
        {
            parent = list;
        }

        [EnableFileDrop]
        [CustomName("Source file")]
        public string UniSourceFile { get; set; }

        [CustomAction("Remove uni reference image", false, nameof(UniSourceFile))]
        public void RemoveUniReferenceImage()
        {
            parent.Remove(this);
        }
    }
}
