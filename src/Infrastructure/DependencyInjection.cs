using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AiPlatform.Infrastructure;

// Infrastructure layer-এর সব service (EF Core, Redis, Qdrant, LLM providers) এখানে register হবে।
// এখন খালি — Phase 1+ এ ভরবে।
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ponytail: placeholder — Phase 1 এ DbContext/Identity, Phase 2 এ LLM providers যোগ হবে
        return services;
    }
}
