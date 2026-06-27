using ReleasePilot.Domain.Aggregates;
using ReleasePilot.Domain.ValueObjects;

namespace ReleasePilot.Application.Abstractions;

public interface IPromotionRepository
{
    Task<Promotion?> GetByIdAsync(PromotionId id, CancellationToken ct);
    Task<IReadOnlyList<Promotion>> GetActiveByApplicationAsync(ApplicationId appId, CancellationToken ct);
    Task<IReadOnlyList<Promotion>> GetCompletedByApplicationAsync(ApplicationId appId, CancellationToken ct);
    Task SaveAsync(Promotion promotion, CancellationToken ct);
    Task<(IReadOnlyList<Promotion> Items, int TotalCount)> GetPagedByApplicationAsync(ApplicationId appId, int page, int pageSize, CancellationToken ct);
}
