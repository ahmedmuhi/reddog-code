# Dapr Component Audit and Naming Convention Decisions

**Date:** 2026-03-01
**Status:** Approved
**Author:** Ahmed Muhi / Claude Code
**Covers:** TASK-001, TASK-002, TASK-002a, TASK-002b, TASK-003

---

## 1. Component Comparison Across All Locations (TASK-001)

Dapr components are currently defined in three locations. The table below compares each logical component across all three.

| Component | `.dapr/components/` (local dapr run) | `manifests/branch/base/components/` (raw K8s) | `charts/reddog/templates/dapr-components/` (Helm) |
|---|---|---|---|
| **reddog.pubsub** | `pubsub.redis`, scopes: `orderservice`, `makelineservice`, `loyaltyservice`, `receiptgenerationservice`, `accountingservice` | `pubsub.rabbitmq`, ns: `reddog-retail`, scopes: `order-service`, `make-line-service`, `loyalty-service`, `receipt-generation-service`, `accounting-service` | `pubsub.{{ .Values.dapr.pubsub.type }}`, ns: `{{ .Values.global.namespace }}`, scopes: templated from `.Values.services.*.dapr.appId` |
| **reddog.state.loyalty** | `state.redis`, scope: `loyaltyservice` | `state.redis`, ns: `reddog-retail`, scope: `loyalty-service`. Cloud overlays: cosmosdb (Azure), dynamodb (AWS), firestore (GCP) | `state.{{ .Values.dapr.stateStore.type }}`, ns: templated, scope: templated |
| **reddog.state.makeline** | `state.redis`, scope: `makelineservice` | `state.redis`, ns: `reddog-retail`, scope: `make-line-service`. Cloud overlays: cosmosdb (Azure), dynamodb (AWS), firestore (GCP) | `state.{{ .Values.dapr.stateStore.type }}`, ns: templated, scope: templated |
| **reddog.binding.receipt** | `bindings.localstorage`, scope: `receiptgenerationservice` | `bindings.azure.blobstorage`, ns: `reddog-retail`, scope: `receipt-generation-service`. Cloud overlays: s3 (AWS), gcs (GCP) | `bindings.localstorage`, ns: templated, scope: templated |
| **reddog.secretstore** | `secretstores.local.file`, no scopes | `secretstores.azure.keyvault`, ns: `reddog-retail`, no scopes | `secretstores.{{ .Values.dapr.secretStore.type }}`, ns: templated, no scopes |
| **Cron binding** | **MISSING** | name: `orders`, `bindings.cron`, ns: `reddog-retail`, scope: `virtual-worker` | name: `virtualworker.cron`, `bindings.cron`, ns: templated, scope: templated |
| **Configuration** | **MISSING** | name: `reddog.config`, kind: Configuration, ns: `reddog-retail` | name: `reddog.configuration`, kind: Configuration, ns: templated |

### Coverage gaps

- `.dapr/components/` is missing the cron binding and Configuration resource entirely.
- `.dapr/components/` does not scope to `virtualworker`, `virtualcustomers`, or `bootstrapper` (these services were not part of the `dapr run` workflow).
- `.dapr/components/` is being decommissioned — `kind + Helm` is the sole supported local workflow going forward.

### Bug: Cron binding name mismatch in Helm chart

The Helm template (`charts/reddog/templates/dapr-components/binding-virtualworker.yaml`) defines the cron binding as `name: virtualworker.cron`. However, Dapr cron bindings invoke `POST /<binding-name>` on the target app. The VirtualWorker controller at `RedDog.VirtualWorker/Controllers/VirtualWorkerController.cs:12` has:

```csharp
[HttpPost("orders")]
public async Task<IActionResult> ProcessOrders(CancellationToken cancellationToken)
```

Dapr would POST to `/virtualworker.cron`, which does not match the `/orders` endpoint. The manifests correctly use `name: orders`. **This is a bug in the Helm chart that must be fixed in Phase 1b (TASK-004).**

### Configuration resource name inconsistency

Manifests use `name: reddog.config`; Helm uses `name: reddog.configuration`. The `dapr.io/config` annotation in service templates references this name. **Canonical name: `reddog.configuration`** (more descriptive, already used by all Helm templates).

---

## 2. App ID Naming Inconsistencies (TASK-002)

