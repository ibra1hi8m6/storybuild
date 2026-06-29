using System.Net;
using Microsoft.Extensions.Logging;

namespace Infrastructure.AI;

/// <summary>
/// DelegatingHandler that transparently rotates Gemini API keys on 429 / RESOURCE_EXHAUSTED.
/// Attach to every HttpClient that talks to the Gemini API.
/// </summary>
public sealed class GeminiKeyRotatingHandler(
    GeminiKeyStore keyStore,
    ILogger<GeminiKeyRotatingHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < Math.Max(1, keyStore.Count); attempt++)
        {
            var uri   = ReplaceKey(request.RequestUri!, keyStore.CurrentKey);
            var clone = await CloneAsync(request, uri);

            var response = await base.SendAsync(clone, cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                logger.LogWarning(
                    "[Gemini] 429 on attempt {A}/{T} — rotating key.",
                    attempt + 1, keyStore.Count);
                keyStore.Rotate();
                continue;
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (body.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning(
                        "[Gemini] RESOURCE_EXHAUSTED on attempt {A}/{T} — rotating key.",
                        attempt + 1, keyStore.Count);
                    keyStore.Rotate();
                    continue;
                }
                // Real 400 — rebuild content (already consumed) and return
                response.Content = new StringContent(body,
                    System.Text.Encoding.UTF8, "application/json");
                return response;
            }

            return response;
        }

        logger.LogError("[Gemini] All {Count} API key(s) exhausted.", keyStore.Count);
        return new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("{\"error\":\"All Gemini API keys exhausted\"}",
                System.Text.Encoding.UTF8, "application/json")
        };
    }

    private static Uri ReplaceKey(Uri original, string key)
    {
        var ub    = new UriBuilder(original);
        var query = ub.Query.TrimStart('?');

        var parts = query
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !p.StartsWith("key=", StringComparison.OrdinalIgnoreCase))
            .ToList();

        parts.Add($"key={Uri.EscapeDataString(key)}");
        ub.Query = string.Join("&", parts);
        return ub.Uri;
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage original, Uri newUri)
    {
        var clone = new HttpRequestMessage(original.Method, newUri);

        foreach (var h in original.Headers)
            clone.Headers.TryAddWithoutValidation(h.Key, h.Value);

        if (original.Content is not null)
        {
            var bytes = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(bytes);
            foreach (var h in original.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }

        return clone;
    }
}
