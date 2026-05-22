using MuApiPlugin.Models.GeminiOmni;
using PluginBase;

namespace MuApiPlugin
{
    public class AudioItemPayload : IPayloadPropertyVisibility, IApiPollingPayload
    {
        public AudioItemPayload()
        {
        }

        public AudioItemPayload(string text)
        {
            GeminiOmniAudio.ExampleDialogue = Truncate(text, 120);
        }

        [HideAllChildren]
        [ParentName("")]
        public GeminiOmniAudioItemPayload GeminiOmniAudio { get; set; } = new();

        public string PollingId { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is not AudioTrackPayload typedPayload)
            {
                return true;
            }

            return propertyName switch
            {
                nameof(GeminiOmniAudio) => AudioTrackPayload.IsGeminiOmniAudio(typedPayload),
                _ when AudioTrackPayload.IsGeminiOmniAudio(typedPayload) => GeminiOmniAudio.ShouldPropertyBeVisible(propertyName, typedPayload.Model),
                _ => true
            };
        }

        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            {
                return text;
            }

            return text[..maxLength];
        }
    }
}
