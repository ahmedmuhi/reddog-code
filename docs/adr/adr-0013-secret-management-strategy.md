---
title: "ADR-0013: Secret Management Strategy"
status: "Accepted"
date: "2025-11-14"
authors: "Red Dog Modernization Team"
tags: ["security", "configuration", "secrets", "kubernetes", "helm", "keda", "dapr", "cloud-agnostic"]
supersedes: ""
superseded_by: ""
---

# ADR-0013: Secret Management Strategy

## Status

**Accepted**

## Implementation Status

**Current State:** 🟡 In Progress

**Already in place:**

- Helm _infrastructure_ chart creates `sqlserver-secret` and other `Opaque` secrets from gitignored `values/values-<env>.yaml`.  
- Application workloads consume credentials via Kubernetes Secrets (environment variables or mounted files).  

**Not yet standardized:**

- KEDA `TriggerAuthentication` secret handling (RabbitMQ, SQL, etc.).  
- Non-SQL infra components (Redis with auth, SMTP, external APIs).  
- Cloud secret manager integration (Key Vault / Secrets Manager / Secret Manager) and Workload Identity patterns.  
- Clear rules for application code: _how_ secrets are accessed (Dapr vs env vars) and _what_ counts as “secret” vs “config”.

---

## Context

Red Dog’s modernization relies on:

- **Dapr** as the abstraction layer for app-facing configuration and secrets (ADR-0002, ADR-0004).  
- **Helm + Kubernetes** for infrastructure components (SQL Server, Redis, RabbitMQ, KEDA, cert-manager, ingress).  

Without a shared strategy, teams have started using a mix of approaches:

- Application services reading secrets from `Environment.GetEnvironmentVariable`, bypassing Dapr.  
- Infrastructure charts embedding sensitive values in Helm values or inline YAML.  
- No standard for how AKS/EKS/GKE/Container Apps clusters should pull secrets from cloud secret managers while keeping manifests cloud-agnostic.

This ADR defines a consistent, cloud-agnostic secret management strategy across:

- **Environments:** local/kind, AKS, EKS, GKE, Azure Container Apps.  
- **Workloads:** application services, data plane components, KEDA, controllers, and Dapr components.

---

## Scope

### In Scope

- All workloads in the Red Dog platform that need credentials:
  - Application services (.NET, Go, Python, Node) running under Dapr.
  - Datastores (SQL Server, Redis), RabbitMQ, SMTP, etc.
  - Platform components that require credentials (KEDA, cert-manager issuers, external scalers).
  - Dapr components that access secret backends.
- All target environments:
  - Local clusters (kind / dev AKS).
  - AKS, EKS, and GKE.
  - Azure Container Apps (where applicable).

### Out of Scope

- Human identity and access management (SSH keys, developer GitHub tokens, etc.).  
- Secret generation policy (who creates passwords, strength rules) beyond basic recommendations.  
- Organization-wide security tooling (SIEM/SOC, DLP, etc.).

---

## Problem

The current ad-hoc approaches create several risks:

- **Inconsistent access patterns:** some services use Dapr, others read env vars directly; infra components embed literals.  
- **Cloud-specific drift:** AKS vs EKS vs GKE vs Container Apps may end up with divergent manifests and secret flows.  
- **Security debt:** potential for secrets in Git history, re-use of production credentials in non-prod, unclear rotation process.  

We need:

- A clear **layered model** (source → transport → consumption).  
- Explicit rules for **application vs infrastructure** consumers.  
- A strategy that remains **cloud-agnostic** and aligns with ADR-0002/0004/0006.

---

## Decision

We adopt a three-layer model for secrets:

1. **Source layer:** where secret material originates (Key Vault, Secrets Manager, Secret Manager, local values files).  
2. **Transport layer:** how secrets are delivered into the cluster / runtime (Kubernetes / Container Apps Secret objects).  
3. **Consumption layer:** how workloads read secrets (Dapr secret store for application code, native secrets for infra components).

### 1. Consumption Layer (How Workloads Read Secrets)

#### 1.1 Application services

- Application code **MUST** obtain secrets via the **Dapr Secret API**, not by reading Kubernetes Secrets directly.
- Application services **MUST NOT**:
  - Read secret values directly from `Environment.GetEnvironmentVariable` (except for standard non-secret infra env like `ASPNETCORE_ENVIRONMENT` per ADR-0006).
  - Mount Kubernetes Secrets volumes and parse files manually for credentials.
- Dapr secret components are the **only** supported app-facing secret interface:
  - `.NET`: `DaprClient.GetSecretAsync(...)`.
  - Go/Python/Node: equivalent Dapr SDK calls.

