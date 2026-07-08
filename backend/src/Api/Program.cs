using System.Text;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using AiPlatform.Api;
using AiPlatform.Api.Common;
using AiPlatform.Application;
using AiPlatform.Infrastructure;
using AiPlatform.Infrastructure.Auth;

// --- Bootstrap logger: startup crash-ও যেন log হয় ---
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog — appsettings থেকে config পড়ে (structured + correlation, Phase 0 বেসিক)
    builder.Host.UseSerilog((context, services, config) => config
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Layer wiring
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // --- JWT authentication ---
    var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
        ?? throw new InvalidOperationException("Jwt config অনুপস্থিত।");
    if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
        throw new InvalidOperationException(
            $"Jwt:SecretKey সেট নেই। Current environment = '{builder.Environment.EnvironmentName}'. " +
            "Development হলে appsettings.Development.json/user-secrets দেখো; না হলে VS profile Development-এ দাও।");

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.Zero   // exp ঠিক ১৫ মিনিটেই শেষ, default ৫ মিনিট ছাড় না
            };
        });
    builder.Services.AddAuthorization();

    builder.Services.AddControllers();

    // ProblemDetails + global exception handler — unexpected error consistent RFC 7807
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    // API versioning — সব endpoint /api/v{n}; version না দিলে v1 ধরে
    builder.Services
        .AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";     // v1, v2...
            options.SubstituteApiVersionInUrl = true;
        });

    // Swagger — প্রতি version-এর জন্য আলাদা doc (ConfigureSwaggerOptions)
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

    // CORS — শুধু React app-এর origin (appsettings: Cors:AllowedOrigins)
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options => options.AddPolicy("frontend", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));   // refresh cookie-র জন্য (Phase 1)

    // Health checks
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.UseSerilogRequestLogging();     // প্রতি request এক লাইনে log (method, path, status, ms)
    app.UseExceptionHandler();          // unhandled → ProblemDetails

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
            foreach (var desc in provider.ApiVersionDescriptions)
                options.SwaggerEndpoint($"/swagger/{desc.GroupName}/swagger.json", desc.GroupName.ToUpperInvariant());
        });

        // dev-এ root "/" খুললে সরাসরি swagger UI দেখাও
        app.MapGet("/", () => Results.Redirect("/swagger"));
    }

    app.UseHttpsRedirection();
    app.UseCors("frontend");
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    // base role (User, Admin) নিশ্চিত করো
    using (var scope = app.Services.CreateScope())
        await AiPlatform.Infrastructure.Persistence.IdentitySeeder.SeedRolesAsync(scope.ServiceProvider);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// integration test (Phase 8) WebApplicationFactory<Program> ধরতে পারে
public partial class Program;
