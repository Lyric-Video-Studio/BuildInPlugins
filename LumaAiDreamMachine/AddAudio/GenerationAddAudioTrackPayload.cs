namespace LumaAiDreamMachinePlugin.AddAudio
{
    internal class GenerationAddAudioTrackPayload
    {
        private string prompt = "";
        public string Prompt { get => prompt; set => prompt = value; }

        private string negativePrompt = "";
        public string NegativePrompt { get => negativePrompt; set => negativePrompt = value; }
    }
}