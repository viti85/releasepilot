using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReleasePilot.Application.Abstractions;
using ReleasePilot.Infrastructure.Messaging;
using ReleasePilot.Infrastructure.Messaging.Consumers;
using ReleasePilot.Infrastructure.Persistence;
using ReleasePilot.Infrastructure.Persistence.Repositories;

namespace ReleasePilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ReleasePilotDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<IEventBus, MassTransitEventBus>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<AuditLogConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(configuration["RabbitMq:Host"], h =>
                {
                    h.Username(configuration["RabbitMq:Username"]!);
                    h.Password(configuration["RabbitMq:Password"]!);
                });
                cfg.ReceiveEndpoint("audit-log", e =>
                    e.ConfigureConsumer<AuditLogConsumer>(ctx));
                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