_Exceptions_: platform-provided identity tokens (e.g., managed identity, Workload Identity tokens), which are not “secrets” in this ADR’s sense and are handled by the platform.

#### 1.2 Infrastructure and platform components

- Non-application workloads **MUST** consume secrets via native secret mechanisms:
  - **Kubernetes:** `Secret` objects referenced with `valueFrom.secretKeyRef` or mounted volumes (e.g., KEDA `TriggerAuthentication`, SQL Server, RabbitMQ, cert-manager).  
  - **Azure Container Apps:** Container Apps Secret resources referenced via `env` / `secretRef`.
- These components **do not** use Dapr secret APIs directly.

---

### 2. Transport Layer (Cluster / Environment Secret Objects)

#### 2.1 Kubernetes clusters (AKS, EKS, GKE, local)

- **Kubernetes Secret** is the canonical transport object:
  - All credentials exposed to workloads **MUST** be stored in `Secret` objects.
  - Workloads **MUST** reference secrets via `secretKeyRef` or volumes; **no** inline literals in Deployments, StatefulSets, KEDA objects, or Dapr component YAML.
- Kubernetes Secrets **MUST NOT**:
  - Be committed with real values (`data`/`stringData`) into Git.
  - Be replaced by ConfigMaps for anything that would be damaging if exposed.

#### 2.2 Azure Container Apps

- Container Apps **Secret** resources are treated as the equivalent transport layer:
  - Secrets are referenced from app containers only by name.
  - The Bicep/YAML definitions for Container Apps **MUST NOT** contain raw secret values; they reference either:
    - Pre-created Container Apps secrets, or
    - Key Vault references where supported.

---

### 3. Source Layer (Where Secret Material Comes From)

We distinguish between **local/dev** and **cloud** sources.

#### 3.1 Local / kind / dev clusters

- Secret material is provided via **gitignored** Helm values files (e.g., `values/values-local.yaml`) and rendered into K8s Secrets as `stringData`.
- Local credentials:
  - **MUST NOT** be reused in shared environments (dev/stage/prod).
  - **SHOULD** be obviously non-production (e.g., `LocalOnly!123`).

#### 3.2 Cloud clusters (AKS, EKS, GKE)

- For persistent environments, the **source of truth** for secrets is a **managed secret store**, not Helm values:
  - Azure Key Vault.  
  - AWS Secrets Manager or SSM Parameter Store.  
  - GCP Secret Manager.
- The **primary pattern** is:

  > Managed secret store → External Secrets Operator (ESO) or Secret Store CSI driver → Kubernetes Secret → Workloads / Dapr secret store.

- Access to secret managers **MUST** use identity-based authentication where available:
  - AKS: Azure AD Workload Identity / managed identity.  
  - EKS: IAM Roles for Service Accounts (IRSA).  
  - GKE: Workload Identity.  
- Long-lived access keys for secret managers **MUST NOT** be baked into images or Helm values; if temporarily used, they must themselves be stored in Kubernetes Secrets and rotated promptly.

#### 3.3 Azure Container Apps

- The recommended pattern is:

  > Key Vault (source) → Container Apps secret integration → Container Apps Secret → env var reference in app.

- Where platform integration is not available, secrets may be provisioned out-of-band (e.g., deployment pipeline) but **must not** be embedded directly in declarative templates tracked in Git.

---

### 4. Classification: What Counts as a Secret?

A value **MUST** be treated as a secret if any of the following apply:

- It grants access to an external system (DB passwords, connection strings, API keys, OAuth client secrets).  
- It authenticates a user or service (JWT signing keys, TLS private keys).  
- It would cause financial or reputational damage if leaked (mail relay credentials, payment provider tokens).  

Non-secret configuration (timeouts, feature flags, max order sizes, UI labels, etc.) is covered by:

- ADR-0004 (Dapr Configuration API for application config).  
- ADR-0006 (infrastructure configuration via environment variables).

Secrets **MUST NOT** be used as general config just because it is convenient (e.g., putting non-sensitive config into secret stores “by default”).

---

### 5. Prohibited Practices

The following are explicitly forbidden:

- Literal secrets (passwords, tokens, connection strings) in:
  - Git-tracked Helm values or Kubernetes manifests.  
  - ConfigMaps or any non-Secret K8s resources.  
  - Application source code or `.sample` files with “real” values.
- Application services bypassing Dapr to read secrets from:
  - `Environment.GetEnvironmentVariable` / `os.environ` / `process.env`.  
  - Mounted secret files under `/etc/secrets` or similar.
