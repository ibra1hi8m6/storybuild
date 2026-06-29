using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Infrastructure.AI;

/// <summary>
/// DelegatingHandler that transparently rotates Cloudflare accounts on 429.
/// Replaces the accountId in the URL path and the Authorization header on every request.
/// </summary>
public sealed partial class CloudflareRotatingHandler(
    CloudflareAccountStore accountStore,
    ILogger<CloudflareRotatingHandler> logger) : DelegatingHandler
{
    [GeneratedRegex(@"/accounts/[a-f0-9]{32}/", RegexOptions.IgnoreCase)]
    private static partial Regex AccountIdPattern();

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < Math.Max(1, accountStore.Count); attempt++)
        {
            var account = accountStore.CurrentAccount;
            if (account is null)
            {
                logger.LogError("[Cloudflare] No accounts configured.");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }

            var uri   = ReplaceAccountId(request.RequestUri!, account.AccountId);
            var clone = await CloneAsync(request, uri, account.ApiToken);

            var response = await base.SendAsync(clone, cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                logger.LogWarning(
                    "[Cloudflare] 429 on account {Id} (attempt {A}/{T}) — rotating.",
                    account.AccountId[..8], attempt + 1, accountStore.Count);
                accountStore.Rotate();
                continue;
            }

            return response;
        }

        logger.LogError("[Cloudflare] All {Count} account(s) exhausted.", accountStore.Count);
        return new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("{\"error\":\"All Cloudflare accounts exhausted\"}",
                System.Text.Encoding.UTF8, "application/json")
        };
    }

    private static Uri ReplaceAccountId(Uri original, string accountId)
    {
        var replaced = AccountIdPattern().Replace(
            original.ToString(),
            $"/accounts/{accountId}/");
        return new Uri(replaced);
    }

    private static async Task<HttpRequestMessage> CloneAsync(
        HttpRequestMessage original, Uri newUri, string apiToken)
    {
        var clone = new HttpRequestMessage(original.Method, newUri);

        foreach (var h in original.Headers)
        {
            if (h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                continue; // will be replaced below
            clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }

        clone.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

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
