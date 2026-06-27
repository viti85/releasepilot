namespace ReleasePilot.Api.Endpoints.Requests;

public sealed record RequestPromotionRequest(
    Guid ApplicationId,
    string Version,
    ReleasePilot.Domain.Enums.Environment TargetEnvironment,
    Guid RequestedByUserId);
