using PluginBase;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace RunwayMlPlugin
{
    public class ItemPayload : IPayloadPropertyVisibility
    {
        private string pollingId;

        [Description("PollingId (generation id) is filled when you make new request. Unlike in other plugins, this is not cleared when generation is completed, because ths id is needed if you want to extend the video. " +
            "If you need to create new variation, clear this id, otherwise the same result will be fetched from the server")]
        public string PollingId { get => pollingId; set => pollingId = value; }

        public string Prompt { get; set; }

        public int Seed { get; set; } = 0;

        [EnableFileDrop]
        public string ImageSource { get => imageSource; set => imageSource = value; }

        private string imageSource;

        [EnableFileDrop]
        [Description("Used for upscaling and as facial expression / movement reference with Act2")]
        public string VideoSource { get; set; }

        [Description("Used with Act2. When enabled, non-facial movements and gestures will be applied to the character in addition to facial expressions")]
        public bool BodyControl { get; set; }

        [Range(1, 5)]
        [Description("Used with Act2. A larger value increases the intensity of the character's expression.")]
        public int ExpressionIntensity { get; set; } = 3;

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp)
            {
                if (propertyName == nameof(VideoSource))
                {
                    return tp.Request.model == "act_two" || tp.Request.model == "upscale_v1";
                }

                if (propertyName == nameof(BodyControl) || propertyName == nameof(ExpressionIntensity))
                {
                    return tp.Request.model == "act_two";
                }

                if (propertyName == nameof(ImageSource) || propertyName == nameof(Prompt))
                {
                    return tp.Request.model != "act_two" && tp.Request.model != "upscale_v1";
                }
            }

            return true;
        }
    }
}