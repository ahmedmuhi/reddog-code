---
title: "ADR-0010: Nginx Ingress Controller for Cloud-Agnostic Traffic Routing"
status: "Implemented"
date: "2025-11-09"
last_updated: "2025-11-23"
authors: "Red Dog Modernization Team"
tags: ["architecture", "decision", "nginx", "ingress", "kubernetes", "multi-cloud"]
supersedes: ""
superseded_by: ""
---

# ADR-0010: Nginx Ingress Controller for Cloud-Agnostic Traffic Routing

## Status

**Implemented** (as of 2025-11-23)

## Implementation Status

**Canonical Deployment:**
- Helm wrapper chart: `charts/external/nginx-ingress/` (ingress-nginx 4.14.0)
- Base configuration: `charts/external/nginx-ingress/values-base.yaml`
- Environment-specific values: `values-azure.yaml`, `values-aws.yaml`, `values-gcp.yaml`
- Red Dog ingress template: `charts/reddog/templates/ingress.yaml`
- Local values example: `values/values-local.yaml` (ingress configuration)

**Dependencies:**
- **Depends On:** ADR-0009 (Helm multi-environment deployment)
- **Supports:** ADR-0008 (kind local development with localhost:80 access)
- **Implements:** Cloud-agnostic HTTP routing per ADR-0007

## Context

Red Dog Coffee requires external HTTP/HTTPS access to microservices across four environments: local (kind), Azure AKS, AWS EKS, and GCP GKE. Kubernetes provides the Ingress API for defining HTTP routing rules, but requires an Ingress Controller implementation.

**Key Requirements:**
- **Cloud-Agnostic**: Same Ingress manifests work across all environments (ADR-0007)
- **Path-Based Routing**: Route by URL path (`/` → UI, `/api/orders` → OrderService)
- **Cost Efficiency**: Single load balancer for all services vs. one per service (75-80% savings)
- **Teaching Focus**: Students learn portable Kubernetes patterns

**Current Implementation:**
Red Dog uses Helm-based deployment (ADR-0009) with:
- Nginx wrapper chart: `charts/external/nginx-ingress/` (ingress-nginx 4.14.0)
- Application ingress template: `charts/reddog/templates/ingress.yaml`
- Environment values: `values-base.yaml` + cloud-specific overrides
- All services accessible via single external endpoint

**Ingress Flow:**
```
User → Cloud Load Balancer → Nginx Controller Pod → Backend Services
      (auto-provisioned)     (reads Ingress rules)  (order, ui, etc.)
```

**Alternatives Evaluated:**
- **Cloud-native controllers** (Azure AGIC, AWS ALB): Rejected - breaks multi-cloud portability
- **LoadBalancer per service**: Rejected - 5× cost, no centralized routing
- **Traefik/HAProxy**: Rejected - different config syntax, lower adoption (18%/12% vs Nginx 64%)

## Decision

**Use Nginx Ingress Controller as the standard ingress implementation across all Red Dog environments.**

**Core Implementation:**
- Deploy via Helm wrapper chart: `charts/external/nginx-ingress/` (wraps upstream `ingress-nginx` 4.14.0 with Red Dog defaults)
- Environment-specific configuration via values files (ADR-0009 pattern)
- Identical Ingress resources across environments (defined in `charts/reddog/templates/ingress.yaml`)
- Cloud load balancers auto-provisioned via `type: LoadBalancer` Service

**Canonical Files:**
- `charts/external/nginx-ingress/Chart.yaml` - Helm chart definition
- `charts/external/nginx-ingress/values-base.yaml` - Common settings (resources, metrics, HA)
- `charts/external/nginx-ingress/values-{azure,aws,gcp}.yaml` - Cloud-specific annotations
- `charts/reddog/templates/ingress.yaml` - Red Dog application ingress template
- `values/values-local.yaml` - Local ingress configuration example

**Deployment:**
See [Nginx Ingress Setup Guide](../guides/nginx-ingress-setup.md) for detailed installation instructions.

**Example Ingress Configuration (from values file):**
```yaml
ingress:
  enabled: true
  className: nginx
  hosts:
    - host: localhost  # Or cloud-specific hostname
      paths:
        - path: /
          service: ui
          port: 80
        - path: /api/orders
          service: orderservice
          port: 80
```

**Rationale:**
- **Cloud-Agnostic**: Same Ingress manifests work on kind, AKS, EKS, GKE
- **Cost-Effective**: Single load balancer vs. one per service (75-80% cost reduction)
- **Industry Standard**: 64% adoption (CNCF 2024), extensive documentation
- **Teaching Value**: Students learn portable Kubernetes Ingress API
- **Feature-Rich**: SSL/TLS, rate limiting, authentication, URL rewriting via annotations
- **Production-Ready**: Battle-tested, active community support

## Consequences

