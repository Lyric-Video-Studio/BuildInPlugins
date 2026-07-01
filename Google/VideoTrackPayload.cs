using PluginBase;

namespace GooglePlugin
{
    public class VideoTrackPayload : IPayloadPropertyVisibility
    {
        public const string ModelGeminiOmniFlashPreview = "gemini-omni-flash-preview";
        public const string VideoTaskAuto = "Auto";
        public const string VideoTaskUnspecified = "Unspecified";
        public const string VideoTaskTextToVideo = "Text to Video";
        public const string VideoTaskImageToVideo = "Image to Video";
        public const string VideoTaskReferenceToVideo = "Reference to Video";
        public const string VideoTaskEdit = "Edit Video";

        [PropertyComboOptions(["veo-3.1-fast-generate-preview", "veo-3.1-generate-preview", "veo-3.1-lite-generate-preview", ModelGeminiOmniFlashPreview])]
        [TriggerReload]
        public string Model { get; set; } = "veo-3.1-fast-generate-preview";
        public string Prompt { get; set; }

        [PropertyComboOptions(["720p", "1080p", "4k"])]
        public string Resolution { get; set; } = "1080p";

        [PropertyComboOptions(["Auto", "16:9", "9:16"])]
        public string AspectRatio { get; set; } = "16:9";

        [PropertyComboOptions([VideoTaskAuto, VideoTaskUnspecified, VideoTaskTextToVideo, VideoTaskImageToVideo, VideoTaskReferenceToVideo, VideoTaskEdit])]
        public string VideoTask { get; set; } = VideoTaskAuto;

        [EnableFileDrop]
        [EnableDoodling]
        public string ImageSource { get; set; }

        [EnableFileDrop]
        [EnableDoodling]
        public string ImageSource2 { get; set; }

        [EnableFileDrop]
        [EnableDoodling]
        public string ImageSource3 { get; set; }

        [EnableFileDrop]
        [EnableDoodling]
        public string ImageSource4 { get; set; }

        [EnableFileDrop]
        [EnableDoodling]
        public string ImageSource5 { get; set; }

        [EnableFileDrop]
        [EnableDoodling]
        public string ImageSource6 { get; set; }

        [EnableFileDrop]
        public string VideoSource { get; set; }

        public string VideoSourceWarning { get; set; } = "Using video source is highly restricted because of Google, event though this model is supposed to be superios in video editing, oh the irony";

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is VideoTrackPayload tp)
            {
                var isOmni = tp.Model == ModelGeminiOmniFlashPreview;
                if (propertyName is nameof(VideoTask) or nameof(VideoSource))
                {
                    return isOmni;
                }

                if (propertyName == nameof(VideoSourceWarning))
                {
                    return isOmni && !string.IsNullOrWhiteSpace(tp.VideoSource);
                }

                if (propertyName == nameof(Resolution))
                {
                    return !isOmni;
                }

                if (propertyName is nameof(ImageSource2) or nameof(ImageSource3))
                {
                    return isOmni || tp.Model == "veo-3.1-fast-generate-preview";
                }

                if (propertyName is nameof(ImageSource4) or nameof(ImageSource5) or nameof(ImageSource6))
                {
                    return isOmni;
                }
            }

            return true;
        }
    }
}




