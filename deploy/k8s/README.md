# Kubernetes Deployment with Helm

This directory contains Helm charts for deploying the Opus Stack to Kubernetes.

## Prerequisites

- Kubernetes cluster (1.24+)
- Helm 3.x
- kubectl configured to access your cluster
- Container images pushed to GitHub Container Registry (via CI/CD)

## Quick Start

### 1. Install the Helm Chart

```bash
# Navigate to the Helm chart directory
cd deploy/k8s/helm

# Install the chart
helm install opus-stack ./opus-stack

# Or with custom values
helm install opus-stack ./opus-stack -f ./opus-stack/values.yaml
```

### 2. Verify Deployment

```bash
# Check all pods are running
kubectl get pods

# Check services
kubectl get svc

# Get the Gateway LoadBalancer external IP
kubectl get svc opus-stack-gateway
```

### 3. Access Services

```bash
# Port forward to access locally
kubectl port-forward svc/opus-stack-gateway 8080:80
kubectl port-forward svc/opus-grafana 3000:3000
kubectl port-forward svc/opus-prometheus 9090:9090
```

## Configuration

### Image Repository

The default configuration points to GitHub Container Registry:

```yaml
global:
  imageRegistry: ghcr.io
  imageRepository: sjs1001/opus-stack-starter-full
  imagePullPolicy: IfNotPresent
  imageTag: "latest"
```

To use your own repository, update [values.yaml](helm/opus-stack/values.yaml:1):

```yaml
global:
  imageRegistry: your-registry.io
  imageRepository: your-org/your-repo
  imageTag: "v1.0.0"
```

### Private Registry Authentication

If using a private registry, create an image pull secret:

```bash
kubectl create secret docker-registry ghcr-secret \
  --docker-server=ghcr.io \
  --docker-username=YOUR_GITHUB_USERNAME \
  --docker-password=YOUR_GITHUB_TOKEN \
  --docker-email=YOUR_EMAIL
```

Then update values.yaml:

```yaml
imagePullSecrets:
  - name: ghcr-secret
```

### Resource Limits

Adjust resource limits for each service in [values.yaml](helm/opus-stack/values.yaml:1):

```yaml
gateway:
  resources:
    limits:
      cpu: 1000m
      memory: 1Gi
    requests:
      cpu: 500m
      memory: 512Mi
```

### Persistence

Enable persistent storage for databases:

```yaml
ravendb:
  persistence:
    enabled: true
    size: 20Gi
    storageClass: "standard"

timescaledb:
  persistence:
    enabled: true
    size: 50Gi
    storageClass: "standard"
```

### Scaling

Adjust replica counts:

```yaml
gateway:
  replicaCount: 3

customer:
  replicaCount: 2
```

## Architecture

### Services

The Helm chart deploys the following services:

**Microservices:**
- `opus-stack-gateway` - API Gateway (LoadBalancer)
- `opus-stack-customer` - Customer Service (ClusterIP)
- `opus-stack-telemetry` - Telemetry Service (ClusterIP)
- `opus-stack-workflow` - Workflow Service (ClusterIP)
- `opus-stack-files` - Files Service (ClusterIP)

**Infrastructure:**
- `opus-ravendb` - Document Database (StatefulSet)
- `opus-timescaledb` - Time-series Database (StatefulSet)
- `opus-rabbitmq` - Message Broker (StatefulSet)
- `opus-mqtt` - MQTT Broker (Deployment)
- `opus-prometheus` - Metrics Collection (StatefulSet)
- `opus-grafana` - Dashboards (StatefulSet, LoadBalancer)

### Networking

- Gateway and Grafana are exposed via LoadBalancer
- All other services use ClusterIP (internal only)
- Services communicate using Kubernetes DNS

### Storage

StatefulSets use PersistentVolumeClaims for data persistence:
- RavenDB: 10Gi
- TimescaleDB: 20Gi
- RabbitMQ: 5Gi
- Prometheus: 10Gi
- Grafana: 5Gi

## Common Operations

### Upgrade Deployment

```bash
# Pull latest images and upgrade
helm upgrade opus-stack ./opus-stack

# Upgrade with new values
helm upgrade opus-stack ./opus-stack -f custom-values.yaml
```

### Rollback

```bash
# List releases
helm history opus-stack

# Rollback to previous release
helm rollback opus-stack

# Rollback to specific revision
helm rollback opus-stack 2
```

### Uninstall

