using PluginBase;
using System.ComponentModel;

namespace FalAiPlugin
{
    public class ItemPayload : IPayloadPropertyVisibility
    {
        private string pollingId;

        [Description("PollingId (generation id) is filled when you make new request. Unlike in other plugins, this is not cleared when generation is completed, because ths id is needed if you want to extend the video. " +
            "If you need to create new variation, clear this id, otherwise the same result will be fetched from the server")]
        public string PollingId { get => pollingId; set => pollingId = value; }

        public string Prompt { get; set; }
        public string NegativePrompt { get; set; }

        public int Seed { get; set; } = 0;

        [Description("Duration of the video in seconds")]
        public string Duration { get; set; } = "5";

        [Description("Duration of the video in seconds")]
        [CustomName("Duration")]
        public string DurationMinimax { get; set; } = "5";

        [Description("Duration of the video in seconds")]
        [CustomName("Duration")]
        public string DurationPixverse { get; set; } = "5";

        [Description("Duration of the video in seconds")]
        [CustomName("Duration")]
        public string DurationVeo { get; set; } = "8s";

        [Description("Duration of the video in seconds")]
        [CustomName("Duration")]
        public int DurationSora { get; set; } = 4;

        [EnableFileDrop]
        public string ImageSource { get; set; }

        [EnableFileDrop]
        public string AudioSource { get; set; }

        public int UpscaleFactor { get; set; } = 2;

        [EnableFileDrop]
        public string FirstFrame { get; set; }

        [EnableFileDrop]
        public string LastFrame { get; set; }

        [EnableFileDrop]
        public string VideoSource
        {
            get; set;
        }

        public ImageSourceContainer ImageSourceCont { get; set; } = new();

        public ItemPayload()
        {
        }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp)
            {
                if (propertyName == nameof(FirstFrame))
                {
                    return tp.Model.Contains("first", StringComparison.CurrentCultureIgnoreCase);
                }

                if (propertyName == nameof(LastFrame))
                {
                    return tp.Model.Contains("last", StringComparison.CurrentCultureIgnoreCase);
                }

                if (propertyName == nameof(ImageSourceCont))
                {
                    return tp.Model.Contains("veo3.1/reference-to-video", StringComparison.CurrentCultureIgnoreCase);
                }

                if (propertyName == nameof(DurationSora))
                {
                    return tp.Model.Contains("sora", StringComparison.CurrentCultureIgnoreCase);
                }

                if (tp.Model.Contains("sora", StringComparison.CurrentCultureIgnoreCase) && (propertyName == nameof(Duration) || propertyName == nameof(NegativePrompt)))
                {
                    return false;
                }

                if (tp.Model.Contains("upscale"))
                {
                    // In upscale, there's really not a lot of things to edit
                    return propertyName == nameof(VideoSource) || propertyName == nameof(UpscaleFactor) || propertyName == nameof(PollingId);
                }

                if (propertyName == nameof(UpscaleFactor))
                {
                    return false;
                }

                if (tp.Model.Contains("omnihuman"))
                {
                    // THis also has very few inputs
                    return propertyName == nameof(ImageSource) || propertyName == nameof(AudioSource) || propertyName == nameof(PollingId);
                }

                if (propertyName == nameof(DurationVeo))
                {
                    return tp.Model.StartsWith("veo");
                }

                if (tp.Model.StartsWith("lucy-edit"))
                {
                    switch (propertyName)
                    {
                        case nameof(NegativePrompt):
                        case nameof(Seed):
                        case nameof(Duration):
                        case nameof(DurationMinimax):
                        case nameof(DurationPixverse):
                            return false;

                        default:
                            break;
                    }
                }

                if (propertyName == nameof(VideoSource))
                {
                    return tp.Model.StartsWith("lucy-edit") || tp.Model.Contains("upscale");
                }

                if (propertyName == nameof(DurationPixverse))
                {
                    return tp.Model.StartsWith("pixverse");
                }

                if (propertyName == nameof(ImageSource))
                {
                    return tp.Model.EndsWith("image-to-video") || tp.Model.EndsWith("speech-to-video");
                }

                if (propertyName == nameof(Duration))
                {
                    return !tp.Model.StartsWith("veo3") && !tp.Model.StartsWith("minimax") && !tp.Model.StartsWith("wan/") && !tp.Model.StartsWith("ltx") && !tp.Model.StartsWith("pixverse");
                }

                if (propertyName == nameof(DurationMinimax))
                {
                    return tp.Model.StartsWith("minimax");
                }

                if (propertyName == nameof(AudioSource))
                {
                    return tp.Model.EndsWith("speech-to-video");
                }

                if (tp.Model.StartsWith("minimax"))
                {
                    switch (propertyName)
                    {
                        case nameof(Duration):
                            return false;

                        case nameof(Seed):
                        case nameof(NegativePrompt):
                            return !tp.Model.Contains("text-to-video");

                        default:
                            break;
                    }
                }
            }

            return true;
        }
    }
}