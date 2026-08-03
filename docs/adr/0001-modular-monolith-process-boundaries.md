# ADR-0001: Modular monolith with explicit process boundaries

- Status: Accepted
- Date: 2026-08-03

## Context

CargoPulse needs strong consistency for shipment state, SLA evaluation, exception ownership, and audit records while integrating asynchronous carrier events. Premature microservices would multiply deployment, transaction, observability, and test complexity before domain boundaries are validated.

At the same time, background retries and scheduled evaluation must not share the API request lifecycle.

## Decision

Implement CargoPulse as a modular monolith with three deployable application processes:

1. `CargoPulse.Api` is the HTTP composition root.
2. `CargoPulse.Worker` is a separate background process for asynchronous and scheduled work.
3. `CargoPulse.Web` is the Blazor operator host and communicates over typed HTTP rather than referencing API implementation assemblies.

PostgreSQL is the source-of-truth technology and RabbitMQ is the asynchronous transport. The delivery claim is at-least-once. Transactional outbox/inbox and idempotent consumers will make duplicate delivery safe; the system will not claim exactly-once processing.

`CargoPulse.ServiceDefaults` may contain shared hosting concerns only: health checks, HTTP resilience/service discovery, telemetry, structured logging, and correlation. It must not become a shared domain dumping ground.

Domain module and building-block projects are created only when they contain executable domain behavior. Empty interfaces and repositories are not scaffolded in Milestone 0.

## Consequences

### Positive

- Business operations can use PostgreSQL transactions across modules where consistency requires them.
- Domain boundaries remain explicit without multiplying network hops and deployment units.
- The worker can restart and scale independently from request handling.
- The UI remains decoupled from API implementation details.
- A future module extraction remains possible after measurements demonstrate a real need.

### Negative

- API and worker deployments must remain version-compatible with one database schema.
- Module boundaries need architecture tests once the first modules exist.
- Long-running or blocking work must never execute in API request handlers.
- Independent scaling is limited to the explicit API, worker, and Web process boundaries.

## Revisit when

A module has independently measured scaling, availability, security, or team-ownership requirements that cannot be met within these process boundaries.
