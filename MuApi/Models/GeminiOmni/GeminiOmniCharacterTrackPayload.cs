namespace MuApiPlugin.Models.GeminiOmni
{
    public class GeminiOmniCharacterTrackPayload
    {
        public const string ModelCharacter = "gemini-omni-character";

        public bool ShouldPropertyBeVisible(string propertyName, string model)
        {
            return IsGeminiOmniCharacterModel(model);
        }

        public static bool IsGeminiOmniCharacterModel(string model)
        {
            return model == ModelCharacter;
        }
    }
}
