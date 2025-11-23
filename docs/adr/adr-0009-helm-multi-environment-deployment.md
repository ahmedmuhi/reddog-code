---
title: "ADR-0009: Helm-Based Multi-Environment Deployment Strategy"
status: "Accepted"
date: "2025-11-09"
authors: "Red Dog Modernization Team"
tags: ["architecture", "decision", "helm", "multi-cloud", "deployment", "kubernetes"]
supersedes: ""
superseded_by: ""
---

# ADR-0009: Helm-Based Multi-Environment Deployment Strategy

## Status

**Accepted**

## Implementation Status

**Current State:** 🟡 In Progress (Helm in active use, migration not complete)

**What’s Working:**

- `charts/reddog/` exists as a Helm v2 application chart with:
  - `Chart.yaml` and service Deployments/Services under `templates/`.
  - Dapr components under `templates/dapr-components/` (pubsub, statestores, secretstore, bindings, configuration).
- `charts/infrastructure/` exists as a Helm chart for infrastructure dependencies.
- `values/values-local.yaml` exists and is used by:
  - `scripts/setup-local-dev.sh`.
  - `.image-manifest-*` and upgrade scripts (`scripts/upgrade-dotnet10.sh`, `scripts/upgrade-build-images.sh`).
- `helm template reddog ./charts/reddog -f values/values-local.yaml` renders successfully for the core services and Dapr components.
- `values/values-local.yaml.sample` and `values/values-azure.yaml.sample` define the intended pattern for environment-specific values.

**What’s Not Yet Complete:**

- Non-local values:
  - `values/values-azure.yaml`, `values/values-aws.yaml`, `values/values-gcp.yaml` do not yet exist as first-class, maintained files (only `*.sample` exists for Azure).
- Raw manifests:
  - `manifests/branch/base/*.yaml` and `manifests/overlays/*/*.yaml` still contain the legacy raw Kubernetes manifests and Dapr components.
  - No `HelmRelease` resources exist in `manifests/` for the `reddog` chart; GitOps wiring (Flux/Argo) still points at raw manifests or is out-of-band.
- Single source of truth:
  - Helm is actively used (especially for local dev and infra upgrades), but the repository still carries both charts and raw manifests. The migration of all environments to Helm as the canonical deployment mechanism is incomplete.

**Next Steps (Implementation-Oriented, Not Binding on Architecture):**

1. Add and maintain environment values files:
   - `values/values-azure.yaml`
   - `values/values-aws.yaml`
   - `values/values-gcp.yaml`
2. Decide and document the canonical layout for values (top-level `values/` directory vs chart-local `values/`), and converge repo + ADR on that pattern.
3. Migrate GitOps manifests to consume the Helm charts (e.g. Flux `HelmRelease`), and explicitly deprecate/remove raw manifests under `manifests/branch` and `manifests/overlays` once migration is complete.
4. Add CI checks:
   - `helm lint ./charts/reddog`
   - `helm template` for all supported `values/values-*.yaml` to prevent template regressions.

---

## Context

Red Dog’s modernization strategy targets four main deployment environments under a cloud-agnostic architecture (ADR-0007) and Dapr-based runtime abstraction (ADR-0002):

- Local: kind (ADR-0008).
- Azure: AKS.
- AWS: EKS.
- GCP: GKE.

Key constraints:

- **DRY:** Avoid duplicating Kubernetes YAML across environments.
- **Cloud-agnostic:** Same application code and core manifests should work across all supported clusters.
- **Dapr-centric:** Dapr abstracts infrastructure differences (Redis vs Cosmos vs Dynamo vs Firestore), but Dapr components themselves still need to be deployed and parameterised per environment.
- **Teaching/demo:** The deployment story should reinforce “same app + same chart, different values” rather than “four different sets of manifests.”
- **Multi-environment:** Local and cloud environments differ in:
  - State store backend (Redis vs cloud services).
  - Secret store (Kubernetes vs cloud secret managers).
  - Ingress endpoints and DNS.
  - Some infrastructure components (e.g. RabbitMQ for cloud-only pub/sub).

The previous setup relied heavily on raw Kubernetes manifests in `manifests/branch` and Kustomize overlays under `manifests/overlays/*`. This created duplication and made multi-environment drift more likely as the number of environments increased.

