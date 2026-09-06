using PluginBase;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SoniloPlugin;

public class ConnectionSettings : IJsonOnDeserialized, IJsonOnSerialized, IJsonOnSerializing, IAllowMcpGeneration
{
    private const string ApiKeyStorageKey = "SoniloPlugin.apiKey";

    private string _url = "https://api.sonilo.com/v1";
    private string _apiKey = "";

    [Description("Sonilo API base URL. The documentation site is not an API endpoint.")]
    public string Url
    {
        get => _url;
        set => _url = string.IsNullOrWhiteSpace(value) ? "https://api.sonilo.com/v1" : value.Trim();
    }

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
