using Microsoft.IdentityModel.Tokens;
using PluginBase;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoogleVeoPlugin
{
    // --- Conceptual Data Models (Same as before - depends on *actual* API) ---

    public class Veo2GenerationRequest
    {
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }

        [JsonPropertyName("negativePrompt")]
        public string NegativePrompt { get; set; }

        [Range(5, 8)]
        [JsonPropertyName("durationSeconds")]
        public int DurationSeconds { get; set; } = 5;

        [JsonPropertyName("aspectRatio")]
        public string AspectRatio { get; set; }

        [JsonPropertyName("enhance_prompt")]
        public bool EnhancePrompt { get; set; }
    }

    public class Veo2JobInitiationResponse
    {
        public string JobId { get; set; }
        public string Status { get; set; }
    }

    public class Veo2JobStatusResponse
    {
        public string JobId { get; set; }
        public string Status { get; set; } // e.g., "PROCESSING", "COMPLETED", "FAILED"
        public float? ProgressPercent { get; set; }
        public string VideoUrl { get; set; } // Available when "COMPLETED"
        public string ErrorMessage { get; set; } // Available when "FAILED"
    }

    /// <summary>
    /// Represents the root object for the Veo 2 prediction request body.
    /// Matches the structure sent to the :predictLongRunning endpoint.
    /// </summary>
    public class VeoPredictionRequest
    {
        /// <summary>
        /// A list containing the specific prompts and potentially other
        /// instance-specific details for generation. In this case, just the prompt.
        /// </summary>
        [JsonPropertyName("instances")]
        public List<VeoInstance> Instances { get; set; }

        /// <summary>
        /// Parameters controlling the overall video generation process.
        /// </summary>
        [JsonPropertyName("parameters")]
        public VeoParameters Parameters { get; set; }

        // Optional: Add a constructor for easier initialization
        public VeoPredictionRequest()
        {
            Instances = new List<VeoInstance>();
            Parameters = new VeoParameters();
        }
    }

    /// <summary>
    /// Represents a single instance within the prediction request,
    /// typically containing the core input like the text prompt.
    /// </summary>
    public class VeoInstance
    {
        /// <summary>
        /// The text prompt describing the video to be generated.
        /// Example: "Panning wide shot of a calico kitten sleeping in the sunshine"
        /// </summary>
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }

        // Note: Add other potential instance properties here if the API supports them
        // (e.g., negative_prompt, seed, input_image_bytes) based on actual API docs.
    }

    /// <summary>
    /// Represents the generation parameters applicable to all instances
    /// in the prediction request.
    /// </summary>
    public class VeoParameters
    {
        /// <summary>
        /// The desired aspect ratio for the generated video.
        /// Example: "16:9", "9:16", "1:1"
        /// </summary>
        [JsonPropertyName("aspectRatio")]
        public string AspectRatio { get; set; }

        /// <summary>
        /// Controls whether realistic human faces can be generated.
        /// Example: "dont_allow", "allow" (check API docs for exact values)
        /// </summary>
        [JsonPropertyName("personGeneration")]
        public string PersonGeneration { get; set; }

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }

        [JsonPropertyName("negativePrompt")]
        public string NegativePrompt { get; set; }

        [Range(5, 8)]
        [JsonPropertyName("durationSeconds")]
        public int DurationSeconds { get; set; }

        [JsonPropertyName("enhance_prompt")]
        public bool EnhancePrompt { get; set; } = true;
    }

    // --- API Client Class ---

    public class Veo2ApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl; // The base URL for the Veo 2 API
        private readonly string _apiKey;     // Store securely!

        // Single, reusable HttpClient instance
        private static readonly HttpClient _sharedHttpClient = CreateHttpClient();

        public Veo2ApiClient(string apiBaseUrl, string apiKey)
        {
            // --- IMPORTANT: Replace these with actual config values when available ---
            _apiBaseUrl = apiBaseUrl ?? "https://hypothetical-veo2-api.google.com/v1/"; // Placeholder!
            _apiKey = apiKey; // Placeholder! Load from secure config

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                Debug.WriteLine("WARNING: Veo 2 API Key is not configured.");
                // Consider throwing an exception or handling this state appropriately
            }
            if (!Uri.TryCreate(_apiBaseUrl, UriKind.Absolute, out _))
            {
                throw new ArgumentException("Invalid Base URL provided.", nameof(apiBaseUrl));
            }

            // Use the shared instance
            _httpClient = _sharedHttpClient;

            // Configure HttpClient instance (only needs to be done once for the shared client)
            // Headers specific to *this* service can be added per-request or if the
            // entire HttpClient instance is dedicated to this service.
            // If _sharedHttpClient is truly shared across different services,
            // put common headers (like Accept) there, and service-specific ones
            // (like Authorization/X-Api-Key) in the request methods or temporarily
            // on DefaultRequestHeaders if you ensure thread safety or exclusive use.

            // For simplicity here, we assume this client class "owns" the configuration
            // on the shared instance for the duration of its use.
            _httpClient.BaseAddress = new Uri(_apiBaseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Remove("X-Api-Key"); // Ensure clean slate if reused
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _apiKey); // Replace with actual auth header/method
            }
        }

        private static HttpClient CreateHttpClient()
        {
            // Configure shared HttpClient settings here (timeouts, etc.)
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5) // Example timeout
            };
            // Don't set BaseAddress or auth headers here if truly shared among different APIs
            return client;
        }

        /// <summary>
        /// Initiates a video generation job with Veo 2.
        /// </summary>
        /// <returns>The initial job response containing the Job ID.</returns>
        public async Task<Veo2JobInitiationResponse?> InitiateVideoGenerationAsync(Veo2GenerationRequest request, string accesskey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                Debug.WriteLine("ERROR: Cannot initiate Veo 2 generation: API Key is missing.");
                return null; // Or throw
            }

            // --- Endpoint: Replace with actual endpoint ---
            string endpoint = $"v1beta/models/veo-2.0-generate-001:predictLongRunning?key={accesskey}";

            try
            {
                Debug.WriteLine($"Initiating Veo 2 video generation for prompt: {request.Prompt}");

                // Add specific auth header for this request if HttpClient is shared broadly
                // var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);
                // requestMessage.Headers.Add("X-Api-Key", _apiKey); // Add specific header
                // requestMessage.Content = JsonContent.Create(request);
                // HttpResponseMessage response = await _httpClient.SendAsync(requestMessage, cancellationToken);

                // Or if DefaultRequestHeaders are managed by this class instance:

                var serialized = "";

                var actualRequest = new VeoPredictionRequest();
                actualRequest.Instances = new List<VeoInstance>();

                var inst = new VeoInstance() { Prompt = request.Prompt };
                actualRequest.Instances.Add(inst);
                actualRequest.Parameters.AspectRatio = request.AspectRatio.ToString();
                actualRequest.Parameters.EnhancePrompt = request.EnhancePrompt;
                actualRequest.Parameters.Prompt = request.Prompt;
                actualRequest.Parameters.DurationSeconds = request.DurationSeconds;
                actualRequest.Parameters.NegativePrompt = request.NegativePrompt;

                try
                {
                    serialized = JsonHelper.Serialize(actualRequest);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    throw;
                }

                var stringContent = new StringContent(serialized);
                stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                HttpResponseMessage response = await _httpClient.PostAsync(endpoint, stringContent, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    Debug.WriteLine($"ERROR: Failed to initiate Veo 2 job. Status: {response.StatusCode}, Reason: {response.ReasonPhrase}, Content: {errorContent}");
                    // Consider throwing a custom exception
                    response.EnsureSuccessStatusCode(); // Throws HttpRequestException for bad status codes
                }

                Veo2JobInitiationResponse? jobResponse = await response.Content.ReadFromJsonAsync<Veo2JobInitiationResponse>(cancellationToken: cancellationToken);
                Debug.WriteLine($"Veo 2 job initiated successfully. Job ID: {jobResponse?.JobId}");
                return jobResponse;
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"ERROR: HTTP request error while initiating Veo 2 job: {ex.Message} (StatusCode: {ex.StatusCode})");
                throw; // Re-throw or handle as appropriate
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"ERROR: Error deserializing Veo 2 initiation response: {ex.Message}");
                throw;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Veo 2 job initiation cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: An unexpected error occurred during Veo 2 job initiation: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Gets the status of a specific video generation job.
        /// </summary>
        public async Task<Veo2JobStatusResponse?> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentNullException(nameof(jobId));
            }
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                Debug.WriteLine("ERROR: Cannot get Veo 2 job status: API Key is missing.");
                return null; // Or throw
            }

            // --- Endpoint: Replace with actual endpoint ---
            string endpoint = $"videos/jobs/{jobId}"; // Hypothetical endpoint

            try
            {
                Debug.WriteLine($"Checking status for Veo 2 job ID: {jobId}");

                // Similar check for auth header if needed for shared HttpClient
                // var requestMessage = new HttpRequestMessage(HttpMethod.Get, endpoint);
                // requestMessage.Headers.Add("X-Api-Key", _apiKey);
                // HttpResponseMessage response = await _httpClient.SendAsync(requestMessage, cancellationToken);
                // response.EnsureSuccessStatusCode();
                // return await response.Content.ReadFromJsonAsync<Veo2JobStatusResponse>(cancellationToken: cancellationToken);

                // Using helper if DefaultRequestHeaders are set:
                Veo2JobStatusResponse? statusResponse = await _httpClient.GetFromJsonAsync<Veo2JobStatusResponse>(endpoint, cancellationToken);
                Debug.WriteLine($"Status for job {jobId}: {statusResponse?.Status}, Progress: {statusResponse?.ProgressPercent}%");
                return statusResponse;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Debug.WriteLine($"WARNING: Veo 2 job ID {jobId} not found.");
                return null; // Job doesn't exist (or endpoint is wrong)
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"ERROR: HTTP request error while getting status for Veo 2 job {jobId}: {ex.Message} (StatusCode: {ex.StatusCode})");
                throw;
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"ERROR: Error deserializing Veo 2 status response for job {jobId}: {ex.Message}");
                throw;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"Veo 2 job status check cancelled for job {jobId}.");
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: An unexpected error occurred while getting status for Veo 2 job {jobId}: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Polls the job status until it completes or fails, or timeout occurs.
        /// Uses a callback to report progress without tying to a specific UI framework.
        /// </summary>
        public async Task<Veo2JobStatusResponse?> PollUntilCompletionAsync(
            string jobId,
            Action<Veo2JobStatusResponse>? progressCallback = null, // Action to report progress
            TimeSpan? pollInterval = null,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            pollInterval ??= TimeSpan.FromSeconds(10); // Default poll interval
            timeout ??= TimeSpan.FromMinutes(15);     // Default timeout

            DateTime startTime = DateTime.UtcNow;
            Veo2JobStatusResponse? currentStatus = null;

            Debug.WriteLine($"Polling status for Veo 2 job {jobId}...");

            try
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                linkedCts.CancelAfter(timeout.Value); // Apply overall timeout

                while (!linkedCts.Token.IsCancellationRequested) // Check combined token
                {
                    try
                    {
                        currentStatus = await GetJobStatusAsync(jobId, linkedCts.Token); // Use linked token
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        Debug.WriteLine($"ERROR: Failed to get job status during polling for {jobId}: {ex.Message}. Retrying...");
                        // Optional: Implement retry logic specific to polling failures before delaying
                    }

                    if (currentStatus != null)
                    {
                        // IMPORTANT: If progressCallback updates UI, it MUST marshal the call
                        // to the UI thread (e.g., Dispatcher.Invoke, Control.Invoke).
                        progressCallback?.Invoke(currentStatus);

                        if (currentStatus.Status == "COMPLETED" || currentStatus.Status == "FAILED")
                        {
                            Debug.WriteLine($"Polling finished for job {jobId}. Final Status: {currentStatus.Status}");
                            return currentStatus;
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Polling for job {jobId}: Status check returned null or failed. Will retry.");
                    }

                    // Wait before the next poll, honouring cancellation
                    await Task.Delay(pollInterval.Value, linkedCts.Token);
                }

                // If loop exits, check cancellation reason
                cancellationToken.ThrowIfCancellationRequested(); // Throws if original token was cancelled
                throw new TimeoutException($"Polling timed out for job {jobId} after {timeout.Value}. Last known status: {currentStatus?.Status ?? "Unknown"}");
            }
            catch (OperationCanceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Debug.WriteLine($"Polling cancelled by user request for job {jobId}.");
                    // Rethrow or return last status depending on desired behaviour
                    // throw;
                    return currentStatus;
                }
                else
                {
                    Debug.WriteLine($"Polling timed out for job {jobId}. Last known status: {currentStatus?.Status ?? "Unknown"}");
                    // Potentially throw TimeoutException explicitly if preferred over TaskCanceledException
                    throw new TimeoutException($"Polling timed out for job {jobId} after {timeout.Value}. Last known status: {currentStatus?.Status ?? "Unknown"}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: Unexpected error during polling loop for job {jobId}: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Optional: Downloads the video from a given URL.
        /// Uses the shared HttpClient if the download URL requires the same authentication,
        /// otherwise consider creating a temporary HttpClient.
        /// </summary>
        public async Task<byte[]?> DownloadVideoAsync(string videoUrl, CancellationToken cancellationToken = default)
        {
            if (!Uri.TryCreate(videoUrl, UriKind.Absolute, out var uri))
            {
                Debug.WriteLine($"ERROR: Invalid video URL provided: {videoUrl}");
                return null;
            }

            try
            {
                Debug.WriteLine($"Downloading video from {videoUrl}");
                // Decide if the shared client (_httpClient) with its auth headers is appropriate,
                // or if a new, clean HttpClient is needed (e.g., for public S3/GCS signed URLs).
                // Using the shared one here for simplicity, assuming it might need the key.
                HttpResponseMessage response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken); // Read headers first
                response.EnsureSuccessStatusCode();

                using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var memoryStream = new MemoryStream();
                await contentStream.CopyToAsync(memoryStream, cancellationToken);

                byte[] videoData = memoryStream.ToArray();
                Debug.WriteLine($"Video downloaded successfully ({videoData.Length} bytes)");
                return videoData;
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"ERROR: Failed to download video from {videoUrl}: {ex.Message} (StatusCode: {ex.StatusCode})");
                return null;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Video download cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: An unexpected error occurred during video download from {videoUrl}: {ex}");
                throw;
            }
        }

        // Dispose the HttpClient if this class instance manages its lifecycle uniquely,
        // BUT if using a truly static shared HttpClient, disposal should happen
        // at application exit, not here. For this example, we assume the client
        // might be disposed if the Veo2ApiClient instance is disposed.
        // A better approach for truly shared clients is often *not* to dispose them until app shutdown.
        public void Dispose()
        {
            // Only dispose if necessary. If _sharedHttpClient is truly static and shared,
            // you might not want to dispose it here. Let's assume for this pattern
            // that disposal of Veo2ApiClient means we are done with it.
            // _httpClient?.Dispose(); // Potentially dispose the shared client - use with caution.
            GC.SuppressFinalize(this);
        }
    }
}