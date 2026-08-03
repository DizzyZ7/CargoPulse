using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CargoPulse.Worker;

internal sealed class WorkerHeartbeatHealthCheck(
    WorkerHeartbeat heartbeat,
    TimeProvider timeProvider) : IHealthCheck
{
    private static readonly TimeSpan MaximumHeartbeatAge = TimeSpan.FromSeconds(30);

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var age = timeProvider.GetUtcNow() - heartbeat.LastBeatAt;
        var result = age <= MaximumHeartbeatAge
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The worker heartbeat is stale.");

        return Task.FromResult(result);
    }
}
