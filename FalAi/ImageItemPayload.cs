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

        public ImageSourceContainer ImageSources { get; set; } = new();

        public ImageItemPayload()
        {
        }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is ImageTrackPayload ip)
            {
                if (ip.Model.Contains("gpt", StringComparison.InvariantCultureIgnoreCase) && (propertyName is nameof(Seed) or nameof(NegativePrompt)))
                {
                    return false;
                }

                if (ip.Model.Contains("imagineart-1.5-", StringComparison.InvariantCultureIgnoreCase) && (propertyName is nameof(NegativePrompt)))
                {
                    return false;
                }

                if (propertyName == nameof(ImageSources))
                {
                    return (ip.Model?.EndsWith("image-to-image") ?? false) || (ip.Model?.Contains("edit") ?? false);
                }

                if (propertyName == nameof(ImageSourceContainer.AddReference))
                {
                    return (ip.Model?.EndsWith("image-to-image") ?? false) || (ip.Model?.Contains("edit") ?? false);
                }
            }
            return true;
        }
    }
}