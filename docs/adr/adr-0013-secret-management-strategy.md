---
title: "ADR-0013: Secret Management Strategy"
status: "Accepted"
date: "2025-11-14"
authors: "Red Dog Modernization Team"
tags: ["security", "configuration", "secrets", "kubernetes", "helm", "keda", "dapr"]
supersedes: ""
superseded_by: ""
---

# ADR-0013: Secret Management Strategy

## Status

**Accepted**

## Implementation Status

**Current State:** 🟡 In Progress

**What's Working Today:**
- Helm infrastructure chart creates `sqlserver-secret` and other opaque secrets from gitignored `values/values-<env>.yaml` inputs (e.g., SQL SA password). `charts/infrastructure/templates/sqlserver-secret.yaml`
- Application workloads already consume credentials through Kubernetes Secrets (env vars or mounted files).

**Gaps / Not Yet Implemented:**
- No standardized guidance for KEDA TriggerAuthentication secrets or non-SQL infrastructure components (RabbitMQ, Redis with auth enabled, SMTP, etc.).
- No documented separation of responsibilities between Kubernetes Secrets (transport) and Dapr secret store components (application access).
- No policy for integrating cloud secret managers (Azure Key Vault, AWS Secrets Manager, GCP Secret Manager) with Kubernetes.

**Next Steps:**
1. Update `CLAUDE.md` / `AGENTS.md` quick-start sections to reference this ADR and describe local secret creation workflow.
2. Add Helm templates (or External Secrets Operator manifests) for RabbitMQ/KEDA credentials in `charts/infrastructure/`.
3. Implement environment-specific secret sourcing (local: Helm stringData; cloud: CSI driver/ESO referencing managed secret stores).
4. Add validation checklist to Phase 1B foundation plans ensuring no plain-text secrets ship in charts or repos.

## Context

The Red Dog modernization effort already relies on:

- **Dapr Secret Store** for application code (ADR-0002).
- **Helm charts + Kubernetes Secrets** for infrastructure components (SQL Server, Redis, RabbitMQ, cert-manager, etc.).

However, several problems surfaced during Phase 1B planning:

1. **Inconsistent Practices:** Some services expect secrets via Dapr, others via env vars, and KEDA scalers have no documented approach.
2. **Environment Drift:** Developers often copy `.env/local` values into Helm charts or forget to rotate them between dev/stage/prod.
3. **Missing Cloud Guidance:** There is no documentation on how AKS/EKS/GKE clusters should source secrets from managed services while keeping the Deployment manifests identical.
4. **Security Debt:** Without a central policy, teams risk committing secrets, reusing prod credentials locally, or skipping rotation entirely.

## Decision

Adopt a **two-layer secret management strategy**:

1. **Transport Layer (Kubernetes Secrets)**
   - All workloads (Deployments, StatefulSets, Jobs, TriggerAuthentications, Dapr components) must read credentials exclusively from Kubernetes Secrets (`Secret` objects).
   - Secret names/keys are defined in Helm values (e.g., `database.passwordSecret`) and referenced via `secretKeyRef` or mounted volumes.
   - No workload may embed literal passwords, tokens, or connection strings in manifests, ConfigMaps, or code.

2. **Source Layer (Environment-Specific Providers)**
   - **Local / kind:** Helm templates populate `stringData` from gitignored `values/values-local.yaml` (developer-provided throwaway credentials).
   - **Cloud clusters (AKS/EKS/GKE/Container Apps):** Secrets are hydrated from managed secret stores (Azure Key Vault, AWS Secrets Manager, GCP Secret Manager) via:
     - External Secrets Operator (ESO) or CSI Secret Store driver, **or**
     - Dapr secret store component writing into Kubernetes Secrets during bootstrap (Phase 8 Workload Identity).
   - Each environment must own its credential material; reuse across environments is prohibited.

**Enforcement Rules:**
- Any new Helm chart, KEDA ScaledObject, or Dapr component must accept secret names via values and reference Kubernetes Secrets.
- Dapr secret store components continue to abstract application-level access, but they themselves source data from Kubernetes Secrets (local) or cloud secret managers (remote).
- Documentation (AGENTS.md / CLAUDE.md / modernization plans) must point to this ADR when describing how to add or rotate credentials.

## Consequences

### Positive

- **POS-001:** Clear separation of responsibilities—Kubernetes Secrets deliver credentials to workloads; Dapr secret store abstracts app consumption.
- **POS-002:** Environment parity—Helm values files reference the same secret names across dev/stage/prod, while actual material differs per environment.
- **POS-003:** Reduced secret sprawl—no more literals in manifests, ConfigMaps, or Git history.
- **POS-004:** Simplifies future automation—External Secrets Operator or CSI providers can hydrate secrets without changing application manifests.
- **POS-005:** Enables KEDA adoption—TriggerAuthentication objects can uniformly reference Kubernetes Secrets, avoiding inline credentials.

### Negative

- **NEG-001:** Additional upfront work to provision secrets per environment (Helm templating locally, ESO/CSI in cloud).
- **NEG-002:** Requires developer discipline to maintain gitignored values files and avoid committing real credentials.
- **NEG-003:** External secret managers introduce new failure modes (expired identities, secret sync lag) that must be monitored.

## Alternatives Considered

1. **Dapr Secret Store Only**
   - **Rejected:** KEDA, cert-manager, and infrastructure containers still require native Kubernetes Secrets; Dapr secret store does not cover their use cases.

2. **Commit Sample Secrets to Repo**
   - **Rejected:** Violates security best practices; risks accidental leakage and encourages reuse of shared credentials.

3. **Cloud-Specific Secret Services Per Environment**
   - **Rejected:** Would require divergent manifests for AKS/EKS/GKE, undermining ADR-0007/0009 cloud-agnostic goals.

## Implementation Notes

- **Naming Convention:** `component-purpose-secret` (e.g., `sqlserver-secret`, `rabbitmq-keda-secret`). Keys must be uppercase snake case (`SA_PASSWORD`, `RABBITMQ_URI`).
- **Helm Values:** All charts must expose `secretName`/`secretKey` entries; sample values remain git-tracked but contain placeholders only.
- **Rotation:** Rotating a secret requires updating the source provider (or Helm values for local), reapplying the secret, and restarting dependent pods (Helm `upgrade --reuse-values` handles this).
- **KEDA:** TriggerAuthentication objects live in the same namespace as their ScaledObjects and reference the standardized secrets mandated here.
- **External Secrets:** When ESO/CSI drivers are introduced, they must populate the same Kubernetes Secret names to avoid manifest drift.

## References

- ADR-0002: Cloud-Agnostic Configuration via Dapr (application-level secrets)
- ADR-0006: Infrastructure Configuration via Environment Variables
- ADR-0009: Helm Multi-Environment Deployment
- plan/upgrade-phase0-platform-foundation-implementation-1.md
- plan/upgrade-keda-2.18-implementation-1.md
- AGENTS.md / CLAUDE.md (development prerequisites)
