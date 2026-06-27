namespace ReleasePilot.Domain.ValueObjects;

public sealed record ApplicationId(Guid Value)
{
    public static ApplicationId New() => new(Guid.NewGuid());
}