---

## Decision

**Use Helm charts with environment-specific values files as the standard packaging and deployment mechanism for Red Dog across local, AKS, EKS, and GKE.**

High-level structure (conceptual):

- Application chart:

  - `charts/reddog/`
    - `Chart.yaml`
    - `templates/` (Deployments, Services, Dapr components, Ingress, etc.)

- Infrastructure chart(s):

  - `charts/infrastructure/` for Redis, SQL Server, and other infra containers.
  - External charts (e.g. `charts/external/nginx-ingress/`, `charts/external/rabbitmq/`) used as dependencies where appropriate.

- Environment values (current pattern in the repo):

  - Top-level `values/` directory:
    - `values/values-local.yaml` (real, used).
    - `values/values-local.yaml.sample` (pattern).
    - `values/values-azure.yaml.sample` (pattern).
    - Future: `values/values-azure.yaml`, `values/values-aws.yaml`, `values/values-gcp.yaml`.

Helm commands follow the "same chart, different values file" pattern, for example:

```bash
# Local (kind)
helm upgrade --install reddog ./charts/reddog -f values/values-local.yaml

# Azure (AKS)
helm upgrade --install reddog ./charts/reddog -f values/values-azure.yaml

# AWS (EKS)
helm upgrade --install reddog ./charts/reddog -f values/values-aws.yaml

# GCP (GKE)
helm upgrade --install reddog ./charts/reddog -f values/values-gcp.yaml
````

The **architectural** decision is:

* Application manifests (Deployments, Services, Dapr components, Ingress) are defined once in the `reddog` Helm chart.
* All environment differences are expressed via values files (`values/values-*.yaml`), not via separate copies of manifests.
* GitOps (Flux/Argo) consumes the same charts and values, rather than maintain separate raw YAML.

---

## Rationale

Key reasons for choosing this approach:

* **HELM-001 – Single source of truth:**
  Application and Dapr component manifests are declared once in Helm templates; environments vary only via values.

* **HELM-002 – Multi-environment clarity:**
  Differences between local, Azure, AWS, and GCP appear in `values/values-*.yaml` instead of being scattered across multiple manifest trees.

* **HELM-003 – Alignment with ADR-0002 and ADR-0007:**

  * ADR-0002 (Dapr abstraction) defines *how* services talk to infra (Dapr APIs).
  * ADR-0007 (containerized infra) defines *where* infra runs (containers vs PaaS).
  * ADR-0009 (this ADR) defines *how deployments are packaged* across environments (Helm chart + values).

* **HELM-004 – Teaching and demo value:**
  The “helm upgrade --install reddog … -f values/values-*.yaml” pattern is easy to explain and matches a real-world multi-environment practice.

* **HELM-005 – Ecosystem and tooling:**
  Helm is widely supported:

  * GitOps controllers (Flux/Argo).
  * CI tooling (lint, template).
  * External charts for infra (cert-manager, Nginx Ingress, RabbitMQ, KEDA, etc.).

* **HELM-006 – Evolution and refactoring:**
  As infra changes (e.g. Redis 7 upgrade, new pub/sub backends), the chart templates can evolve while keeping the environment-specific configuration localised in values files.

---

## Scope

This ADR applies to:

* **Red Dog application deployment**:

  * OrderService, MakeLineService, LoyaltyService, AccountingService, ReceiptGenerationService, VirtualCustomers, VirtualWorker, UI.
  * Their Deployments, Services, Ingress, and Dapr components.

* **Infrastructure deployed via Kubernetes** (where not handled by separate infra-as-code tools):

  * Redis, RabbitMQ, SQL Server containers and similar infra components that are part of the cluster-level deployment.

This ADR does **not** cover:

* How clusters themselves are provisioned (Terraform/Bicep/ARM, etc.).
* Non-Kubernetes packaging strategies (e.g. direct `az containerapp` commands).
* Secret content or rotation strategy (covered by ADR-0013).
* Object storage bindings strategy (covered by ADR-0012).

---

## Relationship to Other ADRs

| ADR                                           | Concern              | Role of Helm in relation to it                                                                                     |
| --------------------------------------------- | -------------------- | ------------------------------------------------------------------------------------------------------------------ |
| ADR-0002 – Dapr Abstraction                   | Runtime connectivity | Helm deploys and parameterises Dapr components (state, pub/sub, bindings, secret stores).                          |
| ADR-0004 – Dapr Config API                    | App configuration    | Helm may set component names and basic wiring, but feature flags and app config remain in Dapr config.             |
| ADR-0006 – Infra Config via Env Vars          | Runtime infra config | Helm injects env var values (from `values/values-*.yaml` and secrets), but does not change the app’s config model. |
| ADR-0007 – Cloud-Agnostic Deployment Strategy | Deployment topology  | Helm implements the charts that realise containerized infra vs PaaS choices in each environment.                   |
| ADR-0008 – kind Local Dev                     | Local cluster        | Helm is the deployment mechanism to kind, using `values/values-local.yaml`.                                        |
| ADR-0010 – Nginx Ingress                      | Ingress choice       | Helm deploys and configures Nginx Ingress in each environment (directly or via external chart).                    |
| ADR-0013 – Secret Management                  | Secrets              | Helm renders Kubernetes secrets and secret-backed Dapr components based on environment-specific values.            |

---

## Consequences

### Positive

* **POS-001 – DRY manifests:**
  One set of templates for all environments; minimal duplication.

* **POS-002 – Clear environment deltas:**
  Differences live in `values/values-*.yaml`, which can be diffed and reviewed explicitly.

* **POS-003 – GitOps-friendly:**
  Flux/Argo can manage `reddog` and `infrastructure` charts across clusters; no need to maintain parallel raw YAML trees.

* **POS-004 – Supports local-to-cloud parity:**
  Local kind cluster uses exactly the same chart as AKS/EKS/GKE, with a different values file.

* **POS-005 – Easier upgrades:**
  Shared templates simplify upgrades to Dapr, Redis, RabbitMQ, etc., because you change logic once and validate across values.

### Negative

* **NEG-001 – Helm learning curve:**
  Team members must understand Helm templating and values management.

* **NEG-002 – Values file complexity:**
  As environments grow (local, multiple AKS clusters, multiple EKS clusters), the number of values files can expand and require coordination.

* **NEG-003 – Migration cost:**
  Transitioning from raw manifests in `manifests/branch` and `manifests/overlays` to Helm charts requires working through existing customisations and overlays.

---

## Alternatives Considered

### Kustomize overlays

* **Description:** Base manifests in one location, environment overlays in `overlays/*`.
* **Reason not chosen:**
  Sufficient for small variations, but less expressive than Helm when Dapr components, multiple backend types, and conditional resources are involved. Helm provides richer templating and integrates better with existing tooling already adopted in this repo.

### Environment-specific manifest directories

* **Description:** Separate directories per environment (`manifests/local`, `manifests/azure`, `manifests/aws`, `manifests/gcp`) with mostly duplicated YAML.
* **Reason not chosen:**
  High duplication, high drift risk, and confusing for students and maintainers. Violates the DRY goal.

### Terraform Kubernetes provider for app deployment

* **Description:** Use Terraform to apply Kubernetes resources for Red Dog.
* **Reason not chosen:**
  Terraform is better suited for provisioning clusters and external infra; Helm is a better match for packaging and evolving Kubernetes-native applications and Dapr components.

### RADIUS or other higher-level platforms

* **Description:** Use RADIUS (or similar) for unified multi-cloud app deployment.
* **Reason not chosen (for now):**
  Maturity, ecosystem and coverage (particularly GCP) are not yet at a point where they can replace Helm as the primary packaging mechanism. Helm remains the base; higher-level tools can be layered on later if required.

---

## Implementation Notes (Non-Normative)

These points are descriptive of current and intended practice; they are not part of the architectural contract:

* The repository currently:

  * Uses `values/values-local.yaml` as the main local dev values file.
  * Uses `values/values-*.yaml.sample` to document planned structure for cloud environments.
  * Includes Helm charts for `reddog` and `infrastructure`, and external charts (e.g. Nginx Ingress, RabbitMQ) in `charts/external/`.

* Over time, the target is:

  * All new deployment work for Red Dog to go through Helm charts.
  * GitOps manifests to reference Helm charts and values only.
  * Raw manifests under `manifests/branch` and `manifests/overlays` to be explicitly deprecated and removed once migrations are complete.
