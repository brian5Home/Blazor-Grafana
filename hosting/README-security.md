## Securing the OTel Collector with a Token

This document describes a **simple token-based approach** to securing the OpenTelemetry Collector endpoint used by your applications.

The high-level idea:
- Expose the collector only behind a **reverse proxy** (e.g. Nginx, Traefik, API gateway).
- Require a **static bearer token** on incoming requests.
- Configure your .NET applications to send that token via OTLP headers.

> Note: The sample `docker-compose.yml` in this repo is intentionally open for local development. Use these patterns when you expose the collector in shared or higher environments.

---

### 1. Recommended architecture

- **Collector runs on an internal network**, not directly exposed to the public internet.
- A **reverse proxy** listens on the public port (e.g. `443`), validates an **`Authorization: Bearer <TOKEN>`** header, and only forwards requests with a valid token to the collector.
- Applications are configured with:
  - The proxy URL as the OTLP endpoint.
  - The same shared token, sent as an OTLP header.

This keeps token validation logic in one place (the proxy) and avoids custom auth logic in the collector itself.

---

### 2. Example: Nginx reverse proxy with bearer token

#### 2.1 Basic Nginx config

Example snippet that:
- Listens on port 443 (TLS omitted here for brevity).
- Checks for a fixed bearer token.
- Proxies valid traffic to the collector (`otel-collector:4317` for OTLP gRPC via HTTP/2).

```nginx
server {
    listen 443 ssl http2;
    server_name otel.example.com;

    # ssl_certificate /path/to/cert.pem;
    # ssl_certificate_key /path/to/key.pem;

    # Shared secret token
    set $expected_token "YOUR_SECURE_RANDOM_TOKEN_HERE";

    location / {
        if ($http_authorization !~* "^Bearer\s+$expected_token$") {
            return 401;
        }

        proxy_pass http://otel-collector:4317;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $remote_addr;
    }
}
```

You would run this Nginx container on the same Docker network as `otel-collector` and expose only Nginx externally.

---

### 3. .NET configuration: sending the token

You can pass custom headers for OTLP exporters either via configuration (e.g. `appsettings.json`) or via standard **OTEL environment variables**.

#### 3.1 `appsettings.json` with headers

```json
{
  "Otlp": {
    "Endpoint": "https://otel.example.com",  // Nginx / proxy URL
    "Headers": {
      "Authorization": "Bearer YOUR_SECURE_RANDOM_TOKEN_HERE"
    }
  }
}
```

`Program.cs`:

```csharp
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var endpoint = builder.Configuration["Otlp:Endpoint"] ?? "https://otel.example.com";
var authHeader = builder.Configuration["Otlp:Headers:Authorization"];

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("MySecureService"))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation();
        t.AddHttpClientInstrumentation();
        t.AddOtlpExporter(o =>
        {
            o.Endpoint = new Uri(endpoint);
            if (!string.IsNullOrWhiteSpace(authHeader))
            {
                o.Headers = $"Authorization={authHeader}";
            }
        });
    })
    .WithLogging(l =>
    {
        l.AddOtlpExporter(o =>
        {
            o.Endpoint = new Uri(endpoint);
            if (!string.IsNullOrWhiteSpace(authHeader))
            {
                o.Headers = $"Authorization={authHeader}";
            }
        });
        l.AddConsoleExporter();
    });

var app = builder.Build();
app.Run();
```

This sends **both traces and logs** to the collector through the proxy, including the `Authorization` header.

#### 3.2 Using OTEL env vars for headers

You can also use `OTEL_EXPORTER_OTLP_HEADERS` to define headers in a portable way:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=https://otel.example.com
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
OTEL_SERVICE_NAME=MySecureService
OTEL_EXPORTER_OTLP_HEADERS=Authorization=Bearer%20YOUR_SECURE_RANDOM_TOKEN_HERE
```

Then in `Program.cs`, you can keep the exporter configuration minimal:

```csharp
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(builder.Configuration["OTEL_SERVICE_NAME"] ?? "MySecureService"))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation();
        t.AddHttpClientInstrumentation();
        t.AddOtlpExporter(); // uses OTEL_* env vars, including headers
    })
    .WithLogging(l =>
    {
        l.AddOtlpExporter(); // uses OTEL_* env vars, including headers
        l.AddConsoleExporter();
    });

var app = builder.Build();
app.Run();
```

---

### 4. Token management recommendations

- **Generate strong random tokens** (at least 32+ bytes of entropy).
- Store tokens in **secret managers** (e.g. Azure Key Vault, AWS Secrets Manager, Kubernetes secrets) rather than in plain config files.
- Rotate tokens regularly and keep an overlap window where both old and new tokens are accepted during rollout.
- Use **TLS** (HTTPS) to prevent token leakage in transit.

