using System;
using System.Collections.Generic;
using System.Text;

namespace FalAiPlugin.ModelVisibilityHandlers
{
    public class Seedance2R2VHandler: ModelVisibilityHandlerBase
    {
        public Seedance2R2VHandler() 
        {
            ModelCategory = "Bytedance";
            ModelPath = "bytedance/seedance-2.0/reference-to-video";
        }

        public override bool ShouldTrackPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp)
            {
                return (propertyName is nameof(tp.Prompt) or nameof(tp.ResolutionLtx) or nameof(tp.AspectRatioWan26) or nameof(tp.GenerateAudio) or nameof(tp.Seed)) || IsImageReferences(propertyName);
            }
            return base.ShouldTrackPropertyBeVisible(propertyName, trackPayload, itemPayload);
        }

        public override bool ShouldItemPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (itemPayload is ItemPayload ip)
            {
                return (propertyName is nameof(ip.Prompt) or nameof(ip.DurationSeedream20)) || 
                    IsImageReferences(propertyName) || IsVideoReferences(propertyName) || IsAudioReferences(propertyName);
                    
            }
            return base.ShouldItemPropertyBeVisible(propertyName, trackPayload, itemPayload);
        }

        public override void ConvertRequest(VideoRequest reg)
        {
            reg.end_image_url = reg.last_frame_url;
            reg.last_frame_url = null;
            base.ConvertRequest(reg);
        }
    }
}
