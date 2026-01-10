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
        public string DurationWan26 { get; set; } = "5";

        [Description("Duration of the video in seconds")]
        [CustomName("Duration")]
        public string DurationMinimax { get; set; } = "6";

        [Description("Duration of the video in seconds")]
        [CustomName("Duration")]
        public string DurationPixverse { get; set; } = "5";

        [Description("Duration of the video in seconds")]
        [CustomName("Duration")]
        public string DurationVeo { get; set; } = "8s";

        [Description("Duration of the video in seconds")]
        [CustomName("Duration")]
        public string DurationSeedream { get; set; } = "12";

        [Description("Duration of the video in seconds")]
        [CustomName("Duration")]
        public int DurationLtx2 { get; set; } = 6;

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

        public string CharacterOrientation { get; set; } = "video";

        public ImageSourceContainer ImageSourceCont { get; set; } = new();

        public ItemPayload()
        {
        }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp)
            {
                if (propertyName == nameof(PollingId))
                {
                    return true;
                }

                if (tp.Model != null && tp.Model.Contains("one-to-all-animation"))
                {
                    // Only image source and video ref
                    return propertyName is nameof(ImageSource) or nameof(VideoSource) or nameof(Prompt) or nameof(NegativePrompt);
                }

                if (tp.Model != null && tp.Model.Contains("motion-control"))
                {
                    // Only image source and video ref
                    return propertyName is nameof(ImageSource) or nameof(VideoSource) or nameof(CharacterOrientation);
                }

                if (tp.Model != null && tp.Model.Contains("hailuo-2.3-fast") && propertyName is nameof(NegativePrompt) or nameof(Seed) or nameof(ImageSourceContainer.AddReference))
                {
                    return false;
                }

                if (tp.Model != null && tp.Model.Contains("bytedance/seedance"))
                {
                    if (propertyName is nameof(LastFrame) or nameof(ImageSource))
                    {
                        return tp.Model.Contains("image-to-video");
                    }
                    return propertyName is nameof(DurationSeedream) or nameof(Prompt);
                }
                else if(propertyName is nameof(DurationSeedream))
                {
                    // Hide minimax specific
                    return false;
                }

                if (tp.Model != null && tp.Model.StartsWith("wan/v2.6/", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (tp.Model.Contains("image", StringComparison.InvariantCultureIgnoreCase) && propertyName == nameof(ImageSource))
                    {
                        return true;
                    }
                    return propertyName == nameof(Prompt) || propertyName == nameof(NegativePrompt) || propertyName == nameof(DurationWan26) || propertyName == nameof(Seed);
                }
                else if (propertyName == nameof(DurationWan26))
                {
                    return false;
                }

                if (tp.Model == "creatify/aurora")
                {
                    return propertyName == nameof(ImageSource) || propertyName == nameof(AudioSource);
                }

                if (tp.Model == "editto")
                {
                    return propertyName == nameof(VideoSource) || propertyName == nameof(Prompt);
                }

                if (tp.Model == "kling-video/ai-avatar/v2/pro")
                {
                    return propertyName == nameof(Prompt) || propertyName == nameof(ImageSource) || propertyName == nameof(AudioSource);
                }

                // Hmm, this system is not really meant for this, but maybe it works:
                if (tp.Model.StartsWith("wan-alpha") && !Prompt.Contains("The background of this video is transparent"))
                {
                    Prompt += "The background of this video is transparent";
                }

                if (tp.Model.Contains("ltxv-2") && (propertyName == nameof(NegativePrompt) || propertyName == nameof(Seed)))
                {
                    return false;
                }
                if (propertyName == nameof(FirstFrame))
                {
                    return tp.Model.Contains("first", StringComparison.CurrentCultureIgnoreCase) || tp.Model.Contains("kling-video/o1", StringComparison.CurrentCultureIgnoreCase);
                }

                if (propertyName == nameof(LastFrame))
                {
                    return tp.Model.Contains("last", StringComparison.CurrentCultureIgnoreCase) || tp.Model.Contains("kling-video/o1", StringComparison.CurrentCultureIgnoreCase);
                }

                if (propertyName == nameof(ImageSourceCont))
                {
                    return tp.Model.Contains("veo3.1/reference-to-video", StringComparison.CurrentCultureIgnoreCase);
                }

                if (propertyName == nameof(DurationLtx2))
                {
                    return tp.Model.Contains("ltxv-2", StringComparison.CurrentCultureIgnoreCase);
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
                    return (tp.Model.Contains("image-to-video") || tp.Model.Contains("speech-to-video")) && !tp.Model.Contains("kling-video/o1", StringComparison.InvariantCultureIgnoreCase);
                }

                if (propertyName == nameof(Duration))
                {
                    return !tp.Model.StartsWith("veo3") && !tp.Model.StartsWith("minimax") && !tp.Model.StartsWith("wan/") && !tp.Model.StartsWith("wan-alpha")
                        && !tp.Model.StartsWith("ltx") && !tp.Model.StartsWith("pixverse");
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

                if (tp.Model.StartsWith("kling-video/v2.6/") && propertyName == nameof(Seed))
                {
                    return false;
                }
            }

            return true;
        }
    }
}