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
        public bool GenerateAudio { get; set; }

        [Description("Duration of the video in seconds")]
        public string Duration { get; set; } = "5";

        [Description("Duration of the video in seconds")]
        [CustomName("Duration")]
        public string DurationMinimax { get; set; } = "5";

        [EnableFileDrop]
        public string ImageSource { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp)
            {
                if (propertyName == nameof(ImageSource))
                {
                    return tp.Model.EndsWith("image-to-video");
                }

                if (propertyName == nameof(Duration))
                {
                    return !tp.Model.StartsWith("veo3");
                }

                if (propertyName == nameof(DurationMinimax))
                {
                    return tp.Model.StartsWith("minimax");
                }

                if (tp.Model.StartsWith("minimax") && propertyName == nameof(Duration))
                {
                    return false;
                }
            }

            return true;
        }
    }
}