using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Infrastructure.Auth;
using AiPlatform.Infrastructure.Identity;
using AiPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace AiPlatform.Infrastructure;

// Infrastructure layer-এর সব service (EF Core, Identity, Redis, Qdrant, LLM providers) এখানে register হয়।
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));

        services
            .AddIdentityCore<AppUser>(options =>
            {
                // Password policy — FAANG-level baseline
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;

                // Account lockout — brute-force রোধ
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();   // forgot/reset-password token (Phase 1e)

        // JWT + refresh token settings
        services.Configure<Auth.JwtSettings>(configuration.GetSection(Auth.JwtSettings.SectionName));

        // Redis — refresh-token store + (পরে) cache/rate-limit
        var redisConnection = configuration.GetSection("Redis")["Connection"]
            ?? throw new InvalidOperationException("Redis:Connection সেট নেই।");
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IRefreshTokenService, RefreshTokenService>();

        return services;
    }
}
