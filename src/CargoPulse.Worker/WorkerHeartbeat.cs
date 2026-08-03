using System.Diagnostics.Metrics;

namespace CargoPulse.Worker;

public sealed class WorkerHeartbeat : IDisposable
{
    public const string MeterName = "CargoPulse.Worker";

    private readonly Meter _meter = new(MeterName);
    private readonly Counter<long> _heartbeatCounter;
    private long _lastBeatUnixSeconds;

    public WorkerHeartbeat(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        _lastBeatUnixSeconds = timeProvider.GetUtcNow().ToUnixTimeSeconds();
        _heartbeatCounter = _meter.CreateCounter<long>(
            "worker_heartbeat_total",
            description: "Number of worker heartbeat ticks emitted.");
        _meter.CreateObservableGauge(
            "worker_heartbeat_timestamp_seconds",
            () => Interlocked.Read(ref _lastBeatUnixSeconds),
            unit: "s",
            description: "Unix timestamp of the most recent worker heartbeat.");
    }

    public DateTimeOffset LastBeatAt => DateTimeOffset.FromUnixTimeSeconds(
        Interlocked.Read(ref _lastBeatUnixSeconds));

    public void Beat(DateTimeOffset occurredAt)
    {
        Interlocked.Exchange(ref _lastBeatUnixSeconds, occurredAt.ToUnixTimeSeconds());
        _heartbeatCounter.Add(1);
    }

    public void Dispose() => _meter.Dispose();
}
