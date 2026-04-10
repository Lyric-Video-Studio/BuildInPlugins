using System;
using System.Collections.Generic;
using System.Text;

namespace FalAiPlugin.ModelVisibilityHandlers
{
    public class ModelVisibilityHandlerBase
    {
        public string ModelCategory { get; set; }
        public string ModelAlternativeCategory { get; set; }
        public string ModelPath { get; set; }

        public virtual bool ShouldTrackPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            return false;
        }

        public virtual bool ShouldItemPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            return false;
        }

        public bool IsImageReferences(string propertyName)
        {
            return propertyName == nameof(TrackPayload.ImageSourceCont) || ImageSourceContainer.IsImageRefName(propertyName);
        }

        public bool IsVideoReferences(string propertyName)
        {
            return propertyName == nameof(ItemPayload.VideoSourceCont) || VideoSourceContainer.IsVideoRefName(propertyName);
        }

        public bool IsAudioReferences(string propertyName)
        {
            return propertyName == nameof(ItemPayload.AudioSource) || AudioSourceContainer.IsAudioRefName(propertyName);
        }

        public virtual void ConvertRequest(VideoRequest reg)
        {            
        }
    }
}
