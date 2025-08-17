# BusyBee demo app

A minimal web app showcasing BusyBee: a background job queue with OpenTelemetry, Swagger, Prometheus metrics, and Seq for
centralized logging and tracing.

## Quick start (Docker Compose)

Requirements: Docker + Docker Compose.

```bash
cd examples/DemoApp
docker compose up -d
```

Stop and clean up:

```bash
docker compose down -v
```

## Services and URLs

- Demo API: http://localhost:8100
    - Swagger UI: http://localhost:8100/swagger
    - Prometheus metrics: http://localhost:8100/metrics
- Seq (logs & tracing): http://localhost:8200
    - The app exports OTLP logs and traces to Seq.
- Prometheus (metrics): http://localhost:8300
    - Scrapes the app’s /metrics endpoint (see prometheus.yml).

## API examples

- Enqueue a job (POST /queue):

```bash
curl -X POST http://localhost:8100/queue
```

Response contains the JobId; the job runs asynchronously. Logs and traces are sent to Seq. Jobs may fail or timeout randomly to demonstrate error handling.

- Get aggregated execution log (GET /queue):

```bash
curl http://localhost:8100/queue
```

Returns a grouped log of job events ordered by enqueue and event timestamps.

## Helpful links

- Not familiar with Seq? Here is a quick intro to [traces view](https://datalust.co/docs/tracing#viewing-a-trace).