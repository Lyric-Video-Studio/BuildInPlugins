namespace LumaAiDreamMachinePlugin
{
    public class LumaAgentsVideoRequest
    {
        public string model { get; set; } = "ray-3.2";
        public string type { get; set; } = "video";
        public string prompt { get; set; } = "";
        public string aspect_ratio { get; set; }
        public LumaAgentsMediaReference source { get; set; }
        public LumaAgentsVideoOptions video { get; set; } = new LumaAgentsVideoOptions();
    }

    public class LumaAgentsVideoOptions
    {
        public string resolution { get; set; }
        public string duration { get; set; }
        public bool? loop { get; set; }
        public bool? hdr { get; set; }
        public bool? exr_export { get; set; }
        public LumaAgentsMediaReference start_frame { get; set; }
        public LumaAgentsMediaReference end_frame { get; set; }
        public LumaAgentsMediaReference[] keyframes { get; set; }
        public int[] keyframe_indexes { get; set; }
        public LumaAgentsVideoEditOptions edit { get; set; }
        public LumaAgentsVideoSourcePosition source_position { get; set; }
    }

    public class LumaAgentsVideoEditOptions
    {
        public string strength { get; set; }
        public bool? auto_controls { get; set; }
        public LumaAgentsMediaReference[] keyframes { get; set; }
        public int[] keyframe_indexes { get; set; }
    }

    public class LumaAgentsVideoSourcePosition
    {
        public double x_norm { get; set; }
        public double y_norm { get; set; }
        public double w_norm { get; set; }
        public double h_norm { get; set; }
    }
}
