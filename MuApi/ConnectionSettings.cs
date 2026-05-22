using System.ComponentModel;
using PluginBase;
using System.Text.Json.Serialization;

namespace MuApiPlugin
{
    public class ConnectionSettings : IJsonOnDeserialized, IJsonOnSerialized, IJsonOnSerializing
    {
        private const string AccessTokenKey = "MuApiPlugin.accessKey";
        private string url = "https://api.muapi.ai/api/v1/";
        private string accessToken;

        [Description("MuApi API base url. Leave this as default unless MuApi changes their API host.")]
        public string Url { get => url; set => url = value; }

        [Description("MuApi API key. Uploads are free, but MuApi still requires a valid credit-enabled key to use upload and generation endpoints.")]
        [EditorWidth(320)]
        [MaskInput]
        public string AccessToken { get => accessToken; set => accessToken = value; }

        [IgnoreDynamicEdit]
        public List<GeminiOmniAudioProfileInfo> GeminiOmniAudioProfiles { get; set; } = new();

        [IgnoreDynamicEdit]
        public List<GeminiOmniCharacterProfileInfo> GeminiOmniCharacterProfiles { get; set; } = new();

        public void OnDeserialized()
        {
            try
            {
                AccessToken = SecureStorageWrapper.SecStorage.GetKey(AccessTokenKey);
            }
            catch (Exception)
            {
                AccessToken = "";
            }
        }

        public void OnSerialized()
        {
            OnDeserialized();
        }

        public void OnSerializing()
        {
            if (!string.IsNullOrEmpty(AccessToken))
            {
                SecureStorageWrapper.SecStorage.SetKey(AccessTokenKey, AccessToken);
            }
        }

        internal void DeleteTokens()
        {
            SecureStorageWrapper.SecStorage.DeleteKey(AccessTokenKey);
        }

        public void AddOrUpdateGeminiOmniAudioProfile(string name, string audioId)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(audioId))
            {
                return;
            }

            GeminiOmniAudioProfiles.RemoveAll(profile =>
                string.Equals(profile.Name, name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(profile.AudioId, audioId, StringComparison.OrdinalIgnoreCase));

            GeminiOmniAudioProfiles.Add(new GeminiOmniAudioProfileInfo()
            {
                Name = name.Trim(),
                AudioId = audioId.Trim()
            });
        }

        public string ResolveGeminiOmniAudioId(string nameOrId)
        {
            if (string.IsNullOrWhiteSpace(nameOrId))
            {
                return null;
            }

            var trimmed = nameOrId.Trim();
            if (string.Equals(trimmed, GeminiOmniProfileOptions.None, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var match = GeminiOmniAudioProfiles.FirstOrDefault(profile =>
                string.Equals(profile.Name, trimmed, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(profile.AudioId, trimmed, StringComparison.OrdinalIgnoreCase));

            return string.IsNullOrWhiteSpace(match?.AudioId) ? trimmed : match.AudioId;
        }

        public string[] GetGeminiOmniAudioProfileNames()
        {
            return GeminiOmniAudioProfiles
                .Where(profile => !string.IsNullOrWhiteSpace(profile.Name) && !string.IsNullOrWhiteSpace(profile.AudioId))
                .OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)
                .Select(profile => profile.Name)
                .ToArray();
        }

        public void AddOrUpdateGeminiOmniCharacterProfile(string name, string characterId)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(characterId))
            {
                return;
            }

            GeminiOmniCharacterProfiles.RemoveAll(profile =>
                string.Equals(profile.Name, name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(profile.CharacterId, characterId, StringComparison.OrdinalIgnoreCase));

            GeminiOmniCharacterProfiles.Add(new GeminiOmniCharacterProfileInfo()
            {
                Name = name.Trim(),
                CharacterId = characterId.Trim()
            });
        }

        public string ResolveGeminiOmniCharacterId(string nameOrId)
        {
            if (string.IsNullOrWhiteSpace(nameOrId))
            {
                return null;
            }

            var trimmed = nameOrId.Trim();
            if (string.Equals(trimmed, GeminiOmniProfileOptions.None, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var match = GeminiOmniCharacterProfiles.FirstOrDefault(profile =>
                string.Equals(profile.Name, trimmed, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(profile.CharacterId, trimmed, StringComparison.OrdinalIgnoreCase));

            return string.IsNullOrWhiteSpace(match?.CharacterId) ? trimmed : match.CharacterId;
        }

        public string[] GetGeminiOmniCharacterProfileNames()
        {
            return GeminiOmniCharacterProfiles
                .Where(profile => !string.IsNullOrWhiteSpace(profile.Name) && !string.IsNullOrWhiteSpace(profile.CharacterId))
                .OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)
                .Select(profile => profile.Name)
                .ToArray();
        }
    }

    public class GeminiOmniAudioProfileInfo
    {
        public string Name { get; set; }

        public string AudioId { get; set; }
    }

    public class GeminiOmniCharacterProfileInfo
    {
        public string Name { get; set; }

        public string CharacterId { get; set; }
    }

    public static class GeminiOmniProfileOptions
    {
        public const string None = "None";
    }
}
