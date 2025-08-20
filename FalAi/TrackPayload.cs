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
        public string AspectRatio { get; set; } = "1080p";
        public string Resolution { get; set; } = "16:9";

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            /*if (trackPayload is TrackPayload tp)
            {
                if (propertyName == nameof(ReferenceImage) || propertyName == nameof(ReferenceVideo))
                {
                    return tp.Request.model == "act_two";
                }

                if (propertyName == nameof(Request.duration) || propertyName == nameof(Request.promptText))
                {
                    return tp.Request.model != "act_two" && tp.Request.model != "upscale_v1";
                }
            }*/

            return true;
        }
    }
}