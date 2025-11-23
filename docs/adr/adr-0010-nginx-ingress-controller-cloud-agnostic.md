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

**Implemented**

This ADR records the architectural decision to use the Nginx Ingress Controller as the
standard HTTP/HTTPS ingress implementation for Red Dog across all Kubernetes-based
environments.

Implementation details (exact Helm values, install commands, and troubleshooting) are
tracked in:

- `docs/guides/nginx-ingress-setup.md`
- `knowledge/ki-deploy-helm-multi-environment-001.md`
- relevant implementation plans under `plan/`

As of 2025-11-23:

- Nginx is deployed via a Helm wrapper chart under `charts/external/nginx-ingress/`.
- Red Dog application ingress is defined once in
  `charts/reddog/templates/ingress.yaml` and configured via values files.
- Local development uses `values/values-local.yaml`; cloud deployments use
  `values-base.yaml` plus environment-specific values under
  `charts/external/nginx-ingress/values-{azure,aws,gcp}.yaml`.

This ADR does not track day-to-day configuration changes or upgrade history.

## Context

Red Dog services must be exposed over HTTP/HTTPS across multiple environments:

- Local: kind
- Cloud: Azure AKS, AWS EKS, GCP GKE

Requirements:

- **R-001: Cloud-agnostic routing**  
  One ingress definition per environment, not per cloud vendor. Same Ingress template
  should work across clusters (ADR-0007, ADR-0009).

- **R-002: Path-based routing**  
  A single external endpoint must route to multiple services by URL path
  (e.g. `/` → UI, `/api/orders` → OrderService).

- **R-003: Cost efficiency**  
  Use a single load balancer per environment, not one per service.

- **R-004: Teaching value**  
  The repo should demonstrate portable Kubernetes patterns rather than
  cloud-specific ingress controllers.

Kubernetes exposes HTTP routing through the Ingress API, but requires an Ingress
Controller to implement that API and provision underlying load balancers.

We needed a controller that:

- Works consistently across kind, AKS, EKS, and GKE.
- Uses the standard `networking.k8s.io/v1` Ingress API.
- Does not tie the sample to a single cloud provider’s PaaS ingress solution.

## Decision

**Adopt Nginx Ingress Controller as the standard ingress implementation for Red Dog
across all Kubernetes-based environments.**

### Scope

Within this decision:

- The **Ingress Controller** is Nginx, deployed as pods in the cluster.
- External HTTP/HTTPS traffic terminates at Nginx and is routed to Red Dog services via
  Kubernetes Services.
- A single Red Dog Ingress resource is rendered from
  `charts/reddog/templates/ingress.yaml`, configured by environment-specific values.

Outside this decision:

- Non-HTTP/HTTPS protocols (e.g. gRPC without HTTP/1.1, TCP-only services) may still
  use dedicated `Service` objects with `type: LoadBalancer` or other mechanisms.
- Detailed operational practices (certificate management, dashboard selection,
  observability stack) are handled in guides and implementation plans.

### Canonical contracts and files

- **Controller deployment:**
  - `charts/external/nginx-ingress/Chart.yaml`
  - `charts/external/nginx-ingress/values-base.yaml`
  - `charts/external/nginx-ingress/values-{azure,aws,gcp}.yaml`

- **Application ingress:**
  - Template: `charts/reddog/templates/ingress.yaml`
  - Local example: `values/values-local.yaml` (`ingress` section)

- **Values contract (simplified):**
  - `ingress.enabled` (bool)
  - `ingress.className` (e.g. `nginx`)
  - `ingress.tls.enabled` (bool)
  - `ingress.annotations` (map)
  - `ingress.hosts[*].host`
  - `ingress.hosts[*].paths[*].{path,pathType,service,port}`

To expose a new service externally, environment-specific values files are updated;
the ingress template is not modified per service.

## Consequences

### Positive

- **POS-001: Cloud-agnostic HTTP entrypoint**  
  The same ingress template and values pattern apply to kind, AKS, EKS, and GKE
  (ADR-0007, ADR-0009).

