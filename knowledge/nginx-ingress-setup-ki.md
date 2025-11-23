---
id: KI-NGINX_INGRESS_SETUP-001
title: Nginx Ingress Controller Setup & Standards
tags:
  - red-dog
  - ingress
  - nginx
  - kubernetes
  - deployment
last_updated: 2025-11-24
source_sessions: []
source_plans: []
confidence: high
status: Active
owner: Platform Engineering
notes: Replaces docs/guides/nginx-ingress-setup.md
---

# Summary

This Knowledge Item defines the standard installation, configuration, and usage patterns for the Nginx Ingress Controller in the Red Dog project. It covers repository structure, environment-specific installation methods (local vs. cloud), and the contract for exposing services.

## Key Facts

- **FACT-001**: The Helm wrapper chart for cloud deployments is located at `charts/external/nginx-ingress/`.
- **FACT-002**: Shared configuration values are stored in `charts/external/nginx-ingress/values-base.yaml`.
- **FACT-003**: Cloud-specific overrides are maintained in `values-azure.yaml`, `values-aws.yaml`, and `values-gcp.yaml` within the wrapper chart directory.
- **FACT-004**: The application ingress resource is generated from a single template at `charts/reddog/templates/ingress.yaml`.
- **FACT-005**: Local (kind) environments use the upstream static manifest for installation, not the Helm wrapper chart.

## Constraints

- **CON-001**: Ingress routes MUST be defined in environment-specific values files (e.g., `values/values-local.yaml`), NEVER by editing the `ingress.yaml` template directly.
- **CON-002**: The `ingress.className` value MUST match the installed controller's ingress class (default is `nginx`).
- **CON-003**: Cloud-specific LoadBalancer annotations (e.g., for Azure, AWS, GCP) MUST be configured in the wrapper chart's values files, NOT in application manifests.
- **CON-004**: When `ingress.tls.enabled` is true, the secret `reddog-tls` MUST exist or be provisioned via cert-manager annotations.

## Patterns & Recommendations

- **PAT-001**: To expose a new service, add a new entry to the `paths` list under the appropriate `host` in the values file and redeploy the `reddog` Helm release.
- **PAT-002**: Use the upstream kind manifest (`https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/kind/deploy.yaml`) for local development to ensure compatibility.
- **PAT-003**: For cloud deployments (AKS/EKS/GKE), always use the wrapper chart with both the base values and the specific cloud provider values file.

## Risks & Open Questions

### Risks

- **RISK-001**: Installing the Helm wrapper chart in a local `kind` cluster without significant value adjustments may fail or conflict with local port mappings.

### Open Questions

- **OPEN-001**: Full local (kind) validation workflows and automated tests for Ingress routing are still pending (per ADR-0010 status).

## Source & Provenance

- Derived from: `docs/guides/nginx-ingress-setup.md` (migrated to KI)
- Related ADRs:
  - `docs/adr/adr-0010-nginx-ingress-controller-cloud-agnostic.md`
- External Docs:
  - [Nginx Ingress Official Docs](https://kubernetes.github.io/ingress-nginx/)
