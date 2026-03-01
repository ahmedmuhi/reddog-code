# KI: Configuration Architecture

> Stable facts, patterns, and constraints for the Red Dog configuration system
> after the configuration consolidation project (Phases 1–5).

## Status

**Active** — reflects the repository as of March 2026.

## The 4-Layer Configuration Model

Red Dog uses four layers, evaluated top-to-bottom (later layers override earlier ones):

| Layer | What lives here | Where it lives | Who changes it |
|---|---|---|---|
| 1. Chart defaults | Stable, environment-agnostic settings (image repos, Dapr app IDs, default resources, component types) | `charts/reddog/values.yaml`, `charts/infrastructure/values.yaml` | Chart maintainers (PR) |
| 2. Environment overrides | Settings that vary per environment (Redis host, ingress hosts, SQL password, Dapr metadata, replica counts) | `values/values-<env>.yaml` (gitignored) | Operator / developer |
| 3. Runtime secrets | Credentials, connection strings, API keys | Kubernetes Secrets (local: Helm-rendered from values; cloud: ESO/CSI from managed stores) | Infra team / secret rotation |
| 4. Dapr Configuration API | Business config (store ID, max order size, feature flags) that may change at runtime | Redis (local), Azure App Config / managed store (cloud) — via `configuration.redis` Dapr component | Product / ops team |

### Layer Interactions

```
┌─────────────────────────────────────────────────────────┐
│  Layer 4: Dapr Config API  (runtime, no restart)        │
│  └─ redis-master:6379 → virtual-customers reads keys    │
├─────────────────────────────────────────────────────────┤
│  Layer 3: K8s Secrets  (restart may be needed)          │
│  └─ sqlserver-secret, reddog-sql                        │
├─────────────────────────────────────────────────────────┤
│  Layer 2: values/values-local.yaml  (helm upgrade)      │
│  └─ dapr.stateStore.metadata, ingress.hosts, etc.       │
├─────────────────────────────────────────────────────────┤
│  Layer 1: charts/*/values.yaml  (PR + helm upgrade)     │
│  └─ services.orderservice.image.repository, etc.        │
└─────────────────────────────────────────────────────────┘
```

## Key File Paths

| Purpose | Path |
|---|---|
| App chart defaults | `charts/reddog/values.yaml` |
| Infrastructure chart defaults | `charts/infrastructure/values.yaml` |
| Local overrides | `values/values-local.yaml` (gitignored) |
| Local sample | `values/values-local.yaml.sample` (committed) |
| Azure sample | `values/values-azure.yaml.sample` (committed) |
| Dapr components | `charts/reddog/templates/dapr-components/*.yaml` (8 files) |
| Infrastructure templates | `charts/infrastructure/templates/*.yaml` |
| Setup script | `scripts/setup-local-dev.sh` |
| Gitignore pattern | `values/values-*.yaml` ignored, `!values/values-*.yaml.sample` allowed |

## Helm Deep Merge Behaviour

Helm uses Sprig's `mergeOverwrite` for values:

- **Maps** are deep-merged: keys in the override file add to or replace keys in chart defaults.
- **Lists** are replaced entirely: if `values-local.yaml` defines `dapr.stateStore.metadata`, the whole list replaces the chart default — items are not appended.

**Implication:** Each environment values file must supply the complete `metadata` list for any Dapr component it overrides. You cannot add a single metadata item; you must repeat the full list.

## Gitignore and Sample Pattern

```gitignore
# In .gitignore:
values/values-*.yaml          # Ignore all real values (contain secrets)
!values/values-*.yaml.sample  # Allow committed samples
```

- Real values files are **never committed** (they contain SQL passwords, Redis credentials, etc.).
- Samples use `CHANGEME` placeholders and are safe to commit.
- `setup-local-dev.sh` auto-copies `values-local.yaml.sample` → `values-local.yaml` if the latter is missing.

## Dapr Component Architecture

