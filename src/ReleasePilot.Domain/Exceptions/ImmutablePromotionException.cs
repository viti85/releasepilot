using ReleasePilot.Domain.Enums;
using ReleasePilot.Domain.ValueObjects;

namespace ReleasePilot.Domain.Exceptions;

public sealed class ImmutablePromotionException : DomainException
{
    public PromotionId PromotionId { get; }
    public PromotionStatus Status { get; }

    public ImmutablePromotionException(PromotionId promotionId, PromotionStatus status)
        : base($"Promotion {promotionId.Value} cannot be mutated because it is {status}.")
    {
        PromotionId = promotionId;
        Status = status;
    }
}
