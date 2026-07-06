using AiPlatform.Application.Common.Models;

namespace AiPlatform.Application.Common.Interfaces;

// Refresh token Redis-এ থাকে। 1d এ rotation + reuse-detection যোগ হবে।
public interface IRefreshTokenService
{
    // নতুন refresh token বানিয়ে store করে ফেরত দেয়।
    Task<RefreshTokenResult> IssueAsync(string userId, CancellationToken cancellationToken);
}
