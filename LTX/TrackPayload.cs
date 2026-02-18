using PluginBase;

namespace LTXPlugin
{
    public class TrackPayload : IPayloadPropertyVisibility
    {
        public const string FastModel = "ltx-2-fast";
        public const string ResHd = "1920x1080";

        [PropertyComboOptions([FastModel, "ltx-2-pro"])]
        public string Model { get; set; } = FastModel;
        public string Prompt { get; set; }

        [PropertyComboOptions(["25", "50"])]
        public int Fps { get; set; } = 25;

        [PropertyComboOptions([ResHd, "2560x1440", "3840x2160"])]
        public string Resolution { get; set; } = ResHd;

        [EnableFileDrop]
        public string ImageSource { get; set; }

        [EnableFileDrop]
        public string AudioSource { get; set; }        

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {    
            return true;
        }
    }
}