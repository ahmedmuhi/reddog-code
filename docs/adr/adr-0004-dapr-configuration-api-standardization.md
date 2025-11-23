---
title: "ADR-0004: Application Configuration via Dapr Configuration API"
status: "Accepted"
date: "2025-11-02"
authors: "Red Dog Modernization Team"
tags: ["architecture", "decision", "dapr", "configuration", "cloud-agnostic"]
supersedes: ""
superseded_by: ""
---

# ADR-0004: Application Configuration via Dapr Configuration API

## Status

Accepted.

This ADR records a long-lived architectural decision. Detailed implementation patterns and per-cloud mappings live in:

- `knowledge/dapr-configuration-api-ki.md`
- ADR-0006: “Infrastructure Configuration via Environment Variables”

## Context

Red Dog is a polyglot microservices system deployed across multiple platforms (AKS, Azure Container Apps, EKS, GKE, and local Kubernetes). We need a consistent, cloud-agnostic way for services to obtain **application configuration**, such as:

- Business settings (e.g. `storeId`, `maxOrderSize`)
- Feature flags (e.g. `enableLoyalty`)
- Operational parameters (e.g. request timeouts, retry counts)

We already decided in:

- ADR-0002 that Dapr is our abstraction layer for platform integrations.
- ADR-0006 that environment variables are the standard for **infrastructure/runtime** configuration (ports, Dapr sidecar ports, runtime mode, etc.).

Today, services tend to read both infrastructure and application settings from environment variables. This:

- Couples config management workflows to the underlying platform (ConfigMaps, Container Apps settings, Parameter Store, etc.).
- Forces restarts/redeployments for many configuration changes.
- Fragments configuration handling across clouds and tools.

We want:

- Cloud-agnostic application code and configuration access.
- The option to update some configuration at runtime without redeploying.
- A single conceptual pattern across .NET, Go, Python, and Node.js services.

## Decision

We standardise on the **Dapr Configuration API** as the mechanism for **application configuration** in Red Dog services.

1. **Application configuration MUST be retrieved via the Dapr Configuration API** (directly or via thin wrappers), not via cloud-specific SDKs.
2. **Infrastructure/runtime configuration MUST continue to use environment variables** (per ADR-0006).
3. Application code MUST NOT depend directly on platform-specific configuration stores (e.g. Azure App Configuration SDK, AWS AppConfig SDK, GCP Config SDK). Those concerns are isolated in Dapr components.

In practice:

- Services call `Dapr Configuration API` to obtain application settings.
- Platform-specific configuration backends (Azure App Configuration, Redis, PostgreSQL, etc.) are plugged in via Dapr components and deployment configuration, not via application code.
- For configuration that will never vary per environment or over time, compile-time constants remain acceptable.

## Scope

In scope for this ADR:

- **Application configuration** that may vary per environment or over time, for example:
  - Business rules and thresholds (e.g. `maxOrderSize`)
  - Feature flags (e.g. `enableLoyalty`)
  - Operational tuning (e.g. timeouts, retry limits) where we expect to tune without code changes.
- All Red Dog application services (.NET, Go, Python, Node.js).

Out of scope for this ADR:

- **Infrastructure/runtime configuration** (ports, Dapr sidecar ports, runtime environment names): covered by ADR-0006.
- Secrets management (already covered by Dapr secret store patterns / other ADRs).
- Low-level details of which configuration backend each environment uses and how it is provisioned (captured in KIs and implementation plans).

## Rationale

Given ADR-0002 (Dapr as abstraction) and ADR-0006 (env vars for infrastructure), this decision:

- Aligns application configuration with our existing Dapr-first strategy:
  - Same pattern as for state, secrets, pub/sub, etc.
- Keeps application code cloud-agnostic:
  - Dapr components map to Azure App Configuration, Redis, PostgreSQL, etc., without code changes.
- Enables runtime configuration updates where needed:
  - Services can subscribe to configuration change events (where supported) instead of being restarted for every change.
- Simplifies teaching and operations:
  - “Infrastructure via env vars, application config via Dapr Config” is a clear, memorable rule.

Alternative approaches (direct cloud SDKs, ConfigMaps-only, image-baked config) either:

- Introduce platform-specific code paths, or
- Prevent runtime changes without redeploy, or
- Require multiple different patterns across environments.

## Consequences

### Positive

- **Cloud-agnostic application code**: the same configuration access pattern works across AKS, Container Apps, EKS, GKE, and local clusters.
- **Cleaner separation of concerns**:
  - Environment variables: “how the container runs”.
  - Dapr Configuration: “what the application does”.
- **Runtime configuration changes are possible**:
  - For selected settings and services, we can update config in the backing store and have services react without redeployment.
- **Operational consistency**:
  - Platform teams manage config in their preferred backing store (Azure App Configuration, Redis, PostgreSQL, etc.) behind a Dapr component, while application code remains unchanged.

### Negative

- **Additional dependency on Dapr for configuration**:
  - If Dapr or the configuration backend is unavailable, services may not be able to start or to obtain updated configuration.
- **More moving parts**:
  - Requires provisioning and managing a configuration backend per environment, plus Dapr components.
- **Implementation complexity**:
  - Subscription-based, runtime refresh scenarios require extra code and testing.
- **Latency and failure modes**:
  - Initial configuration retrieval and subsequent refreshes depend on network calls and retry policies rather than simple env var access.

## Constraints

- Application services MUST NOT call cloud-provider-specific configuration SDKs for application configuration.
- Application services MUST NOT introduce platform detection logic (e.g. “if Azure… else if AWS…”) for configuration.
- Mandatory configuration keys MUST have clearly defined behaviour if missing or invalid (fail-fast or documented fallback). The canonical patterns live in `knowledge/dapr-configuration-api-ki.md`.

## Related Work

- ADR-0002: Dapr abstraction for cloud-agnostic integration.
- ADR-0006: Environment variables for infrastructure/runtime configuration.
- `knowledge/dapr-configuration-api-ki.md`: stable facts, patterns, and constraints for using Dapr Configuration in Red Dog.
- Implementation plans (e.g. `plan/orderservice-dotnet10-upgrade.md`, `plan/modernization-strategy.md`) define service-by-service rollout and migration.

## Alternatives (Brief)

- **Continue using environment variables for application config**  
  Rejected because it ties configuration workflows to specific platforms and requires restarts/redeployments for most changes.

- **Use cloud-specific config SDKs in application code**  
  Rejected because it violates our cloud-agnostic design and increases testing/maintenance complexity.

- **Depend on Kubernetes ConfigMaps/files only**  
  Rejected as Kubernetes-specific and unsuitable for Azure Container Apps and other non-Kubernetes environments, and still restart-oriented.

- **Introduce a separate config server (e.g. Spring Cloud Config, Consul)**  
  Rejected as redundant with Dapr, which we already use as our integration abstraction.

## References

- Dapr docs: Configuration building block and supported configuration store components.
- ADR-0002: Cloud-agnostic integration via Dapr.
- ADR-0006: Environment variables for infrastructure configuration.
- Session logs around Orderservice .NET 10 upgrade and Dapr research.
