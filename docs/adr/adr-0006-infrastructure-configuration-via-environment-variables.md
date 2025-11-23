---
title: "ADR-0006: Infrastructure Configuration via Environment Variables"
status: "Accepted"
date: "2025-11-02"
last_updated: "2025-11-22"
authors: "Red Dog Modernization Team"
tags: ["architecture", "decision", "configuration", "environment-variables", "cloud-agnostic", "polyglot"]
supersedes: ""
superseded_by: ""
---

# ADR-0006: Infrastructure Configuration via Environment Variables

## Status

**Accepted**

Most services already derive ports and Dapr sidecar ports from environment variables.  
Some business/application settings are still configured via environment variables and will be migrated to the Dapr Configuration API under ADR-0004. This ADR does not track implementation status beyond this note.

---

## Context

Red Dog is a polyglot microservices system (.NET, Go, Python, Node.js, Vue.js) that must run unchanged across multiple platforms:

- Local development
- Azure Kubernetes Service (AKS)
- Azure Container Apps
- AWS EKS
- GCP GKE

The system has two distinct configuration concerns:

1. **Infrastructure / runtime configuration**  
   How a container is run and wired into its environment: listening ports, bind addresses, Dapr sidecar ports, runtime mode, log level.

2. **Application configuration**  
   What the application does: business rules, feature flags, per-tenant settings, operational tunables.

Key constraints:

- The same container image should be deployable to dev, staging, and production without rebuilds.
- Orchestrators (Kubernetes, Container Apps, Docker Compose) must know container ports and bindings before the app starts.
- We want a **cloud-agnostic** pattern (ADR-0002): no Azure-only, AWS-only, or GCP-only configuration mechanisms in application code.
- ADR-0004 defines the Dapr Configuration API as the standard for application/business configuration.

Without a clear boundary, teams have been:

- Mixing infrastructure and application settings in environment variables.
- Hard-coding ports and addresses in application code.
- Considering platform-detection logic inside services (“if Azure then port 8080, else 80”), which violates cloud-agnostic goals.

---

## Decision

**Use environment variables as the standard interface between the platform and Red Dog services for infrastructure/runtime configuration.**

Application/business configuration is handled separately via the Dapr Configuration API (ADR-0004).

### In Scope: MUST Use Environment Variables

The following categories are considered **infrastructure/runtime configuration** and MUST be supplied to services via environment variables (or equivalent orchestrator mechanism that surfaces as env vars inside the container):

- **Listening ports and bind addresses**
  - HTTP/gRPC ports (e.g. `ASPNETCORE_URLS`, `PORT`, `HOST`)
- **Dapr sidecar connectivity**
  - `DAPR_HTTP_PORT`, `DAPR_GRPC_PORT`
- **Runtime mode**
  - `ASPNETCORE_ENVIRONMENT`, `NODE_ENV`, `GO_ENV`, `PYTHON_ENV`
- **Logging level and basic sink selection**
  - e.g. `LOG_LEVEL` where it only affects how the process runs, not business behavior

Application code MUST read these values from environment variables at startup rather than hard-coding them or deriving them from platform detection.

### Out of Scope: MUST NOT Use Environment Variables

The following categories MUST NOT be treated as infrastructure config and MUST NOT be modelled as environment-variable “knobs” by default:

- **Business / domain settings**  
  Examples: `storeId`, `maxOrderSize`, `orderTimeout`, loyalty enablement flags.  
  These belong in the Dapr Configuration API per ADR-0004.

- **Feature flags and runtime behaviour toggles**  
  Examples: `enableLoyalty`, `enableReceipts`, `enableExperimentalWorkflow`.  
  These should be controlled via configuration stores or feature-flag services, not env vars.

- **Secrets and credentials**  
  Examples: connection strings, API keys, usernames/passwords, tokens.  
  These MUST be stored in secret stores (e.g. Dapr secret components backed by Key Vault, Secrets Manager, Secret Manager) and consumed via Dapr or cloud-native secret mechanisms, not directly as environment variables, except where the platform itself injects them and no alternative exists.

Where an existing service currently uses environment variables for application settings or secrets, that is considered technical debt to be removed under ADR-0004 and any secrets ADR.

---

## Scope

This decision applies to:

- All Red Dog runtime services (OrderService, AccountingService, MakeLineService, LoyaltyService, ReceiptGenerationService, VirtualCustomers, VirtualWorker, UI backend/build containers).
- All supported platforms (local dev, AKS, EKS, GKE, Azure Container Apps).
- All supported languages (.NET, Go, Python, Node.js).

It does **not** prescribe a particular orchestration tool (Kubernetes, Bicep, Terraform, etc.), only that:

- The **contract between the platform and the service** for infrastructure/runtime settings is environment variables.

---

## Rationale

- **CFG-001 – Cloud-agnostic and platform-neutral**  
  Environment variables are universally available across Docker, Kubernetes, Container Apps, systemd, and process managers. They do not introduce Azure-, AWS-, or GCP-specific APIs into the codebase.

- **CFG-002 – Polyglot friendly**  
  All languages used in Red Dog have first-class support for environment variables. This avoids per-language configuration patterns and keeps the architecture consistent.

- **CFG-003 – Build-once, deploy-many**  
  Ports, bind addresses, Dapr ports, and runtime modes can vary per environment without rebuilding images. The same artifact can be promoted from dev → staging → production just by changing env vars in manifests.

