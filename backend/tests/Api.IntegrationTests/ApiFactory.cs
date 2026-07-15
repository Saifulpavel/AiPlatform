using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace AiPlatform.Api.IntegrationTests;

// পুরো API আসল SQL Server + Redis container-এ বুট করে (Program.cs যেমন আছে — startup migration সহ)।
// fake নয় বলে routing/auth/validation/EF/Redis একসাথে সত্যিকারভাবে যাচাই হয়।
// Qdrant + LLM provider ইচ্ছে করে বাদ: health/ping/auth path এগুলো ছোঁয় না (lazy singleton)।
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // container ephemeral — এই key দিয়ে কিছু sign হয় না, তাই commit করা নিরাপদ (32+ byte দরকার HMAC-SHA256-এ)
    private const string TestJwtSecret = "integration-test-signing-key-0123456789abcdef0123456789abcdef";

    // image tag docker-compose.yml-এর সাথে মেলানো → test আর dev এক version-এ চলে
    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development → refresh cookie Secure=false, নইলে TestServer-এর http:// origin-এ cookie drop হয়
        builder.UseEnvironment(Environments.Development);
    }

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();
        await _redis.StartAsync();

        // container address অবশ্যই environment variable দিয়ে দিতে হবে, ConfigureAppConfiguration দিয়ে নয়:
        // Program.cs startup-এ Jwt/Redis সরাসরি builder.Configuration থেকে পড়ে, আর WebApplicationFactory-র
        // config delegate চলে তারও পরে → override দেরিতে পৌঁছে (token test-key-তে sign, dev-key-তে validate → 401;
        // Redis নীরবে localhost-এ পড়ে)। env var host তৈরির আগেই কনফিগে থাকে, তাই eager read-ও সঠিক মান পায়।
        // `__` = config section separator।
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", _sql.GetConnectionString());
        Environment.SetEnvironmentVariable("Redis__Connection", _redis.GetConnectionString());
        Environment.SetEnvironmentVariable("Jwt__SecretKey", TestJwtSecret);
    }

    // xunit v2 IAsyncLifetime.DisposeAsync (Task) vs WebApplicationFactory.DisposeAsync (ValueTask) —
    // signature আলাদা, তাই `new` দিয়ে ঢেকে ভেতরে base-টাও dispose করি।
    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _sql.DisposeAsync();
        await _redis.DisposeAsync();
    }
}

// এক collection = container একবারই ওঠে, সব test class শেয়ার করে (প্রতি class-এ SQL Server তোলা অসহ্য ধীর)
[CollectionDefinition(nameof(ApiCollection))]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>;
