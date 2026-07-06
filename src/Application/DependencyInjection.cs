using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AiPlatform.Application;

// Application layer-এর সব service এখানে register হয়। Api শুধু AddApplication() ডাকে।
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR — এই assembly-র সব handler auto-register (feature আসলে এখানেই ধরা পড়বে)
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // FluentValidation — এই assembly-র সব validator auto-register
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
