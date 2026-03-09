using PluginBase;

namespace LTXPlugin
{
    public class TrackPayload : IPayloadPropertyVisibility
    {
        public const string FastModel = "ltx-2-fast";
        public const string ResHd = "1920x1080";

        [PropertyComboOptions(["ltx-2-3-pro", "ltx-2-3-fast", FastModel, "ltx-2-pro"])]
        public string Model { get; set; } = "ltx-2-3-pro";
        public string Prompt { get; set; }

        [PropertyComboOptions(["25", "50"])]
        public int Fps { get; set; } = 25;

        [PropertyComboOptions([ResHd, "2560x1440", "3840x2160"])]
        public string Resolution { get; set; } = ResHd;

        [PropertyComboOptions([ResHd, "2560x1440", "3840x2160", "1080x1920", "2160x3840"])]
        [CustomName("Resolution")]
        public string Resolution23 { get; set; } = ResHd;

        [EnableFileDrop]
        public string ImageSource { get; set; }

        [EnableFileDrop]
        public string AudioSource { get; set; }        

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp3 && itemPayload is ItemPayload ip3)
            {

                if (!string.IsNullOrEmpty(tp3.AudioSource) || !string.IsNullOrEmpty(ip3.AudioSource))
                {
                    return propertyName != nameof(Model);
                }

                if (propertyName == nameof(Resolution))
                {
                    return !tp3.Model.Contains("2-3");
                }

                if (propertyName == nameof(Resolution23))
                {
                    return tp3.Model.Contains("2-3");
                }
            }
            return true;
        }
    }
}