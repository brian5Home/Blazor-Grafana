# Blazor Grafana App

A .NET 8 Blazor Server app with a Web API, SQL Server, OpenTelemetry, and a Grafana observability stack (Grafana, Loki, Tempo) running in Docker Compose.

**Repository name:** Use **Blazor-Grafana** (correct spelling of *Grafana*). If your folder or repo is named "Blazor-Graphana", rename it to **Blazor-Grafana**.

## Credentials

| Service     | URL / Target      | User   | Password           |
|------------|-------------------|--------|--------------------|
| **Grafana** | http://localhost:3000 | `admin` | `admin` |
| **SQL Server** | `localhost,1433`  | `sa`   | `YourStrong!Passw0rd` |

Full details and production notes: **[docs/CREDENTIALS.md](docs/CREDENTIALS.md)**.  
App-by-app documentation and links to official docs: **[docs/APPS.md](docs/APPS.md)**.

## Features

- **Blazor Server** – UI with a data table, CRUD for products, and a simple report
- **Web API** – REST API for products and report summary (traces and logs sent via OpenTelemetry)
- **SQL Server** – Entity Framework Core with a `Product` entity and migrations
- **OpenTelemetry** – Traces (and logs) from Blazor and API sent to an OpenTelemetry Collector
- **Grafana stack** – Collector → Tempo (traces), Loki (logs); Grafana for dashboards and Explore

## Quick Start (Docker Compose)

1. **Start everything**

   ```bash
   docker compose up -d --build
   ```

2. **Open the app and generate traffic**

   - Blazor app: http://localhost:5000  
   - Use **Products** (list, add, edit, delete) and **Reports** so the Blazor app calls the API and produces traces.

3. **View telemetry in Grafana**

   - Grafana: http://localhost:3000 — login: **admin** / **admin** (see [docs/CREDENTIALS.md](docs/CREDENTIALS.md))
   - **Explore** → choose **Tempo** → run a query (e.g. search by service name `BlazorGrafanaApp.Blazor` or `BlazorGrafanaApp.Api`) to see traces between the Blazor app and the API.
   - **Explore** → choose **Loki** to query logs.

## Running Locally (without Docker)

1. **SQL Server** – Run SQL Server (e.g. in Docker) and set the connection string in `Api/appsettings.Development.json`.

2. **API**

   ```bash
   cd Api
   dotnet run
   ```

   API will listen on http://localhost:5080 (or the port in launchSettings). It runs EF migrations and seeds a few products.

3. **Blazor**

   - In `BlazorApp/appsettings.Development.json` set `Api:BaseUrl` to your API URL (e.g. `http://localhost:5080`).
   - Optionally set `Otlp:Endpoint` to an OpenTelemetry Collector (e.g. `http://localhost:4317`) if you run the stack.

   ```bash
   cd BlazorApp
   dotnet run
   ```

4. **Optional: only the observability stack in Docker**

   ```bash
   docker compose up -d sqlserver loki tempo otel-collector grafana
   ```

   Then run the API and Blazor locally with `Otlp:Endpoint` pointing at `http://localhost:4317`.

## Solution Layout

- **BlazorApp** – Blazor Server UI (Products data table + CRUD, Reports), calls API, sends OTLP to the collector.
- **Api** – ASP.NET Core Web API (product CRUD + report summary), EF Core + SQL Server, sends OTLP to the collector.
- **BlazorGrafanaApp.Core** – Shared EF Core entities and `AppDbContext`; used by the API.

## Telemetry Flow

- Blazor and API use the OpenTelemetry .NET SDK and export **traces** (and **logs** where applicable) via **OTLP** to the **OpenTelemetry Collector** (gRPC on port 4317).
- The collector sends:
  - **Traces** → **Tempo**
  - **Logs** → **Loki**
- **Grafana** is provisioned with datasources for Tempo and Loki so you can inspect traces and logs and see calls between the Blazor app and the API.

## Ports

| Service        | Port (host) | Notes                    |
|----------------|-------------|---------------------------|
| Blazor App     | 5000        | Blazor Server UI          |
| API            | 5080        | REST API                  |
| SQL Server     | 1433        | Database                  |
| Grafana        | 3000        | Monitoring UI            |
| Loki           | 3100        | Logs                      |
| Tempo          | 3200        | Traces                    |
| OTEL Collector | 4317 (gRPC), 4318 (HTTP) | OTLP intake      |

## Configuration

- **API**: `Api/appsettings.json` – `ConnectionStrings:DefaultConnection`, `Otlp:Endpoint`.
- **Blazor**: `BlazorApp/appsettings.json` – `Api:BaseUrl`, `Otlp:Endpoint`.
- **Docker**: Overrides are set in `docker-compose.yml` (e.g. API and Blazor point to `otel-collector:4317` and Blazor to `api:8080`).

## Documentation

- **[docs/README.md](docs/README.md)** – Documentation index  
- **[docs/CREDENTIALS.md](docs/CREDENTIALS.md)** – Grafana, SQL Server, and other credentials  
- **[docs/APPS.md](docs/APPS.md)** – Per-app/service docs and links to official documentation (Blazor, ASP.NET Core, EF Core, Grafana, Loki, Tempo, OpenTelemetry)  
- **[docs/GRAFANA_USAGE.md](docs/GRAFANA_USAGE.md)** – How to see the data sent to Grafana (traces, logs) and how to add a data source  
- **[docs/GRAFANA_PARTS.md](docs/GRAFANA_PARTS.md)** – Checklist of all Grafana parts and where they are documented
