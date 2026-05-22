using PluginBase;
using System.Collections.ObjectModel;

namespace MuApiPlugin
{
    public class AudioIdContainer
    {
        public ObservableCollection<AudioIdItem> AudioIds { get; set; } = new();

        public AudioIdContainer()
        {
            AudioIdItem.RemoveReference += (sender, _) =>
            {
                if (sender is AudioIdItem audioId)
                {
                    AudioIds.Remove(audioId);
                }
            };
        }

        [CustomAction("Add audio id")]
        public void AddAudioId()
        {
            AudioIds.Add(new AudioIdItem());
        }

        public static bool IsAudioIdName(string propertyName)
        {
            return propertyName is nameof(AddAudioId) or nameof(AudioIdItem.AudioId) or nameof(AudioIdItem.RemoveAudioId);
        }
    }

    public class CharacterIdContainer
    {
        public ObservableCollection<CharacterIdItem> CharacterIds { get; set; } = new();

        public CharacterIdContainer()
        {
            CharacterIdItem.RemoveReference += (sender, _) =>
            {
                if (sender is CharacterIdItem characterId)
                {
                    CharacterIds.Remove(characterId);
                }
            };
        }

        [CustomAction("Add character id")]
        public void AddCharacterId()
        {
            CharacterIds.Add(new CharacterIdItem());
        }

        public static bool IsCharacterIdName(string propertyName)
        {
            return propertyName is nameof(AddCharacterId) or nameof(CharacterIdItem.CharacterId) or nameof(CharacterIdItem.RemoveCharacterId);
        }
    }

    public class AudioIdItem
    {
        public static event EventHandler RemoveReference;

        [EditorWidth(260)]
        public string AudioId { get; set; }

        [CustomAction("Remove", false, nameof(AudioId))]
        public void RemoveAudioId()
        {
            RemoveReference?.Invoke(this, EventArgs.Empty);
        }
    }

    public class CharacterIdItem
    {
        public static event EventHandler RemoveReference;

        [EditorWidth(260)]
        public string CharacterId { get; set; }

        [CustomAction("Remove", false, nameof(CharacterId))]
        public void RemoveCharacterId()
        {
            RemoveReference?.Invoke(this, EventArgs.Empty);
        }
    }
}
