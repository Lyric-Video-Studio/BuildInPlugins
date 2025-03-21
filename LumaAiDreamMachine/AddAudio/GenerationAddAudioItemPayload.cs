namespace LumaAiDreamMachinePlugin.AddAudio
{
    internal class GenerationAddAudioItemPayload
    {
        private string generationId;
        private string pollingId;
        public string GenerationId { get => generationId; set => generationId = value; }
        public string PollingId { get => pollingId; set => pollingId = value; }

        private string prompt = "";
        public string Prompt { get => prompt; set => prompt = value; }

        private string negativePrompt = "";
        public string NegativePrompt { get => negativePrompt; set => negativePrompt = value; }
    }
}