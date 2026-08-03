using CargoPulse.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CargoPulse.IntegrationTests;

public sealed class CargoPulseApiFactory : WebApplicationFactory<ApiAssemblyMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseEnvironment("Testing");
        TestHostDefaults.ConfigureInfrastructureFree(builder, "self");
    }
}
