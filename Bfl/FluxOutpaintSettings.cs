using PluginBase;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BflTxtToImgPlugin
{
    public class FluxOutpaintSettings
    {
        [Description("Target canvas width in pixels. Must be at least 64.")]
        [Range(64, int.MaxValue)]
        public int Width { get; set; } = 1920;

        [Description("Target canvas height in pixels. Must be at least 64.")]
        [Range(64, int.MaxValue)]
        public int Height { get; set; } = 1080;

        [Description("Optional left offset in pixels for the source image. Leave empty to center horizontally.")]
        public int? ReferenceOffsetX { get; set; }

        [Description("Optional top offset in pixels for the source image. Leave empty to center vertically.")]
        public int? ReferenceOffsetY { get; set; }

        [Description("Crop the source image to the canvas bounds if it extends outside the target area.")]
        public bool AutoCrop { get; set; }

        [PropertyComboOptions(["png", "jpeg"])]
        [CustomName("Output format")]
        public string OutputFormat { get; set; } = "png";
    }
}
