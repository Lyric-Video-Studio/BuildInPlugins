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
            }

            return true;
        }
    }
}