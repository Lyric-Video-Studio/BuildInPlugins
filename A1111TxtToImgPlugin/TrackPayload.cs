using System.ComponentModel;

namespace A1111TxtToImgPlugin
{
    public class TrackPayload
    {
        private string sd_model_checkpoint;
        private StableDiffusionProcessingTxt2Img txt2ImgPayload = new StableDiffusionProcessingTxt2Img();

        [Description("Checkpoint/model")]
        public string Sd_model_checkpoint { get => sd_model_checkpoint; set => sd_model_checkpoint = value; }

        [Description("Image settings")]
        public StableDiffusionProcessingTxt2Img Settings { get => txt2ImgPayload; set => txt2ImgPayload = value; }
    }
}
