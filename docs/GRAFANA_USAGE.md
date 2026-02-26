# How to Use Grafana: View Data & Add Data Sources

This guide explains how to **see the telemetry data** sent to Grafana (traces and logs) and how to **add a data source** in Grafana.

**Prerequisites:** Grafana is running at http://localhost:3000. Log in with **admin** / **admin** (see [CREDENTIALS.md](CREDENTIALS.md)).

**All Grafana parts:** For a checklist of every Grafana-related part in this project and where it is documented, see [GRAFANA_PARTS.md](GRAFANA_PARTS.md).

---

## 1. See the data sent to Grafana

The Blazor app and API send **traces** (and optionally logs) via OpenTelemetry to the OpenTelemetry Collector, which forwards them to **Tempo** (traces) and **Loki** (logs). Grafana is already configured with **Tempo** and **Loki** datasources, so you can view that data in **Explore**.

### Generate some traffic first

1. Open the Blazor app: http://localhost:5000  
2. Go to **Products** – list, add, or edit a product.  
3. Go to **Reports** – load the summary.  

Each page load and API call produces traces. After a short delay (seconds), they appear in Tempo.

---

### View traces (Tempo)

Traces show the flow of requests: Blazor → API → database, with timing and spans.

1. In Grafana, open **Explore** (compass icon in the left sidebar).  
2. At the top, choose the **Tempo** data source from the dropdown.  
3. **Search** tab (default):  
   - **Service name:** e.g. `BlazorGrafanaApp.Blazor` or `BlazorGrafanaApp.Api`  
   - Set **Time range** (e.g. “Last 15 minutes”)  
   - Click **Run query**.  
4. Click a trace in the results to see the full trace with spans (HTTP requests, DB calls, etc.).  

**Tip:** Use **Search** by service name to find traces from the Blazor app or the API. You’ll see spans for HTTP requests and, for the API, Entity Framework/database operations.

---

### View logs (Loki)

The Blazor app and API export **logs** via OpenTelemetry (OTLP) to the collector, which forwards them to Loki. Use Loki in Grafana to view these logs.

1. In Grafana, open **Explore**.  
2. At the top, choose the **Loki** data source.  
3. **Code** tab: run a LogQL query (see below).  
4. Set **Time range** (e.g. “Last 15 minutes”) and click **Run query**.  

**Useful queries for app logs:**  
- All logs from the API: `{service_name="BlazorGrafanaApp.Api"}` or `{job="BlazorGrafanaApp.Api"}`  
- All logs from the Blazor app: `{service_name="BlazorGrafanaApp.Blazor"}` or `{job="BlazorGrafanaApp.Blazor"}`  
- Both: `{service_name=~"BlazorGrafanaApp.(Api|Blazor)"}`  
- Any app logs (if label names differ): `{job=~".+"}` or `{}` with a short time range  

**If you don’t see logs:**  
1. **Confirm the app is logging:** run `docker compose logs api` (or `docker compose logs blazorapp`). You should see OpenTelemetry log lines (e.g. “Listing all products”, “Generating report summary”). If not, the app isn’t emitting logs.  
2. **Generate traffic:** open the Blazor app (Products, Reports), then in Grafana set the time range to “Last 5 minutes” and run the query again.  
3. **No caching:** Loki and the collector do not cache log results; each query runs against stored data. Use “Last 5 minutes” or “Last 15 minutes” and click **Run query** again after generating new traffic.  
4. **Loki 3.x + OTLP:** This stack uses Loki 3.x with native OTLP ingestion. The collector sends logs to Loki at `/otlp/v1/logs`. If you upgraded from Loki 2.x, run `docker compose up -d --force-recreate loki otel-collector` so both use the new setup.  

Loki is set as the **default** data source in this project.

---

### Trace-to-logs (already configured)

The Tempo datasource is provisioned with **traces to logs** linking: from a span in Tempo you can jump to related logs in Loki (e.g. “Logs for this span”). That works automatically once logs are flowing to Loki; no extra setup needed.

---

## 2. Add a data source

You can add data sources in the UI or by provisioning (YAML). The stack already includes **Tempo** and **Loki** via provisioning.

### Add a data source in the UI

