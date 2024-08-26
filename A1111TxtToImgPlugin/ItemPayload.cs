using System.ComponentModel;

namespace A1111TxtToImgPlugin
{
    public class ItemPayload
    {
        private string positivePrompt = "Progressive metal band from Finland playing in forest";
        private string negativePrompt = "";
        private int seed;

        public string PositivePrompt { get => positivePrompt; set => positivePrompt = value; }
        public string NegativePrompt { get => negativePrompt; set => negativePrompt = value; }

        [Description("0 = use seed from track settings")]
        public int Seed { get => seed; set => seed = value; }
    }
}