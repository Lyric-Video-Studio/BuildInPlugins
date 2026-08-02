using PluginBase;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace MinimaxPlugin
{
    public class ItemPayload : IPayloadPropertyVisibility
    {
        public string Prompt { get; set; } = "";
        [Description("PollingId (generation id); clear it to create a new variation.")]
        public string PollingId { get; set; }
        [Description("First frame for video")][EnableFileDrop] public string ImagePath { get; set; }
        [Description("MiniMax-H3 last frame (requires first frame; cannot mix with references)")][EnableFileDrop] public string LastFramePath { get; set; }
        public SubjectRefContainer SubjectReferences { get; set; } = new();
        [Description("MiniMax-H3 reference images, videos, and audio. Cannot mix with frames.")]
        public H3ReferenceContainer H3References { get; set; } = new();

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            var model = (trackPayload as TrackPayload)?.VideoModel ?? (trackPayload as TrackPayload)?.Settings?.model;
            if (propertyName == nameof(ImagePath)) return true;
            if (propertyName == nameof(LastFramePath) || propertyName == nameof(H3References)) return model == "MiniMax-H3";
            if (propertyName == nameof(SubjectReferences) || propertyName == "AddSubject") return model != "MiniMax-Hailuo-2.3" && model != "MiniMax-H3";
            return true;
        }

        public class SubjectRefContainer : IJsonOnDeserialized
        {
            public ObservableCollection<SubjectRef> SubjectReferences { get; set; } = new();
            [CustomAction("Add subject reference")] public void AddSubject() => SubjectReferences.Add(new SubjectRef(SubjectReferences));
            public void OnDeserialized() { foreach (var item in SubjectReferences) item.AddParent(SubjectReferences); }
        }
    }
}