```bash
# Uninstall the release
helm uninstall opus-stack

# Clean up PVCs (data will be lost!)
kubectl delete pvc -l app.kubernetes.io/instance=opus-stack
```

### View Logs

```bash
# Gateway logs
kubectl logs -l app.kubernetes.io/component=gateway -f

# Customer service logs
kubectl logs -l app.kubernetes.io/component=customer -f

# All services
kubectl logs -l app.kubernetes.io/instance=opus-stack -f --max-log-requests=10
```

### Scale Services

```bash
# Scale gateway to 5 replicas
kubectl scale deployment opus-stack-gateway --replicas=5

# Or via Helm
helm upgrade opus-stack ./opus-stack --set gateway.replicaCount=5
```

## Monitoring

### Prometheus

Access Prometheus UI:

```bash
kubectl port-forward svc/opus-prometheus 9090:9090
# Open http://localhost:9090
```

Prometheus scrapes metrics from all services at `/metrics` endpoints.

### Grafana

Access Grafana:

```bash
kubectl port-forward svc/opus-grafana 3000:3000
# Open http://localhost:3000
# Default credentials: admin/admin
```

Add Prometheus data source:
- URL: `http://opus-prometheus:9090`

## Troubleshooting

### Pods Not Starting

```bash
# Check pod status
kubectl get pods

# Describe pod to see events
kubectl describe pod <pod-name>

# Check logs
kubectl logs <pod-name>
```

### Image Pull Errors

If you see `ImagePullBackOff`:

1. Verify images exist in registry
2. Check image pull secrets are configured
3. Verify service account has access

```bash
# Check if images are accessible
kubectl run test --image=ghcr.io/sjs1001/opus-stack-starter-full/gateway:latest --rm -it -- /bin/sh
```

### Service Discovery Issues

Verify DNS resolution:

```bash
# Run a test pod
kubectl run -it --rm debug --image=busybox --restart=Never -- sh

# Inside the pod, test DNS
nslookup opus-ravendb
nslookup opus-timescaledb
```

### Database Connection Issues

Check database services are running:

```bash
kubectl get pods -l app.kubernetes.io/component=ravendb
kubectl get pods -l app.kubernetes.io/component=timescaledb

# Port forward to test locally
kubectl port-forward svc/opus-ravendb 8080:8080
kubectl port-forward svc/opus-timescaledb 5432:5432
```

## Production Considerations

### High Availability

For production, increase replica counts and enable pod anti-affinity:

```yaml
gateway:
  replicaCount: 3
  affinity:
    podAntiAffinity:
      preferredDuringSchedulingIgnoredDuringExecution:
      - weight: 100
        podAffinityTerm:
          labelSelector:
            matchExpressions:
            - key: app.kubernetes.io/component
              operator: In
              values:
              - gateway
          topologyKey: kubernetes.io/hostname
```

### Resource Quotas

Set appropriate resource limits based on load testing:

```yaml
resources:
  limits:
    cpu: 2000m
    memory: 2Gi
  requests:
    cpu: 1000m
    memory: 1Gi
```

### Security

1. Enable NetworkPolicies to restrict pod communication
2. Use Secrets for sensitive configuration
3. Enable RBAC
4. Use Pod Security Policies
5. Enable JWT validation in production:

```yaml
config:
  jwt:
    enableValidation: "true"
```

### Backup Strategy

Backup persistent volumes regularly:

```bash
# RavenDB backup via API
kubectl exec -it opus-ravendb-0 -- curl -X POST http://localhost:8080/admin/databases/opus/backup

# TimescaleDB backup
kubectl exec -it opus-timescaledb-0 -- pg_dump -U postgres telemetry > backup.sql
```

## CI/CD Integration

### GitHub Actions

The repository includes a GitHub Actions workflow that automatically builds and pushes Docker images to GHCR. After each push to main:

1. Images are built and pushed with `latest` and SHA tags
2. Update Helm values to use the new tag:

```bash
helm upgrade opus-stack ./opus-stack --set global.imageTag=main-abc1234
```

### Automated Deployments

Example ArgoCD Application:

```yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: opus-stack
spec:
  project: default
  source:
    repoURL: https://github.com/SJS1001/opus-stack-starter-full
    targetRevision: HEAD
    path: deploy/k8s/helm/opus-stack
  destination:
    server: https://kubernetes.default.svc
    namespace: opus
  syncPolicy:
    automated:
      prune: true
      selfHeal: true
```

## Support

For issues or questions:
- GitHub Issues: https://github.com/SJS1001/opus-stack-starter-full/issues
- Documentation: See root README.md
