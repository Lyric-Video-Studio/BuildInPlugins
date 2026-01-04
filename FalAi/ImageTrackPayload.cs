using PluginBase;
using System.Collections.ObjectModel;

namespace FalAiPlugin
{
    public class ImageTrackPayload : IPayloadPropertyVisibility
    {
        public event EventHandler ModelChanged;

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
        public string SizeImagen4 { get; set; } = "16:9";

        [CustomName("Size")]
        public string SizeGpt15 { get; set; } = "1536x1024";

        public int WidthPx { get; set; } = 1920;
        public int HeigthPx { get; set; } = 1080;

        public string BackGround { get; set; } = "auto";

        public ImageSourceContainer ImageSource { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is ImageTrackPayload ip)
            {
                if (propertyName == nameof(WidthPx) || propertyName == nameof(HeigthPx)) // TODO: Käyköhän tää kaikille??
                {
                    return !ip.Model.Contains("gpt-image-1.5");
                }

                if (ip.Model.Contains("gpt", StringComparison.InvariantCultureIgnoreCase) && (propertyName is nameof(Seed) or nameof(NegativePrompt)))
                {
                    return false;
                }

                if (propertyName is nameof(SizeGpt15) or nameof(BackGround))
                {
                    return ip.Model.Contains("gpt-image-1.5", StringComparison.InvariantCultureIgnoreCase);
                }

                if (propertyName == nameof(ImageSource))
                {
                    return (ip.Model?.EndsWith("image-to-image") ?? false) || (ip.Model?.Contains("edit") ?? false);
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