All 8 Dapr components live in `charts/reddog/templates/dapr-components/`:

| Component | Dapr Type | Component Name | Scoped To |
|---|---|---|---|
| `pubsub.yaml` | `pubsub.redis` | `reddog.pubsub` | order-service, make-line-service, accounting-service, loyalty-service, receipt-generation-service |
| `statestore-makeline.yaml` | `state.redis` | `reddog.state.makeline` | make-line-service |
| `statestore-loyalty.yaml` | `state.redis` | `reddog.state.loyalty` | loyalty-service |
| `secretstore.yaml` | `secretstores.kubernetes` | `reddog.secretstore` | All services |
| `binding-receipt.yaml` | `bindings.localstorage` | `reddog.binding.receipt` | receipt-generation-service |
| `binding-virtualworker.yaml` | `bindings.cron` | `reddog.binding.virtualworker` | virtual-worker |
| `configuration.yaml` | `configuration.redis` | `reddog.configuration` | (Dapr runtime config — tracing, health checks) |
| `configstore.yaml` | `configuration.redis` | `reddog.config` | virtual-customers |

- Component **types** are set in chart defaults (Layer 1) and overridable in values files (Layer 2).
- Component **metadata** (hosts, passwords, connection details) come from environment values files.
- All components are **app-scoped** (explicit `scopes` lists) per project convention.

## VirtualCustomers Dapr Config Pilot (Layer 4)

VirtualCustomers is the pilot service for the Dapr Configuration API (ADR-0004):

- **7 business keys** stored in Redis and accessed via `AddDaprConfigurationStore("reddog.config", keys, client, 60s)`.
- Keys use `||` as separator in Redis; the Dapr SDK maps `||` to `:` for `IConfiguration` binding (e.g., `VirtualCustomers||StoreId` → `VirtualCustomers:StoreId`).
- **3 operational keys** remain as env vars (`NumOrders`, `DisableDaprCalls`, `OrderServiceAppId`).
- A `config-seeder-job.yaml` (redis:7.2-alpine) seeds default values on Helm install using `SET key value NX` (no-overwrite).

## Cloud Environment Patterns

For cloud environments (AKS, EKS, GKE):

- **Infrastructure chart is disabled** (`infrastructure.redis.enabled: false`, `infrastructure.sqlserver.enabled: false`) — managed PaaS replaces in-cluster containers.
- Dapr component **types change** (e.g., `pubsub.rabbitmq` instead of `pubsub.redis`, `state.azure.cosmosdb` instead of `state.redis`).
- **Metadata references secrets** via `secretKeyRef` rather than inline values.
- Secret store changes from `secretstores.kubernetes` to `secretstores.azure.keyvault` (or equivalent).

See `values/values-azure.yaml.sample` for a concrete Azure example.

## Escape Hatches

Helm templates support optional escape hatches for advanced customisation without chart changes:

- **Pod-level:** `{{ with .Values.<service>.podSpec }}{{ toYaml . | nindent N }}{{ end }}`
- **Container-level:** `{{ with .Values.<service>.containerSpec }}{{ toYaml . | nindent N }}{{ end }}`
- **Dapr components:** `{{ with .Values.dapr.<component>.extraMetadata }}{{ toYaml . | nindent N }}{{ end }}`

These are optional — omitting them from values files produces clean YAML with no empty blocks.

## Related Documents

- [ADR-0002](../docs/adr/adr-0002-cloud-agnostic-configuration-via-dapr.md) — Dapr abstraction layer
- [ADR-0004](../docs/adr/adr-0004-dapr-configuration-api-standardization.md) — Dapr Configuration API
- [ADR-0006](../docs/adr/adr-0006-infrastructure-configuration-via-environment-variables.md) — Env vars for infra config
- [ADR-0009](../docs/adr/adr-0009-helm-multi-environment-deployment.md) — Helm multi-environment deployment
- [ADR-0013](../docs/adr/adr-0013-secret-management-strategy.md) — Secret management strategy
