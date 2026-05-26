using PluginBase;
using System.ComponentModel;

namespace FalAiPlugin
{
    internal class AudioItemPayload : IPayloadPropertyVisibility
    {
        public string PollingId { get; set; }
        public string Prompt { get; set; }

        [EnableFileDrop]
        [Description("Video file or public url used as the sync source for Mirelo SFX")]
        public string VideoSource { get; set; }

        [Description("Optional seed. Leave 0 for random.")]
        public int Seed { get; set; }

        [Description("Duration of the generated audio in seconds. Supported range is 1-10.")]
        [PropertyComboOptions(["1", "2", "3", "4", "5", "6", "7", "8", "9", "10"])]
        public float Duration { get; set; } = 10;

        [Description("Start position in the source video, in seconds.")]
        public float StartOffset { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is AudioTrackPayload tp && AudioTrackPayload.IsSfxModel(tp.Model))
            {
                return propertyName is nameof(PollingId) or nameof(Prompt) or nameof(VideoSource) or nameof(Seed) or nameof(Duration) or nameof(StartOffset);
            }

            if (propertyName is nameof(VideoSource) or nameof(Seed) or nameof(Duration) or nameof(StartOffset))
            {
                return false;
            }

            return true;
        }
    }
}
