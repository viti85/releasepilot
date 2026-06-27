using MediatR;
using ReleasePilot.Application.Abstractions;
using ReleasePilot.Application.Dtos;
using ReleasePilot.Application.Queries;
using ReleasePilot.Domain.Aggregates;
using ReleasePilot.Domain.Enums;

namespace ReleasePilot.Application.Handlers;

public sealed class GetApplicationStatusHandler(
    IPromotionRepository repository) : IRequestHandler<GetApplicationStatusQuery, ApplicationStatusDto>
{
    public async Task<ApplicationStatusDto> Handle(GetApplicationStatusQuery query, CancellationToken ct)
    {
        var appId = new ApplicationId(query.ApplicationId);

        var active = await repository.GetActiveByApplicationAsync(appId, ct);
        var completed = await repository.GetCompletedByApplicationAsync(appId, ct);

        var environments = Enum.GetValues<Environment>()
            .ToDictionary(
                env => env,
                env => new EnvironmentStatusDto(
                    LastCompletedVersion: completed
                        .Where(p => p.TargetEnvironment == env && p.Status == PromotionStatus.Completed)
                        .OrderByDescending(p => p.CompletedAt)
                        .FirstOrDefault()?.Version.Value,
                    ActivePromotion: active
                        .Where(p => p.TargetEnvironment == env)
                        .Select(ToSummary)
                        .FirstOrDefault()));

        return new ApplicationStatusDto(query.ApplicationId, environments);
    }

    private static PromotionSummaryDto ToSummary(Promotion p) =>
        new(
            p.Id.Value,
            p.Version.Value,
            p.TargetEnvironment.ToString(),
            p.Status.ToString(),
            p.RequestedAt);
}
