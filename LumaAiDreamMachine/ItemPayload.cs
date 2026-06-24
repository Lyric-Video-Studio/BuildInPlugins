using PluginBase;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LumaAiDreamMachinePlugin
{
    public class ItemPayload : IJsonOnDeserialized, IPayloadPropertyVisibility
    {
        public const string ImageModeStartEnd = "start/end frame";
        public const string ImageModeMultiFrame = "multiframe";

        public event EventHandler ImageModeChanged;

        [IgnoreDynamicEdit]
        public bool IsVideo { get; set; } = true;

        private string prompt = "";
        private string imageMode = ImageModeStartEnd;
        private string pollingId;

        public KeyFrames KeyFrames { get; set; } = new KeyFrames();

        public string Prompt { get => prompt; set => prompt = value; }

        [Description("Used for modify video")]
        [EnableFileDrop]
        public string VideoFile { get; set; }

        [Description("Optional source generation id for ray-3.2 edit or reframe. If set, it is used instead of uploading Video file")]
        public string SourceGenerationId { get; set; }

        [TriggerReload]
        [PropertyComboOptions([ImageModeStartEnd, ImageModeMultiFrame])]
        [CustomName("Image mode")]
        public string ImageMode
        {
            get => imageMode;
            set
            {
                var notify = IPayloadPropertyVisibility.UserInitiatedSet && imageMode != value;
                imageMode = value;

                if (notify)
                {
                    ImageModeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [Description("Guide frames anchored at arbitrary output-frame positions for ray-3.2 multi-keyframe image-to-video")]
        public ObservableCollection<RayMultiKeyFrame> MultiKeyFrames { get; set; } = new ObservableCollection<RayMultiKeyFrame>();

        [Description("Optional, but recommended for modify video. You can copy frame path from video item with right click context menu")]
        [EnableFileDrop]
        public string FirstFrame { get; set; }

        [Description("PollingId (generation id) is filled when you make new request. Unlike in other plugins, this is not cleared when generation is completed, " +
            "because this id is needed if you want to extend the video. " +
            "If you need to create new variation, clear this id, otherwise the same result will be fetched from the server")]
        public string PollingId { get => pollingId; set => pollingId = value; }

        [CustomAction("Add multiframe keyframe")]
        public void AddMultiKeyFrame()
        {
            var item = new RayMultiKeyFrame();
            item.AddParent(MultiKeyFrames);
            MultiKeyFrames.Add(item);
        }

        public void OnDeserialized()
        {
            foreach (var item in MultiKeyFrames)
            {
                item.AddParent(MultiKeyFrames);
            }
        }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (itemPayload is ItemPayload ip)
            {
                var hasSourceVideo = !string.IsNullOrWhiteSpace(ip.VideoFile) || !string.IsNullOrWhiteSpace(ip.SourceGenerationId);
                var typedTrackPayload = trackPayload as TrackPayload;
                var isRayVideo = typedTrackPayload?.Settings?.model == "ray-3.2";
                var isReframe = isRayVideo && typedTrackPayload.SourceVideoMode == TrackPayload.SourceVideoModeReframe;
                var useMultiFrame = isRayVideo && !hasSourceVideo && ip.ImageMode == ImageModeMultiFrame;

                if (propertyName == nameof(ItemPayload.KeyFrames.frame0.url) || propertyName == nameof(ItemPayload.KeyFrames.frame0.id))
                {
                    return !hasSourceVideo && !useMultiFrame;
                }

                if (propertyName == nameof(ItemPayload.FirstFrame))
                {
                    return hasSourceVideo && !isReframe;
                }

                if (propertyName == nameof(SourceGenerationId))
                {
                    return isRayVideo;
                }

                if (propertyName == nameof(ImageMode))
                {
                    return isRayVideo && !hasSourceVideo;
                }

                if (propertyName is nameof(MultiKeyFrames) or nameof(AddMultiKeyFrame) or nameof(RayMultiKeyFrame.ImageSource) or nameof(RayMultiKeyFrame.GenerationId) or nameof(RayMultiKeyFrame.FrameIndex) or nameof(RayMultiKeyFrame.RemoveMultiKeyFrame))
                {
                    return useMultiFrame;
                }
            }
            return true;
        }
    }

    public class RayMultiKeyFrame
    {
        private ObservableCollection<RayMultiKeyFrame> parent;

        public void AddParent(ObservableCollection<RayMultiKeyFrame> list)
        {
            parent = list;
        }

        [EnableFileDrop]
        [CustomName("Image source")]
        public string ImageSource { get; set; }

        [CustomName("Generation id")]
        public string GenerationId { get; set; }

        [CustomName("Frame index")]
        public int FrameIndex { get; set; }

        [CustomAction("Remove multiframe keyframe", false, nameof(ImageSource))]
        public void RemoveMultiKeyFrame()
        {
            parent?.Remove(this);
        }
    }
}
