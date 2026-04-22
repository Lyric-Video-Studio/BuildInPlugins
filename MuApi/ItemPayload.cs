using MuApiPlugin.Models.GptImage2;
using MuApiPlugin.Models.Seedance2;
using PluginBase;
using System.Collections.ObjectModel;

namespace MuApiPlugin
{
    public class ItemPayload : IPayloadPropertyVisibility
    {
        public ItemPayload()
        {
        }

        public ItemPayload(string text, bool isImageSource)
        {
            if (isImageSource)
            {
                Seedance2.ImageReferences.ImageSources.Add(new ImageReferenceItem() { ImageFile = text });
            }
            else
            {
                Seedance2.Prompt = text;
            }
        }

        [HideAllChildren]
        [ParentName("")]
        public Seedance2ItemPayload Seedance2 { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp)
            {
                switch (propertyName)
                {
                    case nameof(Seedance2):
                        return TrackPayload.IsSeedance2(tp);
                    default:
                        break;
                }

                if (TrackPayload.IsSeedance2(tp))
                {
                    return Seedance2.ShouldPropertyBeVisible(propertyName, tp.Model);
                }
            }
            return true;
        }
    }

    public class ImageReferenceContainer
    {
        public ObservableCollection<ImageReferenceItem> ImageSources { get; set; } = new();

        public ImageReferenceContainer()
        {
            ImageReferenceItem.RemoveReference += (sender, _) =>
            {
                if (sender is ImageReferenceItem imageReference)
                {
                    ImageSources.Remove(imageReference);
                }
            };
        }

        [CustomAction("Add image reference")]
        public void AddImageReference()
        {
            ImageSources.Add(new ImageReferenceItem());
        }

        public static bool IsImageRefName(string propertyName)
        {
            return propertyName is nameof(AddImageReference) or nameof(ImageReferenceItem.ImageFile) or nameof(ImageReferenceItem.RemoveImage);
        }
    }

    public class AudioReferenceContainer
    {
        public ObservableCollection<AudioReferenceItem> AudioSources { get; set; } = new();

        public AudioReferenceContainer()
        {
            AudioReferenceItem.RemoveReference += (sender, _) =>
            {
                if (sender is AudioReferenceItem audioReference)
                {
                    AudioSources.Remove(audioReference);
                }
            };
        }

        [CustomAction("Add audio reference")]
        public void AddAudioReference()
        {
            AudioSources.Add(new AudioReferenceItem());
        }

        public static bool IsAudioRefName(string propertyName)
        {
            return propertyName is nameof(AddAudioReference) or nameof(AudioReferenceItem.AudioFile) or nameof(AudioReferenceItem.RemoveAudio);
        }
    }

    public class VideoReferenceContainer
    {
        public ObservableCollection<VideoReferenceItem> VideoSources { get; set; } = new();

        public VideoReferenceContainer()
        {
            VideoReferenceItem.RemoveReference += (sender, _) =>
            {
                if (sender is VideoReferenceItem audioReference)
                {
                    VideoSources.Remove(audioReference);
                }
            };
        }

        [CustomAction("Add video reference")]
        public void AddVideoReference()
        {
            VideoSources.Add(new VideoReferenceItem());
        }

        public static bool IsVideoRefName(string propertyName)
        {
            return propertyName is nameof(AddVideoReference) or nameof(VideoReferenceItem.VideoFile) or nameof(VideoReferenceItem.RemoveVideo);
        }
    }

    public class ImageReferenceItem
    {
        public static event EventHandler RemoveReference;

        [EnableFileDrop]
        public string ImageFile { get; set; }

        [CustomAction("Remove")]
        public void RemoveImage()
        {
            RemoveReference?.Invoke(this, EventArgs.Empty);
        }
    }

    public class AudioReferenceItem
    {
        public static event EventHandler RemoveReference;

        [EnableFileDrop]
        public string AudioFile { get; set; }

        [CustomAction("Remove")]
        public void RemoveAudio()
        {
            RemoveReference?.Invoke(this, EventArgs.Empty);
        }
    }

    public class VideoReferenceItem
    {
        public static event EventHandler RemoveReference;

        [EnableFileDrop]
        public string VideoFile { get; set; }

        [CustomAction("Remove")]
        public void RemoveVideo()
        {
            RemoveReference?.Invoke(this, EventArgs.Empty);
        }
    }
}
