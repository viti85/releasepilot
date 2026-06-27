namespace ReleasePilot.Domain.Exceptions;

public sealed class UnauthorizedApprovalException : DomainException
{
    public string UserId { get; }

    public UnauthorizedApprovalException(string userId)
        : base($"User '{userId}' is not authorized to approve promotions.")
    {
        UserId = userId;
    }
}
