using MistralTxtToImgPlugin;
using PluginBase;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public class MistralImageClient
{
    private readonly HttpClient _httpClient;
    private const string ApiUrl = "https://api.mistral.ai/v1";

    public MistralImageClient(string apiKey)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<Agent> CreateImageGenerationAgentAsync(string model = "mistral-medium-2505")
    {
        var requestBody = new
        {
            model,
            name = "Image Generation Agent",
            description = "Agent used to generate images.",
            instructions = "Use the image generation tool when you have to create images.",
            tools = new[] { new { type = "image_generation" } }
        };

        var jsonRequest = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{ApiUrl}/agents", content);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Agent>(jsonResponse);
    }

    public async Task<ConversationResponse> StartConversationAsync(string agentId, string prompt)
    {
        var requestBody = new
        {
            agent_id = agentId,
            inputs = prompt
        };

        var jsonRequest = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{ApiUrl}/conversations", content);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ConversationResponse>(jsonResponse);
    }

    public async Task<byte[]> DownloadImageAsync(string fileId)
    {
        return await _httpClient.GetByteArrayAsync($"{ApiUrl}/files/{fileId}/content");
    }
}

// --- Data Models using System.Text.Json ---

public record Agent(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("name")] string Name
);

public record ConversationResponse(
    [property: JsonPropertyName("conversation_id")] string ConversationId,
    [property: JsonPropertyName("outputs")] List<OutputEntry> Outputs
);

public record OutputEntry(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("content")] JsonElement Content
);

public record ToolFile(
    [property: JsonPropertyName("file_id")] string FileId,
    [property: JsonPropertyName("file_name")] string FileName
);

public class MistralImages
{
    public static async Task<ImageResponse> CreateImage(string prompt, ConnectionSettings connectionSettings, Action<object> saveConnection)
    {
        // Replace with your actual Mistral API key
        if (string.IsNullOrEmpty(connectionSettings.AccessToken))
        {
            System.Diagnostics.Debug.WriteLine("Please set the MISTRAL_API_KEY environment variable.");
            return new ImageResponse() { Success = false, ErrorMsg = "API key missing" };
        }

        var client = new MistralImageClient(connectionSettings.AccessToken);

        try
        {
            System.Diagnostics.Debug.WriteLine("Creating image generation agent...");
            var agentId = connectionSettings.AgentId;

            if (string.IsNullOrEmpty(agentId))
            {
                var agent = await client.CreateImageGenerationAgentAsync();
                System.Diagnostics.Debug.WriteLine($"Agent created with ID: {agent.Id}");
                agentId = agent.Id;
                connectionSettings.AgentId = agentId;
                saveConnection.Invoke(connectionSettings);
            }

            System.Diagnostics.Debug.WriteLine("Starting conversation to generate an image...");
            var conversationResponse = await client.StartConversationAsync(agentId, prompt);

            var toolFileContent = conversationResponse.Outputs
                .Where(o => o.Type == "message.output")
                .SelectMany(o => o.Content.EnumerateArray())
                .FirstOrDefault(c => c.TryGetProperty("type", out var type) && type.GetString() == "tool_file");

            if (toolFileContent.ValueKind != JsonValueKind.Undefined)
            {
                var fileId = toolFileContent.GetProperty("file_id").GetString();
                System.Diagnostics.Debug.WriteLine($"Image generation complete. File ID: {fileId}");

                System.Diagnostics.Debug.WriteLine("Downloading image...");
                var imageBytes = await client.DownloadImageAsync(fileId);

                return new ImageResponse() { ImageFormat = "png", Image = Convert.ToBase64String(imageBytes), Success = true };
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Could not find a generated file in the conversation response.");
                return new ImageResponse() { Success = false, ErrorMsg = "Could not find a generated file in the conversation response." };
            }
        }
        catch (HttpRequestException e)
        {
            System.Diagnostics.Debug.WriteLine($"Error: {e.Message}");
            return new ImageResponse() { Success = false, ErrorMsg = e.Message };
        }
    }
}