- Reusing the same secret value across environments (e.g., dev, stage, prod all sharing one DB password).
- Treating Kubernetes Secrets as “encryption” instead of a transport: base64 is encoding, not cryptography; at-rest and RBAC protections belong in the cluster/etcd configuration.

---

## Consequences

### Positive

- **POS-001 – Consistent access pattern for apps:** All application services use Dapr secret store, aligning with ADR-0002/0004 and keeping app code cloud-agnostic.  
- **POS-002 – Cloud-agnostic manifests:** The same manifests/Helm charts work across AKS/EKS/GKE; differences are isolated to secret-source plumbing (ESO/CSI/Key Vault integration).  
- **POS-003 – Reduced leakage risk:** No secrets in Git; clear rules for where secrets may and may not appear.  
- **POS-004 – Easier rotation:** Secrets are rotated at the source (Key Vault / Secrets Manager), with ESO/CSI refreshing corresponding K8s Secrets and Dapr + workloads consuming the updated values.  
- **POS-005 – KEDA and infra alignment:** KEDA, databases, and infra components all adopt a consistent Kubernetes Secret–centric pattern, simplifying scaling configs and infra ops.  

### Negative

- **NEG-001 – Additional setup work:** ESO/CSI, secret managers, and Workload Identity require initial setup per environment.  
- **NEG-002 – Developer overhead locally:** Developers must maintain gitignored values files or other local-secret mechanisms instead of “just hardcoding” test credentials.  
- **NEG-003 – New failure modes:** Misconfigured ESO/CSI, expired identities, or mis-scoped access policies can break secret syncing and thus application startup.  
- **NEG-004 – Indirection:** Debugging “where does this secret really come from?” now requires understanding the chain from app → Dapr → K8s Secret → ESO/CSI → cloud secret manager.

---

## Implementation Notes

### Naming conventions

- Kubernetes Secret names: `component-purpose-secret`, e.g.:
  - `sqlserver-secret`, `rabbitmq-secret`, `rabbitmq-keda-secret`, `smtp-secret`.  
- Keys inside Secrets:
  - Prefer uppercase snake case where used as environment variables (e.g., `SA_PASSWORD`, `RABBITMQ_URI`).  
  - When tools require specific key names, follow the tool’s conventions (document the exception).

### Helm values

- All charts must expose secret-related configuration via values such as:
  - `database.secretName`, `database.usernameKey`, `database.passwordKey`.  
  - `rabbitmq.authSecretName`, `rabbitmq.uriKey`.  
- Sample values files in Git:
  - Contain **placeholders only** (e.g., `CHANGEME`), never real values.  
  - Clearly document that real values belong in gitignored `values-<env>.yaml` or cloud secret managers.

### Rotation

- Rotation flow (Kubernetes clusters):
  1. Rotate the value in the **source** (Key Vault / Secrets Manager / Secret Manager or local values file).  
  2. Allow ESO/CSI to sync new values into K8s Secrets (or re-run Helm for local-only).  
  3. Restart or roll pods if the workload does not auto-reload credentials.  
- Rotation cadence:
  - DB passwords and external API keys **SHOULD** be rotated at least annually, and when staff or environment boundaries change.  
  - TLS/private keys follow certificate issuance policy (cert-manager, ACME, etc.).

### KEDA

- KEDA `TriggerAuthentication` objects:
  - Live in the same namespace as the corresponding workloads.  
  - Reference the same Kubernetes Secrets that app pods use, where sensible (e.g., `RABBITMQ_URI`).  
  - Must not embed connection strings inline.

### Dapr secret components

- For local/dev:
  - Dapr secret components may read from Kubernetes Secrets or local secret stores, but app code still only calls Dapr.  
- For cloud:
  - Prefer Dapr secret components that talk **directly** to managed secret stores (Key Vault / Secrets Manager / Secret Manager) where available.  
  - Where that is not possible, Dapr secret components may use K8s Secrets as their backing store (which are themselves hydrated from cloud secret managers via ESO/CSI).

---

## References

- **ADR-0002:** Cloud-Agnostic Configuration via Dapr (abstraction principle).  
- **ADR-0004:** Dapr Configuration API for Application Configuration Management.  
- **ADR-0006:** Infrastructure Configuration via Environment Variables.  
- **ADR-0007 / ADR-0009:** Cloud-agnostic deployment and Helm multi-environment strategy.  
- **plan/upgrade-phase0-platform-foundation-implementation-1.md**  
- **plan/upgrade-keda-2.18-implementation-1.md**  
- **AGENTS.md / CLAUDE.md:** Development prerequisites and agent behaviour (MUST reference this ADR when generating manifests involving secrets).
