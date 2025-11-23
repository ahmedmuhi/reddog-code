---
id: KI-DEPLOY_HELM_MULTI_ENVIRONMENT-001
title: Helm-based multi-environment deployment
tags:
  - helm
  - kubernetes
  - deployment
  - configuration
  - multi-environment
last_updated: 2025-11-22
source_sessions: []
source_plans: []
confidence: high
status: Active
owner: platform-team
notes: Canonical pattern for using a single Helm chart with per-environment values files across local and cloud clusters.
---

# Summary

This knowledge item describes how to use a single Helm chart to deploy an application to multiple Kubernetes environments (local and cloud) by pushing all environment-specific differences into values files. It defines the long-lived rules for chart structure, values layout, and the separation of concerns between templates, values, and secrets. It also highlights the role of this pattern in supporting cloud-agnostic Dapr components and GitOps tooling.

## Key Facts

- **FACT-001**: The primary deployment mechanism for Kubernetes workloads is a **single Helm application chart** per logical application, optionally complemented by separate infrastructure charts (e.g. SQL Server, Redis, ingress).
- **FACT-002**: Environment-specific configuration is provided via **per-environment values files** (e.g. `values/values-local.yaml`, `values/values-azure.yaml`, `values/values-aws.yaml`, `values/values-gcp.yaml`) rather than separate manifest directories.
- **FACT-003**: All Kubernetes environments (local clusters and cloud clusters) are deployed from the **same Helm templates**, with behavior differing only through `.Values` overrides.
- **FACT-004**: Dapr components (state store, pub/sub, secret store, bindings, configuration) are templated once in the Helm chart and parameterised by values such as component `type` and `metadata`, enabling the same application code to run against different backends per environment.
- **FACT-005**: Secrets and credentials are **not stored directly in committed values files**; instead, values files refer to Kubernetes secrets or cloud secret stores (e.g. Key Vault, Secrets Manager) via `secretKeyRef` or equivalent indirection.
- **FACT-006**: The canonical Helm command pattern for humans and automation is `helm upgrade --install <release> ./charts/<app> -f values/values-<env>.yaml`, where `<env>` matches a maintained environment values file.
- **FACT-007**: Legacy raw manifests (and Kustomize overlays, where present) may still exist, but once a Helm chart is introduced for a workload, the chart is treated as the **single source of truth** for those resources going forward.

## Constraints

- **CON-001**: New Kubernetes-based services and components MUST be deployable via Helm; introducing a new primary deployment path (e.g. raw manifests or ad-hoc kubectl apply) for the same workloads is not allowed.
- **CON-002**: Environment-specific differences (e.g. Dapr backend type, connection endpoints, ingress hostnames, resource sizes) MUST be expressed in values files, not by duplicating or forking YAML manifests per environment.
- **CON-003**: Committed values files MUST NOT contain real secrets (passwords, keys, tokens, connection strings with credentials); they may only contain references to secrets or secret stores.
- **CON-004**: Where GitOps tools (e.g. Flux, ArgoCD) are used, they MUST consume the same Helm charts that humans use, rather than maintaining a divergent set of raw manifests for the same application.
- **CON-005**: Any change that requires environment-specific behavior MUST be achievable by modifying values files and/or adding narrowly scoped template conditionals, without introducing environment-specific chart forks.

## Patterns & Recommendations

- **PAT-001**: Define a single **application chart** (`charts/<app>/Chart.yaml` + `templates/`) with environment-neutral manifests; place per-environment configuration in `values/values-<env>.yaml` files at the repo root (or another clearly documented shared location).
- **PAT-002**: For each environment, maintain a first-class values file that sets at least: `global.environment`, Dapr component types and metadata, ingress hostnames and TLS behavior, and sensible resource requests/limits and replica counts for that environment.
- **PAT-003**: Model Dapr components in templates so that only `.Values.dapr.*` changes between environments (e.g. `state.redis` vs `state.azure.cosmosdb` vs `state.aws.dynamodb` vs `state.gcp.firestore`), keeping application code and resource kinds stable.
- **PAT-004**: Use CI to run `helm lint` and `helm template` against all supported environment values files to catch template regressions early and to ensure every environment stays supported as charts evolve.
- **PAT-005**: Isolate shared infrastructure concerns (e.g. SQL Server, Redis, ingress controllers) into one or more dedicated **infrastructure charts** that can be deployed alongside the application chart using the same values pattern, rather than hand-authored manifests.
- **PAT-006**: When deprecating legacy raw manifests or overlays, mark them clearly as deprecated and plan a migration path to the Helm chart, rather than maintaining two parallel "primary" deployment stacks.

## Risks & Open Questions

### Risks

- **RISK-001**: If legacy raw manifests or overlays remain in active use alongside Helm charts, configuration drift and confusion can occur (e.g. different labels, ports, or Dapr settings between two sources).
- **RISK-002**: Missing or stale environment values files (for example, `values-aws.yaml` or `values-gcp.yaml` lagging behind `values-local.yaml`) can create a false sense of multi-environment readiness while only the local or primary cloud environment is actually tested.
- **RISK-003**: Overuse of complex conditional logic in Helm templates (e.g. many `if`/`else` branches keyed on environment) can make charts hard to reason about and debug, reducing the maintainability of the deployment layer.

### Open Questions

- **OPEN-001**: For environments where GitOps is in use, what is the precise timeline and process for fully migrating from raw manifests/overlays to HelmRelease (or equivalent) objects that reference the canonical charts?
- **OPEN-002**: What repository-level conventions (naming, directory layout, documentation) should be enforced for multiple applications sharing the same cluster, to ensure consistent use of the Helm + values pattern across teams?
- **OPEN-003**: How aggressively should we clean up and remove deprecated raw manifests once Helm charts are stable, especially if some external documentation or scripts still reference those manifests?

## Source & Provenance

- Derived from:
  - Review and refinement of the multi-environment Helm deployment decision in ADR-0009.
  - Repository inspection of existing Helm charts (`charts/`), values files (`values/values-local.yaml`, `values/values-azure.yaml.sample`), and legacy manifests under `manifests/`.
- Related ADRs:
  - `docs/adr/adr-0009-helm-multi-environment-deployment.md` (Helm as the multi-environment deployment mechanism).
  - `docs/adr/adr-0008-kind-local-development-environment.md` (local Kubernetes environment using the same Helm charts and values pattern).
  - `docs/adr/adr-0013-secret-management-strategy.md` (secret handling patterns that interact with values and Dapr components).
- Related implementation plans and guides:
  - `plan/upgrade-phase0-platform-foundation-implementation-1.md`
  - `plan/migrate-state-stores-cloud-native-implementation-1.md`
  - `docs/guides/dotnet10-upgrade-procedure.md` (examples of using Helm commands during service upgrades)
