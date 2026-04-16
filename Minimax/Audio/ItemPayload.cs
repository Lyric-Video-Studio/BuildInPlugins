using PluginBase;

namespace MinimaxPlugin.Audio
{
    public class ItemPayload : IPayloadPropertyVisibility
    {
        public string Text { get; set; }
        public string Prompt { get; set; }

        [ParentName("")]
        public MusicRequest MusicReq { get; set; } = new MusicRequest();

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is T2ARequest tp)
            {
                if (propertyName is nameof(ItemPayload.Text))
                {
                    return !MusicRequest.IsMusicModel(tp.Model);
                }
                else if (propertyName == nameof(ItemPayload.Prompt) || propertyName is nameof(MusicRequest.Lyrics) 
                    or nameof(MusicRequest.IsInstrumental) 
                    or nameof(MusicRequest.LyricsOptimizer) or nameof(MusicRequest.AudioSetting))
                {
                    return MusicRequest.IsMusicModel(tp.Model);
                } 
                else if (propertyName == nameof(MusicRequest.Audio))
                {
                    return tp.Model != null && tp.Model.Contains("cover");
                }
            }

            return true;
        }
    }
}