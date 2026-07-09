namespace FalAiPlugin.ModelVisibilityHandlers
{
    public class Seedream5ProTextToImageHandler : ModelVisibilityHandlerBase
    {
        public const string Model = "bytedance/seedream/v5/pro/text-to-image";

        public Seedream5ProTextToImageHandler()
        {
            ModelCategory = "Bytedance";
            ModelPath = Model;
        }

        public override bool ShouldImageTrackPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is ImageTrackPayload tp)
            {
                return propertyName is nameof(tp.Prompt) or nameof(tp.WidthPx) or nameof(tp.HeigthPx);
            }

            return base.ShouldImageTrackPropertyBeVisible(propertyName, trackPayload, itemPayload);
        }

        public override bool ShouldImageItemPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (itemPayload is ImageItemPayload ip)
            {
                return propertyName is nameof(ip.Prompt);
            }

            return base.ShouldImageItemPropertyBeVisible(propertyName, trackPayload, itemPayload);
        }
    }

    public class Seedream5ProEditHandler : Seedream5ProTextToImageHandler
    {
        public new const string Model = "bytedance/seedream/v5/pro/edit";

        public Seedream5ProEditHandler()
        {
            ModelPath = Model;
        }

        public override bool ShouldImageTrackPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            return base.ShouldImageTrackPropertyBeVisible(propertyName, trackPayload, itemPayload) ||
                IsImageGeneratorReferences(propertyName);
        }

        public override bool ShouldImageItemPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            return base.ShouldImageItemPropertyBeVisible(propertyName, trackPayload, itemPayload) ||
                IsImageGeneratorReferences(propertyName);
        }
    }
}
