using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin.Models.Seedance2
{
    public class Seedance2TrackPayload
    {
        public const string ModelT2V = "seedance-v2.0-t2v";
        public const string ModelI2V = "seedance-v2.0-i2v";
        public const string ModelOmniRef = "seedance-2.0-omni-reference";
        [Description("Track-level prompt prefix. Reference uploaded media in your prompt with @image1, @image2, @audio1 etc")]
        [EditorWidth(360)]
        public string Prompt { get; set; }

        [PropertyComboOptions(["16:9", "9:16", "4:3", "3:4"])]
        public string AspectRatio { get; set; } = "16:9";

        [PropertyComboOptions(["basic", "high", "4:3", "3:4"])]
        public string Quality { get; set; } = "high";

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {
            if (model == ModelOmniRef)
            {
                // todo add video image and audio refs
            }
            return true;
        }
    }
}
