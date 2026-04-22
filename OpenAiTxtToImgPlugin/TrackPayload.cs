using OpenAI;
using OpenAI.Images;
using OpenAI.Models;
using PluginBase;
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
        [PropertyComboOptions(["gpt-image-1"/*, "gpt-image-2"*/])]
        public string Model { get; set; } = "gpt-image-2";

        public string Prompt { get; set; }

        public TrackPayload(string prompt)
        {
            Prompt = prompt;  
        }
        public TrackPayload()
        {

        }
    }
}
