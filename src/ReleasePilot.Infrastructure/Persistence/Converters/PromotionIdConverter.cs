using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReleasePilot.Domain.ValueObjects;

namespace ReleasePilot.Infrastructure.Persistence.Converters;

public sealed class PromotionIdConverter : ValueConverter<PromotionId, Guid>
{
    public PromotionIdConverter() : base(
        id => id.Value,
        value => new PromotionId(value))
    { }
}
