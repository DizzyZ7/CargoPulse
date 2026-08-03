# Security policy

## Supported status

CargoPulse is an early MVP foundation and is not production-ready. Milestone 0 does not implement application authentication, authorization, carrier webhooks, customer data, or shipment persistence.

## Reporting a vulnerability

Do not open a public issue containing exploit details, credentials, shipment data, or personal information. Use the repository's private GitHub Security Advisory reporting flow. Include the affected revision, reproduction conditions, impact, and a minimal sanitized proof of concept.

## Milestone 0 controls

- No real credentials, tokens, or keys are committed.
- Compose requires explicit local PostgreSQL and RabbitMQ passwords and binds ports to loopback.
- API CORS has no allowed origins by default.
- Non-preflight API requests have an immediate-network-peer-partitioned rate limit and a 1 MiB body limit. A trusted reverse-proxy configuration is required before treating forwarded client addresses as the partition key.
- Health probes bypass the application quota and must be restricted to a management network by the production ingress.
- Problem Details and health responses omit exception and connection details.
- Correlation IDs are length- and character-validated before logging or propagation.
- Logs are structured JSON and must not include bodies, credentials, cookies, raw tracking payloads, or connection strings.
- Runtime containers use chiseled .NET images and an explicit non-root user.
- NuGet direct and transitive dependencies are locked and audited; warnings fail the build.
- CI uses least-privilege workflow permissions, full commit pins for third-party actions, CodeQL, and dependency updates.

## Initial threat model

| Boundary or asset | Representative threat | Current mitigation | Remaining work |
|---|---|---|---|
| Public API process | oversized or high-rate requests | body limit, immediate-peer rate limiter, sanitized errors | trusted proxy policy, ingress limits, endpoint policies, authentication |
| Correlation/log pipeline | log injection or secret disclosure | constrained header alphabet, no body logging | domain data classification and audit redaction tests |
| PostgreSQL credentials | committed or broadly exposed secret | local untracked `.env`, Aspire secret parameters | production secret manager and rotation |
| RabbitMQ credentials | default or reused credentials | explicit local secret, non-default user/vhost | TLS, least-privilege broker permissions, rotation |
| Container runtime | privilege escalation | non-root chiseled app images | image scanning and deployed runtime policy |
| Dependency graph | known vulnerable transitive package | lock files, NuGet audit, Dependabot, CodeQL | SBOM and signed release provenance |
| Future carrier webhook | spoofing, replay, payload abuse | not exposed in Milestone 0 | HMAC, timestamp window, replay store, size/rate policies |
| Future user actions | unauthorized access or repudiation | not exposed in Milestone 0 | JWT, RBAC, optimistic concurrency, audit trail |

## Explicitly not implemented yet

JWT/RBAC, integration-client credentials, password hashing, carrier-secret encryption, HMAC verification, replay protection, domain audit records, notification egress controls, and production TLS/CORS policy arrive with the features they protect. Their absence is a blocker to production use, not an accepted production risk.
