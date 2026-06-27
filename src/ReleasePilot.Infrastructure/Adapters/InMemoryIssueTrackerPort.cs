using ReleasePilot.Application.Ports;
using ReleasePilot.Domain.ValueObjects;

namespace ReleasePilot.Infrastructure.Adapters;

public class InMemoryIssueTrackerPort : IIssueTrackerPort
{
    public Task<IReadOnlyList<WorkItemDto>> GetLinkedItemsAsync(PromotionId id, CancellationToken ct)
    {
        IReadOnlyList<WorkItemDto> workItems =
        [
            new WorkItemDto("ISSUE-1", "Add login feature", "https://issues.example.com/1"),
            new WorkItemDto("ISSUE-2", "Fix signup bug", "https://issues.example.com/2")
        ];

        return Task.FromResult(workItems);
    }
}
