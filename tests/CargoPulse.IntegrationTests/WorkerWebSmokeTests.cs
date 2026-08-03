using System.Net;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace CargoPulse.IntegrationTests;

public sealed class WorkerFoundationSmokeTests(CargoPulseWorkerFactory factory)
    : IClassFixture<CargoPulseWorkerFactory>
{
    [Fact]
    public async Task WorkerHostExposesHealthyFoundationEndpoints()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        using var rootResponse = await client.GetAsync("/", cancellationToken);
        using var healthResponse = await client.GetAsync("/health", cancellationToken);
        using var readyResponse = await client.GetAsync("/ready", cancellationToken);

        rootResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        readyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var readyStream = await readyResponse.Content.ReadAsStreamAsync(cancellationToken);
        using var readyDocument = await JsonDocument.ParseAsync(
            readyStream,
            cancellationToken: cancellationToken);

        readyDocument.RootElement.GetProperty("checks")
            .TryGetProperty("worker-heartbeat", out var heartbeatCheck)
            .Should().BeTrue();
        heartbeatCheck.GetProperty("status").GetString().Should().Be("Healthy");
    }
}

public sealed class WebFoundationSmokeTests(CargoPulseWebFactory factory)
    : IClassFixture<CargoPulseWebFactory>
{
    [Fact]
    public async Task WebHostRendersFoundationAndReportsHealthy()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        using var rootResponse = await client.GetAsync("/", cancellationToken);
        using var healthResponse = await client.GetAsync("/health", cancellationToken);
        using var readyResponse = await client.GetAsync("/ready", cancellationToken);
        var rootContent = await rootResponse.Content.ReadAsStringAsync(cancellationToken);

        rootResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        rootContent.Should().Contain("CargoPulse");
        healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        readyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
