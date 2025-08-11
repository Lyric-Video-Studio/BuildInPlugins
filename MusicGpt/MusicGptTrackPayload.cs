using PluginBase;
using System.ComponentModel;

namespace MusicGptPlugin
{
    public class MusicGptAudioTrackPayload
    {
        public string Prompt { get; set; }

        public string MusicStyle { get; set; }

        public bool Instrumental { get; set; }

        private bool voiceOnly;
        public bool VoiceOnly { get => voiceOnly; set => voiceOnly = value; }

        private string voiceId;

        private bool speechOnly;

        [Description("Generate speech/narration from the prompt, no music")]
        [IgnoreDynamicEdit]
        public bool SpeechOnly { get => speechOnly; set => speechOnly = value; }

        private string gender;

        [Description("Gender of the speeker. Used on speech / narration only")]
        public string Gender { get => gender; set => gender = value; }

        [Description("For speech/narration voice, does not have effect in music generation")]
        [IgnoreDynamicEdit]
        public string VoiceId { get => voiceId; set => voiceId = value; }
    }
}