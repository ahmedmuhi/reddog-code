# Nginx Ingress Controller Setup Guide

This guide provides detailed installation and configuration instructions for the Nginx Ingress Controller across different environments (Local, Azure AKS, AWS EKS, GCP GKE).

For architectural decisions and rationale, see [ADR-0010: Nginx Ingress Controller](../adr/adr-0010-nginx-ingress-controller-cloud-agnostic.md).

## Quick Reference

| Environment | Installation Method | Access |
|-------------|---------------------|--------|
| **Local (kind)** | kind-specific manifest | `http://localhost` |
| **Azure AKS** | Helm chart + Azure values | Load Balancer DNS |
| **AWS EKS** | Helm chart + AWS values | Network Load Balancer |
| **GCP GKE** | Helm chart + GCP values | External Load Balancer |

## Installation

### Local Development (kind)

**Prerequisites:**
- kind cluster with port mappings configured (see `kind-config.yaml`)
- kubectl configured for your kind cluster

**Install Nginx Ingress:**
```bash
# Apply kind-specific manifest (includes NodePort configuration)
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/kind/deploy.yaml

# Wait for deployment to be ready
kubectl wait --namespace ingress-nginx \
  --for=condition=ready pod \
  --selector=app.kubernetes.io/component=controller \
  --timeout=90s
```

**Verify Installation:**
```bash
# Check controller pods
kubectl get pods -n ingress-nginx

# Test connectivity
curl http://localhost/healthz
```

### Cloud Environments (AKS/EKS/GKE)

The Red Dog project uses a Helm wrapper chart that deploys the official `ingress-nginx` chart with environment-specific configurations.

**Chart Location:** `charts/external/nginx-ingress/`

**Deploy via Helm:**
```bash
# Azure AKS
helm install nginx-ingress charts/external/nginx-ingress/ \
  -f charts/external/nginx-ingress/values-base.yaml \
  -f charts/external/nginx-ingress/values-azure.yaml \
  --namespace ingress-nginx --create-namespace

# AWS EKS
helm install nginx-ingress charts/external/nginx-ingress/ \
  -f charts/external/nginx-ingress/values-base.yaml \
  -f charts/external/nginx-ingress/values-aws.yaml \
  --namespace ingress-nginx --create-namespace

# GCP GKE
helm install nginx-ingress charts/external/nginx-ingress/ \
  -f charts/external/nginx-ingress/values-base.yaml \
  -f charts/external/nginx-ingress/values-gcp.yaml \
  --namespace ingress-nginx --create-namespace
```

**Get External IP:**
```bash
kubectl get svc -n ingress-nginx
```

## Configuration Examples

### Ingress Resource (Red Dog Application)

The main Red Dog ingress is defined in `charts/reddog/templates/ingress.yaml` and configured via values files.

**Example Configuration (values-local.yaml):**
```yaml
ingress:
  enabled: true
  className: nginx
  tls:
    enabled: false  # TLS disabled for local dev
  annotations: {}
  hosts:
    - host: localhost
      paths:
        - path: /
          pathType: Prefix
          service: ui
          port: 80
        - path: /api/orders
          pathType: Prefix
          service: orderservice
          port: 80
        - path: /api/makeline
          pathType: Prefix
          service: makelineservice
          port: 80
```

**Generated Ingress Resource:**
```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: reddog-ingress
  namespace: default
spec:
  ingressClassName: nginx
  rules:
  - host: localhost
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: ui
            port:
              number: 80
      - path: /api/orders
        pathType: Prefix
        backend:
          service:
            name: orderservice
            port:
              number: 80
```

### TLS/HTTPS Configuration (Production)

**With cert-manager for automatic certificates:**

```yaml
ingress:
  enabled: true
  className: nginx
  tls:
    enabled: true
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod
  hosts:
    - host: reddog.azure.example.com
      paths:
        - path: /
          pathType: Prefix
          service: ui
          port: 80
```

**Manual TLS Secret:**
```bash
# Create TLS secret
kubectl create secret tls reddog-tls \
  --cert=path/to/tls.crt \
  --key=path/to/tls.key
```

### Advanced Annotations

**URL Rewriting:**
```yaml
annotations:
  nginx.ingress.kubernetes.io/rewrite-target: /$2
```

**Rate Limiting:**
```yaml
annotations:
  nginx.ingress.kubernetes.io/limit-rps: "10"
  nginx.ingress.kubernetes.io/limit-connections: "100"
```

**CORS Support:**
```yaml
annotations:
  nginx.ingress.kubernetes.io/enable-cors: "true"
  nginx.ingress.kubernetes.io/cors-allow-origin: "*"
  nginx.ingress.kubernetes.io/cors-allow-methods: "GET, POST, PUT, DELETE, OPTIONS"
```

**Authentication:**
```yaml
annotations:
  nginx.ingress.kubernetes.io/auth-type: basic
  nginx.ingress.kubernetes.io/auth-secret: basic-auth
  nginx.ingress.kubernetes.io/auth-realm: "Authentication Required"
```

## Environment-Specific Settings

### Azure AKS

