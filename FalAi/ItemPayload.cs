using PluginBase;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

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

        [EnableFileDrop]
        public string ImageSource { get; set; }

        [EnableFileDrop]
        public string AudioSource { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp)
            {
                if (propertyName == nameof(DurationPixverse))
                {
                    tp.Model.StartsWith("pixverse");
                }

                if (propertyName == nameof(ImageSource))
                {
                    return tp.Model.EndsWith("image-to-video") || tp.Model.EndsWith("speech-to-video");
                }

                if (propertyName == nameof(Duration))
                {
                    return !tp.Model.StartsWith("veo3") && !tp.Model.StartsWith("minimax") && !tp.Model.StartsWith("wan") && !tp.Model.StartsWith("ltx") && !tp.Model.StartsWith("pixverse");
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