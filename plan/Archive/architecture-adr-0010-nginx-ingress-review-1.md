---
goal: Review and realign ADR-0010 Nginx Ingress Controller with current repo state
version: 1
date_created: 2025-11-22
last_updated: 2025-11-22
owner: Red Dog Modernization Team
status: Planned
tags: [architecture, adr, ingress, nginx, documentation]
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

This plan defines the steps required to review, reconcile, and update `ADR-0010: Nginx Ingress Controller (Cloud-Agnostic)` so that it accurately reflects the current repository state and architectural patterns. It also introduces an explicit concision goal: ADR-0010 should be reduced from roughly 500 lines to approximately 250 lines while preserving all important decisions and patterns. The plan focuses on separating long-lived decisions from implementation details, aligning ingress strategy with Helm-based multi-environment deployment, and wiring ADR-0010 into the emerging knowledge/ADR structure.

## 1. Requirements & Constraints

- **REQ-001**: ADR-0010 MUST accurately describe the canonical ingress strategy for Red Dog across local, AKS, EKS, and GKE environments.
- **REQ-002**: ADR-0010 MUST clearly separate architectural decisions (what/why) from ephemeral implementation details (how/where).
- **REQ-003**: ADR-0010 MUST identify which artifacts are canonical (Helm chart, values files, GitOps objects) and which are legacy or examples.
- **REQ-004**: ADR-0010 MUST be consistent with the Helm-based multi-environment pattern described in ADR-0009 and related knowledge items.
- **REQ-005**: ADR-0010 MUST avoid claiming the existence of files or GitOps objects that are not present in the repository.
- **REQ-006**: ADR-0010 SHOULD be streamlined from its current ~500 lines to approximately 250 lines (±10%), by removing duplication, moving long examples into guides, and tightening prose, without losing any key decisions or constraints.

- **SEC-001**: ADR-0010 MUST NOT include real secrets, production-only hostnames, or sensitive configuration; such values MUST be represented via placeholders or references to values files.
- **SEC-002**: Any recommendations for TLS, certificates, or public exposure MUST follow existing security/PKI guidance (e.g. cert-manager usage), not invent new patterns.

- **CON-001**: The ADR-0010 file structure (front matter, headings, numbering) MUST remain compatible with the existing ADR index and links under `docs/adr/`.
- **CON-002**: Changes to ADR-0010 MUST NOT silently invalidate other ADRs; conflicts MUST be explicitly resolved or called out.
- **CON-003**: Ingress strategy MUST continue to support the demo/teaching goals of Red Dog (simple local host-based access and cloud-ready patterns).

- **GUD-001**: Use minimal, representative YAML and command snippets in ADR-0010 and move longer examples into `docs/guides/` or sample files.
- **GUD-002**: Prefer referencing existing files (Helm charts, values, manifests) over duplicating them in the ADR body.

- **PAT-001**: Treat ingress-nginx as an infrastructure concern deployed via Helm, with environment-specific behaviour controlled via values files.
- **PAT-002**: Where possible, reuse existing patterns captured in `knowledge/ki-red-dog-architecture-001.md` and `knowledge/ki-da-dapr-components-cloud-agnostic-001.md`.

## 2. Implementation Steps

### Implementation Phase 1

- GOAL-001: Establish an accurate map of how Nginx ingress is currently represented in the repo and how that compares to ADR-0010.

| Task ID  | Description                                                                                             | Location                                                                    | DependsOn | Status      | Date       |
|----------|---------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------|-----------|-------------|------------|
| TASK-001 | Review ADR-0010 to extract its stated decisions, assumptions, and current status.                      | docs/adr/adr-0010-nginx-ingress-controller-cloud-agnostic.md#Decision      |           | NotStarted  |            |
| TASK-002 | Review ADR-0010 for embedded YAML and command examples and list which ones are conceptual vs specific. | docs/adr/adr-0010-nginx-ingress-controller-cloud-agnostic.md#Consequences  | TASK-001  | NotStarted  |            |
| TASK-003 | Enumerate all ingress-related artifacts (Helm charts, values, manifests, plans) in the repository.     |                                                                             | TASK-001  | NotStarted  |            |
| TASK-004 | Identify the current canonical ingress deployment path (Helm vs raw manifests vs GitOps) in practice.  |                                                                             | TASK-003  | NotStarted  |            |
| TASK-005 | Compare ADR-0010’s claims (GitOps wiring, environments, versions) to the discovered repo state.        | /plan/architecture-adr-0010-nginx-ingress-review-1.md#Implementation-Phase-1 | TASK-002,TASK-004 | NotStarted  |            |
| TASK-006 | Document mismatches and ambiguities (e.g. legacy Flux v1 HelmRelease snippets) as input for rewrite.   | /plan/architecture-adr-0010-nginx-ingress-review-1.md#Implementation-Phase-2 | TASK-005  | NotStarted  |            |

