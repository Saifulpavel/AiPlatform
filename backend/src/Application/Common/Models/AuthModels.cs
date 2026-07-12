namespace AiPlatform.Application.Common.Models;

// Token generation-এর জন্য দরকারি user তথ্য (Identity type Application-এ আসে না)।
public record AuthUser(string Id, string Email, string FullName, IReadOnlyList<string> Roles, Guid TenantId = default);

// Access token + কখন expire করবে।
public record AccessToken(string Token, DateTime ExpiresAtUtc);

// Refresh token + expiry (opaque string, JWT নয়)।
public record RefreshTokenResult(string Token, DateTime ExpiresAtUtc);

// Rotate-এর ফল: কার token + নতুন refresh token।
public record RefreshRotation(string UserId, RefreshTokenResult NewToken);

// Login-এর পূর্ণ ফল — controller access token body-তে দেয়, refresh cookie-তে বসায়।
public record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
