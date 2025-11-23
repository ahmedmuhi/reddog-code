---
id: KI-DAPR_CONFIGURATION_API-001
title: Dapr Configuration API Usage for Application Settings
tags:
  - red-dog
  - dapr
  - configuration
  - cloud-agnostic
  - patterns
last_updated: 2025-11-22
source_sessions:
  - .claude/sessions/2025-11-02-1105-orderservice-dotnet10-refinement.md
source_plans:
  - plan/orderservice-dotnet10-upgrade.md
  - plan/modernization-strategy.md
confidence: high
status: Active
owner: Red Dog Modernization Team
notes: Canonical usage patterns; ADR-0004 and ADR-0006 define the higher-level decisions.
---

# Summary

This Knowledge Item captures stable, reusable knowledge about how Red Dog services use the Dapr Configuration API for application settings. It defines what belongs in Dapr Configuration vs environment variables, how we structure configuration keys, and the core access patterns (startup load and optional runtime refresh). ADR-0004 and ADR-0006 define the architectural decisions; this KI focuses on durable facts, constraints, and patterns.

## Key Facts

- **FACT-001**: Red Dog uses Dapr as the abstraction layer for platform integrations (state, secrets, pub/sub, configuration), per ADR-0002.
- **FACT-002**: Application configuration that may vary per environment or over time (business rules, feature flags, tunable parameters) is accessed via the Dapr Configuration API, not via environment variables.
- **FACT-003**: Infrastructure/runtime configuration (ports, Dapr sidecar ports, runtime environment names) is provided via environment variables, per ADR-0006.
- **FACT-004**: Dapr configuration components map to platform-specific backends (e.g. Azure App Configuration, Redis, PostgreSQL) without changing application code.
- **FACT-005**: Configuration keys are treated as read-only inputs to a service; services do not write configuration values back via Dapr.
- **FACT-006**: Services MAY subscribe to configuration changes via Dapr (where supported) to react to runtime updates (e.g. enabling/disabling feature flags).

## Constraints

- **CON-001**: Application services MUST NOT call platform-specific configuration SDKs (Azure App Configuration SDK, AWS AppConfig SDK, etc.) for application settings. All such access MUST go through Dapr Configuration components.
- **CON-002**: Application services MUST NOT introduce platform detection logic (e.g. “if AKS then read X, if Container Apps read Y”) for application configuration.
- **CON-003**: Environment variables MUST NOT be used for application configuration that is expected to change per environment or over time, except as a temporary compatibility bridge during migrations.
- **CON-004**: Each service MUST clearly define which configuration keys are mandatory and whether the behaviour on missing/invalid values is fail-fast startup or a documented, safe default.
- **CON-005**: Dapr configuration component names used by Red Dog MUST be stable and documented (e.g. `reddog.config` as the primary application configuration component for most services).
- **CON-006**: Configuration key names MUST be stable and non-ambiguous across environments; renames require careful migration (dual-key period or transform logic).

## Patterns & Recommendations

- **PAT-001**: **Configuration Responsibility Split**  
  - Use environment variables for “how the container runs” (ports, bind addresses, Dapr sidecar ports, runtime environment).
  - Use the Dapr Configuration API for “what the application does” (business logic, feature flags, tunable behaviour).
  - Keep this split consistent across all languages.

- **PAT-002**: **Startup Configuration Load Pattern**  
  - At service startup:
    - Retrieve a minimal set of required keys from Dapr Config (e.g. `storeId`, `maxOrderSize`, `requestTimeout`, key feature flags).
    - Validate and normalise values (parse integers/booleans, check ranges).
    - Fail fast if mandatory keys are missing or invalid, rather than running with unknown configuration.
  - Surface configuration into the service via:
    - Strongly-typed options / configuration objects, or
    - A small, well-defined “configuration provider” abstraction, not scattered `GetConfiguration` calls.