### Implementation Phase 2

- GOAL-002: Rewrite ADR-0010 so that its status, decision, and structure match the actual repo state and ADR hygiene guidelines, and reduce its length from ~500 lines to approximately 250 lines (±10%) by removing duplication, moving long examples into guides, and tightening prose.

| Task ID  | Description                                                                                                                    | Location                                                                    | DependsOn | Status      | Date       |
|----------|--------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------|-----------|-------------|------------|
| TASK-007 | Update ADR-0010 front matter and status text to match its real usage (e.g. Accepted, with clear scope).                       | docs/adr/adr-0010-nginx-ingress-controller-cloud-agnostic.md#top           | TASK-006  | NotStarted  |            |
| TASK-008 | Rewrite the Decision section to clearly state the canonical ingress strategy for all environments.                             | docs/adr/adr-0010-nginx-ingress-controller-cloud-agnostic.md#Decision      | TASK-006  | NotStarted  |            |
| TASK-009 | Introduce a concise Context/Current State section that accurately describes Helm charts, values, and any GitOps integration.   | docs/adr/adr-0010-nginx-ingress-controller-cloud-agnostic.md#Context       | TASK-006  | NotStarted  |            |
| TASK-010 | Move or trim large YAML and command blocks from ADR-0010 into one or more guides under `docs/guides/`, leaving references, to help reach the ~250-line target. | docs/adr/adr-0010-nginx-ingress-controller-cloud-agnostic.md#Consequences  | TASK-006  | NotStarted  |            |
| TASK-011 | Clearly label any remaining raw ingress manifests or Flux v1 examples as legacy or historical, not canonical.                 | docs/adr/adr-0010-nginx-ingress-controller-cloud-agnostic.md#Alternatives  | TASK-006  | NotStarted  |            |
| TASK-012 | Ensure ADR-0010 explicitly states which files are the canonical source of truth for ingress (Helm chart, values files, etc.). | docs/adr/adr-0010-nginx-ingress-controller-cloud-agnostic.md#Consequences  | TASK-008  | NotStarted  |            |

### Implementation Phase 3

- GOAL-003: Align ADR-0010 with knowledge items and surrounding ADRs so future agents and humans reuse decisions consistently.

| Task ID  | Description                                                                                                                        | Location                                                                    | DependsOn | Status      | Date       |
|----------|------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------|-----------|-------------|------------|
| TASK-013 | Decide whether a dedicated ingress-nginx Knowledge Item is required and, if so, define its scope and identifier.                   | knowledge/KNOWLEDGE_ITEM_TEMPLATE.md#Summary                               | TASK-008  | NotStarted  |            |
| TASK-014 | If proceeding, author a KI (e.g. `knowledge/ki-ingress-nginx-strategy-001.md`) summarising stable ingress facts and patterns.      | knowledge/ki-ingress-nginx-strategy-001.md#Summary                         | TASK-013  | NotStarted  |            |
| TASK-015 | Update ADR-0010’s References section to point to relevant KIs (architecture, Dapr, ingress) and to ADR-0008/ADR-0009.              | docs/adr/adr-0010-nginx-ingress-controller-cloud-agnostic.md#References    | TASK-008  | NotStarted  |            |
| TASK-016 | Add a short “Guidance for future plans” subsection in ADR-0010 explaining how new services should integrate with ingress-nginx.   | docs/adr/adr-0010-nginx-ingress-controller-cloud-agnostic.md#Consequences  | TASK-008  | NotStarted  |            |
| TASK-017 | Ensure ADR indexes (`docs/adr/README.md`, `docs/adr/ADR_SUMMARIES.md`) reflect the updated status and scope of ADR-0010.          | docs/adr/README.md#adr-index                                                | TASK-007  | NotStarted  |            |

## 3. Alternatives

- **ALT-001**: Deprecate ADR-0010 and create a brand-new ADR describing ingress strategy from scratch.  
  - Rejected because existing references and historical decisions are valuable; updating ADR-0010 preserves continuity.
- **ALT-002**: Split ingress into multiple ADRs (one per cloud provider).  
  - Rejected because ingress-nginx is intended as a cloud-agnostic component; environment-specific behaviour belongs in values and guides, not separate ADRs.
- **ALT-003**: Treat ingress strategy as purely implementation detail documented in guides only (no ADR).  
  - Rejected because ingress is a cross-cutting architectural decision affecting exposure, security, and multi-environment design.

