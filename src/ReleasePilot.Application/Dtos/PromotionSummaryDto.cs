namespace ReleasePilot.Application.Dtos;

public sealed record PromotionSummaryDto(
    Guid Id,
    string Version,
    string TargetEnvironment,
    string Status,
    DateTime RequestedAt);
