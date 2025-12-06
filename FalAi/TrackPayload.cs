using PluginBase;
using System.ComponentModel;

namespace FalAiPlugin
{
    public class TrackPayload : IPayloadPropertyVisibility
    {
        public static event EventHandler ModelChanged;

        private string model = "veo3/fast";

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
        public string AspectRatio { get; set; } = "16:9";

        [CustomName("Resolution")]
        public string ResolutionMinimax { get; set; } = "768P";

        [CustomName("Resolution")]
        public string ResolutionWan { get; set; } = "720p";

        [CustomName("Resolution")]
        public string ResolutionLtx { get; set; } = "720p";

        [CustomName("Resolution")]
        public string ResolutionLtx2 { get; set; } = "1080p";

        public string Resolution { get; set; } = "1080p";

        [Description("Number of frames to generate. Must be between 80 to 120")]
        public int NumberOfFrames { get; set; } = 80;

        [Description("Frames per second of the generated video. Must be between 4 to 60")]
        public int FramesPerSecond { get; set; } = 16;

        [CustomName("FPS")]
        public int FramesPerSecondLtx2 { get; set; } = 25;

        public bool GenerateAudio { get; set; }

        public string Style { get; set; }
        public string CameraMovement { get; set; }

        [EnableFileDrop]
        public string ImageSource { get; set; }

        [CustomName("Aspect ratio")]
        public string AspectRatioSora { get; internal set; } = "16:9";

        public ImageSourceContainer ImageSourceCont { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp)
            {
                if (propertyName == nameof(ResolutionLtx2) || propertyName == nameof(FramesPerSecondLtx2))
                {
                    return tp.Model.Contains("ltxv-2");
                }

                if (Model.StartsWith("ltxv-2"))
                {
                    if (propertyName == nameof(Seed) || propertyName == nameof(NegativePrompt) || propertyName == nameof(AspectRatio) || propertyName == nameof(Resolution) || propertyName == nameof(NumberOfFrames))
                    {
                        return false;
                    }
                }

                if (propertyName == nameof(ImageSourceCont))
                {
                    return tp.Model.Contains("veo3.1/reference-to-video", StringComparison.CurrentCultureIgnoreCase);
                }
                if (propertyName == nameof(AspectRatioSora))
                {
                    return tp.Model.Contains("sora", StringComparison.CurrentCultureIgnoreCase);
                }

                if (propertyName == nameof(AspectRatio) && tp.Model.Contains("sora", StringComparison.CurrentCultureIgnoreCase))
                {
                    return false;
                }

                if (tp.Model.Contains("sora", StringComparison.CurrentCultureIgnoreCase) && (propertyName == nameof(NegativePrompt)))
                {
                    return false;
                }

                if (tp.Model.Contains("upscale"))
                {
                    // In upscale, there's really not a lot of things to edit
                    return propertyName == nameof(Model);
                }

                if (tp.Model.Contains("omnihuman"))
                {
                    // THis also has very few inputs
                    return propertyName == nameof(Model) || propertyName == nameof(ImageSource);
                }

                if (propertyName == nameof(ImageSource))
                {
                    return false;
                }

                if (propertyName == nameof(GenerateAudio))
                {
                    return Model.StartsWith("veo") || Model.StartsWith("ltxv-2") || Model.Contains("kling-video/v2.6/pro");
                }

                if (propertyName == nameof(Style) || propertyName == nameof(CameraMovement))
                {
                    return Model.StartsWith("pixverse");
                }

                if (tp.Model.StartsWith("wan") && !tp.Model.StartsWith("wan-25"))
                {
                    switch (propertyName)
                    {
                        case nameof(AspectRatio):
                        case nameof(Resolution):
                            return false;

                        default:
                            break;
                    }
                }
                else
                {
                    switch (propertyName)
                    {
                        case nameof(ResolutionWan):
                            return false;

                        case nameof(NumberOfFrames):
                        case nameof(FramesPerSecond):
                            return tp.Model.StartsWith("ltxv") && !tp.Model.StartsWith("ltxv-2");
                    }
                }

                if (tp.Model.StartsWith("kling"))
                {
                    switch (propertyName)
                    {
                        case nameof(Resolution):
                        case nameof(ResolutionMinimax):
                        case nameof(ResolutionWan):
                            return false;

                        case nameof(Seed):
                            return false;

                        case nameof(AspectRatio):
                            return tp.Model.Contains("text-to-video");

                        default:
                            break;
                    }
                }

                if (propertyName == nameof(ResolutionLtx))
                {
                    return (tp.Model.StartsWith("ltx") && !tp.Model.StartsWith("ltxv-2")) || tp.Model.StartsWith("lucy-edit");
                }

                if (tp.Model.StartsWith("ltx") || tp.Model.StartsWith("lucy-edit"))
                {
                    switch (propertyName)
                    {
                        case nameof(ResolutionMinimax):
                        case nameof(Resolution):
                            return false;
                    }
                }

                if (tp.Model.StartsWith("lucy-edit"))
                {
                    switch (propertyName)
                    {
                        case nameof(AspectRatio):
                        case nameof(NegativePrompt):
                        case nameof(Seed):
                            return false;

                        default:
                            break;
                    }
                }

                if (tp.Model.StartsWith("minimax"))
                {
                    switch (propertyName)
                    {
                        case nameof(AspectRatio):
                        case nameof(Resolution):
                            return false;

                        case nameof(ResolutionMinimax):
                            return tp.Model.Contains("standard") && !Model.Contains("text-to-video");

                        default:
                            break;
                    }

                    if (Model.Contains("text-to-video"))
                    {
                        switch (propertyName)
                        {
                            case nameof(Seed):
                            case nameof(NegativePrompt):
                                return false;

                            default:
                                break;
                        }
                    }
                }
                else
                {
                    switch (propertyName)
                    {
                        case nameof(ResolutionMinimax):
                            return tp.Model.StartsWith("ltxv");
                    }
                }
            }

            return true;
        }
    }
}