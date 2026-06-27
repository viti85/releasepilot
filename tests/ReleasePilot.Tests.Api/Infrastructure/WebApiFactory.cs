using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReleasePilot.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace ReleasePilot.Tests.Api.Infrastructure;

public class WebApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _dbContainer;
    private RabbitMqContainer? _rabbitContainer;

    public bool DockerAvailable { get; private set; } = true;

    public async Task InitializeAsync()
    {
        try
        {
            _dbContainer = new PostgreSqlBuilder("postgres:16-alpine")
                .WithDatabase("releasepilot")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            _rabbitContainer = new RabbitMqBuilder("rabbitmq:3-alpine")
                .WithUsername("guest")
                .WithPassword("guest")
                .Build();

            await _dbContainer.StartAsync();
            await _rabbitContainer.StartAsync();

            // Apply EF Core migrations
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ReleasePilotDbContext>();
            await db.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            DockerAvailable = false;
            _dbContainer = null;
            _rabbitContainer = null;
            Console.WriteLine($"Docker/Testcontainers is not available: {ex.Message}. Tests will be skipped.");
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Clear default logging providers to prevent EventLog ObjectDisposedExceptions during host shutdown
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });

        if (!DockerAvailable || _dbContainer is null || _rabbitContainer is null)
        {
            return;
        }

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", _dbContainer.GetConnectionString() },
                { "RabbitMq:Host", $"{_rabbitContainer.Hostname}:{_rabbitContainer.GetMappedPublicPort(5672)}" },
                { "RabbitMq:Username", "guest" },
                { "RabbitMq:Password", "guest" }
            });
        });
    }

    public new async Task DisposeAsync()
    {
        if (DockerAvailable)
        {
            try
            {
                if (_dbContainer is not null)
                {
                    await _dbContainer.StopAsync();
                }
                if (_rabbitContainer is not null)
                {
                    await _rabbitContainer.StopAsync();
                }
            }
            catch
            {
                // Ignore errors during container disposal
            }
        }
        await base.DisposeAsync();
    }
}
