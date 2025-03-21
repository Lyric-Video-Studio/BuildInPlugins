namespace LumaAiDreamMachinePlugin.VideoUpscale
{
    internal class GenerationUpscaleItemPayload
    {
        private string generationId;
        private string pollingId;
        public string GenerationId { get => generationId; set => generationId = value; }
        public string PollingId { get => pollingId; set => pollingId = value; }
    }
}