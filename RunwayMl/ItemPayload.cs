using PluginBase;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace RunwayMlPlugin
{
    public class ItemPayload : IPayloadPropertyVisibility
    {
        private string pollingId;

        [Description("PollingId (generation id) is filled when you make new request. Unlike in other plugins, this is not cleared when generation is completed, because ths id is needed if you want to extend the video. " +
            "If you need to create new variation, clear this id, otherwise the same result will be fetched from the server")]
        public string PollingId { get => pollingId; set => pollingId = value; }

        public string Prompt { get; set; }

        public int Seed { get; set; } = 0;

        [EnableFileDrop]
        public string ImageSource { get => imageSource; set => imageSource = value; }

        private string imageSource;

        [EnableFileDrop]
        [Description("Used for video-to-models")]
        public string VideoSource { get; set; }

        [Description("Used with Act2. When enabled, non-facial movements and gestures will be applied to the character in addition to facial expressions")]
        public bool BodyControl { get; set; }

        [Range(1, 5)]
        [Description("Used with Act2. A larger value increases the intensity of the character's expression.")]
        public int ExpressionIntensity { get; set; } = 3;

        [Description("Used with Act2. Image or video required. If video is selected, it will be used, not image")]
        public string ReferenceImage { get; set; }

        [Description("Reference images for video. In prompt, you must describe how to use the references in video")]
        public ObservableCollection<AlephReferences> References { get; set; } = new();

        [CustomAction("Add reference")]
        public void AddReference()
        {
            References.Add(new AlephReferences() { });
        }

        public ItemPayload()
        {
            AlephReferences.RemoveReference += (s, e) =>
            {
                if (s is AlephReferences r)
                {
                    References.Remove(r);
                }
            };
        }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp)
            {
                if (propertyName == nameof(VideoSource))
                {
                    return tp.Request.model == "act_two" || tp.Request.model == "upscale_v1" || tp.Request.model == "gen4_aleph"; ;
                }

                if (propertyName == nameof(BodyControl) || propertyName == nameof(ExpressionIntensity))
                {
                    return tp.Request.model == "act_two";
                }

                if (propertyName == nameof(ImageSource))
                {
                    return tp.Request.model != "act_two" && tp.Request.model != "upscale_v1" && tp.Request.model != "gen4_aleph";
                }

                if (propertyName == nameof(Prompt))
                {
                    return tp.Request.model != "act_two" && tp.Request.model != "upscale_v1";
                }

                if (propertyName == nameof(ReferenceImage))
                {
                    return tp.Request.model == "act_two";
                }

                if (propertyName == nameof(References))
                {
                    return tp.Request.model == "gen4_aleph";
                }
            }

            return true;
        }

        public class AlephReferences
        {
            public static event EventHandler RemoveReference;

            [EnableFileDrop]
            public string Path { get; set; }

            [CustomAction("Remove")]
            public void Remove()
            {
                RemoveReference.Invoke(this, null);
            }
        }
    }
}