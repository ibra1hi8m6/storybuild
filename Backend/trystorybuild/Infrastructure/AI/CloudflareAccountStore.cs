using Microsoft.Extensions.Configuration;

namespace Infrastructure.AI;

public sealed record CloudflareAccount(string AccountId, string ApiToken);

public sealed class CloudflareAccountStore
{
    private readonly CloudflareAccount[] _accounts;
    private int _currentIndex;

    public CloudflareAccountStore(IConfiguration configuration)
    {
        var section = configuration.GetSection("Cloudflare:Accounts");
        _accounts = section.Get<CloudflareAccount[]>() ?? [];

        // Fallback: single account for backwards compatibility
        if (_accounts.Length == 0)
        {
            var id    = configuration["Cloudflare:AccountId"];
            var token = configuration["Cloudflare:ApiToken"];
            if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(token))
                _accounts = [new CloudflareAccount(id, token)];
        }
    }

    public CloudflareAccount? CurrentAccount =>
        _accounts.Length > 0 ? _accounts[_currentIndex] : null;

    public int Count => _accounts.Length;

    /// <summary>Rotates to the next account. Returns false if only one account exists.</summary>
    public bool Rotate()
    {
        if (_accounts.Length <= 1) return false;
        int current, next;
        do
        {
            current = _currentIndex;
            next    = (current + 1) % _accounts.Length;
        }
        while (Interlocked.CompareExchange(ref _currentIndex, next, current) != current);
        return true;
    }
}
