namespace FalAiPlugin.ModelVisibilityHandlers
{
    public class LtxAudioToVideoHandler : ModelVisibilityHandlerBase
    {
        public const string Model = "ltx-2.3-quality/audio-to-video";

        public LtxAudioToVideoHandler()
        {
            ModelCategory = "Ltxv";
            ModelAlternativeCategory = "Lip sync";
            ModelPath = Model;
        }

        public override bool ShouldTrackPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp)
            {
                return propertyName is nameof(tp.Prompt) or nameof(tp.NegativePrompt) or nameof(tp.ImageSource);
            }

            return base.ShouldTrackPropertyBeVisible(propertyName, trackPayload, itemPayload);
        }

        public override bool ShouldItemPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (itemPayload is ItemPayload ip)
            {
                return propertyName is nameof(ip.Prompt) or nameof(ip.NegativePrompt) or nameof(ip.Seed) or nameof(ip.ImageSource) or nameof(ip.AudioSource);
            }

            return base.ShouldItemPropertyBeVisible(propertyName, trackPayload, itemPayload);
        }
    }
}
