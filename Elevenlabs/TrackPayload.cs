using PluginBase;
using System.ComponentModel;

namespace ElevenLabsPlugin
{
    public class ElevenLabsAudioTrackPayload : IPayloadPropertyVisibility
    {
        /*private string prompt = "";
        public string Prompt { get => prompt; set => prompt = value; }

        public string MusicStyle { get => musicStyle; set => musicStyle = value; }

        public bool Instrumental { get => instumental; set => instumental = value; }

        private bool voiceOnly;
        public bool VoiceOnly { get => voiceOnly; set => voiceOnly = value; }*/

        private string voiceId;

        /*private bool speechOnly;

        [Description("Generate speech/narration from the prompt, no music")]
        [IgnoreDynamicEdit]
        public bool SpeechOnly { get => speechOnly; set => speechOnly = value; }

        private string gender;

        [Description("Gender of the speeker. Used on speech / narration only")]
        public string Gender { get => gender; set => gender = value; }*/

        public string VoiceId { get => voiceId; set => voiceId = value; }

        [Description("NOTE: Only available for subscribed users.")]
        public bool Music { get; set; }

        [Description("Music lenght in seconds, between 10 and 180")]
        public int Length { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is ElevenLabsAudioTrackPayload tp)
            {
                if (propertyName == nameof(VoiceId))
                {
                    return !tp.Music;
                }

                if (propertyName == nameof(Length))
                {
                    return tp.Music;
                }
            }

            return true;
        }
    }
}