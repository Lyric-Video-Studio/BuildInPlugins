using PluginBase;

namespace GooglePlugin
{
    public class ImageItemPayload : IPayloadPropertyVisibility
    {
        public string Prompt { get; set; }

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
            if (trackPayload is ImageTrackPayload tp &&
                propertyName is nameof(ImageSource5)
                    or nameof(ImageSource6)
                    or nameof(ImageSource7)
                    or nameof(ImageSource8)
                    or nameof(ImageSource9)
                    or nameof(ImageSource10)
                    or nameof(ImageSource11)
                    or nameof(ImageSource12)
                    or nameof(ImageSource13)
                    or nameof(ImageSource14))
            {
                return ImageTrackPayload.SupportsExtendedReferenceImages(tp.Model);
            }

            return true;
        }
    }
}

