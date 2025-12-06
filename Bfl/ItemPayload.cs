using Bfl;
using PluginBase;
using System.ComponentModel;

namespace BflTxtToImgPlugin
{
    public class ItemPayload : IPayloadPropertyVisibility
    {
        private string prompt = "Progressive metal band from Finland playing in forest";
        private string imageSource;
        private int seed = 0;
        private string pollingId = "";
        private string pollingUrl = "";

        public string Prompt { get => prompt; set => prompt = value; }

        [EnableFileDrop]
        public string ImageSource { get => imageSource; set => imageSource = value; }

        public int Seed { get => seed; set => seed = value; }

        [Description("Only modify if you know what you are doing. This is used to fetch results from sever. If you need to re-generate item, clear this and then generate, otherwise same result if fecthed")]
        public string PollingUrl { get => pollingUrl; set => pollingUrl = value; }

        [Description("Check this is you want to use the Flux Kontext pro image editing")]
        public bool EditImage { get; set; }

        [EnableFileDrop]
        public string InputImage { get; set; }

        public string InputImage2 { get; set; }
        public string InputImage3 { get; set; }
        public string InputImage4 { get; set; }
        public string InputImage5 { get; set; }
        public string InputImage6 { get; set; }
        public string InputImage7 { get; set; }
        public string InputImage8 { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp)
            {
                if (propertyName == nameof(ItemPayload.EditImage))
                {
                    return tp.OldModels;
                }

                if (propertyName == nameof(ItemPayload.ImageSource))
                {
                    return tp.OldModels;
                }

                if (propertyName != null && propertyName.StartsWith(nameof(ItemPayload.InputImage)))
                {
                    return !tp.OldModels;
                }
            }

            return true;
        }
    }
}