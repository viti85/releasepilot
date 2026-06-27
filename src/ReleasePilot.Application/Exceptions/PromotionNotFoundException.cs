namespace ReleasePilot.Application.Exceptions;

public sealed class PromotionNotFoundException : Exception
{
    public PromotionNotFoundException(Guid promotionId)
        : base($"Promotion '{promotionId}' was not found.") { }
}
