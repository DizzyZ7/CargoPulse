using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CargoPulse.ServiceDefaults;

internal static class HealthResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(report);

        var response = new HealthResponse(
            report.Status.ToString(),
            report.TotalDuration.TotalMilliseconds,
            report.Entries
                .OrderBy(static entry => entry.Key, StringComparer.Ordinal)
                .ToDictionary(
                    static entry => entry.Key,
                    static entry => new HealthCheckResponse(
                        entry.Value.Status.ToString(),
                        entry.Value.Duration.TotalMilliseconds),
                    StringComparer.Ordinal));

        return context.Response.WriteAsJsonAsync(response, context.RequestAborted);
    }

    private sealed record HealthResponse(
        string Status,
        double DurationMilliseconds,
        IReadOnlyDictionary<string, HealthCheckResponse> Checks);

    private sealed record HealthCheckResponse(string Status, double DurationMilliseconds);
}
