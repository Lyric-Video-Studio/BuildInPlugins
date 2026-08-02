using MinimaxPlugin.Audio;
using PluginBase;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static MinimaxPlugin.ItemPayload;

namespace MinimaxPlugin
{
    public class TrackPayload : IPayloadPropertyVisibility, IJsonOnDeserialized
    {
        private Request imgToVidPayload = new Request();
        private string videoModel;
        [Description("Video model")][TriggerReload]
        public string VideoModel { get => videoModel ?? Settings?.model ?? "MiniMax-Hailuo-02"; set => videoModel = value; }
        [Description("Video settings")][IgnorePropertyName]
        public Request Settings { get => imgToVidPayload; set => imgToVidPayload = value; }
        [Description("MiniMax-H3 V2 settings")]
        public H3Settings H3Settings { get; set; } = new();
        public SubjectRefContainer SubjectReferences { get; set; } = new();
        [Description("MiniMax-H3 reference inputs. Do not combine these with first or last frames.")]
        public H3ReferenceContainer H3References { get; set; } = new();
        public void OnDeserialized() => videoModel ??= Settings?.model ?? "MiniMax-Hailuo-02";
        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            var model = (trackPayload as TrackPayload)?.VideoModel ?? "MiniMax-Hailuo-02";
            if (propertyName == nameof(VideoModel)) return true;
            if (propertyName == nameof(Settings)) return model != "MiniMax-H3";
            if (propertyName == nameof(H3Settings) || propertyName == nameof(H3References)) return model == "MiniMax-H3";
            if (propertyName == nameof(SubjectReferences) || propertyName == "AddSubject") return model != "MiniMax-Hailuo-2.3" && model != "MiniMax-H3";
            return true;
        }
    }
    public class SubjectRef
    {
        [JsonIgnore] private ObservableCollection<SubjectRef> parent;
        public SubjectRef() { } public SubjectRef(ObservableCollection<SubjectRef> parent) { this.parent = parent; }
        [EnableFileDrop] public string Path { get; set; }
        [CustomAction("Remove subject reference")] public void RemoveSubject() => parent?.Remove(this);
        internal void AddParent(ObservableCollection<SubjectRef> subjectReferences) => parent = subjectReferences;
    }
}
