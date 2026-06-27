using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ApplicationId = ReleasePilot.Domain.ValueObjects.ApplicationId;

namespace ReleasePilot.Infrastructure.Persistence.Converters;

public sealed class ApplicationIdConverter : ValueConverter<ApplicationId, Guid>
{
    public ApplicationIdConverter() : base(
        id => id.Value,
        value => new ApplicationId(value))
    { }
}
