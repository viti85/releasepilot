using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ReleasePilot.Application.Behaviors;

namespace ReleasePilot.Application;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(ServiceExtensions).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
