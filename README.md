# OPUS Stack Starter (.NET 9)

A production-ready microservices platform built with .NET 9, featuring comprehensive observability, security, and data persistence.

## Architecture

### Microservices
- **Gateway API** - API gateway with JWT authentication and CORS
- **Customer API** - Customer management with RavenDB and event publishing
- **Telemetry API** - IoT data ingestion via MQTT with TimescaleDB storage
- **Workflow API** - Workflow orchestration service
- **Files API** - Blob storage and Parquet file export

### Infrastructure
- **RavenDB** - Document database
- **TimescaleDB** - Time-series PostgreSQL with 90-day retention
- **RabbitMQ** - Message broker for event-driven architecture
- **MQTT (Mosquitto)** - IoT device communication
- **Azurite** - Local Azure Blob Storage emulator
- **Prometheus** - Metrics collection and alerting
- **Grafana** - Metrics visualization and dashboards

## Features

### Observability
- OpenTelemetry instrumentation on all APIs
- Prometheus metrics at `/metrics` endpoints
- Distributed tracing for HTTP requests
- Grafana dashboards (port 3000, admin/admin)

### Security
- JWT Bearer authentication
- Configurable validation (disabled for local dev)
- CORS enabled on Gateway
- Swagger UI on all APIs

### Data Persistence
- RavenDB database: `opus`
- TimescaleDB hypertable: `telemetry` (90-day retention policy)
- Azurite containers: `telemetry`, `observability-test`

## Quick Start

### Prerequisites
- Docker Desktop
- .NET 9.0 SDK
- Azure CLI (for blob operations)

### Local Development

1. Start all services:
```bash
cd deploy/compose
docker compose up -d
```

2. Verify services:
```bash
curl http://localhost:8088/health  # Gateway
curl http://localhost:8088/metrics # Prometheus metrics
```

3. Access UIs:
- Gateway Swagger: http://localhost:8088/swagger
- Files Swagger: http://localhost:8089/swagger
- RavenDB Studio: http://localhost:8080
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3000 (admin/admin)
- RabbitMQ Management: http://localhost:15672 (guest/guest)

### Hot Reload Development

Run individual services with hot reload:
```bash
cd src/services/gateway/Opus.Gateway.Api
dotnet watch run
```

## CI/CD Setup

The repository includes a GitHub Actions workflow that automatically builds and pushes Docker images to GitHub Container Registry.

### Required Setup

**No additional secrets required!** The workflow uses `GITHUB_TOKEN` which is automatically provided by GitHub Actions.

### How It Works

1. **On Push to Main**: Builds all 5 service images and pushes to GHCR
2. **On Pull Request**: Builds images and runs tests (no push)
3. **Image Tagging**:
   - `latest` - Latest main branch
   - `main-<sha>` - Specific commit on main
   - `pr-<number>` - Pull request builds

### Image Locations

After the workflow runs, images will be available at:
```
ghcr.io/<your-username>/opus-stack-starter-full/gateway:latest
ghcr.io/<your-username>/opus-stack-starter-full/customer:latest
ghcr.io/<your-username>/opus-stack-starter-full/telemetry:latest
ghcr.io/<your-username>/opus-stack-starter-full/workflow:latest
ghcr.io/<your-username>/opus-stack-starter-full/files:latest
```

### Making Images Public (Optional)

By default, GHCR images are private. To make them public:

1. Go to your GitHub profile → Packages
2. Click on each package (opus-stack-starter-full/gateway, etc.)
3. Package Settings → Change visibility → Public

## API Endpoints

### Gateway API (port 8088)
- `GET /health` - Health check
- `GET /gateway/ping` - Simple ping
- `GET /metrics` - Prometheus metrics

### Customer API
- `GET /customers/{id}` - Get customer by ID
- `POST /customers` - Create customer (publishes event to RabbitMQ)

### Telemetry API
- `GET /metrics/latest/{deviceId}` - Get latest 50 metrics for device
- Subscribes to MQTT topic: `device/+/+`

### Workflow API
- `POST /workflow/start/{type}` - Start workflow instance

### Files API (port 8089)
- `POST /export/parquet/{container}/{name}` - Generate and upload parquet file

## Configuration

All environment variables are in [deploy/compose/.env](deploy/compose/.env):

- **JWT**: Validation disabled for local dev
- **Databases**: Connection strings for RavenDB, TimescaleDB
- **Message Brokers**: RabbitMQ, MQTT endpoints
- **Storage**: Azurite blob storage configuration

## Building

Build all services:
```bash
cd src
dotnet build
```

Build individual service:
```bash
cd src/services/gateway/Opus.Gateway.Api
dotnet build
```

## Testing

Run parquet export:
```bash
curl -X POST http://localhost:8089/export/parquet/test-metrics/run2
```

Download blob:
```bash
az storage blob download \
  -c telemetry \
  -n run1 \
  -f run1.parquet \
  --connection-string "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"
```

## Project Structure

```
opus-stack-starter-full/
├── src/
│   ├── building-blocks/
│   │   ├── Opus.Messaging/      # RabbitMQ messaging
│   │   ├── Opus.Security/       # JWT authentication
│   │   └── Opus.Storage/        # Blob storage + Parquet
│   └── services/
│       ├── gateway/Opus.Gateway.Api/
│       ├── customer/Opus.Customer.Api/
│       ├── telemetry/Opus.Telemetry.Api/
│       ├── workflow/Opus.Workflow.Api/
│       └── files/Opus.Files.Api/
├── deploy/
│   └── compose/
│       ├── docker-compose.yml
│       ├── prometheus.yml
│       └── .env
└── .github/
    └── workflows/
        └── ci-cd.yml
```

## Technologies

- **.NET 9.0** - Modern C# with minimal APIs
- **Docker Compose** - Local orchestration
- **OpenTelemetry** - Observability standard
- **RavenDB 6.0** - NoSQL document database
- **TimescaleDB 2.14** - Time-series PostgreSQL
- **RabbitMQ 3** - AMQP message broker
- **Mosquitto 2** - MQTT broker
- **Prometheus** - Metrics collection
- **Grafana** - Visualization
- **GitHub Actions** - CI/CD automation

## License

MIT
