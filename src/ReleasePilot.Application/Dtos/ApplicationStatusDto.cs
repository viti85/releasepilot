namespace ReleasePilot.Application.Dtos;

public sealed record ApplicationStatusDto(
    Guid ApplicationId,
    IReadOnlyDictionary<Environment, EnvironmentStatusDto> Environments);
