namespace FalAiPlugin.ModelVisibilityHandlers
{
    public class TopazGenerativeUpscaleHandler : ModelVisibilityHandlerBase
    {
        public const string Model = "topaz/upscale/video/generative";

        public TopazGenerativeUpscaleHandler()
        {
            ModelCategory = "Upscale";
            ModelPath = Model;
        }

        public override bool ShouldTrackPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            return propertyName == nameof(TrackPayload.Model);
        }

        public override bool ShouldItemPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (propertyName == nameof(ItemPayload.TopazSoftness) && itemPayload is ItemPayload ip)
            {
                return ip.TopazGenerativeModel == "Starlight Precise 2.6";
            }

            return propertyName is nameof(ItemPayload.VideoSource)
                or nameof(ItemPayload.TopazGenerativeModel)
                or nameof(ItemPayload.TopazUpscaleFactor)
                or nameof(ItemPayload.TopazTargetFps)
                or nameof(ItemPayload.TopazH264Output);
        }

        public override void ConvertRequest(VideoRequest reg, object trackPayload, object itemPayload)
        {
            if (itemPayload is ItemPayload ip)
            {
                reg.prompt = null;
                reg.negative_prompt = null;
                reg.enable_safety_checker = null;
                reg.model = ip.TopazGenerativeModel;
                reg.upscale_factor = ip.TopazUpscaleFactor;
                reg.target_fps = ip.TopazTargetFps;
                reg.softness = ip.TopazGenerativeModel == "Starlight Precise 2.6" ? ip.TopazSoftness : null;
                reg.h264_output = ip.TopazH264Output;
            }

            base.ConvertRequest(reg, trackPayload, itemPayload);
        }
    }
}
