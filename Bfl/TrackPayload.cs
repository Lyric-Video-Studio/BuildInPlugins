using Bfl;
using PluginBase;
using System.ComponentModel;

namespace BflTxtToImgPlugin
{
    public class TrackPayload : IPayloadPropertyVisibility
    {
        private Flux2Inputs txt2ImgPayloadNew = new();

        [Description("Image settings")]
        [IgnorePropertyName]
        [CustomName("Settings")]
        public Flux2Inputs SettingsNew { get => txt2ImgPayloadNew; set => txt2ImgPayloadNew = value; }

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
            if (trackPayload is TrackPayload tp)
            {
                if (propertyName is nameof(Flux2Inputs.Webhook_secret) or nameof(Flux2Inputs.Webhook_url) or nameof(Flux2Inputs.AdditionalProperties))
                {
                    return false;
                }
            }

            return true;
        }
    }
}