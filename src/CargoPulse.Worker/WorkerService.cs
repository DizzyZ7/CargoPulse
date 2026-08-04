namespace CargoPulse.Worker;

internal sealed partial class WorkerService(
    ILogger<WorkerService> logger,
    WorkerHeartbeat heartbeat,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerStarted(logger, HeartbeatInterval);
        heartbeat.Beat(timeProvider.GetUtcNow());

        using var timer = new PeriodicTimer(HeartbeatInterval, timeProvider);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                heartbeat.Beat(timeProvider.GetUtcNow());
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            LogWorkerStopped(logger);
        }
    }

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "CargoPulse worker started; heartbeat interval is {HeartbeatInterval}.")]
    private static partial void LogWorkerStarted(ILogger logger, TimeSpan heartbeatInterval);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "CargoPulse worker stopped.")]
    private static partial void LogWorkerStopped(ILogger logger);
}
