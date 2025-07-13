using PluginBase;

namespace MusicGptPlugin
{
    public class MusicGptItemPayload
    {
        private string pollingId;
        public string PollingId { get => pollingId; set => pollingId = value; }

        private string prompt = "";
        public string Prompt { get => prompt; set => prompt = value; }

        private string lyrics = "";

        [EditorColumnSpan(2)]
        public string Lyrics { get => lyrics; set => lyrics = value; }

    }
}