**DNS Label Configuration:**
```yaml
ingress-nginx:
  controller:
    service:
      annotations:
        service.beta.kubernetes.io/azure-dns-label-name: "reddog-prod"
        service.beta.kubernetes.io/azure-load-balancer-sku: "Standard"
```

Creates FQDN: `reddog-prod.<region>.cloudapp.azure.com`

### AWS EKS

**Network Load Balancer Configuration:**
```yaml
ingress-nginx:
  controller:
    service:
      annotations:
        service.beta.kubernetes.io/aws-load-balancer-type: "nlb"
        service.beta.kubernetes.io/aws-load-balancer-cross-zone-load-balancing-enabled: "true"
```

### GCP GKE

**External Load Balancer Configuration:**
```yaml
ingress-nginx:
  controller:
    service:
      annotations:
        cloud.google.com/load-balancer-type: "External"
```

## Monitoring and Observability

### Prometheus Metrics

Metrics are enabled by default in `values-base.yaml`:

```yaml
ingress-nginx:
  controller:
    metrics:
      enabled: true
      serviceMonitor:
        enabled: false  # Enable when Prometheus deployed
```

**Scrape Metrics Manually:**
```bash
kubectl port-forward -n ingress-nginx svc/nginx-ingress-controller-metrics 9113:9113
curl http://localhost:9113/metrics
```

### Access Logs

**View Nginx Access Logs:**
```bash
kubectl logs -n ingress-nginx deployment/nginx-ingress-controller -f
```

**Common Log Fields:**
- Remote address
- Request method and path
- Response status code
- Upstream response time
- Request size/response size

### Grafana Dashboard

Import the official Nginx Ingress dashboard:
- **Dashboard ID:** 9614 (from grafana.com)
- **Metrics Source:** Prometheus scraping controller metrics

## Troubleshooting

### Controller Not Starting

**Check pod status:**
```bash
kubectl get pods -n ingress-nginx
kubectl describe pod -n ingress-nginx <pod-name>
```

**Common issues:**
- Resource limits too low
- Port 80/443 already in use (kind)
- Insufficient RBAC permissions

### Ingress Not Routing Traffic

**Verify Ingress resource:**
```bash
kubectl get ingress
kubectl describe ingress reddog-ingress
```

**Check controller logs:**
```bash
kubectl logs -n ingress-nginx deployment/nginx-ingress-controller | grep -i error
```

**Validate backend service:**
```bash
kubectl get svc <service-name>
kubectl get endpoints <service-name>
```

### Port Conflicts (Local Development)

If ports 80/443 are already in use on your local machine:

**Option 1: Use alternative ports:**
```yaml
# kind-config.yaml
extraPortMappings:
- containerPort: 80
  hostPort: 8080  # Change to 8080
  protocol: TCP
```

Access via `http://localhost:8080`

**Option 2: Stop conflicting services:**
```bash
# Windows (IIS)
net stop w3svc

# Linux (Apache)
sudo systemctl stop apache2

# macOS (built-in Apache)
sudo apachectl stop
```

## Performance Tuning

### Resource Limits

Default limits from `values-base.yaml`:
```yaml
controller:
  resources:
    requests:
      cpu: 100m
      memory: 128Mi
    limits:
      cpu: 500m
      memory: 512Mi
```

**For production workloads, increase limits:**
```yaml
controller:
  resources:
    requests:
      cpu: 500m
      memory: 512Mi
    limits:
      cpu: 2000m
      memory: 2Gi
```

### High Availability

Enable multiple replicas with anti-affinity:
```yaml
controller:
  replicaCount: 3
  affinity:
    podAntiAffinity:
      preferredDuringSchedulingIgnoredDuringExecution:
        - weight: 100
          podAffinityTerm:
            labelSelector:
              matchLabels:
                app.kubernetes.io/name: ingress-nginx
            topologyKey: kubernetes.io/hostname
```

### Connection Settings

Optimize for high traffic:
```yaml
controller:
  config:
    use-forwarded-headers: "true"
    compute-full-forwarded-for: "true"
    use-proxy-protocol: "false"
    keep-alive: "75"
    keep-alive-requests: "100"
```

## Migration from Cloud-Native Controllers

If migrating from Azure AGIC, AWS ALB, or GCP Ingress:

1. **Install Nginx Ingress** (as described above)
2. **Update IngressClass:**
   ```yaml
   spec:
     ingressClassName: nginx  # Change from 'azure', 'alb', etc.
   ```
3. **Update Annotations:** Remove cloud-specific annotations, add Nginx ones
4. **Test in Parallel:** Run both controllers temporarily, switch DNS after validation
5. **Remove Old Controller:** Clean up old ingress controller resources

## References

- [Nginx Ingress Official Docs](https://kubernetes.github.io/ingress-nginx/)
- [kind Ingress Guide](https://kind.sigs.k8s.io/docs/user/ingress/)
- [Kubernetes Ingress Concepts](https://kubernetes.io/docs/concepts/services-networking/ingress/)
- [ADR-0010: Architectural Decision](../adr/adr-0010-nginx-ingress-controller-cloud-agnostic.md)
- [Helm Chart Source](https://github.com/kubernetes/ingress-nginx/tree/main/charts/ingress-nginx)
