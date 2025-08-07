using PluginBase;
using System.Reactive.Subjects;

namespace WanPlugin
{
    public class TrackPayload
    {
        private Request request = new Request();

        [IgnorePropertyName]
        public Request Request { get => request; set => request = value; }
    }
}