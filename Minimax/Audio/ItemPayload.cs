using PluginBase;

namespace MinimaxPlugin.Audio
{
    public class ItemPayload : IPayloadPropertyVisibility
    {
        public string Text { get; set; }

        public string Prompt { get; set; }
        public string Lyrics { get; set; }

        public bool ShouldPropertyBeVisible(string propertyName, object trackPayload, object itemPayload)
        {
            if (trackPayload is T2ARequest tp)
            {
                if (propertyName == nameof(ItemPayload.Text))
                {
                    return tp.Model != MusicRequest.MusicModel;
                }
                else if (propertyName == nameof(ItemPayload.Prompt) || propertyName == nameof(ItemPayload.Lyrics))
                {
                    return tp.Model == MusicRequest.MusicModel;
                }
            }

            return true;
        }
    }
}