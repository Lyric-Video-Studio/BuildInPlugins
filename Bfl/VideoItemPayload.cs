using PluginBase;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BflTxtToImgPlugin
{
    public class VideoItemPayload : IPayloadPropertyVisibility
    {
        public string Prompt { get; set; } = "Progressive metal band from Finland playing in a forest";

        [Description("Only modify if you know what you are doing. This URL is used to resume polling a submitted generation. Clear it to create a new generation.")]
        public string PollingUrl { get; set; } = "";

        // Keeps a completed draft from being mistaken for a pending Draft Enhance job.
        public string SubmittedMode { get; set; } = "";

        [EnableFileDrop, TriggerReload] public string InputImage { get; set; }

        [CustomName("Image 1 time (seconds)")]
        [Description("Optional FLUX 3 keyframe time in seconds. -1 = automatic placement; 0 or more = exact position.")]
        [Range(-1d, 20d)]
        public double InputImageSeconds { get; set; } = -1;
        [EnableFileDrop, TriggerReload] public string InputImage2 { get; set; }

        [CustomName("Image 2 time (seconds)")]
        [Description("Optional FLUX 3 keyframe time in seconds. -1 = automatic placement; 0 or more = exact position.")]
        [Range(-1d, 20d)]
        public double InputImage2Seconds { get; set; } = -1;
        [EnableFileDrop, TriggerReload] public string InputImage3 { get; set; }

        [CustomName("Image 3 time (seconds)")]
        [Description("Optional FLUX 3 keyframe time in seconds. -1 = automatic placement; 0 or more = exact position.")]
        [Range(-1d, 20d)]
        public double InputImage3Seconds { get; set; } = -1;
        [EnableFileDrop, TriggerReload] public string InputImage4 { get; set; }

        [CustomName("Image 4 time (seconds)")]
        [Description("Optional FLUX 3 keyframe time in seconds. -1 = automatic placement; 0 or more = exact position.")]
        [Range(-1d, 20d)]
        public double InputImage4Seconds { get; set; } = -1;
        [EnableFileDrop, TriggerReload] public string InputImage5 { get; set; }

        [CustomName("Image 5 time (seconds)")]
        [Description("Optional FLUX 3 keyframe time in seconds. -1 = automatic placement; 0 or more = exact position.")]
        [Range(-1d, 20d)]
        public double InputImage5Seconds { get; set; } = -1;
        [EnableFileDrop, TriggerReload] public string InputImage6 { get; set; }

        [CustomName("Image 6 time (seconds)")]
        [Description("Optional FLUX 3 keyframe time in seconds. -1 = automatic placement; 0 or more = exact position.")]
        [Range(-1d, 20d)]
        public double InputImage6Seconds { get; set; } = -1;
        [EnableFileDrop, TriggerReload] public string InputImage7 { get; set; }

        [CustomName("Image 7 time (seconds)")]
        [Description("Optional FLUX 3 keyframe time in seconds. -1 = automatic placement; 0 or more = exact position.")]
        [Range(-1d, 20d)]
        public double InputImage7Seconds { get; set; } = -1;
        [EnableFileDrop, TriggerReload] public string InputImage8 { get; set; }

        [CustomName("Image 8 time (seconds)")]
        [Description("Optional FLUX 3 keyframe time in seconds. -1 = automatic placement; 0 or more = exact position.")]
        [Range(-1d, 20d)]
        public double InputImage8Seconds { get; set; } = -1;
        [EnableFileDrop, TriggerReload] public string InputImage9 { get; set; }

        [CustomName("Image 9 time (seconds)")]
        [Description("Optional FLUX 3 keyframe time in seconds. -1 = automatic placement; 0 or more = exact position.")]
        [Range(-1d, 20d)]
        public double InputImage9Seconds { get; set; } = -1;
        [EnableFileDrop, TriggerReload] public string InputImage10 { get; set; }

        [CustomName("Image 10 time (seconds)")]
        [Description("Optional FLUX 3 keyframe time in seconds. -1 = automatic placement; 0 or more = exact position.")]
        [Range(-1d, 20d)]
        public double InputImage10Seconds { get; set; } = -1;
        [EnableFileDrop] public string InputVideo { get; set; }

        [EnableFileDrop]
        [Description("Local draft-cache .bin bundle returned by BFL. Draft Enhance uses this to render the matching full-quality video.")]
        public string DraftCache { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is not VideoTrackPayload track)
            {
                return true;
            }

            if (propertyName == nameof(SubmittedMode))
            {
                return false;
            }

            var imageProperty = propertyName is nameof(InputImage) or nameof(InputImage2) or nameof(InputImage3) or nameof(InputImage4) or nameof(InputImage5)
                or nameof(InputImage6) or nameof(InputImage7) or nameof(InputImage8) or nameof(InputImage9) or nameof(InputImage10);

            if (imageProperty)
            {
                return track.Mode == VideoTrackPayload.ModeImageToVideo;
            }

            if (propertyName is nameof(InputImageSeconds) or nameof(InputImage2Seconds) or nameof(InputImage3Seconds) or nameof(InputImage4Seconds) or nameof(InputImage5Seconds) or nameof(InputImage6Seconds) or nameof(InputImage7Seconds) or nameof(InputImage8Seconds) or nameof(InputImage9Seconds) or nameof(InputImage10Seconds))
            {
                if (track.Mode != VideoTrackPayload.ModeImageToVideo)
                {
                    return false;
                }

                // Keep the edit panel uncluttered until an image is assigned to a slot.
                return propertyName switch
                {
                    nameof(InputImageSeconds) => !string.IsNullOrWhiteSpace(InputImage),
                    nameof(InputImage2Seconds) => !string.IsNullOrWhiteSpace(InputImage2),
                    nameof(InputImage3Seconds) => !string.IsNullOrWhiteSpace(InputImage3),
                    nameof(InputImage4Seconds) => !string.IsNullOrWhiteSpace(InputImage4),
                    nameof(InputImage5Seconds) => !string.IsNullOrWhiteSpace(InputImage5),
                    nameof(InputImage6Seconds) => !string.IsNullOrWhiteSpace(InputImage6),
                    nameof(InputImage7Seconds) => !string.IsNullOrWhiteSpace(InputImage7),
                    nameof(InputImage8Seconds) => !string.IsNullOrWhiteSpace(InputImage8),
                    nameof(InputImage9Seconds) => !string.IsNullOrWhiteSpace(InputImage9),
                    nameof(InputImage10Seconds) => !string.IsNullOrWhiteSpace(InputImage10),
                    _ => false,
                };
            }

            if (propertyName == nameof(InputVideo))
            {
                return track.Mode is VideoTrackPayload.ModeVideoContinuation or VideoTrackPayload.ModeVideoEdit;
            }

            return propertyName != nameof(DraftCache) || track.Mode == VideoTrackPayload.ModeDraftEnhance;
        }
    }
}
