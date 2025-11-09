---
title: "ADR-0010: Nginx Ingress Controller for Cloud-Agnostic Traffic Routing"
status: "Accepted"
date: "2025-11-09"
authors: "Red Dog Modernization Team"
tags: ["architecture", "decision", "nginx", "ingress", "kubernetes", "multi-cloud"]
supersedes: ""
superseded_by: ""
---

# ADR-0010: Nginx Ingress Controller for Cloud-Agnostic Traffic Routing

## Status

**Accepted**

## Context

Red Dog Coffee requires external HTTP/HTTPS access to microservices and UI across four deployment environments (local kind, Azure AKS, AWS EKS, GCP GKE). Kubernetes provides the Ingress API resource for defining HTTP routing rules, but does NOT include a default Ingress Controller implementation.

**Key Constraints:**
- **Cloud-Agnostic Architecture**: Same Ingress manifests must work identically across local, Azure, AWS, and GCP (ADR-0007)
- **Multi-Service External Access**: UI (port 8080), OrderService (port 5100), MakeLineService (port 5200), AccountingService (port 5700), and others require public access
- **Cost Efficiency**: Minimize cloud infrastructure costs for teaching/demo scenarios
- **Path-Based Routing**: Must support routing based on URL paths (e.g., `/` → UI, `/api/orders` → OrderService)
- **Teaching Focus**: Students should learn portable, transferable Ingress patterns

**HTTP Traffic Routing Requirements:**

| Service | Path | Backend |
|---------|------|---------|
| **UI (Vue.js)** | `/` | reddog-ui:8080 |
| **OrderService** | `/api/orders` | order-service:5100 |
| **MakeLineService** | `/queue` | makeline-service:5200 |
| **AccountingService** | `/accounting` | accounting-service:5700 |

**Ingress Architecture Explained:**

```
Internet User (http://reddog.example.com)
    ↓
Cloud Load Balancer (Auto-Provisioned)
    Azure Load Balancer (AKS)
    AWS Network Load Balancer (EKS)
    GCP Load Balancer (GKE)
    Docker Host (kind on localhost:80)
    ↓ (Single external IP: 40.1.2.3)
Ingress Controller Pod (Nginx, Traefik, etc.)
    Reads Ingress resource rules
    Routes based on path/hostname
    ↓
Backend Services (order-service, ui, makeline-service)
```

**Key Insight**: The cloud load balancer is **automatically provisioned** when the Ingress Controller creates a Service of `type: LoadBalancer`. Users do NOT manually configure Azure Application Gateway, AWS ALB, or GCP Load Balancing unless using cloud-native ingress controllers.

**Available Ingress Controller Options:**

| Controller | Cloud Support | Adoption | Cloud-Agnostic? | Configuration |
|-----------|---------------|----------|----------------|---------------|
| **Nginx Ingress** | All (kind, AKS, EKS, GKE) | 64% (CNCF 2024) | ✅ Yes | Same YAML everywhere |
| **Azure AGIC** | Azure only | Azure-specific | ❌ No | Azure Application Gateway required |
| **AWS ALB Controller** | AWS only | AWS-specific | ❌ No | AWS ALB required |
| **GKE Ingress** | GCP only | GCP-specific | ❌ No | GCP LB required |
| **Traefik** | All | 18% (CNCF 2024) | ✅ Yes | Different config syntax |
| **HAProxy** | All | 12% (CNCF 2024) | ✅ Yes | Different config syntax |
| **Contour** | All | 8% (CNCF 2024) | ✅ Yes | Envoy-based |

**Cost Analysis (5 External Services):**

**Option A: Nginx Ingress (One Load Balancer)**
- Azure AKS: 1 × $20/month = **$20/month**
- AWS EKS: 1 × $18/month = **$18/month**
- GCP GKE: 1 × $20/month = **$20/month**

**Option B: LoadBalancer Service Per Service (No Ingress)**
- Azure AKS: 5 × $20/month = **$100/month**
- AWS EKS: 5 × $18/month = **$90/month**
- GCP GKE: 5 × $20/month = **$100/month**

**Savings with Nginx Ingress**: 75-80% cost reduction

**Current State (Implemented but Undocumented):**
- Nginx Ingress Controller deployed via Helm chart (`manifests/branch/dependencies/nginx/nginx.yaml`)
- Helm chart version 3.31.0 (outdated, from 2021)
- Rationale for Nginx choice never documented
- No ADR explaining cloud-agnostic ingress strategy

## Decision

**Adopt Nginx Ingress Controller as the standard Ingress implementation for Red Dog Coffee across all four deployment environments.**

