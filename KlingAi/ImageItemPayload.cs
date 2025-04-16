using PluginBase;

namespace KlingAiPlugin
{
    public class ImageItemPayload
    {
        public string Prompt { get; set; }
        public string NegativePrompt { get; set; }

        [EnableFileDrop]
        public string CharacterRef { get; set; }

        public string? PollingId { get; internal set; }
    }
}