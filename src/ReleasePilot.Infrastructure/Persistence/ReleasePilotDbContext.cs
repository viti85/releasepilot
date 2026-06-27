using Microsoft.EntityFrameworkCore;
using ReleasePilot.Infrastructure.Persistence.Entities;

namespace ReleasePilot.Infrastructure.Persistence;

public class ReleasePilotDbContext : DbContext
{
    public ReleasePilotDbContext(DbContextOptions<ReleasePilotDbContext> options) : base(options)
    {
    }

    public DbSet<PromotionEntity> Promotions => Set<PromotionEntity>();
    public DbSet<PromotionStateTransitionEntity> StateTransitions => Set<PromotionStateTransitionEntity>();
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReleasePilotDbContext).Assembly);
    }
}