- **PAT-003**: **Runtime Refresh Pattern (Where Needed)**  
  - For a small subset of settings that benefit from runtime changes (typically feature flags and operational thresholds):
    - Use Dapr’s configuration subscription capability.
    - Handle updates in a dedicated background task or equivalent mechanism.
    - Update in-memory state (e.g. feature flag booleans, max size limits) in a thread-safe way.
  - Keep the list of dynamically refreshed keys small and well-documented; most settings can remain “load at startup”.

- **PAT-004**: **Key Naming Conventions**  
  - Use consistent, lower-camelCase keys, optionally prefixed by service:
    - Global/shared keys: `storeId`, `requestTimeout`, `maxOrderSize`.
    - Service-specific keys: `orderService.maxOrderSize`, `loyaltyService.pointsMultiplier`.
    - Feature flags: `feature.enableLoyalty`, `feature.enableReceipts`, etc.
  - Avoid environment-specific names in keys (no `prodStoreId` vs `devStoreId`); environment differences live in store contents, not key names.

- **PAT-005**: **Backend Selection Per Environment**  
  - Choose Dapr configuration components with a bias toward managed services:
    - Local: Redis-backed configuration component (lightweight, easy to run in Docker Compose).
    - Azure: Azure App Configuration-backed component for configuration, leveraging its UI and feature flags.
    - Other clouds / on-prem: a managed or self-hosted configuration backend (e.g. PostgreSQL) exposed via Dapr configuration component.
  - Document the mapping in environment-specific ops docs, not in application code.

- **PAT-006**: **Error Handling and Resilience**  
  - Treat inability to retrieve mandatory configuration at startup as a hard failure: log clearly and exit, so orchestrators can restart the pod.
  - For runtime subscriptions:
    - Implement reasonable backoff/retry for transient failures.
    - Fail safe: if a configuration update fails, the service should continue with last known good values rather than entering an undefined state.

- **PAT-007**: **Testing Practices**  
  - In unit tests, mock the configuration provider abstraction rather than Dapr directly.
  - In integration tests, stand up a lightweight Dapr + local config backend (e.g. Redis) and exercise startup and update flows.
  - Include tests for:
    - Missing/invalid mandatory keys.
    - Runtime change of a feature flag.
    - Backend unavailability (startup vs mid-flight).

## Risks & Open Questions

### Risks

- **RISK-001**: **Increased Dependency on Dapr and Backend Availability**  
  If Dapr sidecars or the configuration backend are unavailable, services may fail to start or not receive updates. This centralises risk in Dapr and the chosen backend.

- **RISK-002**: **Operational Complexity**  
  Each environment requires provisioning and managing a configuration backend and Dapr component configuration. Misconfigurations can cause widespread startup failures.

- **RISK-003**: **Overuse of Runtime Updates**  
  If too many settings are made dynamically updatable, reasoning about system behaviour becomes harder, and subtle production differences may occur between pods or regions.

- **RISK-004**: **Key Proliferation and Drift**  
  Without discipline, the number of configuration keys can grow rapidly, and unused/legacy keys may accumulate in the store.

### Open Questions

- **OPEN-001**: To what extent should we allow fallback from Dapr configuration to environment variables for specific keys (e.g. during migrations), and for how long should such fallbacks be supported?
- **OPEN-002**: What standard SLAs do we require from configuration backends (e.g. Azure App Configuration, Redis, PostgreSQL) to consider them acceptable for production use in Red Dog?
- **OPEN-003**: Should we define a small, shared library/package for each language (e.g. `RedDog.Configuration`) that wraps Dapr Config, to enforce consistent patterns across services?

## Source & Provenance

- Derived from:
  - `.claude/sessions/2025-11-02-1105-orderservice-dotnet10-refinement.md` (Dapr configuration research and discussion).
- Related implementation plans:
  - `plan/orderservice-dotnet10-upgrade.md` (introduces Dapr Config usage in Orderservice).
  - `plan/modernization-strategy.md` (polyglot migration and Dapr adoption).
- Related ADRs:
  - `docs/adr/adr-0002-cloud-agnostic-configuration-via-dapr.md`
  - `docs/adr/adr-0004-dapr-configuration-api-for-application-configuration-management.md`
  - `docs/adr/adr-0006-infrastructure-configuration-via-environment-variables.md`
