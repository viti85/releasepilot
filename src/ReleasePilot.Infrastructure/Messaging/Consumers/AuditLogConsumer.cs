using System.Text;
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
        try
        {
            var domainEvent = context.Message;

            // Retrieve the concrete type name from SupportedMessageTypes if domainEvent is a proxy
            string eventType = domainEvent.GetType().Name;
            if (eventType == "IDomainEvent" && context.SupportedMessageTypes != null)
            {
                var urn = context.SupportedMessageTypes.FirstOrDefault();
                if (!string.IsNullOrEmpty(urn))
                {
                    var lastColon = urn.LastIndexOf(':');
                    if (lastColon >= 0)
                    {
                        eventType = urn.Substring(lastColon + 1);
                    }
                }
            }

            // Extract raw concrete payload from the MassTransit envelope
            string payload;
            try
            {
                var body = context.ReceiveContext.GetBody();
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("message", out var messageElement))
                {
                    payload = messageElement.GetRawText();
                }
                else
                {
                    payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("PAYLOAD PARSE ERROR: " + ex);
                payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
            }

            var auditEntry = new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                EventType = eventType,
                PromotionId = new PromotionId(domainEvent.PromotionId),
                OccurredAt = domainEvent.OccurredAt,
                ActingUserId = domainEvent.ActingUserId,
                Payload = payload
            };

            dbContext.AuditLog.Add(auditEntry);
            await dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine("CONSUME CRITICAL ERROR: " + ex);
            throw;
        }
    }
}
