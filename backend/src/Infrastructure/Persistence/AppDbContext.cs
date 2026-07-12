using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Domain.Entities;
using AiPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AiPlatform.Infrastructure.Persistence;

// Identity tables (users, roles, claims…) + app tables (documents; পরে chat history)।
public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<LlmUsage> LlmUsages => Set<LlmUsage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Document>(entity =>
        {
            entity.Property(d => d.FileName).HasMaxLength(260).IsRequired();
            entity.Property(d => d.ErrorMessage).HasMaxLength(2000);
            // Content nvarchar(max) — length সীমা না দিয়ে
            // soft-deleted auto-hide + tenant isolation (Empty tenant = system/background → সব দেখে)।
            entity.HasQueryFilter(d =>
                !d.IsDeleted && (currentUser.TenantId == Guid.Empty || d.TenantId == currentUser.TenantId));
            entity.HasIndex(d => d.CreatedAtUtc);       // newest-first pagination
            entity.HasIndex(d => d.TenantId);
        });

        builder.Entity<LlmUsage>(entity =>
        {
            entity.Property(u => u.Feature).HasMaxLength(50);
            entity.Property(u => u.Provider).HasMaxLength(50);
            entity.Property(u => u.Model).HasMaxLength(100);
            entity.Property(u => u.EstimatedCostUsd).HasPrecision(18, 8);
            entity.HasIndex(u => u.CreatedAtUtc);
        });
    }
}
