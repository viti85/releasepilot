namespace ReleasePilot.Application.Dtos;

public sealed record PromotionDetailDto(
    Guid Id,
    Guid ApplicationId,
    string Version,
    string TargetEnvironment,
    string Status,
    Guid RequestedBy,
    Guid? ApprovedBy,
    DateTime RequestedAt,
    DateTime? CompletedAt,
    IReadOnlyList<StateTransitionDto> History);
