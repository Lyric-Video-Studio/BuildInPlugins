using PluginBase;

namespace GooglePlugin
{
    public class ImageTrackPayload : IPayloadPropertyVisibility
    {
        [PropertyComboOptions(["gemini-nano-banana-2.1", "gemini-3.1-flash-lite-image", "gemini-3.1-flash-image-preview", "gemini-3-pro-image-preview", "gemini-2.5-flash-image", "models/imagen-4.0-generate-001"])]
        public string Model { get; set; } = "gemini-3.1-flash-lite-image";
        public string Prompt { get; set; }

        [PropertyComboOptions(["1K", "2K", "4K"])]
        public string Size { get; set; } = "1K";

        [PropertyComboOptions(["Auto", "1:1", "1:4", "1:8", "2:3", "3:2", "3:4", "4:1", "4:3", "4:5", "5:4", "8:1", "9:16", "16:9", "21:9"])]
        public string AspectRatio { get; set; } = "Auto";

        [PropertyComboOptions(["MINIMAL", "MEDIUM", "HIGH"])]
        public string ThinkingLevel { get; set; } = "MEDIUM";

        [EnableFileDrop]
        public string ImageSource { get; set; }

        [EnableFileDrop]
        public string ImageSource2 { get; set; }

        [EnableFileDrop]
        public string ImageSource3 { get; set; }

        [EnableFileDrop]
        public string ImageSource4 { get; set; }

        [EnableFileDrop]
        public string ImageSource5 { get; set; }

        [EnableFileDrop]
        public string ImageSource6 { get; set; }

        [EnableFileDrop]
        public string ImageSource7 { get; set; }

        [EnableFileDrop]
        public string ImageSource8 { get; set; }

        [EnableFileDrop]
        public string ImageSource9 { get; set; }

        [EnableFileDrop]
        public string ImageSource10 { get; set; }

        [EnableFileDrop]
        public string ImageSource11 { get; set; }

        [EnableFileDrop]
        public string ImageSource12 { get; set; }

        [EnableFileDrop]
        public string ImageSource13 { get; set; }

        [EnableFileDrop]
        public string ImageSource14 { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (propertyName == nameof(Size))
            {
                return Model is "gemini-nano-banana-2.1" or "gemini-3-pro-image-preview";
            }

            if (propertyName == nameof(ThinkingLevel))
            {
                return Model == "gemini-nano-banana-2.1";
            }

            if (propertyName == nameof(AspectRatio))
            {
                return Model != "models/imagen-4.0-generate-001";
            }

            if (IsExtendedReferenceImageProperty(propertyName))
            {
                return SupportsExtendedReferenceImages(Model);
            }

            return true;
        }

        public static bool SupportsExtendedReferenceImages(string model)
        {
            return model is "gemini-nano-banana-2.1"
                or "gemini-3.1-flash-lite-image"
                or "gemini-3.1-flash-image-preview"
                or "gemini-3-pro-image-preview";
        }

        private static bool IsExtendedReferenceImageProperty(string propertyName)
        {
            return propertyName is nameof(ImageSource5)
                or nameof(ImageSource6)
                or nameof(ImageSource7)
                or nameof(ImageSource8)
                or nameof(ImageSource9)
                or nameof(ImageSource10)
                or nameof(ImageSource11)
                or nameof(ImageSource12)
                or nameof(ImageSource13)
                or nameof(ImageSource14);
            }
        }
    }
}

