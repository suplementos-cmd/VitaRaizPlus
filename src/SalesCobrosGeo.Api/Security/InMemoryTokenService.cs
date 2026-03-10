using System.Collections.Concurrent;

namespace SalesCobrosGeo.Api.Security;

public sealed class InMemoryTokenService : ITokenService
{
    private readonly ConcurrentDictionary<string, TokenSession> _sessions = new(StringComparer.Ordinal);
    private readonly TimeSpan _tokenLifetime;

    public InMemoryTokenService(IConfiguration configuration)
    {
        var minutes = configuration.GetValue<int?>("Security:TokenLifetimeMinutes") ?? 480;
        _tokenLifetime = TimeSpan.FromMinutes(minutes);
    }

    public TokenSession IssueToken(AppUser user)
    {
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var session = new TokenSession(
            Token: token,
            UserName: user.UserName,
            FullName: user.FullName,
            Role: user.Role,
            ExpiresAtUtc: DateTime.UtcNow.Add(_tokenLifetime));

        _sessions[token] = session;
        return session;
    }

    public bool TryValidate(string token, out TokenSession? session)
    {
        session = null;

        if (!_sessions.TryGetValue(token, out var stored))
        {
            return false;
        }

        if (stored.ExpiresAtUtc <= DateTime.UtcNow)
        {
            _sessions.TryRemove(token, out _);
            return false;
        }

        session = stored;
        return true;
    }

    public void Revoke(string token)
    {
        _sessions.TryRemove(token, out _);
    }
}
