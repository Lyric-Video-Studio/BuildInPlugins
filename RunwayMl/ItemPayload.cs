using System.ComponentModel;

namespace RunwayMlPlugin
{
    public class ItemPayload
    {
        private string pollingId;

        [Description("PollingId (generation id) is filled when you make new request. Unlike in other plugins, this is not cleared when generation is completed, because ths id is needed if you want to extend the video. " +
            "If you need to create new variation, clear this id, otherwise the same result will be fetched from the server")]
        public string PollingId { get => pollingId; set => pollingId = value; }

        private Request request = new Request();
        public Request Request { get => request; set => request = value; }

        public string ImageSource { get => imageSource; set => imageSource = value; }

        private string imageSource;
    }
}