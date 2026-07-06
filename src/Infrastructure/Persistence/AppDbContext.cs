using AiPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AiPlatform.Infrastructure.Persistence;

// Identity tables (users, roles, claims…) + পরে app tables (chat history, documents) এখানে যোগ হবে।
public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Phase 2+ এ app entity configuration এখানে আসবে
    }
}
