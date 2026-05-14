namespace FalAiPlugin.ModelVisibilityHandlers
{
    public class HidreamO1ImageHandler : ModelVisibilityHandlerBase
    {
        public const string Model = "hidream-o1-image";

        public HidreamO1ImageHandler()
        {
            ModelCategory = "HiDream";
            ModelPath = Model;
        }

        public override bool ShouldImageTrackPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is ImageTrackPayload ip)
            {
                return propertyName is nameof(ip.Prompt) or nameof(ip.WidthPx) or nameof(ip.HeigthPx) || IsImageGeneratorReferences(propertyName);
            }

            return base.ShouldImageTrackPropertyBeVisible(propertyName, trackPayload, itemPayload);
        }

        public override bool ShouldImageItemPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (itemPayload is ImageItemPayload ip)
            {
                return propertyName is nameof(ip.Prompt) or nameof(ip.Seed) || IsImageGeneratorReferences(propertyName);
            }

            return base.ShouldImageItemPropertyBeVisible(propertyName, trackPayload, itemPayload);
        }

        public override void ConvertRequest(Request reg)
        {
            reg.reference_image_urls = reg.image_urls?.ToArray();
            reg.image_urls = null;
            base.ConvertRequest(reg);
        }
    }

    public class HidreamO1ImageEditHandler : HidreamO1ImageHandler
    {
        public new const string Model = "hidream-o1-image/edit";

        public HidreamO1ImageEditHandler()
        {
            ModelPath = Model;
        }
    }
}
