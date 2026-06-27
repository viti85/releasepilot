namespace ReleasePilot.Api.Endpoints.Requests;

public sealed record ApprovePromotionRequest(
    Guid ApproverId,
    string[] ApproverRoles);
