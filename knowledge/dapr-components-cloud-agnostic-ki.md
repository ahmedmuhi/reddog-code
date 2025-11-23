# KI-DAPR-COMPONENTS-001: Cloud-Agnostic Dapr Components Policy

## Summary

This Knowledge Item defines how Red Dog uses Dapr components to achieve **cloud-agnostic integration** with platform services.
It establishes stable logical component names, separates application code from cloud provider SDKs, and describes how environments map those logical components to concrete backends.

It applies to all services that use Dapr building blocks across **local, Azure, AWS, and GCP** deployments.

---

## Applies To

* All Red Dog services that use:

  * Dapr **secret stores**
  * Dapr **state stores**
  * Dapr **pub/sub**
  * Dapr **input/output bindings**
* All environments:

  * Local development (kind/minikube/Docker Desktop)
  * Azure (AKS, Azure Container Apps)
  * AWS (EKS)
  * GCP (GKE)
* Kubernetes manifests and overlays under `manifests/**`

---

## Key Facts

1. **FACT-001:** Red Dog uses **Dapr components as the default abstraction** over platform services for:

   * Secrets management
   * State storage
   * Pub/Sub messaging
   * Bindings (object storage, schedulers/cron, etc.)

2. **FACT-002:** **Logical Dapr component names are stable across environments.** Examples include:

   * `reddog.secretstore`
   * `reddog.state.makeline`
   * `reddog.state.loyalty`
   * `reddog.pubsub`
   * `reddog.binding.receipt`
   * `reddog.binding.virtualworker`

3. **FACT-003:** Each environment (local, Azure, AWS, GCP) provides its **own implementation** of these logical components via Dapr component YAML:

   * Local: `secretstores.local.file`, Redis-based state/pubsub, local file/object bindings (where applicable).
   * Azure: `secretstores.azure.keyvault`, Redis/Cosmos-backed state, Service Bus pubsub, Blob Storage bindings.
   * AWS: Secrets Manager, DynamoDB/ElastiCache, SNS/SQS, S3 (via appropriate Dapr components).
   * GCP: Secret Manager, Firestore/Memorystore, Pub/Sub, Cloud Storage (via appropriate Dapr components).

4. **FACT-004:** Application code **interacts only with Dapr** (`DaprClient`, Dapr HTTP/gRPC endpoints), not with cloud provider SDKs, for:

   * Secrets, state, pubsub, and bindings.
   * Service-to-service invocation (where Dapr is the standard internal HTTP path).

5. **FACT-005:** The **sidecar pattern** is standard: every service that uses Dapr building blocks runs with a Dapr sidecar, configured via `dapr.io/*` annotations in Kubernetes manifests.

6. **FACT-006:** Platform differences (Azure vs AWS vs GCP vs local) are encoded **in component YAML and environment-specific overlays**, not in application logic.

---

## Constraints

1. **CON-001:** Application code **must not** use cloud provider SDKs (Azure, AWS, GCP) directly for concerns covered by Dapr building blocks (secrets, state, pubsub, bindings) unless explicitly justified by an ADR.

2. **CON-002:** Application code **must not** implement its own Redis, Service Bus, S3, or similar clients for those concerns. All such access must go through Dapr components.

3. **CON-003:** Logical component names (e.g. `reddog.secretstore`, `reddog.pubsub`) **must be consistent** across environments. Environment overlays may change the `type` and `metadata`, but not the logical `name`.

4. **CON-004:** All supported environments (local, Azure, AWS, GCP) **must define** Dapr components for:

   * `reddog.secretstore`
   * `reddog.state.makeline` (if used)
   * `reddog.state.loyalty` (if used)
   * `reddog.pubsub`
   * Any required bindings (`reddog.binding.*`)

5. **CON-005:** Secrets used by Dapr components **must not** be hard-coded in manifests or application code. They must be provided via:

   * Kubernetes secrets,
   * Cloud-native secret managers,
   * Or other secure mechanisms referenced from Dapr component metadata.

