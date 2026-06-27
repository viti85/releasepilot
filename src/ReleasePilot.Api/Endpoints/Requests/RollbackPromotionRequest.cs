namespace ReleasePilot.Api.Endpoints.Requests;

public sealed record RollbackPromotionRequest(
    Guid UserId,
    string Reason);
