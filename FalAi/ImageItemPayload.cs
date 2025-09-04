using PluginBase;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace FalAiPlugin
{
    public class ImageItemPayload
    {
        public string PollingId { get; set; }

        public string Prompt { get; set; }
        public string NegativePrompt { get; set; }
        public int Seed { get; set; }

        public ImageItemPayload()
        {
        }
    }
}