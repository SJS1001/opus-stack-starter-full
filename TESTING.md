# Testing Guide

Complete guide for testing the Opus Stack at every level: local development, Docker Compose, CI/CD pipeline, and Kubernetes deployment.

## Table of Contents

1. [Quick Smoke Test](#quick-smoke-test)
2. [Local Development Testing](#local-development-testing)
3. [Docker Compose Testing](#docker-compose-testing)
4. [CI/CD Pipeline Testing](#cicd-pipeline-testing)
5. [Kubernetes Testing](#kubernetes-testing)
6. [Observability Testing](#observability-testing)
7. [End-to-End Testing](#end-to-end-testing)

---

## Quick Smoke Test

**Goal**: Verify everything works in 5 minutes

### 1. Start Docker Compose

```bash
cd deploy/compose
docker compose up -d
```

### 2. Wait for Services

```bash
# Wait for all containers to be healthy
sleep 30
docker compose ps
```

### 3. Run the Test Script

```bash
# Run from the root directory
./scripts/test-all.sh
```

You should see all tests pass ✓

---

## Local Development Testing

Test individual services during development with hot reload.

### Start a Service Locally

```bash
cd src/services/gateway/Opus.Gateway.Api
dotnet watch run
```

### Test Endpoints

```bash
# Health check
curl http://localhost:5000/health
# Expected: {"ok":true}

# Ping
curl http://localhost:5000/gateway/ping
# Expected: pong

# Swagger UI
open http://localhost:5000/swagger

# Metrics
curl http://localhost:5000/metrics
# Expected: Prometheus metrics output
```

### Test with Infrastructure

Even when running locally, you can connect to Dockerized infrastructure:

```bash
# Start only infrastructure
cd deploy/compose
docker compose up -d raven timescale rabbit mqtt azurite

# Run service locally (uses localhost connections)
cd ../../src/services/customer/Opus.Customer.Api
dotnet watch run
```

---

## Docker Compose Testing

Full stack testing with all services in containers.

### 1. Start All Services

```bash
cd deploy/compose
docker compose up -d
```

### 2. Verify All Containers Running

```bash
docker compose ps

# You should see:
# - gateway (healthy)
# - customer (healthy)
# - telemetry (healthy)
# - workflow (healthy)
# - files (healthy)
# - raven (healthy)
# - timescale (healthy)
# - rabbit (healthy)
# - mqtt (healthy)
# - azurite (healthy)
# - prometheus (healthy)
# - grafana (healthy)
```

### 3. Test Gateway API

```bash
# Health check
curl http://localhost:8088/health
# Expected: {"ok":true}

# Ping
curl http://localhost:8088/gateway/ping
# Expected: pong

# Metrics (OpenTelemetry)
curl -s http://localhost:8088/metrics | grep http_server_request_duration
# Expected: Histogram metrics
```

### 4. Test Customer API

```bash
# Create a customer
curl -X POST http://localhost:8088/customers \
  -H "Content-Type: application/json" \
  -d '{"id":"customer-001","name":"John Doe","email":"john@example.com"}'

# Get customer
curl http://localhost:8088/customers/customer-001
# Expected: {"id":"customer-001","name":"John Doe","email":"john@example.com"}
```

**Note**: Customer API is not exposed externally in docker-compose, you'd need to update the compose file or test through gateway if you add routing.

### 5. Test Files API

```bash
# Create and upload a Parquet file
curl -X POST http://localhost:8089/export/parquet/test-container/test-file-1
# Expected: {"container":"test-container","name":"test-file-1"}

# Verify blob exists in Azurite
az storage blob list \
  --container-name test-container \
  --connection-string "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"

# Download the file
az storage blob download \
  --container-name test-container \
  --name test-file-1 \
  --file ./test-file-1.parquet \
  --connection-string "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"

# Check file size
ls -lh test-file-1.parquet
```

### 6. Test Telemetry API (MQTT)

```bash
# Publish MQTT message (requires mosquitto-clients)
mosquitto_pub -h localhost -p 1883 -t "device/sensor-1/temperature" -m "23.5"

# Wait a moment for processing
sleep 2

# Query latest metrics (requires port-forwarding or exposing telemetry API)
# Note: Telemetry API is not exposed in docker-compose by default
```

### 7. Test RabbitMQ

```bash
# Access RabbitMQ Management UI
open http://localhost:15672
# Username: guest
# Password: guest

# Check queues and exchanges
# You should see customer.events exchange after creating a customer
```

### 8. Test RavenDB

```bash
# Access RavenDB Studio
open http://localhost:8080

# Verify database 'opus' exists
# Check for customer documents
```

### 9. Test TimescaleDB

```bash
# Connect to database
docker exec -it compose-timescale-1 psql -U postgres -d telemetry

# Check hypertable
\dt

# Check retention policy
SELECT * FROM timescaledb_information.jobs WHERE proc_name = 'policy_retention';

# Exit
\q
```

### 10. View Container Logs

```bash
# All services
docker compose logs -f

# Specific service
docker compose logs -f gateway
docker compose logs -f customer
docker compose logs -f files
```

---

## CI/CD Pipeline Testing

Verify GitHub Actions builds and pushes images correctly.

### 1. Check Workflow Status

Visit: https://github.com/SJS1001/opus-stack-starter-full/actions

You should see:
- ✓ CI/CD Pipeline workflow
- Status: Success (green checkmark)
- 5 build jobs (gateway, customer, telemetry, workflow, files)

### 2. Verify Images Pushed to GHCR

Visit: https://github.com/SJS1001?tab=packages

You should see 5 packages:
- `opus-stack-starter-full/gateway`
- `opus-stack-starter-full/customer`
- `opus-stack-starter-full/telemetry`
- `opus-stack-starter-full/workflow`
- `opus-stack-starter-full/files`

### 3. Pull and Test an Image Locally

```bash
# Pull gateway image
docker pull ghcr.io/sjs1001/opus-stack-starter-full/gateway:latest

# Run it
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  ghcr.io/sjs1001/opus-stack-starter-full/gateway:latest

# Test
curl http://localhost:8080/health
```

### 4. Trigger a New Build

```bash
# Make a small change
echo "# Test" >> README.md

# Commit and push
git add README.md
git commit -m "Test CI/CD pipeline"
git push origin main

# Watch the workflow run
open https://github.com/SJS1001/opus-stack-starter-full/actions
```

---

## Kubernetes Testing

Test deployment to a Kubernetes cluster.

### Prerequisites

```bash
# Verify Kubernetes is available
kubectl cluster-info

# Verify Helm is installed
helm version
```

### 1. Install the Helm Chart

```bash
cd deploy/k8s/helm

# Dry run to validate
helm install opus-stack ./opus-stack --dry-run --debug

# Actually install
helm install opus-stack ./opus-stack

# Watch pods come up
kubectl get pods -w
```

### 2. Verify All Pods Running

```bash
kubectl get pods

# Expected output (all Running):
# opus-stack-gateway-xxx
# opus-stack-customer-xxx
# opus-stack-telemetry-xxx
# opus-stack-workflow-xxx
# opus-stack-files-xxx
# opus-ravendb-0
# opus-timescaledb-0
# opus-rabbitmq-0
# opus-mqtt-xxx
# opus-prometheus-0
# opus-grafana-0
```

### 3. Check Services

```bash
kubectl get svc

# Gateway and Grafana should have LoadBalancer type
# Others should be ClusterIP
```

### 4. Port Forward and Test

```bash
# Gateway
kubectl port-forward svc/opus-stack-gateway 8080:80

# In another terminal
curl http://localhost:8080/health
curl http://localhost:8080/gateway/ping
curl http://localhost:8080/metrics
```

### 5. Test Files API

```bash
# Port forward
kubectl port-forward svc/opus-stack-files 8089:80

# Create parquet file
curl -X POST http://localhost:8089/export/parquet/k8s-test/run1
```

### 6. Check Prometheus

```bash
# Port forward
kubectl port-forward svc/opus-prometheus 9090:9090

# Open browser
open http://localhost:9090

# Query: http_server_request_duration_seconds_count
# You should see metrics from all services
```

### 7. Check Grafana

```bash
# Get LoadBalancer IP (or port-forward)
kubectl get svc opus-grafana

# If using port-forward
kubectl port-forward svc/opus-grafana 3000:3000

# Open browser
open http://localhost:3000
# Login: admin/admin

# Add Prometheus data source:
# URL: http://opus-prometheus:9090
```

### 8. View Logs

```bash
# Gateway logs
kubectl logs -l app.kubernetes.io/component=gateway -f

# All service logs
kubectl logs -l app.kubernetes.io/instance=opus-stack -f --max-log-requests=10
```

### 9. Test Scaling

```bash
# Scale gateway to 5 replicas
kubectl scale deployment opus-stack-gateway --replicas=5

# Verify
kubectl get pods -l app.kubernetes.io/component=gateway

# Scale back
kubectl scale deployment opus-stack-gateway --replicas=2
```

### 10. Test Upgrades

```bash
# Modify values
helm upgrade opus-stack ./opus-stack --set gateway.replicaCount=3

# Check rollout
kubectl rollout status deployment/opus-stack-gateway

# Rollback if needed
helm rollback opus-stack
```

---

## Observability Testing

Verify OpenTelemetry, Prometheus, and Grafana integration.

### 1. Check Metrics Endpoints

```bash
# Gateway metrics
curl -s http://localhost:8088/metrics | grep -E "http_server_request|kestrel_active"

# Customer metrics (if exposed)
curl -s http://localhost:8088/metrics | head -50
```

### 2. Verify Prometheus Scraping

```bash
# Open Prometheus
open http://localhost:9090

# Check Targets: Status -> Targets
# All 5 services should be UP:
# - opus-gateway
# - opus-customer
# - opus-telemetry
# - opus-workflow
# - opus-files
```

### 3. Query Metrics in Prometheus

```promql
# Request count
sum(rate(http_server_request_duration_seconds_count[5m])) by (service)

# Request duration
histogram_quantile(0.95, sum(rate(http_server_request_duration_seconds_bucket[5m])) by (le, service))

# Active connections
kestrel_active_connections
```

### 4. Create Grafana Dashboard

1. Open Grafana (http://localhost:3000)
2. Add Prometheus data source
3. Create new dashboard
4. Add panel with query:

```promql
rate(http_server_request_duration_seconds_count{service="gateway"}[5m])
```

---

## End-to-End Testing

Complete workflow testing across all services.

### 1. Create Customer with Event Publishing

```bash
# Create customer (publishes to RabbitMQ)
curl -X POST http://localhost:8088/customers \
  -H "Content-Type: application/json" \
  -d '{"id":"e2e-test-001","name":"Test User","email":"test@example.com"}'

# Verify in RavenDB
open http://localhost:8080
# Navigate to opus database -> Documents

# Verify in RabbitMQ
open http://localhost:15672
# Check customer.events exchange
```

### 2. IoT Data Flow

```bash
# Publish MQTT telemetry
mosquitto_pub -h localhost -p 1883 -t "device/iot-001/temperature" -m "25.3"
mosquitto_pub -h localhost -p 1883 -t "device/iot-001/humidity" -m "65.5"

# Wait for processing
sleep 3

# Query TimescaleDB
docker exec -it compose-timescale-1 psql -U postgres -d telemetry -c \
  "SELECT * FROM telemetry WHERE device_id='iot-001' ORDER BY ts DESC LIMIT 5;"
```

### 3. Parquet Export and Analytics

```bash
# Generate telemetry data
for i in {1..10}; do
  mosquitto_pub -h localhost -p 1883 -t "device/analytics-001/metric" -m "$RANDOM"
  sleep 1
done

# Export to Parquet
curl -X POST http://localhost:8089/export/parquet/analytics/export-$(date +%s)

# List all exports
az storage blob list \
  --container-name analytics \
  --connection-string "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"
```

### 4. Workflow Execution

```bash
# Start workflow
curl -X POST http://localhost:8088/workflow/start/data-processing

# Expected: {"instanceId":"<guid>","type":"data-processing"}
```

### 5. Monitor Everything in Grafana

1. Generate load:

```bash
# Run continuous requests
while true; do
  curl -s http://localhost:8088/health > /dev/null
  curl -s http://localhost:8088/gateway/ping > /dev/null
  sleep 1
done
```

2. Watch metrics in Grafana:
   - Request rate
   - Response times
   - Error rates
   - Resource usage

---

## Troubleshooting

### Docker Compose Issues

```bash
# Restart all services
docker compose down && docker compose up -d

# Check logs for errors
docker compose logs | grep -i error

# Rebuild images
docker compose build --no-cache
docker compose up -d
```

### Connection Refused Errors

```bash
# Check if services are listening
docker compose ps

# Check network connectivity
docker exec -it compose-gateway-1 ping opus-ravendb
docker exec -it compose-gateway-1 nc -zv opus-ravendb 8080
```

### Database Issues

```bash
# Recreate RavenDB database
curl -X DELETE http://localhost:8080/admin/databases?name=opus
curl -X PUT http://localhost:8080/admin/databases -H "Content-Type: application/json" \
  -d '{"DatabaseName":"opus","Settings":{},"Disabled":false}'

# Reset TimescaleDB
docker compose stop timescale
docker volume rm compose_ts-data
docker compose up -d timescale
```

### Image Pull Issues (Kubernetes)

```bash
# Create image pull secret
kubectl create secret docker-registry ghcr-secret \
  --docker-server=ghcr.io \
  --docker-username=YOUR_GITHUB_USERNAME \
  --docker-password=YOUR_GITHUB_TOKEN

# Update values.yaml
# imagePullSecrets:
#   - name: ghcr-secret

# Reinstall
helm upgrade opus-stack ./opus-stack
```

---

## Automated Testing

### Create a Test Suite

See `scripts/test-all.sh` for a complete automated test suite that:
- Verifies all containers are healthy
- Tests all API endpoints
- Validates database connections
- Checks metrics endpoints
- Verifies data persistence

### Run Tests

```bash
# From project root
chmod +x scripts/test-all.sh
./scripts/test-all.sh
```

---

## Success Criteria

Your Opus Stack is fully operational when:

- ✅ All 12 containers are running and healthy
- ✅ Gateway API responds to /health and /ping
- ✅ Customer data persists in RavenDB
- ✅ MQTT messages flow to TimescaleDB
- ✅ Parquet files export to Azurite
- ✅ Prometheus scrapes metrics from all services
- ✅ Grafana displays metrics dashboards
- ✅ RabbitMQ receives published events
- ✅ CI/CD builds and pushes Docker images
- ✅ Kubernetes deployment succeeds with Helm

**Congratulations!** 🎉 You have a production-ready microservices platform!
