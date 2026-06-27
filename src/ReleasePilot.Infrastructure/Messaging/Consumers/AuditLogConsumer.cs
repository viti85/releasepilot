using System.Text.Json;
using MassTransit;
using ReleasePilot.Domain.Abstractions;
using ReleasePilot.Domain.ValueObjects;
using ReleasePilot.Infrastructure.Persistence;
using ReleasePilot.Infrastructure.Persistence.Entities;

namespace ReleasePilot.Infrastructure.Messaging.Consumers;

/// <summary>
/// A single generic consumer is used to handle all domain events implementing <see cref="IDomainEvent"/>.
/// 
/// Justification:
/// 1. Avoiding Boilerplate: Every domain event requires the same logging behavior (extracting common fields
///    and serializing the payload). Creating separate consumers for each event type would lead to duplicate code.
/// 2. Polymorphic Routing: MassTransit's message pipeline natively supports interface/base class subscriptions.
///    When a concrete domain event (e.g., PromotionRequestedEvent) is published, MassTransit handles the
///    hierarchical routing so that it gets delivered to this consumer.
/// 3. Maintainability: If new domain events are introduced in the future, they will automatically be consumed
///    and audited without requiring any code changes, registration updates, or new consumers.
/// </summary>
public class AuditLogConsumer(ReleasePilotDbContext dbContext) : IConsumer<IDomainEvent>
{
    public async Task Consume(ConsumeContext<IDomainEvent> context)
    {
        var domainEvent = context.Message;

        var auditEntry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EventType = domainEvent.GetType().Name,
            PromotionId = new PromotionId(domainEvent.PromotionId),
            OccurredAt = domainEvent.OccurredAt,
            ActingUserId = domainEvent.ActingUserId,
            Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType())
        };

        dbContext.AuditLog.Add(auditEntry);
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
