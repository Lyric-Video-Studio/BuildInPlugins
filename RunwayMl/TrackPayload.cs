using PluginBase;
using System.ComponentModel;

namespace RunwayMlPlugin
{
    public class TrackPayload
    {
        private Request request = new Request();

        [IgnorePropertyName]
        public Request Request { get => request; set => request = value; }
    }
}