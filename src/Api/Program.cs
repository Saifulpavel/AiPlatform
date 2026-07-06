using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using AiPlatform.Api;
using AiPlatform.Application;
using AiPlatform.Infrastructure;

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

    builder.Services.AddControllers();

    // ProblemDetails — সব error RFC 7807 format-এ যাবে
    builder.Services.AddProblemDetails();

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
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

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
