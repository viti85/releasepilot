using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReleasePilot.Infrastructure.Persistence.Converters;
using ReleasePilot.Infrastructure.Persistence.Entities;

namespace ReleasePilot.Infrastructure.Persistence.Configurations;

public class PromotionStateTransitionEntityConfiguration : IEntityTypeConfiguration<PromotionStateTransitionEntity>
{
    public void Configure(EntityTypeBuilder<PromotionStateTransitionEntity> builder)
    {
        builder.ToTable("promotion_state_transitions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.PromotionId)
            .HasConversion(new PromotionIdConverter())
            .HasColumnType("uuid")
            .HasColumnName("promotion_id")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion(new PromotionStatusConverter())
            .HasMaxLength(20)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(x => x.OccurredAt)
            .HasColumnName("occurred_at")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();
    }
}
