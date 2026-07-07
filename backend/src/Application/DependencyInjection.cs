using System.Reflection;
using AiPlatform.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace AiPlatform.Application;

// Application layer-এর সব service এখানে register হয়। Api শুধু AddApplication() ডাকে।
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR — এই assembly-র সব handler auto-register (feature আসলে এখানেই ধরা পড়বে)
        // ValidationBehavior pipeline-এ বসে handler-এর আগে input যাচাই করে
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // FluentValidation — এই assembly-র সব validator auto-register
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
