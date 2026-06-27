using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using DomainEnvironment = ReleasePilot.Domain.Enums.Environment;

namespace ReleasePilot.Infrastructure.Persistence.Converters;

public sealed class EnvironmentConverter : ValueConverter<DomainEnvironment, string>
{
    public EnvironmentConverter() : base(
        env => env.ToString(),
        value => Enum.Parse<DomainEnvironment>(value))
    { }
}
