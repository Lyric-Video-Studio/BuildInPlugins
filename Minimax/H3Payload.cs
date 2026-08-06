using PluginBase;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;
namespace MinimaxPlugin
{
    public class H3Settings : IPayloadPropertyVisibility
    {
        [PropertyComboOptions(["768P", "2K"])] 
        public string resolution { get; set; } = "2K";
        [Description("Output duration in seconds (4 through 15)")]
        [PropertyComboOptions(["4", "5", "6", "7", "8", "9", "10", "11",  "12", "13", "14", "15"])]
        public int duration { get; set; } = 5;
        [PropertyComboOptions(["adaptive", "21:9", "16:9", "4:3", "1:1", "3:4", "9:16"])] 
        public string ratio { get; set; } = "16:9";
        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload) => (trackPayload as TrackPayload)?.VideoModel == "MiniMax-H3";
    }
    public class H3ReferenceContainer : IJsonOnDeserialized, IPayloadPropertyVisibility
    {
        public ObservableCollection<H3Reference> ReferenceImages { get; set; } = new(); public ObservableCollection<H3Reference> ReferenceVideos { get; set; } = new(); public ObservableCollection<H3Reference> ReferenceAudio { get; set; } = new();
        [CustomAction("Add reference image")] public void AddReferenceImage() => ReferenceImages.Add(new H3Reference(ReferenceImages)); [CustomAction("Add reference video")] public void AddReferenceVideo() => ReferenceVideos.Add(new H3Reference(ReferenceVideos)); [CustomAction("Add reference audio")] public void AddReferenceAudio() => ReferenceAudio.Add(new H3Reference(ReferenceAudio));
        public void OnDeserialized() { Fix(ReferenceImages); Fix(ReferenceVideos); Fix(ReferenceAudio); }
        private static void Fix(ObservableCollection<H3Reference> x) { foreach (var v in x) v.AddParent(x); }
        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload) => (trackPayload as TrackPayload)?.VideoModel == "MiniMax-H3";
    }
    public class H3Reference { [JsonIgnore] private ObservableCollection<H3Reference> parent; public H3Reference() { } public H3Reference(ObservableCollection<H3Reference> p) { parent = p; } [Description("Local file, public URL, mm_file:// ID, or data URI")][EnableFileDrop] public string Source { get; set; } [CustomAction("Remove reference")] public void RemoveReference() => parent?.Remove(this); internal void AddParent(ObservableCollection<H3Reference> p) => parent = p; }
    public class H3Request { public string model { get; set; } = "MiniMax-H3"; public List<H3Content> content { get; set; } = new(); public string resolution { get; set; } = "2K"; public int duration { get; set; } = 5; public string ratio { get; set; } = "16:9"; public string callback_url { get; set; } }
    public class H3Content { public string type { get; set; } public string text { get; set; } public H3Url image_url { get; set; } public H3Url video_url { get; set; } public H3Url audio_url { get; set; } public string role { get; set; } }
    public class H3Url { public string url { get; set; } }
    public class H3CreateResponse { public string task_id { get; set; } }
    public class H3QueryResponse { public H3Task task { get; set; } }
    public class H3Task { public string status { get; set; } public H3Result content { get; set; } public H3Error error { get; set; } }
    public class H3Result { public string url { get; set; } }
    public class H3Error { public string message { get; set; } }
}
