using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReleasePilot.Domain.Enums;

namespace ReleasePilot.Infrastructure.Persistence.Converters;

public sealed class PromotionStatusConverter : ValueConverter<PromotionStatus, string>
{
    public PromotionStatusConverter() : base(
        status => status.ToString(),
        value => Enum.Parse<PromotionStatus>(value))
    { }
}
