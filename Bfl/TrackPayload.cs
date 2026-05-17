using Bfl;
using PluginBase;
using System.ComponentModel;

namespace BflTxtToImgPlugin
{
    public class TrackPayload : IPayloadPropertyVisibility
    {
        public const string ModeFlux2 = "FLUX.2";
        public const string ModeOutpaint = "Outpaint";

        private Flux2Inputs txt2ImgPayloadNew = new();
        private FluxOutpaintSettings outpaintSettings = new();

        [TriggerReload]
        [PropertyComboOptions([ModeFlux2, ModeOutpaint])]
        public string Mode { get; set; } = ModeFlux2;

        [Description("Image settings")]
        [IgnorePropertyName]
        [CustomName("Settings")]
        [HideAllChildren]
        public Flux2Inputs SettingsNew { get => txt2ImgPayloadNew; set => txt2ImgPayloadNew = value; }

        [Description("Outpaint settings")]
        [IgnorePropertyName]
        [CustomName("Settings")]
        [HideAllChildren]
        public FluxOutpaintSettings OutpaintSettings { get => outpaintSettings; set => outpaintSettings = value; }

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
                if (propertyName == nameof(SettingsNew))
                {
                    return tp.Mode == ModeFlux2;
                }

                if (propertyName == nameof(OutpaintSettings))
                {
                    return tp.Mode == ModeOutpaint;
                }

                if (tp.Mode == ModeOutpaint &&
                    propertyName is nameof(InputImage2) or nameof(InputImage3) or nameof(InputImage4) or nameof(InputImage5) or nameof(InputImage6) or nameof(InputImage7) or nameof(InputImage8))
                {
                    return false;
                }

                if (propertyName is nameof(Flux2Inputs.Webhook_secret) or nameof(Flux2Inputs.Webhook_url) or nameof(Flux2Inputs.AdditionalProperties))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
