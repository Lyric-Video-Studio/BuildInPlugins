using PluginBase;
using System.ComponentModel;

namespace BflTxtToImgPlugin
{
    public class ItemPayload
    {
        private string prompt = "Progressive metal band from Finland playing in forest";
        private string imageSource;
        private string seed = "0";
        private string pollingId = "";

        public string Prompt { get => prompt; set => prompt = value; }

        [EnableFileDrop]
        public string ImageSource { get => imageSource; set => imageSource = value; }

        public string Seed { get => seed; set => seed = value; }

        [Description("Only modify if you know what you are doing. This is used to fetch results from sever. If you need to re-generate item, clear this and then generata, otherwise same result if fecthed")]
        public string PollingId { get => pollingId; set => pollingId = value; }
    }
}