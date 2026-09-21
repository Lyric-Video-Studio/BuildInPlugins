using PluginBase;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SoniloPlugin;

public class ConnectionSettings : IJsonOnDeserialized, IJsonOnSerialized, IJsonOnSerializing, IAllowMcpGeneration
{
    private const string ApiKeyStorageKey = "SoniloPlugin.apiKey";

    private string _apiKey = "";

    [JsonIgnore]
    public string Url => "https://api.sonilo.com/v1";

    [Description("Sonilo API key from https://platform.sonilo.com/. Generations use Sonilo credits. Lyric Video Studio cannot refund credits used by the provider.")]
    [EditorWidth(320)]
    [MaskInput]
    public string ApiKey
    {
        get => _apiKey;
        set => _apiKey = value ?? "";
    }

    public bool AllowMcpAccess { get; set; } = false;

    public void OnDeserialized()
    {
        try
        {
            ApiKey = SecureStorageWrapper.SecStorage?.GetKey(ApiKeyStorageKey) ?? "";
        }
        catch
        {
            ApiKey = "";
        }
    }

    public void OnSerializing()
    {
        if (!string.IsNullOrWhiteSpace(ApiKey))
        {
            SecureStorageWrapper.SecStorage?.SetKey(ApiKeyStorageKey, ApiKey);
            ApiKey = "";
        }
    }

    public void OnSerialized()
    {
        // Restore the secret into the in-memory settings object after serialization.
        OnDeserialized();
    }

    internal void DeleteTokens()
    {
        try
        {
            SecureStorageWrapper.SecStorage?.DeleteKey(ApiKeyStorageKey);
        }
        catch
        {
            // User-data deletion should remain best-effort if secure storage is unavailable.
        }

        ApiKey = "";
    }
}
