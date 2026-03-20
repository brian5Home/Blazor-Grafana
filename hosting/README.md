## Self-hosted Grafana + Loki + Tempo + OTel Collector

This folder contains a minimal `docker-compose` stack you can use as a shared observability backend for **multiple applications** running in Docker or elsewhere.

Stack:
- **Grafana**: UI for dashboards and exploring logs/traces.
- **Loki**: Log storage and query backend.
- **Tempo**: Distributed tracing backend.
- **OpenTelemetry Collector**: Central OTLP endpoint for your apps, forwards logs and traces to Loki and Tempo.

All services are on a single Docker network so any app container can send OTLP traffic to `otel-collector`.

---

### 1. Files

- **`docker-compose.yml`**  
  Orchestrates the four services and creates named volumes:
  - Grafana: `http://localhost:3000` (admin / admin)
  - Loki: `http://localhost:3100`
  - Tempo: `http://localhost:3200`
  - OTel Collector (for your apps):
    - OTLP gRPC: `otel-collector:4317`
    - OTLP HTTP: `otel-collector:4318`

- **`otel-collector-config.yaml`**  
  Collector config that:
  - Receives **logs and traces** over OTLP (gRPC/HTTP).
  - Exports **logs to Loki**.
  - Exports **traces to Tempo**.

- **`loki-config.yaml`**  
  Simple single-node Loki configuration using the local filesystem for storage. Fine for local/dev usage.

- **`tempo-config.yaml`**  
  Minimal single-node Tempo configuration, using local filesystem storage and enabling OTLP receivers.

---

### 2. Starting the stack

From the repo root:

```bash
cd hosting
docker compose up -d
```

Verify containers:

```bash
docker compose ps
```

Then open Grafana:

- URL: `http://localhost:3000`
- User: `admin`
- Password: `admin`

You can now add Loki and Tempo as data sources inside Grafana (usually auto-detects on default URLs):

- Loki: `http://loki:3100`
- Tempo: `http://tempo:3200`

---

### 3. Pointing applications at the Collector

Any application (in Docker or on your host) that uses OpenTelemetry can send logs and traces to the collector via OTLP:

- **OTLP gRPC endpoint**: `http://localhost:4317` (from host) or `http://otel-collector:4317` (from another container on the same Docker network).
- **OTLP HTTP endpoint**: `http://localhost:4318` (from host) or `http://otel-collector:4318` (from another container).

For example, the existing API in this solution uses:

- Configuration key: `Otlp:Endpoint`
- Typical value in Docker: `http://otel-collector:4317`

For other applications, set the OTLP exporter endpoint accordingly, e.g.:

- .NET: `OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317`
- Node.js: `OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317`

As long as they ship OTLP logs and traces to the collector, they will end up in Loki and Tempo and become visible in Grafana.

---

### 4. .NET examples for telemetry and logging

#### 4.1 Minimal ASP.NET Core setup (traces + logs)

`appsettings.json`:

```json
{
  "Otlp": {
    "Endpoint": "http://otel-collector:4317"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

`Program.cs` (for a typical .NET 8+ API running in Docker alongside this stack):

```csharp
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var otlpEndpoint = builder.Configuration["Otlp:Endpoint"] ?? "http://otel-collector:4317";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("MySampleService"))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation();
        t.AddHttpClientInstrumentation();
        t.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
    })
    .WithLogging(l =>
    {
        l.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
        l.AddConsoleExporter();
    });

// ... rest of your usual ASP.NET Core setup

var app = builder.Build();

app.MapGet("/ping", (ILogger<Program> logger) =>
{
    logger.LogInformation("Ping called");
    return Results.Ok("pong");
});

app.Run();
```

When this app runs in Docker on the same network as the `hosting` stack, set:

- `Otlp:Endpoint=http://otel-collector:4317`

Logs go to Loki, traces go to Tempo, and both are viewable in Grafana.

#### 4.2 Using environment variables in .NET

Instead of `Otlp:Endpoint` in config, you can rely entirely on the standard OTEL environment variables:

```bash
OTEL_SERVICE_NAME=MySampleService
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
```

In your `Program.cs`, use the default OpenTelemetry configuration:

```csharp
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(builder.Configuration["OTEL_SERVICE_NAME"] ?? "MySampleService"))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation();
        t.AddHttpClientInstrumentation();
        t.AddOtlpExporter(); // reads OTEL_* env vars
    })
    .WithLogging(l =>
    {
        l.AddOtlpExporter(); // reads OTEL_* env vars
        l.AddConsoleExporter();
    });

var app = builder.Build();
app.Run();
```

This is convenient when you want to reuse the same container image in multiple environments and only change the destination collector via environment variables.

---

### 5. Stopping and cleaning up

To stop the stack (keeping data volumes):

```bash
cd hosting
docker compose down
```

To remove containers **and** all persisted volumes:

```bash
cd hosting
docker compose down -v
```



Here’s how OTLP HTTP/protobuf URLs work and how to set them.

Default ports
Protocol	Default port	Example base URL
gRPC	4317	http://localhost:4317
HTTP/protobuf	4318	http://localhost:4318
Your code uses http://localhost:4317 (gRPC). For HTTP/protobuf you normally use port 4318 (or whatever your collector/backend exposes).

How to get the exact URLs
1. Local collector (e.g. OpenTelemetry Collector, Grafana Alloy)

HTTP/protobuf is usually on port 4318.
Base URL: http://<host>:4318
The SDK adds paths like /v1/traces, /v1/logs, /v1/metrics; you only configure the base.
2. Grafana Cloud

In Grafana: Connections → Add new connection → OpenTelemetry.
Copy the “OTLP Endpoint” URL and note “HTTP” vs “gRPC”.
HTTP/protobuf URL looks like:
https://otlp-gateway-prod-<region>.grafana.net/otlp
(exact host depends on your stack/region).
3. Self‑hosted Grafana (Grafana Stack)

If you use Grafana Alloy or OTel Collector in front:
Alloy HTTP OTLP: often http://<alloy-host>:4318.
Check the receiver config (e.g. otlp/http) and the port you exposed.
4. Other backends (Jaeger, Datadog, etc.)

Use the “OTLP” or “OpenTelemetry” section of their docs; they’ll give a base URL and say whether it’s gRPC (4317) or HTTP (4318).
In your app
Use one base URL per protocol. For HTTP/protobuf:

Set the base URL (e.g. http://localhost:4318 or your Grafana/collector URL).
Set the protocol to HTTP/protobuf in code or config.
Example using config and HTTP/protobuf in Startup.cs:

Config (e.g. appsettings.json):

{
  "Otlp": {
    "Endpoint": "http://localhost:4318",
    "Protocol": "HttpProtobuf"
  }
}
Summary:

gRPC: base URL with port 4317 (e.g. http://localhost:4317).
HTTP/protobuf: base URL with port 4318 (e.g. http://localhost:4318), or the HTTP URL your cloud/collector docs give.
You only set the base URL; the SDK adds /v1/traces, /v1/logs, /v1/metrics.
To use HTTP/protobuf in code, set o.Protocol = OtlpExportProtocol.HttpProtobuf and point o.Endpoint at that base URL (e.g. http://localhost:4318 or your Grafana/collector HTTP OTLP URL).