**Implementation:**
- Deploy Nginx Ingress Controller via Helm chart in each environment
- Use same Ingress resource manifests (`kind: Ingress`) across local, Azure, AWS, GCP
- Cloud load balancer automatically provisioned when Nginx creates `type: LoadBalancer` Service
- Path-based routing configured in Ingress resources (not Nginx configuration files)

**Deployment Per Environment:**

```bash
# Local (kind) - Special kind-specific manifest
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/kind/deploy.yaml

# Azure (AKS) - Helm chart
helm install nginx-ingress ingress-nginx/ingress-nginx \
  --namespace ingress-nginx --create-namespace

# AWS (EKS) - Helm chart
helm install nginx-ingress ingress-nginx/ingress-nginx \
  --namespace ingress-nginx --create-namespace

# GCP (GKE) - Helm chart
helm install nginx-ingress ingress-nginx/ingress-nginx \
  --namespace ingress-nginx --create-namespace
```

**Ingress Resource Example (Identical Across Environments):**

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: reddog-ingress
  namespace: reddog
  annotations:
    nginx.ingress.kubernetes.io/rewrite-target: /
spec:
  ingressClassName: nginx
  rules:
  - host: reddog.example.com  # From Helm values (localhost, reddog.azure.example.com, etc.)
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: reddog-ui
            port:
              number: 8080
      - path: /api/orders
        pathType: Prefix
        backend:
          service:
            name: order-service
            port:
              number: 5100
      - path: /queue
        pathType: Prefix
        backend:
          service:
            name: makeline-service
            port:
              number: 5200
```

**Rationale:**

- **NGINX-001: Cloud-Agnostic**: Same Ingress YAML works on kind, AKS, EKS, GKE without modification
- **NGINX-002: Cost-Effective**: 75-80% savings vs LoadBalancer-per-service approach (one LB vs five LBs)
- **NGINX-003: Industry Standard**: 64% market share (CNCF 2024 survey) ensures community support and learning resources
- **NGINX-004: Teaching Value**: Students learn Kubernetes-standard Ingress API (transferable to any cloud)
- **NGINX-005: Path-Based Routing**: Supports clean URL structure (`/`, `/api/orders`, `/queue`) for multi-service access
- **NGINX-006: Production-Ready**: Battle-tested in production Kubernetes clusters globally
- **NGINX-007: Feature-Rich**: SSL/TLS termination, rate limiting, authentication, URL rewriting via annotations
- **NGINX-008: kind Compatibility**: Official kind documentation provides Nginx Ingress setup guide

## Consequences

### Positive

- **POS-001: Unified Configuration**: Same `ingress.yaml` manifest deploys to all four environments
- **POS-002: Single External IP**: One cloud load balancer per cluster (vs multiple for LoadBalancer services)
- **POS-003: Cost Savings**: $60-80/month savings across three cloud environments vs LoadBalancer approach
- **POS-004: Kubernetes-Native**: Uses standard Ingress API (not proprietary cloud solutions)
- **POS-005: Easy Local Testing**: kind supports Nginx Ingress via port mappings (localhost:80 → cluster)
- **POS-006: SSL/TLS Support**: cert-manager integration for automatic HTTPS certificates
- **POS-007: Flexible Routing**: Path-based (`/api/orders`), host-based (`api.reddog.com`), header-based routing
- **POS-008: Teaching Portability**: Reinforces cloud-agnostic architecture principle (ADR-0007)
- **POS-009: Community Support**: Large community, extensive documentation, active development
- **POS-010: Observability**: Prometheus metrics, access logs, and Grafana dashboards available

### Negative

- **NEG-001: Additional Component**: Nginx Ingress Controller pods consume cluster resources (~100MB RAM per replica)
- **NEG-002: Layer 7 Only**: Ingress only supports HTTP/HTTPS; TCP/UDP services require LoadBalancer or NodePort
- **NEG-003: Cloud-Native Features Lost**: Azure Application Gateway WAF, AWS ALB target groups not available
- **NEG-004: Upgrade Maintenance**: Nginx Ingress must be upgraded separately from application deployments
- **NEG-005: Performance vs Cloud-Native**: Azure AGIC has 50% lower latency than Nginx Ingress (Microsoft benchmarks)
- **NEG-006: Port Conflicts (Local)**: kind requires ports 80/443 available on localhost (conflicts with IIS, Apache)

### Mitigations

- **MIT-001: Resource Limits**: Configure Nginx pods with resource limits (100m CPU, 128Mi RAM) for cost control
- **MIT-002: LoadBalancer for TCP**: Use separate LoadBalancer Service for non-HTTP protocols (e.g., gRPC on port 50051)
- **MIT-003: Helm Automation**: Include Nginx Ingress in infrastructure Helm chart for automated deployment
- **MIT-004: Alternative Ports**: Document port 8080/8443 mappings for local developers with port conflicts
- **MIT-005: Performance Optimization**: Enable keepalive, compression, and caching in Nginx for production workloads
- **MIT-006: Cloud-Native Option**: Document migration path to Azure AGIC/AWS ALB for production if needed

## Relationship to Existing ADRs

This ADR supports and complements other architectural decisions:

| ADR | How Nginx Ingress Supports It |
|-----|-------------------------------|
| **ADR-0007: Cloud-Agnostic Deployment** | Nginx Ingress works identically on all clouds (vs cloud-native controllers) |
| **ADR-0008: kind Local Development** | Nginx Ingress works on kind via port mappings (localhost:80) |
| **ADR-0009: Helm Multi-Environment** | Nginx deployed via Helm chart; same Ingress YAML across values files |
| **ADR-0002: Dapr Abstraction** | No conflict; Ingress routes external traffic, Dapr handles internal service-to-service |

**Separation of Concerns:**
- **Nginx Ingress**: Routes **external** HTTP traffic (Internet → Kubernetes services)
- **Dapr**: Handles **internal** service-to-service communication (OrderService → LoyaltyService)
- **Kubernetes Service**: Provides internal load balancing within cluster

**Example Flow:**
```
User → Nginx Ingress → OrderService pod
                           ↓
                    Dapr sidecar invokes → LoyaltyService
