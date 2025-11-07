#!/bin/bash

# Opus Stack Automated Test Suite
# Tests all services in Docker Compose environment

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Counters
PASSED=0
FAILED=0

# Helper functions
print_header() {
    echo ""
    echo "========================================="
    echo "$1"
    echo "========================================="
}

print_test() {
    echo -n "Testing: $1... "
}

pass() {
    echo -e "${GREEN}✓ PASS${NC}"
    ((PASSED++))
}

fail() {
    echo -e "${RED}✗ FAIL${NC}"
    echo "  Error: $1"
    ((FAILED++))
}

# Change to compose directory
cd "$(dirname "$0")/../deploy/compose" || exit 1

print_header "Opus Stack Automated Test Suite"

# Test 1: Check Docker Compose is running
print_header "1. Infrastructure Health Checks"

print_test "Docker Compose running"
if docker compose ps > /dev/null 2>&1; then
    pass
else
    fail "Docker Compose not running. Run 'docker compose up -d' first"
    exit 1
fi

# Test 2: Check all containers are running
print_test "All containers running"
RUNNING=$(docker compose ps | grep -c "Up" || true)
if [ "$RUNNING" -ge 12 ]; then
    pass
else
    fail "Only $RUNNING/12 containers running"
fi

# Test 3: Gateway health
print_header "2. Gateway API Tests"

print_test "Gateway health endpoint"
if curl -sf http://localhost:8088/health | grep -q "ok"; then
    pass
else
    fail "Gateway health check failed"
fi

print_test "Gateway ping endpoint"
RESPONSE=$(curl -sf http://localhost:8088/gateway/ping || echo "")
if [ "$RESPONSE" = "pong" ]; then
    pass
else
    fail "Expected 'pong', got '$RESPONSE'"
fi

print_test "Gateway metrics endpoint"
if curl -sf http://localhost:8088/metrics | grep -q "http_server_request_duration"; then
    pass
else
    fail "Metrics endpoint not returning Prometheus metrics"
fi

print_test "Gateway Swagger UI"
if curl -sf http://localhost:8088/swagger/index.html | grep -q "Swagger"; then
    pass
else
    fail "Swagger UI not accessible"
fi

# Test 4: Files API
print_header "3. Files API Tests"

print_test "Files API health"
if curl -sf http://localhost:8089/health > /dev/null 2>&1; then
    pass
else
    fail "Files API not responding"
fi

print_test "Parquet export"
TIMESTAMP=$(date +%s)
RESPONSE=$(curl -sf -X POST "http://localhost:8089/export/parquet/test-suite/test-$TIMESTAMP" || echo "")
if echo "$RESPONSE" | grep -q "test-suite"; then
    pass
else
    fail "Parquet export failed"
fi

# Test 5: RavenDB
print_header "4. Database Tests"

print_test "RavenDB accessible"
if curl -sf http://localhost:8080/databases/opus/stats | grep -q "CountOfDocuments"; then
    pass
else
    fail "RavenDB not accessible or database 'opus' missing"
fi

print_test "TimescaleDB accessible"
if docker exec compose-timescale-1 psql -U postgres -d telemetry -c "\dt" 2>&1 | grep -q "telemetry"; then
    pass
else
    fail "TimescaleDB not accessible or hypertable missing"
fi

print_test "TimescaleDB retention policy"
if docker exec compose-timescale-1 psql -U postgres -d telemetry -c \
   "SELECT * FROM timescaledb_information.jobs WHERE proc_name='policy_retention';" 2>&1 | grep -q "policy_retention"; then
    pass
else
    fail "Retention policy not configured"
fi

# Test 6: Message Brokers
print_header "5. Message Broker Tests"

print_test "RabbitMQ Management UI"
if curl -sf -u guest:guest http://localhost:15672/api/overview | grep -q "rabbitmq_version"; then
    pass
else
    fail "RabbitMQ Management not accessible"
fi

print_test "MQTT Broker listening"
if nc -z localhost 1883 2>/dev/null; then
    pass
else
    fail "MQTT broker not listening on port 1883"
fi

# Test 7: Storage
print_header "6. Storage Tests"

print_test "Azurite Blob Service"
if curl -sf http://localhost:10000/devstoreaccount1?comp=list | grep -q "EnumerationResults"; then
    pass
else
    fail "Azurite not accessible"
fi

print_test "Test container exists"
export AZURE_STORAGE_CONNECTION_STRING="DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"
if az storage container exists --name test-suite --connection-string "$AZURE_STORAGE_CONNECTION_STRING" 2>&1 | grep -q "true"; then
    pass
else
    fail "Test container not found (normal if first run)"
fi

# Test 8: Observability
print_header "7. Observability Tests"

print_test "Prometheus accessible"
if curl -sf http://localhost:9090/-/healthy | grep -q "Healthy"; then
    pass
else
    fail "Prometheus not healthy"
fi

print_test "Prometheus scraping targets"
if curl -sf http://localhost:9090/api/v1/targets | grep -q "opus-gateway"; then
    pass
else
    fail "Prometheus not scraping services"
fi

print_test "Grafana accessible"
if curl -sf http://localhost:3000/api/health | grep -q "ok"; then
    pass
else
    fail "Grafana not accessible"
fi

# Test 9: Service-to-Service Communication
print_header "8. Integration Tests"

print_test "Gateway can resolve RavenDB"
if docker exec compose-gateway-1 ping -c 1 raven > /dev/null 2>&1; then
    pass
else
    fail "Gateway cannot resolve RavenDB hostname"
fi

print_test "Gateway can resolve TimescaleDB"
if docker exec compose-gateway-1 ping -c 1 timescale > /dev/null 2>&1; then
    pass
else
    fail "Gateway cannot resolve TimescaleDB hostname"
fi

# Test 10: Metrics Collection
print_header "9. Metrics Validation"

print_test "Gateway emitting metrics"
METRIC_COUNT=$(curl -sf http://localhost:8088/metrics | grep -c "http_server" || true)
if [ "$METRIC_COUNT" -gt 10 ]; then
    pass
else
    fail "Gateway not emitting enough metrics (found $METRIC_COUNT)"
fi

print_test "Files API emitting metrics"
if curl -sf http://localhost:8089/metrics | grep -q "http_server_request_duration"; then
    pass
else
    fail "Files API not emitting metrics"
fi

# Summary
print_header "Test Summary"
echo ""
echo -e "${GREEN}Passed: $PASSED${NC}"
echo -e "${RED}Failed: $FAILED${NC}"
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✓ ALL TESTS PASSED!${NC}"
    echo ""
    echo "Your Opus Stack is fully operational! 🎉"
    echo ""
    echo "Next steps:"
    echo "  - View Grafana: http://localhost:3000 (admin/admin)"
    echo "  - View Prometheus: http://localhost:9090"
    echo "  - View RavenDB: http://localhost:8080"
    echo "  - View RabbitMQ: http://localhost:15672 (guest/guest)"
    echo "  - Gateway API: http://localhost:8088/swagger"
    echo "  - Files API: http://localhost:8089/swagger"
    echo ""
    exit 0
else
    echo -e "${RED}✗ SOME TESTS FAILED${NC}"
    echo ""
    echo "Check the errors above and verify:"
    echo "  1. All containers are running: docker compose ps"
    echo "  2. Check logs: docker compose logs"
    echo "  3. Restart if needed: docker compose restart"
    echo ""
    exit 1
fi
