using PluginBase;

namespace LTXPlugin
{
    public class ItemPayload : IPayloadPropertyVisibility
    {
        public static string[] MotionTypes => ["none", "dolly_in", "dolly_out", "dolly_left", "dolly_right", "jib_up", "jib_down", "static", "focus_shift"];
        public static string[] DurationFastTypes => ["6", "8", "10", "12", "14", "16", "18", "20"];
        public static string[] DurationTypes => DurationFastTypes.Take(3).ToArray();

        public string Prompt { get; set; }

        [EnableFileDrop]
        public string ImageSource { get; set; }

        [EnableFileDrop]
        public string AudioSource { get; set; }

        [CustomName("Duration")]
        public int DurationFast25 { get; set; } = 0;

        public int Duration { get; set; } = 0;

        public bool GenerateAudio { get; set; }

        public int CameraMotion { get; set; } = 0;

        public ItemPayload()
        {
            
        }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (propertyName == nameof(DurationFast25) && trackPayload is TrackPayload tp)
            {
                return tp.Model == TrackPayload.FastModel;
            }

            if (propertyName == nameof(Duration) && trackPayload is TrackPayload tp2)
            {
                return tp2.Model != TrackPayload.FastModel;
            }

            if (trackPayload is TrackPayload tp3 && itemPayload is ItemPayload ip3 && (!string.IsNullOrEmpty(tp3.AudioSource) || !string.IsNullOrEmpty(ip3.AudioSource)))
            {
                return propertyName != nameof(Duration) || propertyName != nameof(DurationFast25);
            }

            return true;
        }
    }
}