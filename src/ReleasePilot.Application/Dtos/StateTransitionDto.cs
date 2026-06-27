namespace ReleasePilot.Application.Dtos;

public sealed record StateTransitionDto(
    string Status,
    DateTime Timestamp,
    Guid UserId);
