using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CargoPulse.IntegrationTests;

internal static class TestHostDefaults
{
    public static void ConfigureInfrastructureFree(
        IWebHostBuilder builder,
        params string[] retainedHealthChecks)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(retainedHealthChecks);

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:cargopulse"] =
                    "Host=127.0.0.1;Port=1;Database=cargopulse;Username=test;Password=test;Timeout=1",
                ["ConnectionStrings:messaging"] = "amqp://test:test@127.0.0.1:1",
            }));

        var retainedNames = retainedHealthChecks.ToHashSet(StringComparer.Ordinal);
        builder.ConfigureTestServices(services => services.PostConfigure<HealthCheckServiceOptions>(options =>
        {
            var registrationsToRemove = options.Registrations
                .Where(registration => !retainedNames.Contains(registration.Name))
                .ToArray();

            foreach (var registration in registrationsToRemove)
            {
                options.Registrations.Remove(registration);
            }
        }));
    }
}
