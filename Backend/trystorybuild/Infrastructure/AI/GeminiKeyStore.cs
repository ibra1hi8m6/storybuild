using Microsoft.Extensions.Configuration;

namespace Infrastructure.AI;

public sealed class GeminiKeyStore
{
    private readonly string[] _keys;
    private int _currentIndex;

    public GeminiKeyStore(IConfiguration configuration)
    {
        _keys = configuration.GetSection("Gemini:ApiKeys").Get<string[]>() ?? [];

        // Fallback: single key for backwards compatibility
        if (_keys.Length == 0)
        {
            var single = configuration["Gemini:ApiKey"];
            if (!string.IsNullOrWhiteSpace(single))
                _keys = [single];
        }
    }

    public string CurrentKey =>
        _keys.Length > 0 ? _keys[_currentIndex] : string.Empty;

    public int Count => _keys.Length;

    /// <summary>Rotates to the next key. Returns false if only one key exists.</summary>
    public bool Rotate()
    {
        if (_keys.Length <= 1) return false;
        int current, next;
        do
        {
            current = _currentIndex;
            next    = (current + 1) % _keys.Length;
        }
        while (Interlocked.CompareExchange(ref _currentIndex, next, current) != current);
        return true;
    }
}
