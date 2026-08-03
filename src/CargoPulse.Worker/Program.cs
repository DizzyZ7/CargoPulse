using CargoPulse.ServiceDefaults;
using CargoPulse.Worker;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.AddCargoPulseServiceDefaults();

builder.AddNpgsqlDataSource("cargopulse");
builder.AddRabbitMQClient("messaging");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<WorkerHeartbeat>();
builder.Services.AddHostedService<WorkerService>();
builder.Services.AddHealthChecks()
    .AddCheck<WorkerHeartbeatHealthCheck>("worker-heartbeat", tags: ["ready"]);
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddMeter(WorkerHeartbeat.MeterName));

var app = builder.Build();

EnsureConnectionStringIsConfigured(app.Configuration, "cargopulse");
EnsureConnectionStringIsConfigured(app.Configuration, "messaging");

app.UseCargoPulseHttpDefaults();
app.MapGet("/", () => Results.Ok(new
{
    Service = "CargoPulse.Worker",
    Milestone = "Milestone 0",
    Status = "Background host and heartbeat are operational.",
}))
    .WithName("GetWorkerInformation")
    .WithTags("System");
app.MapCargoPulseDefaultEndpoints();

app.Run();

static void EnsureConnectionStringIsConfigured(IConfiguration configuration, string name)
{
    if (string.IsNullOrWhiteSpace(configuration.GetConnectionString(name)))
    {
        throw new InvalidOperationException($"Required connection string '{name}' is not configured.");
    }
}

public partial class Program;
