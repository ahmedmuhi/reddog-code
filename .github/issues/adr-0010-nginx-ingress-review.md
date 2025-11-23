---
title: "Implement ADR-0010: Review & Align Nginx Ingress Controller"
labels: ["area/infra", "type/architecture", "priority/medium"]
assignees: []
---

# Implement ADR-0010: Nginx Ingress Controller (Cloud-Agnostic) — Review & Cleanup

This issue tracks the implementation plan described in `plan/architecture-adr-0010-nginx-ingress-review-1.md`.

Goal: review ADR-0010, reconcile it with the current repo state and Helm-based multi-environment patterns, and produce a concise, accurate ADR plus any supporting guides or knowledge items.

When completed this will:

- Make `docs/adr/adr-0010-nginx-ingress-controller-cloud-agnostic.md` reflect the true canonical ingress strategy used in the repository.
- Reduce the ADR to be concise (~250 lines ±10%) while preserving the core decisions and constraints.
- Move long examples and commands from the ADR into supporting guides under `docs/guides/`.
- Add or update any required Knowledge Items and ADR indexes.

Related plan: `plan/architecture-adr-0010-nginx-ingress-review-1.md`

## Acceptance Criteria ✅

1. ADR-0010 updated to accurately reflect Helm-based ingress deployment for local and cloud targets and its status is correct (Accepted/Implemented as appropriate).
2. ADR content is shortened to approximately 250 lines (±10%) with long examples moved to `docs/guides/` or referenced sample files.
3. All file references and anchors referenced in the ADR are valid and renderable in GitHub.
4. Helm chart(s) or values that are designated as canonical are clearly identified in the ADR and verified via `helm template` for basic rendering (no syntax errors).
5. Any legacy Flux v1 or obsolete examples are clearly labelled as historical and not canonical.
6. If a new knowledge item is required (KI for ingress), it is created under `knowledge/` and cross-referenced from the ADR.
7. ADR index files and summaries are updated to reflect the new status.

## Implementation checklist (work items)

- [ ] TASK-001: Review current ADR to extract decisions and assumptions.
- [ ] TASK-002: Audit embedded YAML/commands and identify conceptual vs specific examples.
- [ ] TASK-003: Enumerate ingress-related artifacts in the repo (charts, values, manifests).
- [ ] TASK-004: Identify the canonical ingress deployment path in practice.
- [ ] TASK-005: Compare ADR claims to discovered repo state and list mismatches.
- [ ] TASK-006: Document mismatches/ambiguities and prepare rewrite plan.
- [ ] TASK-007: Update ADR front matter and status text.
- [ ] TASK-008: Rewrite Decision section to clearly define canonical ingress strategy.
- [ ] TASK-009: Add/clarify Context/Current State describing Helm charts/values/GitOps.
- [ ] TASK-010: Move or trim large YAML/commands into `docs/guides/` with references.
- [ ] TASK-011: Mark any legacy snippets as historical in ADR.
- [ ] TASK-012: State canonical source of truth files in ADR (chart, values path).
- [ ] TASK-013: Decide if a dedicated ingress KI is required.
- [ ] TASK-014: If required, author `knowledge/ki-ingress-nginx-strategy-001.md`.
- [ ] TASK-015: Update ADR References to point to the new or updated KI(s).
- [ ] TASK-016: Add “Guidance for future plans” to ADR.
- [ ] TASK-017: Update ADR indexes to reflect new status and summary.

## Testing

- For relevant tasks, run `helm template` on the chart(s) identified as canonical to ensure they render without errors.
- Validate internal links and anchors in the ADR render correctly on GitHub.
- Optionally deploy into a disposable cluster (kind) to validate conceptual steps.

## Notes

Follow the repository's `plan/architecture-adr-0010-nginx-ingress-review-1.md` for details, constraints, risks and testing guidance.  Keep changes minimal, surgical and well-verified.

---

If you'd like, I can open a GitHub web issue from this text next (requires network/permissions) or create a PR that adds this issue file to the repository where the maintainer can review and push it live on GitHub.
