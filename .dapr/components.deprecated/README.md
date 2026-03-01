# Deprecated: Local Dapr Components (dapr run workflow)

**Status:** Deprecated as of 2026-03-01
**Replaced by:** `charts/reddog/templates/dapr-components/`

## What these files were

These component YAML files were used with the `dapr run` CLI for local development outside Kubernetes. They defined Redis-based state stores, pub/sub, a local file secret store, and a local storage binding — all pointing to `localhost`.

## Why they are deprecated

- Local development now uses **kind + Helm** exclusively (see `scripts/setup-local-dev.sh`).
- All Dapr components are rendered from Helm templates in `charts/reddog/templates/dapr-components/`.
- These files use the old no-separator app IDs (`orderservice`, `makelineservice`); the canonical format is kebab-case (`order-service`, `make-line-service`). See `docs/audits/dapr-component-audit.md`.

## Mapping to Helm equivalents

| Legacy file | Helm template |
|---|---|
| `pubsub.yaml` | `charts/reddog/templates/dapr-components/pubsub.yaml` |
| `statestore-makeline.yaml` | `charts/reddog/templates/dapr-components/statestore-makeline.yaml` |
| `statestore-loyalty.yaml` | `charts/reddog/templates/dapr-components/statestore-loyalty.yaml` |
| `secretstore.yaml` | `charts/reddog/templates/dapr-components/secretstore.yaml` |
| `binding-receipt.yaml` | `charts/reddog/templates/dapr-components/binding-receipt.yaml` |

Components missing here (cron binding, Configuration) were always absent from the `dapr run` workflow.
