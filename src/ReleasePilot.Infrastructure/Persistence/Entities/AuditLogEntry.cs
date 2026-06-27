using ReleasePilot.Domain.ValueObjects;

namespace ReleasePilot.Infrastructure.Persistence.Entities;

public class AuditLogEntry
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = default!;
    public PromotionId PromotionId { get; set; } = default!;
    public DateTime OccurredAt { get; set; }
    public Guid ActingUserId { get; set; }
    public string Payload { get; set; } = default!;
}
