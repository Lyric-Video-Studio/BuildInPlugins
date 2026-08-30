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
        public string VideoModel { get => videoModel ?? Settings?.model ?? H3Models.H3; set => videoModel = value; }
        [Description("Video settings")][IgnorePropertyName]
        public Request Settings { get => imgToVidPayload; set => imgToVidPayload = value; }
        [Description("MiniMax-H3 settings")]
        [ParentNameAttribute]
        public H3Settings H3Settings { get; set; } = new();
        [Description("MiniMax-H3-Max fast-generation settings")]
        [ParentNameAttribute]
        public H3MaxSettings H3MaxSettings { get; set; } = new();
        public SubjectRefContainer SubjectReferences { get; set; } = new();
        [Description("MiniMax-H3 reference inputs. Do not combine these with first or last frames.")]
        public H3ReferenceContainer H3References { get; set; } = new();
        public void OnDeserialized()
        {
            videoModel ??= Settings?.model ?? H3Models.H3;
            H3Settings ??= new();
            H3MaxSettings ??= new();
            H3References ??= new();
        }
        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            var model = (trackPayload as TrackPayload)?.VideoModel ?? H3Models.H3;
            if (propertyName == nameof(VideoModel)) return true;
            if (propertyName == nameof(Settings)) return !H3Models.IsH3(model);
            if (propertyName == nameof(H3Settings) || propertyName == nameof(H3References) || H3Models.IsReferenceAction(propertyName)) return model == H3Models.H3;
            if (propertyName == nameof(H3MaxSettings)) return model == H3Models.H3Max;
            if (propertyName == nameof(SubjectReferences) || propertyName == "AddSubject") return model != "MiniMax-Hailuo-2.3" && !H3Models.IsH3(model);
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