```

## Alternatives Considered

### Cloud-Native Ingress Controllers (Rejected)

**Azure Application Gateway Ingress Controller (AGIC):**
- **ALT-001: Description**: Use Azure Application Gateway with AGIC for AKS deployments
- **ALT-002: Rejection Reason**:
  - Azure-only (breaks multi-cloud goal)
  - Different Ingress configuration for Azure vs AWS/GCP
  - Students learn Azure-specific patterns (not transferable)
  - Teaching overhead: "On Azure use AGIC, on AWS use ALB, on GCP use GKE Ingress"
  - Violates ADR-0007 (cloud-agnostic architecture)

**AWS ALB Ingress Controller:**
- **ALT-003: Description**: Use AWS Application Load Balancer Controller for EKS deployments
- **ALT-004: Rejection Reason**: Same as AGIC (AWS-specific, non-portable, teaching complexity)

**GKE Ingress:**
- **ALT-005: Description**: Use native GKE Ingress Controller for GKE deployments
- **ALT-006: Rejection Reason**: Same as AGIC (GCP-specific, non-portable, teaching complexity)

### LoadBalancer Service Per Service (Rejected)

- **ALT-007: Description**: Skip Ingress; use `type: LoadBalancer` for each service
```yaml
apiVersion: v1
kind: Service
metadata:
  name: order-service
spec:
  type: LoadBalancer  # Creates dedicated cloud LB
  ports:
  - port: 5100
```
- **ALT-008: Rejection Reason**:
  - 5× more expensive ($100/month vs $20/month)
  - No path-based routing (each service gets separate IP)
  - No SSL/TLS termination centralized
  - Inefficient for microservices architecture (5+ external IPs)

### Traefik Ingress Controller (Considered but Rejected)

- **ALT-009: Description**: Use Traefik for Ingress implementation
- **ALT-010: Rejection Reason**:
  - Different configuration syntax (Traefik CRDs vs Kubernetes Ingress)
  - Lower adoption (18% vs Nginx's 64%)
  - Less teaching material and community resources
  - Nginx already implemented in Red Dog (migration cost)

### Service Mesh (Istio/Linkerd) for Ingress (Rejected)

- **ALT-011: Description**: Use Istio Gateway or Linkerd Ingress for external traffic
- **ALT-012: Rejection Reason**:
  - Overkill for ingress-only use case (service mesh adds complexity)
  - Requires understanding service mesh concepts (teaching overhead)
  - Heavier resource footprint (Istio control plane ~1GB RAM)
  - Nginx Ingress sufficient for HTTP routing needs

## Implementation Notes

### Current Nginx Deployment (To Be Upgraded)

**Existing Configuration** (`manifests/branch/dependencies/nginx/nginx.yaml`):
```yaml
apiVersion: helm.fluxcd.io/v1
kind: HelmRelease
metadata:
  name: nginx-ingress
  namespace: nginx-ingress
