using PluginBase;
using System.Text.Json.Nodes;

namespace MuApiPlugin.Models.GeminiOmni
{
    internal class GeminiOmniAudioHandler
    {
        public static async Task<AudioResponse> GetAudio(ConnectionSettings connectionSettings, object trackPayload, object itemsPayload, string model,
            IApiPollingPayload pollingPayload, Action<object> saveConnectionSettings)
        {
            if (connectionSettings == null || string.IsNullOrWhiteSpace(connectionSettings.AccessToken))
            {
                return new AudioResponse() { Success = false, ErrorMsg = "Uninitialized" };
            }

            if (JsonHelper.DeepCopy<GeminiOmniAudioTrackPayload>(trackPayload) is not GeminiOmniAudioTrackPayload typedTrackPayload ||
                JsonHelper.DeepCopy<GeminiOmniAudioItemPayload>(itemsPayload) is not GeminiOmniAudioItemPayload typedItemPayload)
            {
                return new AudioResponse() { Success = false, ErrorMsg = "Track payload or item payload object not valid" };
            }

            var request = new AudioProfileRequest()
            {
                audio_id = typedTrackPayload.PresetVoiceId?.Trim(),
                name = typedItemPayload.Name?.Trim(),
                voice_description = string.IsNullOrWhiteSpace(typedItemPayload.VoiceDescription) ? null : typedItemPayload.VoiceDescription.Trim(),
                example_dialogue = string.IsNullOrWhiteSpace(typedItemPayload.ExampleDialogue) ? null : typedItemPayload.ExampleDialogue.Trim()
            };

            System.Diagnostics.Debug.WriteLine($"VITUN MICROSOFT: {request.audio_id}, {request.name}, {request.voice_description}, {request.example_dialogue}");

            var response = await new Client().GetJsonResult(request, model, connectionSettings, pollingPayload,
                MuApiVideoPlugin._saveAndRefreshCallback, MuApiVideoPlugin._textualProgressAction, MuApiVideoPlugin._cancellationToken);

            if (!response.Success)
            {
                return new AudioResponse() { Success = false, ErrorMsg = response.ErrorMsg };
            }

            var createdAudioId = ExtractCreatedAudioId(response.ResultNode);
            if (string.IsNullOrWhiteSpace(createdAudioId))
            {
                return new AudioResponse() { Success = false, ErrorMsg = "MuApi completed the audio profile request, but no voice id was returned." };
            }

            connectionSettings.AddOrUpdateGeminiOmniAudioProfile(request.name, createdAudioId);
            saveConnectionSettings?.Invoke(connectionSettings);

            if (pollingPayload != null)
            {
                pollingPayload.PollingId = "";
            }

            MuApiVideoPlugin._saveAndRefreshCallback?.Invoke(true);

            return new AudioResponse()
            {
                Success = true,
                Params =
                [
                    ("Voice name", request.name),
                    ("Audio ID", createdAudioId)
                ]
            };
        }

        private static string ExtractCreatedAudioId(JsonNode node)
        {
            return GetString(node, "data", "kieAudioId")
                ?? GetString(node, "data", "audioId")
                ?? GetString(node, "data", "audio_id")
                ?? GetString(node, "kieAudioId")
                ?? GetString(node, "audioId")
                ?? GetString(node, "audio_id")
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

                var nested = GetString(entry, "kieAudioId")
                    ?? GetString(entry, "audioId")
                    ?? GetString(entry, "audio_id")
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
    }
}
