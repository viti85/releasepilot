namespace ReleasePilot.Domain.Exceptions;

public sealed class EnvironmentSkippedException : DomainException
{
    public Environment Target { get; }
    public Environment Required { get; }

    public EnvironmentSkippedException(Environment target, Environment required)
        : base($"Cannot promote to {target} before completing {required}.")
    {
        Target = target;
        Required = required;
    }
}
