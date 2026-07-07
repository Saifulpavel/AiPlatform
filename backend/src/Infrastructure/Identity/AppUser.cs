using Microsoft.AspNetCore.Identity;

namespace AiPlatform.Infrastructure.Identity;

// Identity user. Persistence-tied বলে Infrastructure-এ (Domain pure থাকে)।
public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
