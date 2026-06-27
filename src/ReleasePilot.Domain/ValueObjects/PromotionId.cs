namespace ReleasePilot.Domain.ValueObjects;

public sealed record PromotionId(Guid Value)
{
    public static PromotionId New() => new(Guid.NewGuid());
}
