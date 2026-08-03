using System.Net;
using System.Text.Json;
using CargoPulse.ServiceDefaults;
using FluentAssertions;
using Xunit;

namespace CargoPulse.IntegrationTests;

public sealed class FoundationSmokeTests(CargoPulseApiFactory factory)
    : IClassFixture<CargoPulseApiFactory>
{
    [Fact]
    public async Task LivenessEndpointReportsHealthyProcess()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/health", cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(
            responseStream,
            cancellationToken: cancellationToken);

        document.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
        document.RootElement.GetProperty("checks").TryGetProperty("self", out var selfCheck)
            .Should().BeTrue();
        selfCheck.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task ReadinessEndpointReportsHealthyWithoutOptionalLocalDependencies()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/ready", cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ApiPreservesValidCorrelationId()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        const string correlationId = "foundation-smoke-20260803";
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, correlationId);

        using var response = await client.SendAsync(request, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues(CorrelationIdMiddleware.HeaderName)
            .Should().ContainSingle()
            .Which.Should().Be(correlationId);
        response.Headers.GetValues("X-Content-Type-Options")
            .Should().ContainSingle()
            .Which.Should().Be("nosniff");
        response.Headers.GetValues("X-Frame-Options")
            .Should().ContainSingle()
            .Which.Should().Be("DENY");
        response.Headers.GetValues("Referrer-Policy")
            .Should().ContainSingle()
            .Which.Should().Be("no-referrer");
        response.Headers.GetValues("Permissions-Policy")
            .Should().ContainSingle()
            .Which.Should().Be("camera=(), geolocation=(), microphone=()");
    }

    [Fact]
    public async Task ApiReplacesUnsafeCorrelationId()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        const string unsafeCorrelationId = "unsafe header value";
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.TryAddWithoutValidation(
            CorrelationIdMiddleware.HeaderName,
            unsafeCorrelationId);

        using var response = await client.SendAsync(request, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues(CorrelationIdMiddleware.HeaderName)
            .Should().ContainSingle()
            .Which.Should().NotBe(unsafeCorrelationId)
            .And.MatchRegex("^[a-f0-9]{32}$");
    }

    [Fact]
    public async Task UnknownRouteReturnsProblemDetails()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/route-that-does-not-exist", cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(
            responseStream,
            cancellationToken: cancellationToken);
        document.RootElement.GetProperty("status").GetInt32().Should().Be(404);
    }
}
