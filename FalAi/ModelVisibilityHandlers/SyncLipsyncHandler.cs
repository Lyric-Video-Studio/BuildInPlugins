namespace FalAiPlugin.ModelVisibilityHandlers
{
    public class SyncLipsyncHandler : ModelVisibilityHandlerBase
    {
        public const string Model = "sync-lipsync/v3/image-to-video";

        public SyncLipsyncHandler()
        {
            ModelCategory = "Lip sync";
            ModelPath = Model;
        }

        public override bool ShouldTrackPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp)
            {
                return propertyName is nameof(tp.ImageSource);
            }

            return base.ShouldTrackPropertyBeVisible(propertyName, trackPayload, itemPayload);
        }

        public override bool ShouldItemPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (itemPayload is ItemPayload ip)
            {
                return propertyName is nameof(ip.ImageSource) or nameof(ip.AudioSource);
            }

            return base.ShouldItemPropertyBeVisible(propertyName, trackPayload, itemPayload);
        }

        public override void ConvertRequest(VideoRequest reg, object trackPayload, object itemPayload)
        {
            reg.prompt = null;
            reg.negative_prompt = null;
            reg.seed = null;
            reg.duration = null;
            reg.durationInt = null;
            reg.video_url = null;
            reg.image_urls = null;
            reg.reference_image_urls = null;
            reg.reference_image_url = null;
            reg.reference_video_url = null;
            reg.reference_video_urls = null;
            reg.audio_urls = null;
            reg.first_frame_url = null;
            reg.last_frame_url = null;
            reg.start_image_url = null;
            reg.end_image_url = null;
            reg.character_orientation = null;
            reg.enhance_prompt = null;
            reg.mode = null;
            reg.driving_type = null;
            reg.subject_type = null;
            reg.num_inference_steps = null;
            reg.guidance_scale = null;
            reg.shift = null;

            base.ConvertRequest(reg, trackPayload, itemPayload);
        }
    }
}
