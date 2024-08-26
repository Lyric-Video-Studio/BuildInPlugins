
namespace StabilityAiTxtToImgPlugin
{
    public class ItemPayload
    {
        private string positivePrompt = "Progressive metal band from Finland playing in forest";
        private string negativePrompt = "";
        private string seed = "0";

        public string PositivePrompt { get => positivePrompt; set => positivePrompt = value; }
        public string NegativePrompt { get => negativePrompt; set => negativePrompt = value; }
        public string Seed { get => seed; set => seed = value; }
    }
}
