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
                var notifi = IPayloadPropertyVisibility.UserInitiatedSet && model != value;
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
                    return !ip.Model.Contains("gpt-image-1.5") && !ip.Model.Contains("imagineart");
                }

                if (ip.Model.Contains("gpt", StringComparison.InvariantCultureIgnoreCase) && (propertyName is nameof(Seed) or nameof(NegativePrompt)))
                {
                    return false;
                }

                if (ip.Model.Contains("imagineart-1.5-", StringComparison.InvariantCultureIgnoreCase) && (propertyName is nameof(NegativePrompt)))
                {
                    return false;
                }

                if (propertyName is nameof(SizeGpt15) or nameof(BackGround))
                {
                    return ip.Model.Contains("gpt-image-1.5", StringComparison.InvariantCultureIgnoreCase);
                }

                if (propertyName == nameof(ImageSource) || propertyName == nameof(ImageSourceContainer.AddReference))
                {
                    return (ip.Model?.EndsWith("image-to-image") ?? false) || (ip.Model?.Contains("edit") ?? false);
                }

                if (propertyName == nameof(SizeImagen4))
                {
                    return ip.Model == "imagen4/preview" || ip.Model.Contains("imagineart");
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

        [CustomAction("Add image reference")]
        public void AddReference()
        {
            ImageSources.Add(new ImageSourceItem());
        }

        // TODO: This might be bit problematic if same names in child classes
        public static bool IsImageRefName(string name)
        {
            return name is nameof(AddReference) or nameof(ImageSourceItem.ImageFile) or nameof(ImageSourceItem.RemoveImage);
        }
    }

    public class VideoSourceContainer
    {
        public static event EventHandler RemoveReference;

        public ObservableCollection<VideoSourceItem> VideoSources { get; set; } = new();

        public VideoSourceContainer()
        {
            VideoSourceItem.RemoveReference += (s, e) =>
            {
                if (s is VideoSourceItem r)
                {
                    VideoSources.Remove(r);
                }
            };
        }

        // TODO: This might be bit problematic if same names in child classes
        public static bool IsVideoRefName(string name)
        {
            return name is nameof(AddVideoReference) or nameof(VideoSourceItem.VideoFile) or nameof(VideoSourceItem.RemoveVideo);
        }

        [CustomAction("Add video reference")]
        public void AddVideoReference()
        {
            VideoSources.Add(new VideoSourceItem());
        }
    }

    public class AudioSourceContainer
    {
        public static event EventHandler RemoveReference;

        public ObservableCollection<AudioSourceItem> AudioSources { get; set; } = new();

        public AudioSourceContainer()
        {
            AudioSourceItem.RemoveReference += (s, e) =>
            {
                if (s is AudioSourceItem r)
                {
                    AudioSources.Remove(r);
                }
            };
        }

        public static bool IsAudioRefName(string name)
        {
            return name is nameof(AddAudioReference) or nameof(AudioSourceItem.AudioFile) or nameof(AudioSourceItem.RemoveAudio);
        }

        [CustomAction("Add audio reference")]
        public void AddAudioReference()
        {
            AudioSources.Add(new AudioSourceItem());
        }
    }

    public class ImageSourceItem
    {
        public static event EventHandler RemoveReference;

        [EnableFileDrop]
        public string ImageFile { get; set; }

        [CustomAction("Remove")]
        public void RemoveImage()
        {
            RemoveReference?.Invoke(this, null);
        }
    }

    public class VideoSourceItem
    {
        public static event EventHandler RemoveReference;

        [EnableFileDrop]
        public string VideoFile { get; set; }

        [CustomAction("Remove")]
        public void RemoveVideo()
        {
            RemoveReference?.Invoke(this, null);
        }
    }

    public class AudioSourceItem
    {
        public static event EventHandler RemoveReference;

        [EnableFileDrop]
        public string AudioFile { get; set; }

        [CustomAction("Remove")]
        public void RemoveAudio()
        {
            RemoveReference?.Invoke(this, null);
        }
    }
}