- **CFG-004 – 12-Factor alignment**  
  Infrastructure/runtime settings align with the 12-Factor App principle of storing config in the environment, while leaving business configuration to dedicated configuration APIs (Dapr).

- **CFG-005 – Orchestrator integration**  
  Kubernetes, Azure Container Apps, and Docker Compose all inject environment variables natively. No custom config-loader or sidecar is required to get infra settings into the process.

- **CFG-006 – Clear separation of concerns**  
  “How the container runs” (infra) is separated from “what the app does” (business). This makes ownership clearer and reduces accidental coupling between deployment details and domain logic.

---

## Consequences

### Positive

- **POS-001 – No rebuilds for infra changes**  
  Port changes or Dapr port changes are handled by editing manifests, not rebuilding images.

- **POS-002 – Simpler, cloud-agnostic services**  
  Service code does not contain platform-detection branches; all deployment differences live in manifests and env configuration.

- **POS-003 – Consistent cross-language pattern**  
  Every service, regardless of language, follows “read infra config from env at startup”, which simplifies reasoning and teaching.

- **POS-004 – Cleaner CI/CD**  
  Pipelines can use the same image across environments and apply environment-specific configuration via Helm values, ARM/Bicep parameters, or Terraform variables.

- **POS-005 – Easier operational documentation**  
  Operators can reliably answer “how do I change the port / runtime mode?” by looking at env settings in manifests rather than searching code.

### Negative

- **NEG-001 – Environment variable sprawl**  
  Without discipline, services can accumulate many env vars. This increases the documentation burden and risk of misconfiguration.

- **NEG-002 – No live updates**  
  Changing environment variables typically requires a pod restart or new revision. Infra changes (e.g. ports) cannot be hot-reloaded.

- **NEG-003 – Type-safety and validation overhead**  
  Values arrive as strings and must be parsed/validated. Services must fail fast on invalid or missing infra settings to avoid subtle runtime errors.

- **NEG-004 – Misuse risk**  
  Developers may be tempted to put business config or secrets in env vars “because it’s easy.” This must be actively discouraged and treated as a violation of this ADR and ADR-0004/secret guidance.

---

## Alternatives Considered

### Alternative 1: Hard-coded infrastructure settings

**Description:**  
Listening ports, Dapr ports, and bind addresses are compiled into the application code (e.g., `app.listen(5100)`).

**Reasons for rejection:**

- Requires new builds for each environment and platform.
- Increases the risk of shipping binaries compiled with incorrect settings.
- Conflicts with immutable infrastructure and promotion of artefacts across environments.

---

### Alternative 2: Environment-specific configuration files baked into images

**Description:**  
Use `appsettings.json`, `config.yaml`, or similar files baked into the image, possibly with separate images per environment (`orderservice-dev`, `orderservice-prod`).

**Reasons for rejection:**

- Violates “build once, deploy many”; configuration changes require image rebuilds.
- Encourages environment-specific images and configuration drift.
- Harder to standardize across languages and platforms.

---

### Alternative 3: Platform-detection logic inside services

**Description:**  
Application code detects the platform (Azure, AWS, GCP, local) via metadata services and then chooses ports, addresses, etc.

**Reasons for rejection:**

- Introduces cloud-specific code paths and dependencies, contrary to ADR-0002.
- Harder to test and mock.
- Requires updates whenever new platforms or behaviours are added.

---

### Alternative 4: Dapr Configuration API for infrastructure settings

**Description:**  
Use the Dapr Configuration API to fetch ports and infra settings, just like business config.

**Reasons for rejection:**

- Circular dependency: the service must know the Dapr HTTP/gRPC ports in order to call Dapr.
- Orchestrators need port information before the app has started.
- Conflates infra/runtime configuration with domain/application configuration.

---

## Guidance for Future Work

- New services MUST:
  - Read listening ports, bind addresses, and Dapr ports from environment variables at startup.
  - Treat missing required infra env vars as startup failures.
- New business settings, feature flags, and tunables MUST:
  - Be modelled via the Dapr Configuration API (ADR-0004) or a dedicated feature-flag system, not as additional environment variables.
- Secrets MUST:
  - Be provisioned and consumed via secret stores and Dapr secret components, not added as environment variables, unless explicitly justified and documented in a secrets-focused ADR.

If a change proposal appears to require new environment variables, it SHOULD be reviewed against this ADR and ADR-0004 to determine whether it is truly infra/runtime configuration or should live in a configuration/secret store instead.

---

## Relationship to Other ADRs

- **ADR-0002 – Cloud-Agnostic Configuration via Dapr Abstraction**  
  Defines Dapr as the abstraction over cloud-specific services. ADR-0006 complements this by defining how infra/runtime parameters reach the services themselves.

- **ADR-0004 – Dapr Configuration API Standardization**  
  Governs application/business configuration. ADR-0006 explicitly delegates non-infra settings to ADR-0004.

---

## References

- [The Twelve-Factor App – Config](https://12factor.net/config)
- Kubernetes documentation on [environment variables for containers](https://kubernetes.io/docs/tasks/inject-data-application/define-environment-variable-container/)
- Azure Container Apps documentation on [environment variables](https://learn.microsoft.com/azure/container-apps/environment-variables)
