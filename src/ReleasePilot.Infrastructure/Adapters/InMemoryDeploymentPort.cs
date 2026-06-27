using Microsoft.Extensions.Logging;
using ReleasePilot.Application.Ports;
using ReleasePilot.Domain.ValueObjects;

namespace ReleasePilot.Infrastructure.Adapters;

public class InMemoryDeploymentPort : IDeploymentPort
{
    private readonly ILogger<InMemoryDeploymentPort> _logger;
    private readonly int _delayMs;

    public InMemoryDeploymentPort(ILogger<InMemoryDeploymentPort> logger) : this(logger, 0)
    {
    }

    public InMemoryDeploymentPort(ILogger<InMemoryDeploymentPort> logger, int delayMs)
    {
        _logger = logger;
        _delayMs = delayMs;
    }

    public async Task TriggerAsync(PromotionId id, CancellationToken ct)
    {
        _logger.LogInformation("Deployment was triggered for promotionId: {PromotionId}", id.Value);
        if (_delayMs > 0)
        {
            await Task.Delay(_delayMs, ct);
        }
    }
}
