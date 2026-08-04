using System.Threading.RateLimiting;
using CargoPulse.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddCargoPulseServiceDefaults();

builder.AddNpgsqlDataSource("cargopulse");
builder.AddRabbitMQClient("messaging");

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 1_048_576);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(static context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            static _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 300,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1),
                AutoReplenishment = true,
            }));
});

var app = builder.Build();

EnsureConnectionStringIsConfigured(app.Configuration, "cargopulse");
EnsureConnectionStringIsConfigured(app.Configuration, "messaging");

app.UseCargoPulseHttpDefaults();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok(new
{
    Service = "CargoPulse.Api",
    Milestone = "Milestone 0",
    Status = "Foundation is operational; business APIs are introduced in later milestones.",
}))
    .WithName("GetServiceInformation")
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
