using PluginBase;

namespace MusicGptPlugin
{
    internal class MusicGptItemPayload
    {
        private string generationId;
        private string pollingId;
        public string GenerationId { get => generationId; set => generationId = value; }
        public string PollingId { get => pollingId; set => pollingId = value; }

        private string prompt = "";
        public string Prompt { get => prompt; set => prompt = value; }

        private string lyrics = "";

        [EditorColumnSpan(2)]
        public string Lyrics { get => lyrics; set => lyrics = value; }


    }
}