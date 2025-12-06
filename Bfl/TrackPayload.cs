using Bfl;
using PluginBase;
using System.ComponentModel;

namespace BflTxtToImgPlugin
{
    public class TrackPayload : IPayloadPropertyVisibility
    {
        private FluxPro11Inputs txt2ImgPayload = new();
        private Flux2Inputs txt2ImgPayloadNew = new();

        [Description("Image settings")]
        [IgnorePropertyName]
        public FluxPro11Inputs Settings { get => txt2ImgPayload; set => txt2ImgPayload = value; }

        [Description("Image settings")]
        [IgnorePropertyName]
        [CustomName("Settings")]
        public Flux2Inputs SettingsNew { get => txt2ImgPayloadNew; set => txt2ImgPayloadNew = value; }

        [Description("Use old Flux 1 models")]
        public bool OldModels { get; set; }

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
                if (propertyName == nameof(TrackPayload.Settings))
                {
                    return tp.OldModels;
                }

                if (propertyName != null && propertyName.StartsWith(nameof(TrackPayload.InputImage)))
                {
                    return !tp.OldModels;
                }
            }

            return true;
        }
    }
}