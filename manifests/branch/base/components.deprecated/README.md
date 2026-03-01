# Deprecated: Raw Kubernetes Dapr Component Manifests

**Status:** Deprecated as of 2026-03-01
**Replaced by:** `charts/reddog/templates/dapr-components/`

## What these files were

These raw Kubernetes manifests defined Dapr components for the `branch` deployment environment. They targeted the `reddog-retail` namespace and included cloud-specific backends (Azure Blob Storage, Azure Key Vault, RabbitMQ).

## Why they are deprecated

- All Dapr components are now rendered from Helm templates in `charts/reddog/templates/dapr-components/`.
- Helm templates are parameterized for multi-environment use (local, Azure, AWS, GCP) via values files.
- The canonical namespace is `reddog` (not `reddog-retail`).
- The `deployments/` directory alongside this one is also legacy and will be archived in Phase 3 (Kustomize elimination).

## Mapping to Helm equivalents

| Legacy file | Helm template |
|---|---|
| `reddog.pubsub.yaml` | `charts/reddog/templates/dapr-components/pubsub.yaml` |
| `reddog.state.makeline.yaml` | `charts/reddog/templates/dapr-components/statestore-makeline.yaml` |
| `reddog.state.loyalty.yaml` | `charts/reddog/templates/dapr-components/statestore-loyalty.yaml` |
| `reddog.secretstore.yaml` | `charts/reddog/templates/dapr-components/secretstore.yaml` |
| `reddog.binding.receipt.yaml` | `charts/reddog/templates/dapr-components/binding-receipt.yaml` |
| `reddog.binding.virtualworker.yaml` | `charts/reddog/templates/dapr-components/binding-virtualworker.yaml` |
| `reddog.config.yaml` | `charts/reddog/templates/dapr-components/configuration.yaml` |
