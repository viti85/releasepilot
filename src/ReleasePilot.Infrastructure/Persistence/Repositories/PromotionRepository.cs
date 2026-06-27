using Microsoft.EntityFrameworkCore;
using ReleasePilot.Application.Abstractions;
using ReleasePilot.Domain.Aggregates;
using ReleasePilot.Domain.Enums;
using ReleasePilot.Domain.ValueObjects;
using ReleasePilot.Infrastructure.Persistence.Entities;
using ApplicationId = ReleasePilot.Domain.ValueObjects.ApplicationId;

namespace ReleasePilot.Infrastructure.Persistence.Repositories;

public sealed class PromotionRepository(ReleasePilotDbContext context) : IPromotionRepository
{
    private static readonly PromotionStatus[] ActiveStatuses =
        [PromotionStatus.Pending, PromotionStatus.Approved, PromotionStatus.InProgress];

    public async Task<Promotion?> GetByIdAsync(PromotionId id, CancellationToken ct)
    {
        var entity = await context.Promotions
            .Include(p => p.StateTransitions)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<IReadOnlyList<Promotion>> GetActiveByApplicationAsync(ApplicationId appId, CancellationToken ct)
    {
        var entities = await context.Promotions
            .Include(p => p.StateTransitions)
            .Where(p => p.ApplicationId == appId && ActiveStatuses.Contains(p.Status))
            .ToListAsync(ct);

        return entities.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyList<Promotion>> GetCompletedByApplicationAsync(ApplicationId appId, CancellationToken ct)
    {
        var entities = await context.Promotions
            .Include(p => p.StateTransitions)
            .Where(p => p.ApplicationId == appId && p.Status == PromotionStatus.Completed)
            .ToListAsync(ct);

        return entities.Select(MapToDomain).ToList();
    }

    public async Task<(IReadOnlyList<Promotion> Items, int TotalCount)> GetPagedByApplicationAsync(
        ApplicationId appId, int page, int pageSize, CancellationToken ct)
    {
        var query = context.Promotions
            .Include(p => p.StateTransitions)
            .Where(p => p.ApplicationId == appId)
            .OrderByDescending(p => p.RequestedAt);

        var totalCount = await query.CountAsync(ct);
        var entities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (entities.Select(MapToDomain).ToList(), totalCount);
    }

    public async Task SaveAsync(Promotion promotion, CancellationToken ct)
    {
        var existing = await context.Promotions
            .Include(p => p.StateTransitions)
            .FirstOrDefaultAsync(p => p.Id == promotion.Id, ct);

        if (existing is null)
        {
            context.Promotions.Add(MapToEntity(promotion));
        }
        else
        {
            existing.Status = promotion.Status;
            existing.ApprovedBy = promotion.ApprovedBy;
            existing.CompletedAt = promotion.CompletedAt;

            var existingCount = existing.StateTransitions.Count;
            foreach (var t in promotion.StateHistory.Skip(existingCount))
            {
                existing.StateTransitions.Add(new PromotionStateTransitionEntity
                {
                    Id = Guid.NewGuid(),
                    PromotionId = promotion.Id,
                    Status = t.Status,
                    OccurredAt = t.Timestamp,
                    UserId = t.UserId
                });
            }
        }

        await context.SaveChangesAsync(ct);
    }

    private static Promotion MapToDomain(PromotionEntity entity)
    {
        var stateHistory = entity.StateTransitions
            .OrderBy(t => t.OccurredAt)
            .Select(t => new PromotionStateTransition(t.Status, t.OccurredAt, t.UserId));

        return Promotion.Reconstitute(
            entity.Id,
            entity.ApplicationId,
            entity.Version,
            entity.TargetEnvironment,
            entity.Status,
            entity.RequestedBy,
            entity.ApprovedBy,
            entity.RequestedAt,
            entity.CompletedAt,
            stateHistory);
    }

    private static PromotionEntity MapToEntity(Promotion promotion) =>
        new()
        {
            Id = promotion.Id,
            ApplicationId = promotion.ApplicationId,
            Version = promotion.Version,
            TargetEnvironment = promotion.TargetEnvironment,
            Status = promotion.Status,
            RequestedBy = promotion.RequestedBy,
            ApprovedBy = promotion.ApprovedBy,
            RequestedAt = promotion.RequestedAt,
            CompletedAt = promotion.CompletedAt,
            StateTransitions = promotion.StateHistory
                .Select(t => new PromotionStateTransitionEntity
                {
                    Id = Guid.NewGuid(),
                    PromotionId = promotion.Id,
                    Status = t.Status,
                    OccurredAt = t.Timestamp,
                    UserId = t.UserId
                })
                .ToList()
        };
}
