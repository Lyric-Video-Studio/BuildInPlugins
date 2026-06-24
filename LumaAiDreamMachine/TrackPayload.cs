using PluginBase;
using System.ComponentModel;

namespace LumaAiDreamMachinePlugin
{
    public class TrackPayload : IPayloadPropertyVisibility
    {
        public const string SourceVideoModeEdit = "edit";
        public const string SourceVideoModeReframe = "reframe";

        [IgnoreDynamicEdit]
        public bool IsVideo { get; set; } = true;

        private Request imgToVidPayload = new Request();

        [Description("Video settings")]
        [IgnorePropertyName]
        public Request Settings { get => imgToVidPayload; set => imgToVidPayload = value; }

        [TriggerReload]
        [PropertyComboOptions([SourceVideoModeEdit, SourceVideoModeReframe])]
        [CustomName("Source video mode")]
        public string SourceVideoMode { get; set; } = SourceVideoModeEdit;

        public string VideoEditMode { get; set; } = "flex_1";

        [Description("Optional guide frame for video edit. You can copy frame path from video item with right click context menu")]
        [EnableFileDrop]
        public string FirstFrame { get; set; }

        [Description("HDR output for ray-3.2 generation and video edit")]
        public bool Hdr { get; set; }

        [Description("EXR export for ray-3.2 generation and video edit. Requires HDR")]
        public bool ExrExport { get; set; }

        [Description("Set the source video placement manually for ray-3.2 reframing")]
        public bool UseCustomSourcePosition { get; set; }

        public double SourcePositionXNorm { get; set; } = 0.0;
        public double SourcePositionYNorm { get; set; } = 0.0;
        public double SourcePositionWNorm { get; set; } = 1.0;
        public double SourcePositionHNorm { get; set; } = 1.0;

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (itemPayload is ItemPayload ip)
            {
                var hasSourceVideo = !string.IsNullOrWhiteSpace(ip.VideoFile) || !string.IsNullOrWhiteSpace(ip.SourceGenerationId);
                var isRayVideo = Settings?.model == "ray-3.2";
                var isReframe = isRayVideo && hasSourceVideo && SourceVideoMode == SourceVideoModeReframe;

                if (propertyName == nameof(Request.loop) || propertyName == nameof(Request.duration))
                {
                    return !hasSourceVideo;
                }

                if (propertyName == nameof(Request.aspect_ratio))
                {
                    return !hasSourceVideo || isReframe;
                }

                if (propertyName == nameof(Request.resolution))
                {
                    return !hasSourceVideo || isRayVideo;
                }

                if (propertyName == nameof(SourceVideoMode))
                {
                    return isRayVideo && hasSourceVideo;
                }

                if (propertyName == nameof(VideoEditMode) || propertyName == nameof(FirstFrame))
                {
                    return hasSourceVideo && !isReframe;
                }

                if (propertyName == nameof(Hdr) || propertyName == nameof(ExrExport))
                {
                    return isRayVideo && (!hasSourceVideo || !isReframe);
                }

                if (propertyName == nameof(UseCustomSourcePosition))
                {
                    return isReframe;
                }

                if (propertyName is nameof(SourcePositionXNorm) or nameof(SourcePositionYNorm) or nameof(SourcePositionWNorm) or nameof(SourcePositionHNorm))
                {
                    return isReframe && UseCustomSourcePosition;
                }
            }
            return true;
        }
    }
}
