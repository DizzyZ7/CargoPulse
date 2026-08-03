# Local development runbook

## Purpose

Start and verify the CargoPulse Milestone 0 foundation on a Windows development machine.

## Prerequisites

```powershell
dotnet --version
docker info
docker compose version
```

Expected .NET SDK: `10.0.302` or a compatible newer `10.0.3xx` patch. `docker info` must show a running Linux engine; having only the Docker CLI installed is insufficient.

## Mode A: Aspire (recommended)

Do not start the Compose infrastructure first.

```powershell
dotnet restore CargoPulse.sln --locked-mode
dotnet run --project src/CargoPulse.AppHost
```

Open the Aspire dashboard URL printed in the terminal. Wait for `postgres`, `messaging`, `api`, `worker`, and `web` to report healthy. Open the Web endpoint from the dashboard.

Verify the API and worker through the endpoint links assigned by Aspire:

```text
GET /health  -> 200 when the process is alive
GET /ready   -> 200 only when registered dependencies are ready
```

Stop AppHost with `Ctrl+C`. Its named PostgreSQL and RabbitMQ volumes are intentionally retained.

## Mode B: Docker Compose infrastructure

Do not run AppHost simultaneously with default ports.

1. Create the local environment file.

   ```powershell
   Copy-Item .env.example .env
   ```

2. Generate separate random values for PostgreSQL and RabbitMQ.

   ```powershell
   [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32)).ToLowerInvariant()
   ```

3. Put the values in `.env`, then validate and start infrastructure.

   ```powershell
   docker compose config --quiet
   docker compose up -d --wait
   docker compose ps
   ```

4. Configure `ConnectionStrings__cargopulse` and `ConnectionStrings__messaging` in the process environment before starting the API/worker directly. Never commit these values. The default host configuration does not load an arbitrary `appsettings.Local.json`.

5. Stop containers while retaining data.

   ```powershell
   docker compose down
   ```

Deleting named volumes permanently removes local PostgreSQL/RabbitMQ data and is intentionally not part of the normal stop procedure.

## Verification without containers

The isolated test factories start API, worker, and Web in process. They inject non-routable placeholder connection strings so the production composition remains fail-closed, then remove only the external PostgreSQL and RabbitMQ health checks:

```powershell
dotnet restore CargoPulse.sln --locked-mode
dotnet build CargoPulse.sln --configuration Release --no-restore
dotnet test CargoPulse.sln --configuration Release --no-build -- --minimum-expected-tests 7
```

This verifies HTTP foundation behavior, not PostgreSQL or RabbitMQ connectivity. Container-backed integration tests begin with the persistence and messaging milestones.

## Troubleshooting

### AppHost reports that no container runtime is available

Start Docker Desktop and wait for `docker info` to return both Client and Server sections. Retry AppHost.

### `dotnet` selects SDK 8 or reports that 10.0.302 is missing

Install .NET SDK 10.0.302 and ensure its installation directory precedes older installations on `PATH`. A runtime installation alone cannot build the solution.

### Restore times out against NuGet

Retry with sequential downloads after checking network access:

```powershell
$env:NUGET_HTTP_TIMEOUT_SECONDS = "300"
dotnet restore CargoPulse.sln --locked-mode --disable-parallel
```

Do not bypass vulnerability audit warnings to work around a network error.

### `/health` is healthy but `/ready` is not

This is expected when PostgreSQL, RabbitMQ, or the worker heartbeat is unavailable. Inspect the sanitized check names in `/ready`, then use the Aspire resource logs or `docker compose ps` to identify the dependency.
