using PluginBase;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace FalAiPlugin
{
    public class ImageItemPayload : IPayloadPropertyVisibility
    {
        public string PollingId { get; set; }

        public string Prompt { get; set; }
        public string NegativePrompt { get; set; }
        public int Seed { get; set; }

        public ObservableCollection<ImageSource> ImageSources { get; set; } = new();

        public ImageItemPayload()
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
                    return (ip.Model?.EndsWith("image-to-image") ?? false) || (ip.Model?.Contains("edit") ?? false);
                }
            }
            return true;
        }
    }
}