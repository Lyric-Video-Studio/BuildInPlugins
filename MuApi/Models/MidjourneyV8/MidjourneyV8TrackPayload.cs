using PluginBase;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MuApiPlugin.Models.MidjourneyV8
{
    public class MidjourneyV8TrackPayload
    {
        public const string ModelTxtToImg = "midjourney-v8";

        [Description("Track-level prompt prefix for Midjourney V8.")]
        [EditorWidth(360)]
        public string Prompt { get; set; }

        public ImageReferenceContainer ImageReferences { get; set; } = new();

        [PropertyComboOptions(["1:1", "16:9", "9:16", "4:3", "3:4", "2:3", "3:2"])]
        public string AspectRatio { get; set; } = "1:1";

        [Range(0, 1000)]
        [ShowSlider(0)]
        public int Stylize { get; set; } = 100;

        [Range(0, 100)]
        [ShowSlider(0)]
        public int Chaos { get; set; } = 0;

        [Range(0, 3000)]
        [ShowSlider(0)]
        public int Weird { get; set; } = 0;

        [Description("Same seed and same prompt should give similar results. Leave zero for random behavior.")]
        public long Seed { get; set; } = 0;

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {
            return true;
        }
    }
}
