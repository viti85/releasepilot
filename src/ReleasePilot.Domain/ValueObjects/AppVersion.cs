namespace ReleasePilot.Domain.ValueObjects;

public sealed record AppVersion
{
    public string Value { get; }

    public AppVersion(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }
}
