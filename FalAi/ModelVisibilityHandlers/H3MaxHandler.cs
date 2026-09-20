using PluginBase;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace FalAiPlugin
{
    public class H3MaxCameraKeyframe
    {
        [Range(0, 1)]
        [ShowSlider(2)]
        [Description("Normalized video time: 0 is the start and 1 is the end")]
        public float Time { get; set; }

        [Description("Horizontal camera angle around the subject, in degrees")]
        public float Azimuth { get; set; }

        [Description("Vertical camera angle around the subject, in degrees")]
        public float Elevation { get; set; }

        [Description("Camera distance from the subject in normalized scene units")]
        public float Distance { get; set; } = 1;
    }
}

namespace FalAiPlugin.ModelVisibilityHandlers
{
    internal static class H3MaxRegistration
    {
        [ModuleInitializer]
        internal static void Register()
        {
            ModelVisibilityHandlerBase[] handlers =
            [
                new H3MaxT2VHandler(),
                new H3MaxI2VHandler(),
                new H3MaxReferenceToVideoHandler(),
                new H3MaxCameraControlsHandler(),
                new H3MaxLipSyncHandler(),
                new H3MaxTurboT2VHandler(),
                new H3MaxTurboI2VHandler()
            ];

            foreach (var handler in handlers)
            {
                if (!FalAiImgToVidPlugin.VisibilityHandlers.Any(existing => existing.ModelPath == handler.ModelPath))
                {
                    FalAiImgToVidPlugin.VisibilityHandlers.Add(handler);
                }
            }
        }
    }

    public abstract class H3MaxHandlerBase : ModelVisibilityHandlerBase
    {
        protected H3MaxHandlerBase(string model)
        {
            ModelCategory = "Minimax";
            ModelPath = model;
        }

        public static bool IsH3MaxModel(string model)
        {
            return model is H3MaxT2VHandler.Model
                or H3MaxI2VHandler.Model
                or H3MaxReferenceToVideoHandler.Model
                or H3MaxCameraControlsHandler.Model
                or H3MaxLipSyncHandler.Model
                or H3MaxTurboT2VHandler.Model
                or H3MaxTurboI2VHandler.Model;
        }

        protected static bool IsCommonTrackProperty(string propertyName)
        {
            return propertyName is nameof(TrackPayload.Prompt)
                or nameof(TrackPayload.Seed)
                or nameof(TrackPayload.H3MaxResolution)
                or nameof(TrackPayload.H3MaxPromptExpansionMode);
        }

        protected static bool IsCommonItemProperty(string propertyName)
        {
            return propertyName is nameof(ItemPayload.Prompt)
                or nameof(ItemPayload.Seed)
                or nameof(ItemPayload.DurationH3Max);
        }

        protected static void ApplyCommonRequest(VideoRequest reg, TrackPayload trackPayload, ItemPayload itemPayload)
        {
            reg.negative_prompt = null;
            reg.resolution = trackPayload.H3MaxResolution;
            reg.duration = null;
            reg.durationInt = itemPayload.DurationH3Max;
            reg.prompt_expansion_mode = trackPayload.H3MaxPromptExpansionMode;
            reg.enable_safety_checker = false;
            reg.sync_mode = null;

            reg.model = null;
            reg.upscale_factor = null;
            reg.target_fps = null;
            reg.compression = null;
            reg.noise = null;
            reg.halo = null;
            reg.grain = null;
            reg.recover_detail = null;
            reg.softness = null;
            reg.h264_output = null;
            reg.generate_audio = null;
            reg.audio = null;
            reg.enable_prompt_expansion = null;
            reg.enable_thinking = null;
            reg.num_frames = null;
            reg.frames_per_second = null;
            reg.audio_setting = null;
            reg.style = null;
            reg.camera_movement = null;
            reg.generate_audio_switch = null;
            reg.generate_multi_clip_switch = null;
            reg.character_orientation = null;
            reg.enhance_prompt = null;
            reg.mode = null;
            reg.driving_type = null;
            reg.subject_type = null;
            reg.num_inference_steps = null;
            reg.guidance_scale = null;
            reg.shift = null;
            reg.reference_image_url = null;
            reg.reference_video_url = null;
            reg.start_image_url = null;
            reg.first_frame_url = null;
            reg.enable_transcription = null;
            reg.camera_trajectory = null;
        }

        protected static void UseTargetAudio(VideoRequest reg)
        {
            reg.target_audio_url = reg.audio_url;
            reg.audio_url = null;
            reg.audio_urls = null;
            reg.reference_audio_urls = null;
        }

