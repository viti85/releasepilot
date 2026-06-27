using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReleasePilot.Domain.ValueObjects;

namespace ReleasePilot.Infrastructure.Persistence.Converters;

public sealed class AppVersionConverter : ValueConverter<AppVersion, string>
{
    public AppVersionConverter() : base(
        version => version.Value,
        value => new AppVersion(value))
    { }
}
