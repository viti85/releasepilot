using Microsoft.EntityFrameworkCore;
using ReleasePilot.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace ReleasePilot.Tests.Infrastructure;

public class PostgreSqlContainerFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _useLocalFallback = false;

    public string ConnectionString { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder("postgres:16-alpine")
                .WithDatabase("releasepilot")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
        }
        catch (Exception)
        {
            // Fallback to local PostgreSQL instance since Docker is not running or available
            _useLocalFallback = true;
            ConnectionString = "Host=localhost;Port=5432;Database=releasepilot_test;Username=postgres;Password=12345";
        }

        var options = new DbContextOptionsBuilder<ReleasePilotDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        using var dbContext = new ReleasePilotDbContext(options);
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (!_useLocalFallback && _container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
