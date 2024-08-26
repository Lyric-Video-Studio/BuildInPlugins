using System.ComponentModel;

namespace A1111ImgToImgPlugin
{
    public class TrackPayload
    {
        private string sd_model_checkpoint;
        private string scriptArguments = "[null, 64, \"R-ESRGAN 4x+\", 1.5]";
        private StableDiffusionProcessingImg2Img img2ImgPayload = new StableDiffusionProcessingImg2Img();

        private bool upscale = false;
        private int tileOverlap = 64;
        private float scaleFactor = 2;
        private string upscaler = "R-ESGRAN 4x+";

        [Description("Checkpoint/model")]
        public string Sd_model_checkpoint { get => sd_model_checkpoint; set => sd_model_checkpoint = value; }

        [Description("Image settings")]
        public StableDiffusionProcessingImg2Img Settings { get => img2ImgPayload; set => img2ImgPayload = value; }

        public string ScriptArguments { get => scriptArguments; set => scriptArguments = value; }

        [Description("Check this if you want to upscale the image. Turn down denoising strength to for example 0.2")]
        public bool Upscale { get => upscale; set => upscale = value; }

        [Description("Use when opverlapping")]
        public int TileOverlap { get => tileOverlap; set => tileOverlap = value; }

        public float ScaleFactor { get => scaleFactor; set => scaleFactor = value; }
        public string Upscaler { get => upscaler; set => upscaler = value; }
    }
}