## 4. Dependencies

- **DEP-001**: `docs/adr/adr-0010-nginx-ingress-controller-cloud-agnostic.md` must exist and remain the primary ADR for ingress strategy.
- **DEP-002**: `docs/adr/adr-0008-kind-local-development-environment.md` and `docs/adr/adr-0009-helm-multi-environment-deployment.md` must remain valid, as ADR-0010 will reference their decisions.
- **DEP-003**: Ingress Helm chart and values files (e.g. `/charts/external/nginx-ingress/Chart.yaml`, `/charts/external/nginx-ingress/values-base.yaml`) must be kept in sync with ADR-0010.
- **DEP-004**: Knowledge item specification (`knowledge/KNOWLEDGE_ITEM_TEMPLATE.md`) governs how any new ingress-related KI is authored.
- **DEP-005**: Any existing GitOps tooling (Flux/Argo) configuration for ingress, if reintroduced or updated, must be reflected in ADR-0010 once stable.

## 5. Files

- **FILE-001**: `/docs/adr/adr-0010-nginx-ingress-controller-cloud-agnostic.md` — ADR describing ingress-nginx strategy (primary subject of this plan).
- **FILE-002**: `/docs/adr/adr-0009-helm-multi-environment-deployment.md` — Helm multi-environment deployment ADR; provides patterns ADR-0010 must align with.
- **FILE-003**: `/docs/adr/adr-0008-kind-local-development-environment.md` — Local dev ADR; defines expectations for local ingress behaviour.
- **FILE-004**: `/charts/external/nginx-ingress/Chart.yaml` — Helm chart metadata for ingress-nginx.
- **FILE-005**: `/charts/external/nginx-ingress/values-base.yaml` — Base values for ingress-nginx across environments.
- **FILE-006**: `/docs/adr/README.md` and `/docs/adr/ADR_SUMMARIES.md` — ADR indexes that must be updated to reflect ADR-0010’s status.
- **FILE-007**: `/knowledge/KNOWLEDGE_ITEM_TEMPLATE.md` — Template for defining ingress-related KIs.
- **FILE-008**: `/plan/architecture-adr-0010-nginx-ingress-review-1.md` — This implementation plan.

## 6. Testing

- **TEST-001**: Validate that all file references and section anchors in ADR-0010 are correct (no broken links when rendered in GitHub/Docs).  
  - Covers: TASK-007, TASK-008, TASK-009, TASK-012, TASK-015, TASK-017.
- **TEST-002**: Run `helm template` for the ingress-nginx chart using referenced values to confirm YAML renders without errors and matches the ADR description at a high level.  
  - Covers: TASK-003, TASK-004, TASK-010.
- **TEST-003**: In a disposable cluster (or kind), deploy ingress-nginx following the updated ADR + guides to confirm no missing prerequisites or conceptual gaps.  
  - Covers: TASK-010, TASK-016.
- **TEST-004**: If a new ingress KI is created, verify it is discoverable and consistent: cross-check facts/constraints in the KI against ADR-0010 and related ADRs.  
  - Covers: TASK-013, TASK-014, TASK-015.

## 7. Risks & Assumptions

- **RISK-001**: ADR-0010 may currently describe legacy Flux v1 GitOps patterns that are no longer used; careless edits could erase useful historical context.  
- **RISK-002**: Changing ADR-0010 without updating related docs (guides, plans) could introduce new inconsistencies.  
- **RISK-003**: If ingress behaviour differs between real clusters and the repo configuration (e.g. external charts or ops-owned manifests), ADR-0010 might still be incomplete.

- **ASSUMPTION-001**: ingress-nginx remains the standard ingress controller for Red Dog across all target environments.  
- **ASSUMPTION-002**: No parallel initiative is replacing ingress-nginx with another controller (e.g. Traefik, Istio) during the execution of this plan.  
- **ASSUMPTION-003**: The repository remains the authoritative source of truth for architecture and deployment patterns, not external wikis.

## 8. Related Specifications / Further Reading

- `docs/adr/adr-0010-nginx-ingress-controller-cloud-agnostic.md` — ADR under review.
- `docs/adr/adr-0008-kind-local-development-environment.md` — Local development strategy.
- `docs/adr/adr-0009-helm-multi-environment-deployment.md` — Helm multi-environment deployment strategy.
- `knowledge/ki-red-dog-architecture-001.md` — High-level Red Dog architecture knowledge item.
- `knowledge/ki-da-dapr-components-cloud-agnostic-001.md` — Dapr components cloud-agnostic patterns.
- `knowledge/KNOWLEDGE_ITEM_TEMPLATE.md` — Authoring rules for new knowledge items.
