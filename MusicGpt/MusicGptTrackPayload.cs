namespace MusicGptPlugin
{
    internal class MusicGptAudioTrackPayload
    {
        private string prompt = "";
        public string Prompt { get => prompt; set => prompt = value; }

        private string musicStyle = "";
        public string MusicStyle { get => musicStyle; set => musicStyle = value; }

        private bool instumental;
        public bool Instumental { get => instumental; set => instumental = value; }

        private bool voiceOnly;
        public bool VoiceOnly { get => voiceOnly; set => voiceOnly = value; }

        private string voiceId;
        public string VoiceId { get => voiceId; set => voiceId = value; }
    }
}