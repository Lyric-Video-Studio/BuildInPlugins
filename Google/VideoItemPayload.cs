using PluginBase;
using System.ComponentModel;

namespace GooglePlugin
{
    public class VideoItemPayload: IPayloadPropertyVisibility
    {
        public string Prompt { get; set; }

        [PropertyComboOptions(["3", "4", "5", "6", "7", "8", "9", "10"])]
        public string Duration { get; set; } = "8";

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

        [Description("Paste a LastInteractionId from a previous Gemini Omni request to continue/edit that generated video without re-uploading it. Leave empty for a new one-shot generation.")]
        public string PreviousInteractionId { get; set; }

        [Description("Filled after a successful Gemini Omni request. Copy this value into PreviousInteractionId on a later request to continue or edit that result.")]
        public string LastInteractionId { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is VideoTrackPayload tp)
            {
                var isOmni = tp.Model == VideoTrackPayload.ModelGeminiOmniFlashPreview;
                if (propertyName == nameof(VideoSourceWarning))
                {
                    var itemVideoSource = itemPayload is VideoItemPayload ip ? ip.VideoSource : null;
                    return isOmni && (!string.IsNullOrWhiteSpace(itemVideoSource) || !string.IsNullOrWhiteSpace(tp.VideoSource));
                }

                if (propertyName is nameof(VideoSource) or nameof(PreviousInteractionId) or nameof(LastInteractionId))
                {
                    return isOmni;
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




