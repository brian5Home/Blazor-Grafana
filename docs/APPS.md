# Application & Service Documentation

Overview of each application and service in the Blazor Grafana solution, with links to official documentation for further reading.

---

## Blazor App (Blazor Server)

**Purpose:** Web UI for product management and reporting. Renders the Products data table, CRUD modal, and Reports summary. Calls the API over HTTP and exports OpenTelemetry traces to the collector.

**Location:** `BlazorApp/`  
**URL (Docker):** http://localhost:5000  

**Config:** `BlazorApp/appsettings.json`  
- `Api:BaseUrl` – API base URL (e.g. `http://api:8080` in Docker)  
- `Otlp:Endpoint` – OTLP gRPC endpoint for telemetry (e.g. `http://otel-collector:4317`)

**Key tech:** .NET 8, Blazor Server (interactive), HttpClient, OpenTelemetry (traces).

### Further reading

- [Blazor documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [Blazor Server](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-server)
- [.NET 8](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)

---

## API (ASP.NET Core Web API)

**Purpose:** REST API for products (CRUD) and report summary. Uses Entity Framework Core and SQL Server. Exports OpenTelemetry traces (and optionally logs) to the collector.

**Location:** `Api/`  
**URL (Docker):** http://localhost:5080  

**Endpoints:**
- `GET /api/products` – list products  
- `GET /api/products/{id}` – get one product  
- `POST /api/products` – create product  
- `PUT /api/products/{id}` – update product  
- `DELETE /api/products/{id}` – delete product  
- `GET /api/reports/summary` – report summary (counts, by category, low stock)

**Config:** `Api/appsettings.json`  
- `ConnectionStrings:DefaultConnection` – SQL Server connection string  
- `Otlp:Endpoint` – OTLP gRPC endpoint for telemetry  

**Key tech:** .NET 8, ASP.NET Core Web API, EF Core, SQL Server, OpenTelemetry (traces, EF instrumentation).

### Further reading

- [ASP.NET Core Web API](https://learn.microsoft.com/en-us/aspnet/core/web-api/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/dotnet/)

---

## BlazorGrafanaApp.Core (Class library)

**Purpose:** Shared data layer: EF Core entities (`Product`) and `AppDbContext`. Referenced by the API; migrations are in this project.

**Location:** `BlazorGrafanaApp.Core/`  

**Key types:** `Entities/Product.cs`, `Data/AppDbContext.cs`, `Migrations/`  

### Further reading

- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [EF Core with SQL Server](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/)

---

## SQL Server

**Purpose:** Relational database for the API. Stores the `Products` table; EF Core runs migrations on startup.

**Image:** `mcr.microsoft.com/mssql/server:2022-latest`  
**Port:** 1433  

**Credentials:** See [CREDENTIALS.md](CREDENTIALS.md#sql-server).

### Further reading

- [SQL Server on Docker](https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker)
- [SQL Server documentation](https://learn.microsoft.com/en-us/sql/sql-server/)

---

## Grafana

**Purpose:** Observability UI: dashboards and Explore for Tempo (traces) and Loki (logs). Datasources are provisioned via `docker/grafana/provisioning/datasources/`.

**Image:** `grafana/grafana:11.2.0`  
**URL:** http://localhost:3000  

**Credentials:** See [CREDENTIALS.md](CREDENTIALS.md#grafana).

**Usage:** Explore → Tempo (trace by service name, e.g. `BlazorGrafanaApp.Blazor`, `BlazorGrafanaApp.Api`); Explore → Loki for logs. For a checklist of all Grafana parts and where they are documented, see [GRAFANA_PARTS.md](GRAFANA_PARTS.md).

### Further reading

- [Grafana documentation](https://grafana.com/docs/grafana/latest/)
- [Grafana Explore](https://grafana.com/docs/grafana/latest/explore/)
- [Tempo datasource](https://grafana.com/docs/grafana/latest/datasources/tempo/)
- [Loki datasource](https://grafana.com/docs/grafana/latest/datasources/loki/)

---

## Loki

**Purpose:** Log store. Receives logs from the OpenTelemetry Collector (OTLP). Queried by Grafana for log exploration.

**Image:** `grafana/loki:3.0.0`  
**Port:** 3100  
**Config:** `docker/loki-config.yaml`  

### Further reading

- [Grafana Loki documentation](https://grafana.com/docs/loki/latest/)
- [Loki with Docker](https://grafana.com/docs/loki/latest/setup/install/docker/)
- [Loki OTLP ingestion](https://grafana.com/docs/loki/latest/send-data/otel/)

---

## Tempo

**Purpose:** Trace store. Receives traces from the OpenTelemetry Collector (OTLP). Queried by Grafana for distributed tracing (e.g. Blazor → API → DB).

**Image:** `grafana/tempo:2.3.1`  
**Ports:** 3200 (HTTP), 4317 (OTLP gRPC)  
**Config:** `docker/tempo-config.yaml`  

### Further reading

- [Grafana Tempo documentation](https://grafana.com/docs/tempo/latest/)
- [Tempo configuration](https://grafana.com/docs/tempo/latest/configuration/)
- [Tempo with Docker](https://grafana.com/docs/tempo/latest/setup/set-up-test-app/)

---

## OpenTelemetry Collector

**Purpose:** Receives OTLP (traces and logs) from the Blazor app and API, then exports traces to Tempo and logs to Loki.

**Image:** `otel/opentelemetry-collector-contrib:0.109.0`  
**Ports:** 4317 (gRPC), 4318 (HTTP) – host mapping 14317 / 14318  
**Config:** `docker/otel-collector-config.yaml`  

**Pipelines:**  
- Traces: OTLP → batch → Tempo  
- Logs: OTLP → batch → Loki (OTLP HTTP)

### Further reading

- [OpenTelemetry Collector documentation](https://opentelemetry.io/docs/collector/)
- [Collector configuration](https://opentelemetry.io/docs/collector/configuration/)
- [OTLP exporter](https://github.com/open-telemetry/opentelemetry-collector/blob/main/exporter/otlpexporter/README.md)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/dotnet/)
