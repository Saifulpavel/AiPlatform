using AiPlatform.Domain.Entities;
using AiPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AiPlatform.Infrastructure.Persistence;

// Identity tables (users, roles, claims…) + app tables (documents; পরে chat history)।
public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Document>(entity =>
        {
            entity.Property(d => d.FileName).HasMaxLength(260).IsRequired();
            entity.Property(d => d.ErrorMessage).HasMaxLength(2000);
            // Content nvarchar(max) — length সীমা না দিয়ে
        });
    }
}
