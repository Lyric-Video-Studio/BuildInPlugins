using PluginBase;
using System.ComponentModel;

namespace FalAiPlugin
{
    public class TrackPayload : IPayloadPropertyVisibility
    {
        public string Model { get; set; } = "veo3/fast";
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

        public string Resolution { get; set; } = "1080p";

        [Description("Number of frames to generate. Must be between 80 to 120")]
        public int NumberOfFrames { get; set; } = 80;

        [Description("Frames per second of the generated video. Must be between 4 to 60")]
        public int FramesPerSecond { get; set; } = 16;

        public bool GenerateAudio { get; set; }

        public string Style { get; set; }
        public string CameraMovement { get; set; }

        [EnableFileDrop]
        public string ImageSource { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp)
            {
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
                    return Model.StartsWith("veo");
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
                            return tp.Model.StartsWith("ltxv");
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

                        case nameof(AspectRatio):
                            return tp.Model.Contains("text-to-video");

                        default:
                            break;
                    }
                }

                if (propertyName == nameof(ResolutionLtx))
                {
                    return tp.Model.StartsWith("ltx") || tp.Model.StartsWith("lucy-edit");
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