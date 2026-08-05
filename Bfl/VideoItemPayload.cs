using PluginBase;
using System.ComponentModel;

namespace BflTxtToImgPlugin
{
    public class VideoItemPayload : IPayloadPropertyVisibility
    {
        public string Prompt { get; set; } = "Progressive metal band from Finland playing in a forest";

        [Description("Only modify if you know what you are doing. This URL is used to resume polling a submitted generation. Clear it to create a new generation.")]
        public string PollingUrl { get; set; } = "";

        // Keeps a completed draft from being mistaken for a pending Draft Enhance job.
        public string SubmittedMode { get; set; } = "";

        [EnableFileDrop] public string InputImage { get; set; }
        [EnableFileDrop] public string InputImage2 { get; set; }
        [EnableFileDrop] public string InputImage3 { get; set; }
        [EnableFileDrop] public string InputImage4 { get; set; }
        [EnableFileDrop] public string InputImage5 { get; set; }
        [EnableFileDrop] public string InputImage6 { get; set; }
        [EnableFileDrop] public string InputImage7 { get; set; }
        [EnableFileDrop] public string InputImage8 { get; set; }
        [EnableFileDrop] public string InputImage9 { get; set; }
        [EnableFileDrop] public string InputImage10 { get; set; }
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

            if (propertyName == nameof(InputVideo))
            {
                return track.Mode == VideoTrackPayload.ModeVideoContinuation;
            }

            return propertyName != nameof(DraftCache) || track.Mode == VideoTrackPayload.ModeDraftEnhance;
        }
    }
}
