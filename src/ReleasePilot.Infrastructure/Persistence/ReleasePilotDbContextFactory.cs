using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ReleasePilot.Infrastructure.Persistence;

public class ReleasePilotDbContextFactory : IDesignTimeDbContextFactory<ReleasePilotDbContext>
{
    public ReleasePilotDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("RELEASEPILOT_DB")
            ?? "Host=localhost;Port=5432;Database=releasepilot;Username=postgres;Password=dev";

        var options = new DbContextOptionsBuilder<ReleasePilotDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ReleasePilotDbContext(options);
    }
}
