namespace SoniloPlugin;

internal static class RetryPolicy
{
    internal static async Task<TimeSpan> GetDelayAsync(HttpResponseMessage response, int attempt, CancellationToken cancellationToken)
    {
        if (response.Headers.RetryAfter?.Delta is TimeSpan delta && delta > TimeSpan.Zero)
        {
            return delta;
        }

        if (response.Headers.RetryAfter?.Date is DateTimeOffset date)
        {
            var until = date - DateTimeOffset.UtcNow;
            if (until > TimeSpan.Zero)
            {
                return until;
            }
        }

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (body.Contains("requests per minute", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("request per minute", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("RPM", StringComparison.OrdinalIgnoreCase))
            {
                return TimeSpan.FromMinutes(1);
            }
        }

        return TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 30));
    }
}
