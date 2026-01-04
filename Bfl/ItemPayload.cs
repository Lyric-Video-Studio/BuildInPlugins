using Bfl;
using PluginBase;
using System.ComponentModel;

namespace BflTxtToImgPlugin
{
    public class ItemPayload : IPayloadPropertyVisibility
    {
        private string prompt = "Progressive metal band from Finland playing in forest";
        private int seed = 0;
        private string pollingUrl = "";

        public string Prompt { get => prompt; set => prompt = value; }

        public int Seed { get => seed; set => seed = value; }

        [Description("Only modify if you know what you are doing. This is used to fetch results from sever. If you need to re-generate item, clear this and then generate, otherwise same result if fecthed")]
        public string PollingUrl { get => pollingUrl; set => pollingUrl = value; }

        [EnableFileDrop]
        public string InputImage { get; set; }

        [EnableFileDrop]
        public string InputImage2 { get; set; }

        [EnableFileDrop]
        public string InputImage3 { get; set; }

        [EnableFileDrop]
        public string InputImage4 { get; set; }

        [EnableFileDrop]
        public string InputImage5 { get; set; }

        [EnableFileDrop]
        public string InputImage6 { get; set; }

        [EnableFileDrop]
        public string InputImage7 { get; set; }

        [EnableFileDrop]
        public string InputImage8 { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {            
            return true;
        }
    }
}