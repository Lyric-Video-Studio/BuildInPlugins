using System;
using System.Collections.Generic;
using System.Text;

namespace FalAiPlugin.ModelVisibilityHandlers
{
    public class LegacyFixHandler
    {
        public static bool ShouldVideoTrackPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp && tp.Model != null)
            {
                if (tp.Model.Contains("pixverse/v5.6") && (propertyName is nameof(tp.Resolution) or nameof(tp.NegativePrompt)))
                {
                    return true;
                }

                if (tp.Model.Contains("veo3.1"))
                {
                    if ((tp.Model == "veo3.1" || tp.Model == "veo3.1/fast" || tp.Model == "veo3.1/reference-to-video") && propertyName.StartsWith("Aspect"))
                    {
                        return propertyName is nameof(tp.AspectRatioVeo31T2V);
                    }

                    if (tp.Model.Contains("image-to", StringComparison.InvariantCultureIgnoreCase) && propertyName is nameof(tp.ImageSource))
                    {
                        return true;
                    }

                    return propertyName is nameof(tp.AspectRatioVeo31) or nameof(tp.ResolutionVeo31) or nameof(tp.GenerateAudio) or nameof(tp.Seed) or nameof(tp.NegativePrompt);
                }
            }
            return false;
        }

        public static bool ShouldVideoItemPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp && itemPayload is ItemPayload ip && tp.Model != null)
            {
                if (tp.Model.Contains("veo3.1") && propertyName is nameof(ip.DurationVeo) or nameof(ip.Seed) or nameof(ip.NegativePrompt))
                {
                    if (tp.Model == "veo3.1/reference-to-video" && propertyName is nameof(ip.DurationVeo))
                    {
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
