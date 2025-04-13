using Avalonia.Logging;
using PluginBase;
using System.ComponentModel;

namespace MinimaxPlugin
{
    public class ImageItemPayload
    {
        private string prompt = "";

        //public KeyFrames KeyFrames { get; set; } = new KeyFrames();

        public string Prompt { get => prompt; set => prompt = value; }

        [EnableFileDrop]
        public string CharacterRef { get; set; }

        [Description("0 = new random seed")]
        public long Seed { get; set; }
    }
}