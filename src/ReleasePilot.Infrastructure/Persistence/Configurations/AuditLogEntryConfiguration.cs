using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReleasePilot.Infrastructure.Persistence.Converters;
using ReleasePilot.Infrastructure.Persistence.Entities;

namespace ReleasePilot.Infrastructure.Persistence.Configurations;

public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_log");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .IsRequired();

        builder.Property(x => x.PromotionId)
            .HasConversion(new PromotionIdConverter())
            .HasColumnType("uuid")
            .HasColumnName("promotion_id")
            .IsRequired();

        builder.Property(x => x.OccurredAt)
            .HasColumnName("occurred_at")
            .IsRequired();

        builder.Property(x => x.ActingUserId)
            .HasColumnName("acting_user_id")
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();
    }
}
