using ReleasePilot.Domain.ValueObjects;

namespace ReleasePilot.Application.Ports;

public interface IDeploymentPort
{
    Task TriggerAsync(PromotionId id, CancellationToken ct);
}
