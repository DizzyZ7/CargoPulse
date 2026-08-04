using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace CargoPulse.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    public const string LivenessPath = "/health";
    public const string ReadinessPath = "/ready";

    public static TBuilder AddCargoPulseServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddCargoPulseLogging();
        builder.AddCargoPulseOpenTelemetry();

        builder.Services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(httpClientBuilder =>
        {
            httpClientBuilder.AddStandardResilienceHandler();
            httpClientBuilder.AddServiceDiscovery();
        });

        return builder;
    }

    public static WebApplication UseCargoPulseHttpDefaults(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.GetLevel = static (context, _, exception) =>
            {
                if (exception is not null || context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
                {
                    return LogEventLevel.Error;
                }

                if (context.Request.Path.StartsWithSegments(LivenessPath)
                    || context.Request.Path.StartsWithSegments(ReadinessPath))
                {
                    return context.Response.StatusCode >= StatusCodes.Status400BadRequest
                        ? LogEventLevel.Warning
                        : LogEventLevel.Debug;
                }

                return context.Response.StatusCode >= StatusCodes.Status400BadRequest
                    ? LogEventLevel.Warning
                    : LogEventLevel.Information;
            };
        });

        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(static state =>
            {
                var httpContext = (HttpContext)state;
                httpContext.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
                httpContext.Response.Headers.TryAdd("X-Frame-Options", "DENY");
                httpContext.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
                httpContext.Response.Headers.TryAdd(
                    "Permissions-Policy",
                    "camera=(), geolocation=(), microphone=()");
                return Task.CompletedTask;
            }, context);

            await next(context).ConfigureAwait(false);
        });

        return app;
    }

    public static WebApplication MapCargoPulseDefaultEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapHealthChecks(LivenessPath, new HealthCheckOptions
        {
            Predicate = static registration => registration.Tags.Contains("live"),
            ResponseWriter = HealthResponseWriter.WriteAsync,
        })
            .AllowAnonymous()
            .DisableRateLimiting();

        app.MapHealthChecks(ReadinessPath, new HealthCheckOptions
        {
            Predicate = static _ => true,
            ResponseWriter = HealthResponseWriter.WriteAsync,
        })
            .AllowAnonymous()
            .DisableRateLimiting();

        return app;
    }

    private static TBuilder AddCargoPulseLogging<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddSerilog((_, configuration) => configuration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
            .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
            .WriteTo.Console(new RenderedCompactJsonFormatter()));

        return builder;
    }

    private static TBuilder AddCargoPulseOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var serviceVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

        var openTelemetry = builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: builder.Environment.ApplicationName,
                serviceVersion: serviceVersion))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithTracing(tracing => tracing
                .AddSource(builder.Environment.ApplicationName)
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = context =>
                        !context.Request.Path.StartsWithSegments(LivenessPath)
                        && !context.Request.Path.StartsWithSegments(ReadinessPath);
                })
                .AddHttpClientInstrumentation());

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            openTelemetry.UseOtlpExporter();
        }

        return builder;
    }
}
