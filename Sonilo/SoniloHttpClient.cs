using System.Net;
using System.Net.Http.Headers;

namespace SoniloPlugin;

// Sonilo can return 429 for both concurrency and requests-per-minute limits.
// The normal retry path already respects Retry-After. If an RPM response omits
// that header, synthesize a one-minute delay so we do not burn through retries
// inside the same rate-limit window.
internal sealed class HttpClient : System.Net.Http.HttpClient
{
    public new async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        HttpCompletionOption completionOption,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, completionOption, cancellationToken);

        if (response.StatusCode == HttpStatusCode.TooManyRequests &&
            response.Headers.RetryAfter is null)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (IsRpmLimit(body))
            {
                response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMinutes(1));
            }
        }

        return response;
    }

    private static bool IsRpmLimit(string body)
    {
        return body.Contains("requests per minute", StringComparison.OrdinalIgnoreCase) ||
               body.Contains("request per minute", StringComparison.OrdinalIgnoreCase) ||
               body.Contains("RPM", StringComparison.OrdinalIgnoreCase);
    }
}
