using CargoPulse.Web;
using CargoPulse.Worker;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CargoPulse.IntegrationTests;

public sealed class CargoPulseWorkerFactory : WebApplicationFactory<WorkerAssemblyMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseEnvironment("Testing");
        TestHostDefaults.ConfigureInfrastructureFree(builder, "self", "worker-heartbeat");
    }
}

public sealed class CargoPulseWebFactory : WebApplicationFactory<WebAssemblyMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseEnvironment("Testing");
    }
}
