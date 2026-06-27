namespace ReleasePilot.Application.Dtos;

public sealed record EnvironmentStatusDto(
    string? LastCompletedVersion,
    PromotionSummaryDto? ActivePromotion);
