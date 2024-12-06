using OpenAI.Images;
using OpenAI.Models;
using OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OpenAiTxtToImgPlugin
{
    public class TrackPayload
    {
        public string Model { get; set; }

        public string Prompt { get; set; }

        public int Number { get; set; }

        public string Quality { get; set; }

        public ImageResponseFormat ResponseFormat { get; set; }
        public string Size { get; set; }


        public string Style { get; set; }


        public string User { get; set; }   


        public TrackPayload(string prompt, Model model = null, int numberOfResults = 1, string quality = null, ImageResponseFormat responseFormat = ImageResponseFormat.Url, string size = null, string style = null, string user = null)
        {
            Prompt = prompt;
            Model = (string.IsNullOrWhiteSpace(model?.Id) ? OpenAI.Models.Model.DallE_2 : model);
            Number = numberOfResults;
            Quality = quality;
            ResponseFormat = responseFormat;
            Size = size ?? "1024x1024";
            Style = style;
            User = user;
        }
        public TrackPayload()
        {

        }
    }
}