| Service | `.dapr/` scopes | `manifests/` scopes | Helm `dapr.appId` | Helm `app:` label | Helm K8s Service name | C# default |
|---|---|---|---|---|---|---|
| OrderService | `orderservice` | `order-service` | `orderservice` | `order-service` | `orderservice` | `"orderservice"` (`VirtualCustomerOptions.cs:44`) |
| MakeLineService | `makelineservice` | `make-line-service` | `makelineservice` | `make-line-service` | `makelineservice` | `"makelineservice"` (`DaprOptions.cs:10`) |
| LoyaltyService | `loyaltyservice` | `loyalty-service` | `loyaltyservice` | `loyalty-service` | `loyaltyservice` | — |
| ReceiptGenService | `receiptgenerationservice` | `receipt-generation-service` | `receiptgenerationservice` | `receipt-generation-service` | `receiptgenerationservice` | — |
| AccountingService | `accountingservice` | `accounting-service` | `accountingservice` | `accounting-service` | `accountingservice` | — |
| VirtualWorker | — | `virtual-worker` | `virtualworker` | `virtual-worker` | `virtualworker` | — |
| VirtualCustomers | — | — | `virtualcustomers` | `virtual-customers` | — (no K8s Service) | — |
| Bootstrapper | — | — | `bootstrapper` | `reddog-bootstrapper` | — (Job, no Service) | — |
| UI | — | — | — (no Dapr) | `ui` | — | — |

### Key findings

1. **Two conventions coexist**: no-separator (`orderservice`) in Helm values and `.dapr/components/`; kebab-case (`order-service`) in manifests and Helm labels.
2. **Internal inconsistency within Helm templates**: The same template file uses `app: order-service` for labels but `dapr.appId: orderservice` for the sidecar annotation.
3. **C# code hardcodes no-separator**: `VirtualCustomerOptions.OrderServiceAppId = "orderservice"` and `DaprOptions.MakeLineServiceAppId = "makelineservice"`. These must be updated if kebab-case is adopted.

---

## 3. Namespace Convention Decision (TASK-002a)

**Decision: `reddog`** as the canonical namespace for all application services and Dapr components.

| Location | Current namespace | Canonical |
|---|---|---|
| `.dapr/components/` | (none) | N/A (decommissioned) |
| `manifests/branch/base/` | `reddog-retail` | N/A (to be archived) |
| Helm `values-local.yaml` | `default` | `reddog` |
| Helm `values-azure.yaml.sample` | `reddog` | `reddog` (already correct) |

### Rationale

- `default` pollutes the Kubernetes default namespace and conflicts with other workloads.
- `reddog-retail` is overly specific; the project already uses `reddog` as the canonical short name.
- `reddog` is already used in `values-azure.yaml.sample`, making this the natural choice.
- System components remain in their own namespaces: `dapr-system`, `ingress-nginx`, `cert-manager`.

### Impact

- `values/values-local.yaml`: Change `global.namespace` from `default` to `reddog`.
- Local deployments will need `kubectl create namespace reddog` (or chart creates it).

---

## 4. Component Scope Strategy Decision (TASK-002b)

**Decision: App-scoped** with explicit `scopes` lists for all Dapr components.

### Rationale

- Prevents accidental access — a new service added to the namespace does not automatically get access to state stores or bindings.
- The codebase already follows this pattern for all components.
- Dapr documentation recommends explicit scopes for production workloads.
- Self-documenting: the component YAML shows exactly which services use it.

### Exception

`reddog.secretstore` may remain without scopes (namespace-scoped) since all services may legitimately need secret access. This is documented as an intentional exception.

---

## 5. App ID Naming Convention Decision (TASK-003)

**Decision: Kebab-case** for all Dapr app IDs.

| Current (Helm values) | Canonical |
|---|---|
| `orderservice` | `order-service` |
| `makelineservice` | `make-line-service` |
| `loyaltyservice` | `loyalty-service` |
| `receiptgenerationservice` | `receipt-generation-service` |
| `accountingservice` | `accounting-service` |
| `virtualworker` | `virtual-worker` |
| `virtualcustomers` | `virtual-customers` |
| `bootstrapper` | `bootstrapper` (single word, no change) |

### Rationale

