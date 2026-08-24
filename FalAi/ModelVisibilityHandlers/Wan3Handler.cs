namespace FalAiPlugin.ModelVisibilityHandlers
{
    public abstract class Wan3HandlerBase : ModelVisibilityHandlerBase
    {
        protected Wan3HandlerBase(string model)
        {
            ModelCategory = "Wan";
            ModelPath = model;
        }

        public static bool IsWan3Model(string model)
        {
            return model is Wan3T2VHandler.Model or Wan3I2VHandler.Model or Wan3R2VHandler.Model;
        }

        protected bool IsCommonTrackProperty(string propertyName)
        {
            return propertyName is nameof(TrackPayload.Prompt)
                or nameof(TrackPayload.ResolutionWan3)
                or nameof(TrackPayload.AspectRatioWan3)
                or nameof(TrackPayload.Wan3Audio)
                or nameof(TrackPayload.Wan3EnablePromptExpansion)
                or nameof(TrackPayload.Wan3EnableThinking)
                or nameof(TrackPayload.Wan3EnableSafetyChecker);
        }

        protected bool IsCommonItemProperty(string propertyName)
        {
            return propertyName is nameof(ItemPayload.Prompt)
                or nameof(ItemPayload.Seed)
                or nameof(ItemPayload.DurationWan3);
        }

        public override void ConvertRequest(VideoRequest reg, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp && itemPayload is ItemPayload ip)
            {
                reg.resolution = tp.ResolutionWan3;
                reg.aspect_ratio = tp.AspectRatioWan3;
                reg.duration = null;
                reg.durationInt = ip.DurationWan3;
                reg.audio = tp.Wan3Audio;
                reg.enable_prompt_expansion = tp.Wan3EnablePromptExpansion;
                reg.enable_thinking = tp.Wan3EnableThinking;
                reg.enable_safety_checker = tp.Wan3EnableSafetyChecker;

                // Wan 3.0 does not use the similarly named legacy request fields.
                reg.generate_audio = null;
                reg.enhance_prompt = null;
                reg.negative_prompt = null;
                reg.num_frames = null;
                reg.frames_per_second = null;
            }

            base.ConvertRequest(reg, trackPayload, itemPayload);
        }
    }

    public sealed class Wan3T2VHandler : Wan3HandlerBase
    {
        public const string Model = "alibaba/wan-3.0/text-to-video";

        public Wan3T2VHandler() : base(Model)
        {
        }

        public override bool ShouldTrackPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            return IsCommonTrackProperty(propertyName);
        }

        public override bool ShouldItemPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            return IsCommonItemProperty(propertyName);
        }
    }

    public sealed class Wan3I2VHandler : Wan3HandlerBase
    {
        public const string Model = "alibaba/wan-3.0/image-to-video";

        public Wan3I2VHandler() : base(Model)
        {
        }

        public override bool ShouldTrackPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            return IsCommonTrackProperty(propertyName) || propertyName == nameof(TrackPayload.ImageSource);
        }

        public override bool ShouldItemPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            return IsCommonItemProperty(propertyName)
                || propertyName is nameof(ItemPayload.ImageSource) or nameof(ItemPayload.LastFrame);
        }

        public override void ConvertRequest(VideoRequest reg, object trackPayload, object itemPayload)
        {
            reg.start_image_url = reg.image_url;
            reg.end_image_url = reg.last_frame_url;
            reg.image_url = null;
            reg.last_frame_url = null;
            base.ConvertRequest(reg, trackPayload, itemPayload);
        }
    }

    public sealed class Wan3R2VHandler : Wan3HandlerBase
    {
        public const string Model = "alibaba/wan-3.0/reference-to-video";

        public Wan3R2VHandler() : base(Model)
        {
        }

        public override bool ShouldTrackPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            return IsCommonTrackProperty(propertyName) || IsImageReferences(propertyName);
        }

        public override bool ShouldItemPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            return IsCommonItemProperty(propertyName)
                || IsImageReferences(propertyName)
                || IsVideoReferences(propertyName)
                || propertyName == nameof(ItemPayload.AudioSourceCont)
                || IsAudioReferences(propertyName);
        }

        public override void ConvertRequest(VideoRequest reg, object trackPayload, object itemPayload)
        {
            reg.reference_image_urls = reg.image_urls?.ToArray();
            reg.image_urls = null;

            reg.reference_audio_urls = reg.audio_urls ?? [];
            if (!string.IsNullOrWhiteSpace(reg.audio_url))
            {
                reg.reference_audio_urls.Add(reg.audio_url);
            }

            if (reg.reference_audio_urls.Count == 0)
            {
                reg.reference_audio_urls = null;
            }

            reg.audio_url = null;
            base.ConvertRequest(reg, trackPayload, itemPayload);
        }
    }
}