1. Log in to Grafana (http://localhost:3000).  
2. Go to **Connections** → **Data sources** (or **Administration** → **Data sources** in older versions).  
3. Click **Add data source**.  
4. Choose a type (e.g. **Tempo**, **Loki**, **Prometheus**, **MySQL**, etc.).  
5. Fill in the settings. For our Docker setup:  
   - **Tempo:** URL `http://tempo:3200` (from another container).  
     - From the host, if Grafana ran outside Docker, you’d use `http://localhost:3200`.  
   - **Loki:** URL `http://loki:3100` (from another container).  
     - From the host, `http://localhost:3100`.  
6. Click **Save & test**. Grafana will check the connection.  

**Note:** When Grafana runs in Docker (as in this project), use **service names** as hostnames (`tempo`, `loki`) so Grafana reaches other containers on the same Docker network. When Grafana runs on the host, use `localhost` and the published ports (e.g. 3200, 3100).

### Add a data source by provisioning (YAML)

Data sources can be defined in YAML so they appear automatically (e.g. after a restart). This project already does that for Tempo and Loki.

**Location:** `docker/grafana/provisioning/datasources/datasources.yaml`

**What’s provisioned today:**  
- **Tempo** – URL `http://tempo:3200`, with **Trace to logs** pointing at Loki (so you can jump from a span to related logs).  
- **Loki** – URL `http://loki:3100`, set as the default data source.

**Example – add a new datasource** (e.g. Prometheus):

```yaml
apiVersion: 1
datasources:
  # Existing Tempo and Loki entries stay as-is ...
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
    isDefault: false
```

Then **restart the Grafana container** so it reloads provisioning:

```bash
docker compose restart grafana
```

**Fields you’ll often use:**

- **name** – Label in the Grafana UI.  
- **type** – e.g. `tempo`, `loki`, `prometheus`, `mysql`.  
- **url** – Backend URL (use Docker service name when Grafana runs in Docker).  
- **access** – `proxy` (Grafana proxies requests) or `direct` (browser talks to the backend). Prefer `proxy` when possible.  
- **isDefault** – If `true`, this datasource is selected by default in Explore.  

After adding a data source (UI or YAML), you can use it in **Explore** and in **dashboards** to see the data sent to Grafana (traces in Tempo, logs in Loki, or metrics in Prometheus if you add it).

---

## 3. Videos: Setup and use Grafana (current form)

These videos from **Grafana Labs** cover setup, data sources, Explore (logs, metrics, traces), and dashboards in the same style of Grafana you’re running (self‑hosted with Tempo and Loki).

### Getting started and basics

| Video | Description |
|-------|-------------|
| [Grafana for Beginners (playlist)](https://www.youtube.com/playlist?list=PLDGkOdUX1Ujo27m6qiTPPCpFHVfyKq9jT) | Full beginner series: setup, datasources, Explore, dashboards, visualizations. |
| [Getting Started with Grafana Play](https://www.youtube.com/watch?v=wc9wR6azCQY) | Learn and explore Grafana using Grafana Play. |

### Data sources (Tempo, Loki, and more)

| Video | Description |
|-------|-------------|
| [Adding data sources (Loki, Tempo, and Mimir) \| Grafana for Beginners Ep. 6](https://www.youtube.com/watch?v=cqHO0oYW6Ic) | How to add and configure Loki, Tempo, and Mimir in Grafana (matches our stack). |

### Explore: logs, metrics, and traces

| Video | Description |
|-------|-------------|
| [Exploring logs, metrics, and traces with Grafana \| Grafana for Beginners Ep. 7](https://www.youtube.com/watch?v=1q3YzX2DDM4) | Using **Explore** for logs, metrics, and traces (same workflow as viewing Tempo and Loki here). |

### Traces and dashboards

| Video | Description |
|-------|-------------|
| [Traces Drilldown (Explore Traces) – Demo](https://www.youtube.com/watch?v=a3uB1C2oHA4) | Viewing and drilling into trace data in Grafana (Tempo-style usage). |
| [Understanding Dashboards: Panels, visualizations, queries](https://www.youtube.com/watch?v=vTiIkdDwT-0) | How dashboards, panels, and queries work. |
| [Creating visualizations \| Grafana for Beginners Ep. 9](https://www.youtube.com/watch?v=yNRnLyVntUw) | Building panels and visualizations (gauges, time series, logs, node graphs). |

All links point to the official **Grafana YouTube channel**. UI paths (e.g. **Connections** → **Data sources**, **Explore**) align with the Grafana version used in this project.

---

## Summary

| Goal | Where to go |
|------|---------------------|
| **See traces** (Blazor ↔ API) | **Explore** → data source **Tempo** → Search by service name (`BlazorGrafanaApp.Blazor`, `BlazorGrafanaApp.Api`) |
| **See logs** | **Explore** → data source **Loki** → Run a LogQL query or use Log browser |
| **Add a data source (UI)** | **Connections** → **Data sources** → **Add data source** → choose type, set URL, **Save & test** |
| **Add a data source (YAML)** | Edit `docker/grafana/provisioning/datasources/datasources.yaml`, then `docker compose restart grafana` |

**Documentation:**  
- [Grafana Explore](https://grafana.com/docs/grafana/latest/explore/)  
- [Grafana data sources](https://grafana.com/docs/grafana/latest/administration/data-source-management/)  
- [Grafana provisioning](https://grafana.com/docs/grafana/latest/administration/provisioning/#data-sources)

**Videos:** See [§ 3. Videos](#3-videos-setup-and-use-grafana-current-form) above for setup, data sources, Explore, and traces.