1. **Kubernetes convention**: K8s resource names use kebab-case. Helm templates already use kebab-case for Deployment names and `app:` labels.
2. **Readability**: `receipt-generation-service` is far more readable than `receiptgenerationservice`.
3. **Dapr community practice**: Dapr documentation and samples use kebab-case for app IDs.
4. **DNS compatibility**: Kebab-case is valid DNS (lowercase alphanumeric + hyphens), which is relevant since Dapr service invocation uses app IDs for resolution.
5. **Internal consistency**: Eliminates the current situation where the same Helm template has `app: order-service` (label) but `dapr.appId: orderservice` (annotation).

---

## 6. Impact Analysis

### 6.1 State Data Loss (RISK-002)

Dapr state store keys are prefixed with `<appId>||<key>`. Changing app IDs means existing keys become unreachable under the new prefix.

**Affected services:**
- MakeLineService: keys prefixed `makelineservice||` will not be found under `make-line-service||`
- LoyaltyService: keys prefixed `loyaltyservice||` will not be found under `loyalty-service||`

**Mitigation:**
- **Local/dev**: Flush Redis and start fresh (development data is disposable).
- **Cloud/shared**: Create migration script (TASK-006a) to `RENAME` all keys matching `<old-appid>||*` to `<new-appid>||*`. Coordinate with data owners before executing.

### 6.2 C# Code Changes

| File | Current default | New default |
|---|---|---|
| `RedDog.VirtualCustomers/Configuration/VirtualCustomerOptions.cs:44` | `"orderservice"` | `"order-service"` |
| `RedDog.VirtualWorker/Configuration/DaprOptions.cs:10` | `"makelineservice"` | `"make-line-service"` |

### 6.3 Helm Values Changes

In `values/values-local.yaml` and `values/values-local.yaml.sample`, update `dapr.appId` values:

| Values key (unchanged) | Current value | New value |
|---|---|---|
| `services.orderservice.dapr.appId` | `orderservice` | `order-service` |
| `services.makelineservice.dapr.appId` | `makelineservice` | `make-line-service` |
| `services.loyaltyservice.dapr.appId` | `loyaltyservice` | `loyalty-service` |
| `services.receiptgenerationservice.dapr.appId` | `receiptgenerationservice` | `receipt-generation-service` |
| `services.accountingservice.dapr.appId` | `accountingservice` | `accounting-service` |
| `services.virtualworker.dapr.appId` | `virtualworker` | `virtual-worker` |
| `services.virtualcustomers.dapr.appId` | `virtualcustomers` | `virtual-customers` |
| `bootstrapper.dapr.appId` | `bootstrapper` | `bootstrapper` (no change) |
| `global.namespace` | `default` | `reddog` |

**Important**: The YAML keys themselves (`services.orderservice`, etc.) do NOT change — only the `dapr.appId` values within them. This avoids cascading template changes.

### 6.4 Helm Template Fixes

- **Cron binding**: Change `name: virtualworker.cron` to `name: orders` in `charts/reddog/templates/dapr-components/binding-virtualworker.yaml` to match the VirtualWorker controller endpoint.
- **Configuration name**: Already `reddog.configuration` in Helm — no change needed. Manifests' `reddog.config` will be archived.

### 6.5 Component Names — No Changes

The logical Dapr component names (`reddog.pubsub`, `reddog.state.loyalty`, `reddog.state.makeline`, `reddog.binding.receipt`, `reddog.secretstore`) are already consistent across all locations and are referenced in C# code. These must NOT change. Only app ID scopes, namespace, and the cron binding name need updating.

### 6.6 Not Impacted

- Pub/sub topic names (unchanged).
- C# Dapr component name references (unchanged).
- Cloud overlay manifests in `manifests/overlays/` — these will be archived as part of TASK-011 and already use kebab-case app IDs.

---

## 7. Action Items for Phase 1b

These decisions feed directly into the following tasks:

| Task | Action |
|---|---|
| TASK-004 | Update Dapr component scopes in Helm templates to use canonical kebab-case app IDs. Fix cron binding name to `orders`. |
| TASK-005 | Update Dapr sidecar annotations in service templates (already templated — no change needed, values drive the IDs). |
| TASK-006 | Update `values-local.yaml` and sample file with canonical app ID values and `reddog` namespace. |
| TASK-006a | Create Redis key migration script for state store app ID prefix changes. |
| TASK-006b | Document state migration strategy: local = flush, cloud = coordinate + migrate. |
