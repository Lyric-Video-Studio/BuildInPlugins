using System.ComponentModel;


namespace A1111ImgToImgPlugin
{
    public class ConnectionSettings
    {
        private string a1111Url = "http://127.0.0.1:7861";
        private string a1111Executable = "";
        private string a1111Args = "";

        [Description("Path to a1111 api for overriding the default http://127.0.0.1:7861, this is used when creating new a1111 tracks")]
        public string A1111Url { get => a1111Url; set => a1111Url = value; }

        [Description("Full path to a1111 webui-user.bat, recommended to have for automatic startup and --api command line arg input")]
        public string A1111Executable { get => a1111Executable; set => a1111Executable = value; }

        [Description("Extra arguments to pass for a1111 when starting. Note that --api and --nowebui are added automatically")]
        public string A1111Args { get => a1111Args; set => a1111Args = value; }        
    }
}