6. **CON-006:** Application logic **must not branch** on cloud provider (e.g. `if (CLOUD_PROVIDER == "AZURE") ...`) for Dapr-covered capabilities. Provider differences belong in Dapr component configuration only.

---

## Patterns and Recommendations

1. **PAT-001 – Standard logical component naming**

   * Use a **single logical name** per concern, e.g.:

     * Secrets: `reddog.secretstore`
     * State: `reddog.state.makeline`, `reddog.state.loyalty`
     * Pub/Sub: `reddog.pubsub`
     * Bindings: `reddog.binding.receipt`, `reddog.binding.virtualworker`
   * Reference these names consistently in:

     * Application options classes (e.g. `DaprOptions`)
     * `DaprClient` calls
     * Health checks

2. **PAT-002 – Environment-specific component YAML**

   * Define base components under e.g. `manifests/branch/base/components/` for the default cloud.
   * Add overlays under e.g.:

     * `manifests/overlays/azure/**`
     * `manifests/overlays/aws/**`
     * `manifests/overlays/gcp/**`
     * `manifests/local/branch/**`
   * In overlays, keep `metadata.name` the same (e.g. `reddog.secretstore`) and change only:

     * `type` (e.g. `secretstores.azure.keyvault`, `secretstores.aws.secretsmanager`)
     * `metadata` fields (vault name, region, etc.)

3. **PAT-003 – Local development pattern**

   * For local dev, prefer:

     * `secretstores.local.file` (or equivalent) for secrets.
     * Redis containers for both state and pubsub (single moving part).
     * Local file-based or dev object storage bindings, where needed.
   * Ensure local manifests mirror the logical names used in cloud manifests.

4. **PAT-004 – Using DaprClient in services**

   * Inject `DaprClient` via DI in all .NET services that need Dapr building blocks.
   * Encapsulate Dapr interactions in small service classes (e.g. `LoyaltyStateService`) to:

     * Centralise component names
     * Centralise retry and error handling
   * Prefer options binding for component names (`DaprOptions`) rather than magic strings.

5. **PAT-005 – Adding new capabilities**

   * When introducing a new platform capability (e.g. a new output binding):

     1. Choose a logical component name (`reddog.binding.<something>`).
     2. Define a base component and per-cloud overlays with the same `name`.
     3. Use Dapr binding APIs from application code.
     4. Document the component in the deployment guides.

---

## Risks and Open Questions

1. **RISK-001 – Missing or inconsistent components**

   * If a new environment lacks a `reddog.secretstore` or uses a conflicting `type`, deployments may succeed but secrets/state will fail at runtime.
   * Mitigation: CI/linting for component presence and basic schema checks.

2. **RISK-002 – Advanced provider-specific features**

   * Some advanced features (e.g. HSM-backed keys, provider-specific key operations) may not be surfaced through Dapr.
   * Mitigation: If such features are required, capture an explicit ADR to justify bypassing Dapr for that specific case.

3. **RISK-003 – Debugging complexity**

   * Failures can occur at multiple layers (app → Dapr → platform service).
   * Mitigation: Standardise:

     * Dapr health checks,
     * Component-level diagnostics,
     * Logging patterns around Dapr calls.

4. **OPEN-001 – Component coverage across all clouds**

   * For some Dapr components, there may be no direct equivalent across all providers.
   * Where perfect symmetry is impossible, document the limitations and any provider-specific fallbacks in deployment docs and/or ADRs.

5. **OPEN-002 – Migration from non-Dapr code paths**

   * Legacy code or future experiments may temporarily bypass Dapr.
   * Such exceptions must be:

     * Documented in ADRs,
     * Clearly scoped and time-bounded,
     * Eventually converged back to the Dapr abstraction where feasible.

---

## Sources and Provenance

* **ADR-0002 – Cloud-Agnostic Configuration via Dapr Abstraction**
* Red Dog deployment manifests under `manifests/**`
* Red Dog Web API and platform integration standards
* Dapr documentation on building blocks and hosting models
