using PluginBase;
using System.Text.Json.Nodes;

namespace MuApiPlugin.Models.GeminiOmni
{
    internal class GeminiOmniCharacterImageHandler
    {
        public static async Task<ImageResponse> GetImage(ConnectionSettings connectionSettings, object trackPayload, object itemsPayload, string model,
            IApiPollingPayload pollingPayload, Action<object> saveConnectionSettings)
        {
            if (connectionSettings == null || string.IsNullOrWhiteSpace(connectionSettings.AccessToken))
            {
                return new ImageResponse() { Success = false, ErrorMsg = "Uninitialized" };
            }

            if (JsonHelper.DeepCopy<GeminiOmniCharacterTrackPayload>(trackPayload) is not GeminiOmniCharacterTrackPayload ||
                JsonHelper.DeepCopy<GeminiOmniCharacterItemPayload>(itemsPayload) is not GeminiOmniCharacterItemPayload typedItemPayload)
            {
                return new ImageResponse() { Success = false, ErrorMsg = "Track payload or item payload object not valid" };
            }

            var imageFiles = typedItemPayload.ImageReferences.ImageSources
                .Select(i => i.ImageFile)
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .ToList();

            if (imageFiles.Count != 1)
            {
                return new ImageResponse() { Success = false, ErrorMsg = "Gemini Omni Character requires exactly one reference image" };
            }

            var resolvedAudioIds = CollectIds(
                new[] { typedItemPayload.AudioId1, typedItemPayload.AudioId2, typedItemPayload.AudioId3 }
                    .Select(connectionSettings.ResolveGeminiOmniAudioId),
                3,
                "audio IDs");

            if (!resolvedAudioIds.Success)
            {
                return new ImageResponse() { Success = false, ErrorMsg = resolvedAudioIds.Error };
            }

            var client = new Client();
            string uploadedImage;
            try
            {
                uploadedImage = await client.UploadFile(imageFiles[0], connectionSettings, MuApiVideoPlugin._cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return new ImageResponse() { Success = false, ErrorMsg = "User cancelled" };
            }
            catch (Exception ex)
            {
                return new ImageResponse() { Success = false, ErrorMsg = ex.Message };
            }

            var request = new CharacterProfileRequest()
            {
                descriptions = typedItemPayload.Descriptions?.Trim(),
                character_name = string.IsNullOrWhiteSpace(typedItemPayload.CharacterName) ? null : typedItemPayload.CharacterName.Trim(),
                images_list = [uploadedImage],
                audio_ids = resolvedAudioIds.Values.Count > 0 ? resolvedAudioIds.Values : null
            };

            var response = await client.GetJsonResult(request, model, connectionSettings, pollingPayload,
                MuApiVideoPlugin._saveAndRefreshCallback, MuApiVideoPlugin._textualProgressAction, MuApiVideoPlugin._cancellationToken);

            if (!response.Success)
            {
                return new ImageResponse() { Success = false, ErrorMsg = response.ErrorMsg };
            }

            var createdCharacterId = ExtractCreatedCharacterId(response.ResultNode);
            if (string.IsNullOrWhiteSpace(createdCharacterId))
            {
                return new ImageResponse() { Success = false, ErrorMsg = "MuApi completed the character profile request, but no character id was returned." };
            }

            var effectiveName = string.IsNullOrWhiteSpace(request.character_name)
                ? createdCharacterId
                : request.character_name;

            connectionSettings.AddOrUpdateGeminiOmniCharacterProfile(effectiveName, createdCharacterId);
            saveConnectionSettings?.Invoke(connectionSettings);

            var absolutePath = WorkspaceSettings.GetAbsolutePath(imageFiles[0]);
            if (string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath))
            {
                return new ImageResponse()
                {
                    Success = true,
                    Params =
                    [
                        ("Character name", effectiveName),
                        ("Character ID", createdCharacterId)
                    ]
                };
            }

            var imageBytes = await File.ReadAllBytesAsync(absolutePath, MuApiVideoPlugin._cancellationToken);
            return new ImageResponse()
            {
                Success = true,
                Image = Convert.ToBase64String(imageBytes),
                ImageFormat = GetImageFormat(absolutePath),
                Params =
                [
                    ("Character name", effectiveName),
                    ("Character ID", createdCharacterId)
                ]
            };
        }

        private static (bool Success, string Error, List<string> Values) CollectIds(IEnumerable<string> ids, int maxCount, string idTypeName)
        {
            var values = ids
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (values.Count > maxCount)
            {
                return (false, $"Gemini Omni Character supports up to {maxCount} {idTypeName}", []);
            }

            return (true, "", values);
        }

        private static string ExtractCreatedCharacterId(JsonNode node)
        {
            return GetString(node, "data", "characterId")
                ?? GetString(node, "data", "character_id")
                ?? GetString(node, "characterId")
                ?? GetString(node, "character_id")
                ?? GetOutputValue(node);
        }

        private static string GetOutputValue(JsonNode node)
        {
            if (node?["outputs"] is not JsonArray outputs)
            {
                return null;
            }

            foreach (var entry in outputs)
            {
                if (entry == null)
                {
                    continue;
                }

                if (entry is JsonValue)
                {
                    var raw = entry.ToString();
                    if (!string.IsNullOrWhiteSpace(raw) && !raw.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        return raw;
                    }
                }

                var nested = GetString(entry, "characterId")
                    ?? GetString(entry, "character_id")
                    ?? GetString(entry, "id");

                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }

            return null;
        }

        private static string GetString(JsonNode node, params string[] path)
        {
            var current = node;
            foreach (var part in path)
            {
                current = current?[part];
                if (current == null)
                {
                    return null;
                }
            }

            return current.ToString();
        }

        private static string GetImageFormat(string path)
        {
            return Path.GetExtension(path).TrimStart('.').ToLowerInvariant() switch
            {
                "png" => "png",
                "webp" => "png",
                _ => "jpg"
            };
        }
    }
}
