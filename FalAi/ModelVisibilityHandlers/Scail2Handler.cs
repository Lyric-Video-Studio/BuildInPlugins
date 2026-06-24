namespace FalAiPlugin.ModelVisibilityHandlers
{
    public class Scail2Handler : ModelVisibilityHandlerBase
    {
        public const string Model = "scail-2";

        public Scail2Handler()
        {
            ModelCategory = "Character animation";
            ModelPath = Model;
        }

        public override bool ShouldTrackPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp)
            {
                return propertyName is nameof(tp.Prompt) or nameof(tp.ImageSource) or nameof(tp.ResolutionScail2);
            }

            return base.ShouldTrackPropertyBeVisible(propertyName, trackPayload, itemPayload);
        }

        public override bool ShouldItemPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (itemPayload is ItemPayload ip)
            {
                if (propertyName == nameof(ip.Scail2DrivingType))
                {
                    return ip.Scail2Mode == "animation";
                }

                return propertyName is nameof(ip.Prompt)
                    or nameof(ip.Seed)
                    or nameof(ip.ImageSource)
                    or nameof(ip.VideoSource)
                    or nameof(ip.Scail2Mode)
                    or nameof(ip.Scail2DrivingType)
                    or nameof(ip.Scail2SubjectType)
                    or nameof(ip.Scail2NumInferenceSteps)
                    or nameof(ip.Scail2GuidanceScale)
                    or nameof(ip.Scail2Shift);
            }

            return base.ShouldItemPropertyBeVisible(propertyName, trackPayload, itemPayload);
        }

        public override void ConvertRequest(VideoRequest reg, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp && itemPayload is ItemPayload ip)
            {
                reg.resolution = tp.ResolutionScail2;
                reg.mode = ip.Scail2Mode;
                reg.subject_type = ip.Scail2SubjectType;
                reg.num_inference_steps = ip.Scail2NumInferenceSteps;
                reg.guidance_scale = ip.Scail2GuidanceScale;
                reg.shift = ip.Scail2Shift;
                reg.driving_type = ip.Scail2Mode == "animation" ? ip.Scail2DrivingType : null;
            }

            base.ConvertRequest(reg, trackPayload, itemPayload);
        }
    }
}
