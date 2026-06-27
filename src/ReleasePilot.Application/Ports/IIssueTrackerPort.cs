using ReleasePilot.Domain.ValueObjects;

namespace ReleasePilot.Application.Ports;

public interface IIssueTrackerPort
{
    Task<IReadOnlyList<WorkItemDto>> GetLinkedItemsAsync(PromotionId id, CancellationToken ct);
}
