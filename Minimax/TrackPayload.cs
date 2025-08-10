using MinimaxPlugin.Audio;
using PluginBase;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace MinimaxPlugin
{
    public class TrackPayload : IJsonOnDeserialized
    {
        [IgnoreDynamicEdit]
        public bool IsVideo { get; set; } = true;

        private Request imgToVidPayload = new Request();

        [Description("Video settings")]
        [IgnorePropertyName]
        public Request Settings { get => imgToVidPayload; set => imgToVidPayload = value; }

        public ObservableCollection<SubjectRef> SubjectReferences { get; set; } = new();

        [CustomAction("Add subject reference")]
        public void AddSubject()
        {
            SubjectReferences.Add(new SubjectRef(SubjectReferences));
        }

        public void OnDeserialized()
        {
            foreach (var item in SubjectReferences)
            {
                item.AddParent(SubjectReferences);
            }
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