        protected static void ClearReferences(VideoRequest reg)
        {
            reg.image_urls = null;
            reg.reference_image_urls = null;
            reg.reference_video_urls = null;
            reg.audio_urls = null;
            reg.reference_audio_urls = null;
            reg.reference_image_url = null;
            reg.reference_video_url = null;
            reg.video_url = null;
        }
    }

    public sealed class H3MaxT2VHandler : H3MaxHandlerBase
    {
        public const string Model = "minimax/h3-max/text-to-video";

        public H3MaxT2VHandler() : base(Model)
        {
        }

        public override bool ShouldTrackPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            return IsCommonTrackProperty(propertyName);
        }

        public override bool ShouldItemPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            return IsCommonItemProperty(propertyName)
                || propertyName is nameof(ItemPayload.H3MaxAspectRatio) or nameof(ItemPayload.AudioSource);
        }

        public override void ConvertRequest(VideoRequest reg, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp && itemPayload is ItemPayload ip)
            {
                ApplyCommonRequest(reg, tp, ip);
                reg.aspect_ratio = ip.H3MaxAspectRatio;
                reg.image_url = null;
                reg.end_image_url = null;
                reg.last_frame_url = null;
                ClearReferences(reg);
                UseTargetAudio(reg);
            }
        }
    }

    public sealed class H3MaxI2VHandler : H3MaxHandlerBase
    {
        public const string Model = "minimax/h3-max/image-to-video";

        public H3MaxI2VHandler() : base(Model)
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
            if (trackPayload is TrackPayload tp && itemPayload is ItemPayload ip)
            {
                ApplyCommonRequest(reg, tp, ip);
                reg.aspect_ratio = null;
                reg.end_image_url = reg.last_frame_url;
                reg.last_frame_url = null;
                reg.audio_url = null;
                reg.target_audio_url = null;
                ClearReferences(reg);
            }
        }
    }

    public sealed class H3MaxReferenceToVideoHandler : H3MaxHandlerBase
    {
        public const string Model = "minimax/h3-max/reference-to-video";

        public H3MaxReferenceToVideoHandler() : base(Model)
        {
        }

        public override bool ShouldTrackPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            return IsCommonTrackProperty(propertyName) || IsImageReferences(propertyName);
        }

        public override bool ShouldItemPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            return IsCommonItemProperty(propertyName)
                || propertyName == nameof(ItemPayload.H3MaxReferenceAspectRatio)
                || IsImageReferences(propertyName)
                || IsVideoReferences(propertyName)
                || propertyName == nameof(ItemPayload.AudioSourceCont)
                || IsAudioReferences(propertyName);
        }

        public override void ConvertRequest(VideoRequest reg, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp && itemPayload is ItemPayload ip)
            {
                ApplyCommonRequest(reg, tp, ip);
                reg.aspect_ratio = ip.H3MaxReferenceAspectRatio;

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

                reg.audio_urls = null;
                reg.audio_url = null;
                reg.target_audio_url = null;
                reg.image_url = null;
                reg.video_url = null;
                reg.last_frame_url = null;
                reg.end_image_url = null;
            }
        }
    }

    public sealed class H3MaxCameraControlsHandler : H3MaxHandlerBase
    {
        public const string Model = "minimax/h3-max/camera-controls";

        public H3MaxCameraControlsHandler() : base(Model)
        {
        }

        public override bool ShouldTrackPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            return IsCommonTrackProperty(propertyName) || propertyName == nameof(TrackPayload.ImageSource);
        }

        public override bool ShouldItemPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            return IsCommonItemProperty(propertyName)
                || propertyName is nameof(ItemPayload.ImageSource)
                    or nameof(ItemPayload.H3MaxCameraTrajectory)
                    or nameof(ItemPayload.AddH3MaxCameraKeyframe)
                    or nameof(ItemPayload.RemoveLastH3MaxCameraKeyframe);
        }

        public override void ConvertRequest(VideoRequest reg, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp && itemPayload is ItemPayload ip)
            {
                ApplyCommonRequest(reg, tp, ip);
                reg.aspect_ratio = null;
                reg.audio_url = null;
                reg.target_audio_url = null;
                reg.last_frame_url = null;
                reg.end_image_url = null;
                ClearReferences(reg);

                if (ip.H3MaxCameraTrajectory.Count > 0)
                {
                    reg.camera_trajectory = ip.H3MaxCameraTrajectory
                        .OrderBy(keyframe => keyframe.Time)
                        .Select(keyframe => new H3MaxCameraKeyframeRequest
                        {
                            time = keyframe.Time,
                            azimuth = keyframe.Azimuth,
                            elevation = keyframe.Elevation,
                            distance = keyframe.Distance
                        })
                        .ToList();
                }
            }
        }
    }

    public sealed class H3MaxLipSyncHandler : H3MaxHandlerBase
    {
        public const string Model = "minimax/h3-max/lip-sync/image-to-video";

        public H3MaxLipSyncHandler() : base(Model)
        {
            ModelAlternativeCategory = "Lip sync";
        }

        public override bool ShouldTrackPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            return propertyName is nameof(TrackPayload.ImageSource)
                or nameof(TrackPayload.Seed)
                or nameof(TrackPayload.H3MaxLipSyncResolution)
                or nameof(TrackPayload.H3MaxEnableTranscription);
        }

        public override bool ShouldItemPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            return propertyName is nameof(ItemPayload.ImageSource)
                or nameof(ItemPayload.AudioSource)
                or nameof(ItemPayload.Seed);
        }

        public override void ConvertRequest(VideoRequest reg, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp && itemPayload is ItemPayload ip)
            {
                reg.prompt = null;
                reg.negative_prompt = null;
                reg.resolution = tp.H3MaxLipSyncResolution;
                reg.duration = null;
                reg.durationInt = null;
                reg.aspect_ratio = null;
                reg.enable_safety_checker = false;
                reg.enable_transcription = tp.H3MaxEnableTranscription;
                reg.prompt_expansion_mode = null;
                reg.target_audio_url = null;
                reg.sync_mode = null;

                reg.model = null;
                reg.first_frame_url = null;
                reg.last_frame_url = null;
                reg.start_image_url = null;
                reg.end_image_url = null;
                reg.upscale_factor = null;
                reg.target_fps = null;
                reg.compression = null;
                reg.noise = null;
                reg.halo = null;
                reg.grain = null;
                reg.recover_detail = null;
                reg.softness = null;
                reg.h264_output = null;
                reg.generate_audio = null;
                reg.audio = null;
                reg.enable_prompt_expansion = null;
                reg.enable_thinking = null;
                reg.num_frames = null;
                reg.frames_per_second = null;
                reg.video_url = null;
                reg.audio_setting = null;
                reg.style = null;
                reg.camera_movement = null;
                reg.generate_audio_switch = null;
                reg.generate_multi_clip_switch = null;
                reg.character_orientation = null;
                reg.enhance_prompt = null;
                reg.mode = null;
                reg.driving_type = null;
                reg.subject_type = null;
                reg.num_inference_steps = null;
                reg.guidance_scale = null;
                reg.shift = null;
                reg.reference_image_url = null;
                reg.reference_video_url = null;
                reg.image_urls = null;
                reg.reference_image_urls = null;
                reg.reference_video_urls = null;
                reg.audio_urls = null;
                reg.reference_audio_urls = null;
                reg.camera_trajectory = null;
            }
        }
    }

    public sealed class H3MaxTurboT2VHandler : H3MaxHandlerBase
    {
        public const string Model = "minimax/h3-max-turbo/text-to-video";

        public H3MaxTurboT2VHandler() : base(Model)
        {
        }

        public override bool ShouldTrackPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            return IsCommonTrackProperty(propertyName);
        }

        public override bool ShouldItemPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            return IsCommonItemProperty(propertyName)
                || propertyName is nameof(ItemPayload.H3MaxAspectRatio) or nameof(ItemPayload.AudioSource);
        }

        public override void ConvertRequest(VideoRequest reg, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp && itemPayload is ItemPayload ip)
            {
                ApplyCommonRequest(reg, tp, ip);
                reg.aspect_ratio = ip.H3MaxAspectRatio;
                reg.image_url = null;
                reg.end_image_url = null;
                reg.last_frame_url = null;
                ClearReferences(reg);
                UseTargetAudio(reg);
            }
        }
    }

    public sealed class H3MaxTurboI2VHandler : H3MaxHandlerBase
    {
        public const string Model = "minimax/h3-max-turbo/image-to-video";

        public H3MaxTurboI2VHandler() : base(Model)
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
            if (trackPayload is TrackPayload tp && itemPayload is ItemPayload ip)
            {
                ApplyCommonRequest(reg, tp, ip);
                reg.aspect_ratio = null;
                reg.end_image_url = reg.last_frame_url;
                reg.last_frame_url = null;
                reg.audio_url = null;
                reg.target_audio_url = null;
                ClearReferences(reg);
            }
        }
    }
}