### Positive
- **Unified Configuration**: Same Ingress manifests deploy across all environments
- **Cost Savings**: Single load balancer vs. one per service ($20/month vs. $100/month per cloud)
- **Kubernetes-Native**: Standard Ingress API, not cloud-proprietary solutions
- **Local Development**: kind supports Nginx via port mappings (localhost:80)
- **SSL/TLS Support**: cert-manager integration for automatic HTTPS certificates
- **Feature-Rich**: Path/host-based routing, rate limiting, authentication, CORS, URL rewriting
- **Observability**: Prometheus metrics, access logs, Grafana dashboards
- **Teaching Value**: Portable patterns, transferable to any Kubernetes environment

### Negative
- **Resource Overhead**: Controller pods consume ~128Mi RAM per replica (mitigated via resource limits in values-base.yaml)
- **HTTP/HTTPS Only**: Non-HTTP protocols require separate LoadBalancer services
- **Cloud Features Lost**: Azure WAF, AWS Shield not available (acceptable trade-off for portability)
- **Port Conflicts**: Local development requires ports 80/443 available (documented in setup guide)
- **Upgrade Maintenance**: Controller upgraded separately from applications (standard Helm workflow)

## Relationship to Other ADRs

| ADR | Relationship |
|-----|--------------|
| **ADR-0007** | Ingress controller deployed as containers, not PaaS (cloud-agnostic) |
| **ADR-0008** | Nginx enables localhost:80 access for kind local development |
| **ADR-0009** | Deployed via Helm with environment-specific values |
| **ADR-0002** | Ingress routes external traffic; Dapr handles internal service calls |

**Traffic Flow:**
- **External:** User → Nginx Ingress → Application Services
- **Internal:** Service A → Dapr sidecar → Service B (no Ingress involved)

## Alternatives Considered

### Cloud-Native Controllers (Azure AGIC, AWS ALB, GCP Ingress)
**Rejected:** Breaks multi-cloud portability (ADR-0007). Each cloud requires different configurations and annotations, preventing "write once, deploy anywhere" goal. Students would learn cloud-specific patterns instead of portable Kubernetes skills.

### LoadBalancer Service Per Service
**Rejected:** 5× cost ($100/month vs $20/month per cloud). No centralized routing, SSL termination, or path-based rules. Requires separate external IP per service.

### Traefik Ingress Controller
**Rejected:** Lower adoption (18% vs Nginx 64%), different config syntax (CRDs vs standard Ingress), less teaching materials.

### Service Mesh (Istio/Linkerd) for Ingress
**Rejected:** Complexity overkill for HTTP routing. Service mesh adds ~1GB RAM overhead, requires mesh concepts training. Nginx sufficient for ingress needs.

## Guidance for Future Plans

### Adding New Services
When adding services that require external access:
1. Add path configuration to ingress section in appropriate `values-{env}.yaml`
2. Ingress template (`charts/reddog/templates/ingress.yaml`) automatically generates routes
3. No Nginx-specific configuration needed - use standard Kubernetes Ingress API

### Enabling HTTPS/TLS
For production environments:
1. Deploy cert-manager (see `values/cert-manager/`)
2. Add `cert-manager.io/cluster-issuer` annotation to ingress
3. Enable TLS in values: `ingress.tls.enabled: true`
4. Certificate automatically provisioned and renewed

### Environment-Specific Customization
Modify cloud-specific values files for:
- **Azure**: DNS labels, load balancer SKU
- **AWS**: NLB settings, cross-zone balancing
- **GCP**: External load balancer config
- Base settings in `values-base.yaml` apply to all environments

### Monitoring and Observability
- Metrics enabled by default (`controller.metrics.enabled: true`)
- Access logs via `kubectl logs` in `ingress-nginx` namespace
- Import Grafana dashboard ID 9614 for visualizations

**Detailed Setup Instructions:** See [Nginx Ingress Setup Guide](../guides/nginx-ingress-setup.md)

## References

### Related ADRs
- [ADR-0007: Cloud-Agnostic Deployment Strategy](adr-0007-cloud-agnostic-deployment-strategy.md)
- [ADR-0008: kind Local Development Environment](adr-0008-kind-local-development-environment.md)
- [ADR-0009: Helm Multi-Environment Deployment](adr-0009-helm-multi-environment-deployment.md)

### Knowledge Items
- [KI: Red Dog Architecture](../../knowledge/ki-red-dog-architecture-001.md) - Service boundaries and communication patterns
- [KI: Helm Multi-Environment Deployment](../../knowledge/ki-deploy-helm-multi-environment-001.md) - Values-based configuration

### Implementation Files
- `charts/external/nginx-ingress/Chart.yaml` - Helm chart wrapper
- `charts/external/nginx-ingress/values-base.yaml` - Common configuration
- `charts/external/nginx-ingress/values-{azure,aws,gcp}.yaml` - Cloud-specific settings
- `charts/reddog/templates/ingress.yaml` - Application ingress template
- `values/values-local.yaml` - Local development ingress configuration

### External Documentation
- [Nginx Ingress Official Docs](https://kubernetes.github.io/ingress-nginx/)
- [kind Ingress Guide](https://kind.sigs.k8s.io/docs/user/ingress/)
- [Kubernetes Ingress Concepts](https://kubernetes.io/docs/concepts/services-networking/ingress/)
- [Setup Guide](../guides/nginx-ingress-setup.md) - Detailed installation and troubleshooting
