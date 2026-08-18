using PluginBase;
using System.ComponentModel;

namespace MuApiPlugin.Models.Seedance2
{
    public class Seedance2TrackPayload
    {
        public const string ModelT2V = "seedance-v2.0-t2v";
        public const string ModelI2V = "seedance-v2.0-i2v";
        public const string ModelT2V480p = "seedance-2.0-t2v-480p";
        public const string ModelI2V480p = "seedance-2.0-i2v-480p";
        public const string ModelOmniRef = "seedance-2.0-omni-reference";
        public const string ModelMiniT2V = "seedance-2-mini-text-to-video";
        public const string ModelMiniI2V = "seedance-2-mini-image-to-video";
        public const string ModelMiniOmniRef = "seedance-2-mini-omni-reference";

        public const string Model25SpicyT2V = "seedance-2.5-spicy-text-to-video";
        public const string Model25SpicyI2V = "seedance-2.5-spicy-image-to-video";
        public const string Model25SpicyFirstLast = "seedance-2.5-spicy-first-last-frame";
        public const string Model25SpicyOmni = "seedance-2.5-spicy-omni-reference";
        public const string Model25SpicyVideoEdit = "seedance-2.5-spicy-video-edit";
        public const string Model25SpicyVideoExtend = "seedance-2.5-spicy-video-extend";

        public static readonly string[] SpicyModels =
        [
            Model25SpicyT2V, Model25SpicyI2V, Model25SpicyFirstLast,
            Model25SpicyOmni, Model25SpicyVideoEdit, Model25SpicyVideoExtend
        ];

        [Description("Track-level prompt prefix. Reference uploaded media in your prompt with @image1, @image2, @audio1 etc")]
        [EditorWidth(360)]
        public string Prompt { get; set; }

        [PropertyComboOptions(["16:9", "9:16", "1:1", "4:3", "3:4", "21:9", "9:21"])]
        public string AspectRatio { get; set; } = "16:9";

        [PropertyComboOptions(["basic", "high"])]
        public string Quality { get; set; } = "high";

        [PropertyComboOptions(["480p", "720p", "1080p", "4K"])]
        public string Resolution { get; set; } = "720p";

        [CustomName("Generate audio")]
        public bool GenerateAudio { get; set; } = true;

        [CustomName("Camera fixed")]
        public bool CameraFixed { get; set; }

        [CustomName("High bitrate")]
        public bool HighBitrate { get; set; }

        [Description("Random seed for Seedance 2.5. Use -1 for a random seed.")]
        public int Seed { get; set; } = -1;

        [CustomName("Omni reference task type")]
        [PropertyComboOptions(["auto", "reference", "edit", "extend"])]
        public string OmniReferenceTaskType { get; set; } = "auto";

        public ImageReferenceContainer ImageReferences { get; set; } = new();

        public AudioReferenceContainer AudioReferences { get; set; } = new();

        public VideoReferenceContainer VideoReferences { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {
            if (!SupportsImageReferences(model) && IsImageReferenceProperty(propertyName))
            {
                return false;
            }

            if (!SupportsAudioReferences(model) && IsAudioReferenceProperty(propertyName))
            {
                return false;
            }

            if (!SupportsVideoReferences(model) && IsVideoReferenceProperty(propertyName))
            {
                return false;
            }

            if (propertyName == nameof(Quality) && (IsMiniModel(model) || IsSpicyModel(model)))
            {
                return false;
            }

            if (propertyName == nameof(Resolution) && !UsesResolutionParameter(model))
            {
                return false;
            }

            if (propertyName == nameof(GenerateAudio) && !SupportsGenerateAudio(model))
            {
                return false;
            }

            if (propertyName == nameof(CameraFixed) && !SupportsCameraFixed(model))
            {
                return false;
            }

            if (propertyName == nameof(HighBitrate) && !IsMiniModel(model))
            {
                return false;
            }

            if (propertyName == nameof(Seed) && !IsSeedance25SpicyModel(model))
            {
                return false;
            }

            if (propertyName == nameof(OmniReferenceTaskType) && !IsOmniReferenceModel(model))
            {
                return false;
            }

            return true;
        }

        public static bool IsTextToVideoModel(string model)
        {
            return model is ModelT2V or ModelT2V480p or ModelMiniT2V
                || IsSeedance25SpicyModel(model) && model.Contains("text-to-video", StringComparison.Ordinal);
        }

        public static bool IsImageToVideoModel(string model)
        {
            return model is ModelI2V or ModelI2V480p or ModelMiniI2V
                || IsSeedance25SpicyModel(model) && model.Contains("image-to-video", StringComparison.Ordinal);
        }

        public static bool IsFirstLastFrameModel(string model) =>
            IsSeedance25SpicyModel(model) && model.Contains("first-last-frame", StringComparison.Ordinal);

        public static bool IsOmniReferenceModel(string model) =>
            model is ModelOmniRef or ModelMiniOmniRef
            || IsSeedance25SpicyModel(model) && model.Contains("omni-reference", StringComparison.Ordinal);

        public static bool IsVideoEditModel(string model) =>
            IsSeedance25SpicyModel(model) && model.Contains("video-edit", StringComparison.Ordinal);

        public static bool IsVideoExtendModel(string model) =>
            IsSeedance25SpicyModel(model) && model.Contains("video-extend", StringComparison.Ordinal);

        public static bool IsMiniModel(string model)
        {
            return model is ModelMiniT2V or ModelMiniI2V or ModelMiniOmniRef;
        }

        public static bool IsSpicyModel(string model) => model?.Contains("-spicy-", StringComparison.Ordinal) == true;

        public static bool IsSeedance25SpicyModel(string model) => model?.StartsWith("seedance-2.5-spicy-", StringComparison.Ordinal) == true;

        public static bool UsesResolutionParameter(string model) =>
            IsMiniModel(model) || model is Model25SpicyT2V or Model25SpicyI2V;

        public static bool SupportsGenerateAudio(string model) =>
            IsMiniModel(model) || model is Model25SpicyT2V or Model25SpicyI2V || IsVideoEditModel(model) || IsVideoExtendModel(model);

        public static bool SupportsCameraFixed(string model) => model is Model25SpicyT2V or Model25SpicyI2V;

        public static bool SupportsImageReferences(string model) => !IsTextToVideoModel(model);

        public static bool SupportsAudioReferences(string model) =>
            IsOmniReferenceModel(model) || IsVideoEditModel(model);

        public static bool SupportsVideoReferences(string model) =>
            IsOmniReferenceModel(model) || IsVideoEditModel(model) || IsVideoExtendModel(model);

        public static string GetSpicyCategory(string model)
        {
            return "Seedance 2.5 Spicy";
        }

        private static bool IsImageReferenceProperty(string propertyName) =>
            propertyName == nameof(ImageReferences) || ImageReferenceContainer.IsImageRefName(propertyName);

        private static bool IsAudioReferenceProperty(string propertyName) =>
            propertyName == nameof(AudioReferences) || AudioReferenceContainer.IsAudioRefName(propertyName);

        private static bool IsVideoReferenceProperty(string propertyName) =>
            propertyName == nameof(VideoReferences) || VideoReferenceContainer.IsVideoRefName(propertyName);
    }
}
