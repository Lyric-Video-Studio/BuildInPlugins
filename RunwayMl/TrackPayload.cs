﻿using PluginBase;
using System.ComponentModel;

namespace RunwayMlPlugin
{
    public class TrackPayload : IPayloadPropertyVisibility
    {
        private Request request = new Request();

        [IgnorePropertyName]
        public Request Request { get => request; set => request = value; }

        [Description("Used with Act2. Image or video required. If video is selected, it will be used, not image")]
        public string ReferenceImage { get; set; }

        [Description("Used with Act2. Image or video required. If video is selected, it will be used, not image")]
        public string ReferenceVideo { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is TrackPayload tp)
            {
                if (propertyName == nameof(ReferenceImage) || propertyName == nameof(ReferenceVideo))
                {
                    return tp.Request.model == "act_two";
                }

                if (propertyName == nameof(Request.duration) || propertyName == nameof(Request.promptText))
                {
                    return tp.Request.model != "act_two" && tp.Request.model != "upscale_v1";
                }
            }

            return true;
        }
    }
}