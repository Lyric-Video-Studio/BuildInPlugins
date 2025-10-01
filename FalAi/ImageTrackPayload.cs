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

        [CustomName("Size")]
        public string SizeQwen { get; set; } = "landscape_16_9";

        [CustomName("Size")]
        public string SizeImagen4 { get; set; } = "16:9";

        public ObservableCollection<ImageSource> ImageSources { get; set; } = new();

        public ImageTrackPayload()
        {
            ImageSource.RemoveReference += (s, e) =>
            {
                if (s is ImageSource r)
                {
                    ImageSources.Remove(r);
                }
            };
        }

        [CustomAction("Add reference")]
        public void AddReference()
        {
            ImageSources.Add(new ImageSource());
        }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is ImageTrackPayload ip)
            {
                if (propertyName == nameof(ImageSources))
                {
                    return ip.Model?.EndsWith("image-to-image") ?? false;
                }
                if (propertyName == nameof(SizeQwen))
                {
                    return ip.Model == "qwen-image" || ip.Model == "wan/v2.2-a14b/text-to-image" || ip.Model == "hidream-i1-full";
                }

                if (propertyName == nameof(SizeImagen4))
                {
                    return ip.Model == "imagen4/preview";
                }
            }
            return true;
        }
    }

    public class ImageSource
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