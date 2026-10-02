using PluginBase;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BflTxtToImgPlugin
{
    public class Flux3ImageSettings
    {
        [Description("Output aspect ratio. Auto follows the first reference image when image-to-image is used, otherwise 1:1.")]
        [PropertyComboOptions(["auto", "21:9", "2:1", "16:9", "3:2", "7:5", "4:3", "5:4", "1:1", "4:5", "3:4", "5:7", "2:3", "9:16", "1:2", "9:21"])]
        [CustomName("Aspect ratio")]
        public string AspectRatio { get; set; } = "auto";

        [Description("Output resolution.")]
        [PropertyComboOptions(["768sq", "1k", "1.5k", "2k", "4k"])]
        public string Resolution { get; set; } = "1k";

        [Description("Safety tolerance from 0 (strictest) to 4.")]
        [Range(0, 4)]
        [CustomName("Safety tolerance")]
        public int SafetyTolerance { get; set; } = 2;

        [Description("Allow FLUX.3 to use web and image search to ground the generation.")]
        public bool Grounding { get; set; } = true;
    }
}
