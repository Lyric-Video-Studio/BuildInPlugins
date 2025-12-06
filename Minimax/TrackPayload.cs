using MinimaxPlugin.Audio;
using PluginBase;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static MinimaxPlugin.ItemPayload;

namespace MinimaxPlugin
{
    public class TrackPayload : IPayloadPropertyVisibility
    {
        private Request imgToVidPayload = new Request();

        [Description("Video settings")]
        [IgnorePropertyName]
        public Request Settings { get => imgToVidPayload; set => imgToVidPayload = value; }

        public SubjectRefContainer SubjectReferences { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp && tp.Settings.model == "MiniMax-Hailuo-2.3")
            {
                if (propertyName == "SubjectReferences" || propertyName == "AddSubject")
                {
                    return false;
                }
            }
            return true;
        }
    }

    public class SubjectRef
    {
        [JsonIgnore]
        private ObservableCollection<SubjectRef> parent;

        public SubjectRef()
        {
        }

        public SubjectRef(ObservableCollection<SubjectRef> parent)
        {
            this.parent = parent;
        }

        [EnableFileDrop]
        public string Path { get; set; }

        [CustomAction("Remove subject reference")]
        public void RemoveSubject()
        {
            parent.Remove(this);
        }

        internal void AddParent(ObservableCollection<SubjectRef> subjectReferences)
        {
            parent = subjectReferences;
        }
    }
}