spec:
  releaseName: nginx-ingress
  chart:
    repository: https://kubernetes.github.io/ingress-nginx
    name: ingress-nginx
    version: 3.31.0  # OUTDATED (2021) - should upgrade to 4.10.0+
  values:
    controller:
      service:
        type: LoadBalancer
        annotations:
          service.beta.kubernetes.io/azure-dns-label-name: "paas-vnext-workshop"
      replicaCount: 2
```

**Recommended Upgrades:**
- Chart version: 3.31.0 → 4.10.0+ (latest stable)
- Add Prometheus metrics (`controller.metrics.enabled: true`)
- Add resource limits (`controller.resources`)

### kind Configuration (Local Development)

**kind Cluster Config** (`kind-config.yaml`):
```yaml
kind: Cluster
apiVersion: kind.x-k8s.io/v1alpha4
name: reddog-local
nodes:
- role: control-plane
  extraPortMappings:
  - containerPort: 80
    hostPort: 80
    protocol: TCP
  - containerPort: 443
    hostPort: 443
    protocol: TCP
  kubeadmConfigPatches:
  - |
    kind: InitConfiguration
    nodeRegistration:
      kubeletExtraArgs:
        node-labels: "ingress-ready=true"
```

**Nginx Installation (kind-specific):**
```bash
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/kind/deploy.yaml
```

**Verification:**
```bash
# Wait for Nginx to be ready
kubectl wait --namespace ingress-nginx \
  --for=condition=ready pod \
  --selector=app.kubernetes.io/component=controller \
  --timeout=90s

# Test Ingress
curl http://localhost/api/orders
```

### Cloud Deployment (AKS/EKS/GKE)

**Helm Installation (Cloud Environments):**
```bash
# Add Helm repository
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update

# Install Nginx Ingress
helm install nginx-ingress ingress-nginx/ingress-nginx \
  --namespace ingress-nginx --create-namespace \
  --set controller.replicaCount=2 \
  --set controller.metrics.enabled=true \
  --set controller.service.type=LoadBalancer
```

**Cloud-Specific Annotations:**

**Azure AKS:**
```yaml
service:
  annotations:
    service.beta.kubernetes.io/azure-dns-label-name: reddog
```

**AWS EKS:**
```yaml
service:
  annotations:
    service.beta.kubernetes.io/aws-load-balancer-type: "nlb"
```

**GCP GKE:**
```yaml
service:
  annotations:
    cloud.google.com/load-balancer-type: "External"
```

### Ingress Resource Best Practices

**TLS/HTTPS Support (Production):**
```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: reddog-ingress
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod
spec:
  ingressClassName: nginx
  tls:
  - hosts:
    - reddog.azure.example.com
    secretName: reddog-tls
  rules:
  - host: reddog.azure.example.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: reddog-ui
            port:
              number: 8080
```

**Rate Limiting:**
```yaml
metadata:
  annotations:
    nginx.ingress.kubernetes.io/limit-rps: "10"
```

**CORS Support:**
```yaml
metadata:
  annotations:
    nginx.ingress.kubernetes.io/enable-cors: "true"
    nginx.ingress.kubernetes.io/cors-allow-origin: "*"
```

### Monitoring and Observability

**Prometheus Metrics:**
```yaml
controller:
  metrics:
    enabled: true
    serviceMonitor:
      enabled: true
```

**Grafana Dashboard:**
- Import Nginx Ingress dashboard (ID: 9614) from grafana.com

**Access Logs:**
```bash
kubectl logs -n ingress-nginx deployment/nginx-ingress-controller -f
```

## References

- **REF-001**: Related ADR: `docs/adr/adr-0007-cloud-agnostic-deployment-strategy.md` (Containerized infrastructure)
- **REF-002**: Related ADR: `docs/adr/adr-0008-kind-local-development-environment.md` (kind uses Nginx Ingress)
- **REF-003**: Related ADR: `docs/adr/adr-0009-helm-multi-environment-deployment.md` (Helm deploys Nginx)
- **REF-004**: Research Document: `docs/research/dev-container-alternatives-2025.md` (Nginx Ingress evaluation)
- **REF-005**: Existing Manifest: `manifests/branch/dependencies/nginx/nginx.yaml` (current deployment)
- **REF-006**: Nginx Ingress Official Documentation: https://kubernetes.github.io/ingress-nginx/
- **REF-007**: kind Ingress Guide: https://kind.sigs.k8s.io/docs/user/ingress/
- **REF-008**: Kubernetes Ingress Documentation: https://kubernetes.io/docs/concepts/services-networking/ingress/
- **REF-009**: CNCF Survey 2024: 64% Nginx Ingress adoption in Kubernetes clusters
- **REF-010**: Helm Chart Repository: https://github.com/kubernetes/ingress-nginx/tree/main/charts/ingress-nginx