- **POS-002: Single load balancer per environment**  
  Nginx fronts multiple services via path-based routing, reducing cost and simplifying
  external DNS.

- **POS-003: Standard Kubernetes Ingress API**  
  Students and contributors learn the portable Ingress API rather than cloud-specific
  controllers and CRDs.

- **POS-004: Clear separation of concerns**  
  External traffic (user → Nginx → service) is handled by this ADR; internal
  service-to-service traffic uses Dapr (ADR-0002).

- **POS-005: Extensibility**  
  Nginx supports TLS termination, rate limiting, authentication, and URL rewriting
  via well-documented annotations.

### Negative

- **NEG-001: Controller overhead**  
  Nginx pods consume additional CPU/memory and must be managed as part of the
  platform baseline.

- **NEG-002: HTTP/HTTPS scope only**  
  Non-HTTP protocols still require separate `LoadBalancer` Services or other
  mechanisms.

- **NEG-003: Loss of cloud-specific features**  
  Cloud-native WAF or advanced load-balancer features (e.g. Azure WAF, AWS Shield)
  are not used directly; they must be added separately if required.

- **NEG-004: Operational upkeep**  
  Nginx upgrades (chart and image) must be coordinated and tested separately from
  application releases.

## Alternatives Considered

### Cloud-native ingress controllers (AGIC, AWS ALB, GCP Ingress)

- **ALT-001: Description**  
  Use Azure Application Gateway Ingress Controller on AKS, AWS ALB ingress controller
  on EKS, and GCP’s native ingress solution on GKE.

- **ALT-002: Rejection reason**  
  Ties each environment to a different controller, configuration model, and set of
  annotations. Conflicts with the multi-cloud teaching goal and complicates reuse of
  manifests across providers (ADR-0007).

### LoadBalancer per service

- **ALT-003: Description**  
  Expose each service with its own `Service` of `type: LoadBalancer`.

- **ALT-004: Rejection reason**  
  Increases cost, external IP sprawl, and management overhead. Provides no centralized
  routing or TLS termination pattern.

### Alternative ingress controllers (Traefik, HAProxy, etc.)

- **ALT-005: Description**  
  Use another open-source ingress controller with CRD-based configuration.

- **ALT-006: Rejection reason**  
  Comparable capabilities, but Nginx has higher adoption and more teaching material.
  Choosing Nginx reduces surprise for readers and aligns with common industry defaults.

### Service mesh as primary ingress (Istio/Linkerd)

- **ALT-007: Description**  
  Use a service mesh gateway for all external traffic and intra-cluster routing.

- **ALT-008: Rejection reason**  
  Adds significant complexity and resource overhead for a learning-focused sample.
  For Red Dog, a simple Ingress controller is sufficient at the edge; service mesh can
  be an optional future layer, not the baseline.

## Implementation Notes

These notes connect this decision to other work; operational details are in guides and
plans.

- **IMP-001: Relationship to other ADRs**
  - ADR-0007: Nginx is deployed as containers inside the cluster to preserve
    cloud-agnostic behavior.
  - ADR-0008: Local kind clusters use Nginx to expose the app on `localhost`.
  - ADR-0009: Nginx is installed and configured via Helm with environment-specific
    values files.
  - ADR-0002: Ingress is for external traffic only; internal calls use Dapr sidecars.

- **IMP-002: Where to change routing**
  - To add or change external paths/hosts, update the `ingress` section in the
    relevant values file (local or cloud).
  - Do not modify `charts/reddog/templates/ingress.yaml` for one-off routes unless
    the template contract itself needs to change.

- **IMP-003: Further details**
  - Installation commands, environment-specific flags, and troubleshooting steps are
    documented in `docs/guides/nginx-ingress-setup.md`.
  - Multi-environment Helm deployment patterns are covered in
    `knowledge/ki-deploy-helm-multi-environment-001.md`.

---
