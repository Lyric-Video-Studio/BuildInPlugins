using PluginBase;
using System.Collections.ObjectModel;

namespace FalAiPlugin
{
    public class ImageTrackPayload : IPayloadPropertyVisibility
    {
        public static event EventHandler ModelChanged;

        private string model = "hidream-i1-full";

        public string Model
        {
            get => model;
            set
            {
                var notifi = IPayloadPropertyVisibility.UserInitiatedSet;
                model = value;

                if (notifi)
                {
                    ModelChanged?.Invoke(this, null);
                }
            }
        }

        public string Prompt { get; set; }
        public string NegativePrompt { get; set; }
        public int Seed { get; set; }

        [CustomName("Size")]
        public string SizeQwen { get; set; } = "landscape_16_9";

        [CustomName("Size")]
        public string SizeImagen4 { get; set; } = "16:9";

        public ImageSourceContainer ImageSource { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is ImageTrackPayload ip)
            {
                if (ip.Model.Contains("gpt", StringComparison.InvariantCultureIgnoreCase) && (propertyName is nameof(Seed) or nameof(NegativePrompt)))
                {
                    return false;
                }

                if (propertyName == nameof(ImageSource))
                {
                    return (ip.Model?.EndsWith("image-to-image") ?? false) || (ip.Model?.Contains("edit") ?? false);
                }

                if (propertyName == nameof(SizeQwen))
                {
                    if (ip.Model.Contains("edit") || ip.Model.Contains("image-to-image"))
                    {
                        return false;
                    }
                    return ip.Model == "qwen-image" || ip.Model == "wan/v2.2-a14b/text-to-image" || ip.Model == "hidream-i1-full" || ip.Model.Contains("seedream");
                }

                if (propertyName == nameof(SizeImagen4))
                {
                    return ip.Model == "imagen4/preview";
                }
            }
            return true;
        }
    }

    public class ImageSourceContainer
    {
        public static event EventHandler RemoveReference;

        public ObservableCollection<ImageSourceItem> ImageSources { get; set; } = new();

        public ImageSourceContainer()
        {
            ImageSourceItem.RemoveReference += (s, e) =>
            {
                if (s is ImageSourceItem r)
                {
                    ImageSources.Remove(r);
                }
            };
        }

        [CustomAction("Add reference")]
        public void AddReference()
        {
            ImageSources.Add(new ImageSourceItem());
        }
    }

    public class ImageSourceItem
    {
        public static event EventHandler RemoveReference;

        [EnableFileDrop]
        public string FileSource { get; set; }

        [CustomAction("Remove")]
        public void Remove()
        {
            RemoveReference?.Invoke(this, null);
        }
    }
}