# ADR-0002: Aspire-first local orchestration with explicit readiness and OTLP

- Status: Accepted
- Date: 2026-08-03

## Context

Milestone 0 needs reproducible local infrastructure, dependency health, traces, metrics, and structured logs without pretending that the local orchestrator is a production deployment platform. It must also support developers who prefer to run infrastructure separately.

## Decision

- Use .NET Aspire AppHost as the primary local orchestrator for API, worker, Web, PostgreSQL, RabbitMQ, service discovery, health checks, and the local telemetry dashboard.
- Provide Docker Compose as an infrastructure-only alternative for PostgreSQL and RabbitMQ.
- Expose `/health` for process liveness and `/ready` for all registered dependencies. External dependency failure must remove readiness but must not trigger a liveness restart loop.
- Use Serilog compact JSON on standard output for logs.
- Use OpenTelemetry and OTLP for traces and metrics. Do not depend on the beta in-process Prometheus exporter; the Milestone 6 collector will translate OTLP metrics for Prometheus.
- Keep health payloads and metric labels free of exception details, credentials, connection strings, request bodies, raw carrier payloads, and high-cardinality identifiers. Structured logs may carry validated correlation and trace IDs for investigation.
- Run the worker on the ASP.NET Core host so it has the same probe surface while remaining a separate background process.

## Consequences

- Developers need a running Linux container engine for AppHost infrastructure.
- AppHost and Compose should not run simultaneously with their default ports and volume names.
- Production deployment manifests, Prometheus, Grafana, Tempo, retention, and alerts remain separate Milestone 6 work.
- Worker readiness can detect a stalled background loop through its heartbeat without restarting a healthy process solely because PostgreSQL or RabbitMQ is unavailable.
