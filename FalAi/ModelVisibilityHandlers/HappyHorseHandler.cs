namespace FalAiPlugin.ModelVisibilityHandlers
{
    public class HappyHorseT2VHandler : ModelVisibilityHandlerBase
    {
        public const string Model = "alibaba/happy-horse/text-to-video";

        public HappyHorseT2VHandler()
        {
            ModelCategory = "Alibaba";
            ModelPath = Model;
        }

        public override bool ShouldTrackPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp)
            {
                return propertyName is nameof(tp.Prompt) or nameof(tp.Resolution) or nameof(tp.AspectRatioWan26);
            }

            return base.ShouldTrackPropertyBeVisible(propertyName, trackPayload, itemPayload);
        }

        public override bool ShouldItemPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (itemPayload is ItemPayload ip)
            {
                return propertyName is nameof(ip.Prompt) or nameof(ip.Seed) or nameof(ip.DurationKlingO3);
            }

            return base.ShouldItemPropertyBeVisible(propertyName, trackPayload, itemPayload);
        }
    }

    public class HappyHorseI2VHandler : ModelVisibilityHandlerBase
    {
        public const string Model = "alibaba/happy-horse/image-to-video";

        public HappyHorseI2VHandler()
        {
            ModelCategory = "Alibaba";
            ModelPath = Model;
        }

        public override bool ShouldTrackPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp)
            {
                return propertyName is nameof(tp.Prompt) or nameof(tp.Resolution) or nameof(tp.ImageSource);
            }

            return base.ShouldTrackPropertyBeVisible(propertyName, trackPayload, itemPayload);
        }

        public override bool ShouldItemPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (itemPayload is ItemPayload ip)
            {
                return propertyName is nameof(ip.Prompt) or nameof(ip.Seed) or nameof(ip.DurationKlingO3) or nameof(ip.ImageSource);
            }

            return base.ShouldItemPropertyBeVisible(propertyName, trackPayload, itemPayload);
        }
    }

    public class HappyHorseR2VHandler : ModelVisibilityHandlerBase
    {
        public const string Model = "alibaba/happy-horse/reference-to-video";

        public HappyHorseR2VHandler()
        {
            ModelCategory = "Alibaba";
            ModelPath = Model;
        }

        public override bool ShouldTrackPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp)
            {
                return (propertyName is nameof(tp.Prompt) or nameof(tp.Resolution) or nameof(tp.AspectRatioWan26)) || IsImageReferences(propertyName);
            }

            return base.ShouldTrackPropertyBeVisible(propertyName, trackPayload, itemPayload);
        }

        public override bool ShouldItemPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (itemPayload is ItemPayload ip)
            {
                return (propertyName is nameof(ip.Prompt) or nameof(ip.Seed) or nameof(ip.DurationKlingO3)) || IsImageReferences(propertyName);
            }

            return base.ShouldItemPropertyBeVisible(propertyName, trackPayload, itemPayload);
        }
    }

    public class HappyHorseVideoEditHandler : ModelVisibilityHandlerBase
    {
        public const string Model = "alibaba/happy-horse/video-edit";

        public HappyHorseVideoEditHandler()
        {
            ModelCategory = "Alibaba";
            ModelPath = Model;
        }

        public override bool ShouldTrackPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp)
            {
                return (propertyName is nameof(tp.Prompt) or nameof(tp.Resolution) or nameof(tp.AudioSettingHappyHorseEdit)) || IsImageReferences(propertyName);
            }

            return base.ShouldTrackPropertyBeVisible(propertyName, trackPayload, itemPayload);
        }

        public override bool ShouldItemPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (itemPayload is ItemPayload ip)
            {
                return (propertyName is nameof(ip.Prompt) or nameof(ip.Seed) or nameof(ip.VideoSource)) || IsImageReferences(propertyName);
            }

            return base.ShouldItemPropertyBeVisible(propertyName, trackPayload, itemPayload);
        }

        public override void ConvertRequest(VideoRequest reg)
        {
            reg.reference_image_urls = reg.image_urls?.ToArray();
            reg.image_urls = null;
            reg.image_url = null;
            base.ConvertRequest(reg);
        }
    }
}
