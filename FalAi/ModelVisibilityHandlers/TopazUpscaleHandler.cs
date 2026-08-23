namespace FalAiPlugin.ModelVisibilityHandlers
{
    public class TopazUpscaleHandler : ModelVisibilityHandlerBase
    {
        public const string Model = "topaz/upscale/video/precision";

        public TopazUpscaleHandler()
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
            return propertyName is nameof(ItemPayload.VideoSource)
                or nameof(ItemPayload.TopazModel)
                or nameof(ItemPayload.TopazUpscaleFactor)
                or nameof(ItemPayload.TopazTargetFps)
                or nameof(ItemPayload.TopazCompression)
                or nameof(ItemPayload.TopazNoise)
                or nameof(ItemPayload.TopazHalo)
                or nameof(ItemPayload.TopazGrain)
                or nameof(ItemPayload.TopazRecoverDetail)
                or nameof(ItemPayload.TopazH264Output);
        }

        public override void ConvertRequest(VideoRequest reg, object trackPayload, object itemPayload)
        {
            if (itemPayload is ItemPayload ip)
            {
                reg.prompt = null;
                reg.negative_prompt = null;
                reg.enable_safety_checker = null;
                reg.model = ip.TopazModel;
                reg.upscale_factor = ip.TopazUpscaleFactor;
                reg.target_fps = ip.TopazTargetFps;
                reg.compression = ip.TopazCompression;
                reg.noise = ip.TopazNoise;
                reg.halo = ip.TopazHalo;
                reg.grain = ip.TopazGrain;
                reg.recover_detail = ip.TopazRecoverDetail;
                reg.h264_output = ip.TopazH264Output;
            }

            base.ConvertRequest(reg, trackPayload, itemPayload);
        }
    }
}
