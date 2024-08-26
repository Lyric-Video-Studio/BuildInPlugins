using System.ComponentModel;

namespace A1111ImgToImgPlugin
{
    public class VideoItemPayload
    {
        private string positivePrompt = "Progressive metal band from Finland playing in forest";
        private string negativePrompt = "";
        private int seed;
        private List<string> frames;

        public string PositivePrompt { get => positivePrompt; set => positivePrompt = value; }
        public string NegativePrompt { get => negativePrompt; set => negativePrompt = value; }

        [Description("0 = use seed from track settings")]
        public int Seed { get => seed; set => seed = value; }

        public List<string> Frames { get => frames; set => frames = value; }
    }
}