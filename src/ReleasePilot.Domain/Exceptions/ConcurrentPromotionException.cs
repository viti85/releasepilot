using ReleasePilot.Domain.ValueObjects;

namespace ReleasePilot.Domain.Exceptions;

public sealed class ConcurrentPromotionException : DomainException
{
    public ApplicationId ApplicationId { get; }
    public Environment Target { get; }

    public ConcurrentPromotionException(ApplicationId applicationId, Environment target)
        : base($"An active promotion for application {applicationId.Value} targeting {target} already exists.")
    {
        ApplicationId = applicationId;
        Target = target;
    }
}
