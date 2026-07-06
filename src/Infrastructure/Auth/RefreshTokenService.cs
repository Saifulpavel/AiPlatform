using System.Security.Cryptography;
using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Application.Common.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AiPlatform.Infrastructure.Auth;

// Refresh token = opaque random string, Redis-এ TTL সহ। value = userId।
// 1d এ rotation + reuse-detection (token family) যোগ হবে।
public class RefreshTokenService(IConnectionMultiplexer redis, IOptions<JwtSettings> options)
    : IRefreshTokenService
{
    private readonly JwtSettings _settings = options.Value;
    private const string KeyPrefix = "refresh:";

    public async Task<RefreshTokenResult> IssueAsync(string userId, CancellationToken cancellationToken)
    {
        var token = GenerateSecureToken();
        var ttl = TimeSpan.FromDays(_settings.RefreshTokenDays);
        var expiresAtUtc = DateTime.UtcNow.Add(ttl);

        var db = redis.GetDatabase();
        await db.StringSetAsync($"{KeyPrefix}{token}", userId, ttl);

        return new RefreshTokenResult(token, expiresAtUtc);
    }

    // 256-bit cryptographically-secure token, URL-safe
    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
