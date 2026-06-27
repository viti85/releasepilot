using ReleasePilot.Domain.Enums;
using ReleasePilot.Domain.ValueObjects;

namespace ReleasePilot.Application.Ports;

public interface INotificationPort
{
    Task NotifyTerminalStateAsync(PromotionId id, PromotionStatus status, CancellationToken ct);
}
