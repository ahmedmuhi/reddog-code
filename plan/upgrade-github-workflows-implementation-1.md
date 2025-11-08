---
goal: Modernize GitHub Actions workflows for all Red Dog services per CI/CD strategy and tooling requirements
version: 1.0
date_created: 2025-11-06
last_updated: 2025-11-06
owner: Red Dog Modernization Team
status: Planned
tags: [ci, github-actions, automation, tooling]
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

This plan upgrades every workflow under `.github/workflows/` to comply with `plan/cicd-modernization-strategy.md`, `plan/testing-validation-strategy.md`, and `plan/modernization-strategy.md`. It ensures all service-specific upgrade plans can rely on working GitHub Actions pipelines (tooling audit, build/test, container publish).

## Research References

- `plan/cicd-modernization-strategy.md` (Pipeline inventory, missing features, tooling compliance requirements)
- `plan/testing-validation-strategy.md` (Tool installation requirements and artifact expectations consumed by workflows)
- `docs/research/dotnet-upgrade-analysis.md` (Docker base image requirements, dependency audit context)

## 1. Requirements & Constraints

- **REQ-001**: Workflows must use supported actions versions (`actions/checkout@v4`, `actions/setup-dotnet@v4`, `docker/build-push-action@v5`, `actions/setup-node@v4`).
- **REQ-002**: Every workflow must include a `tooling-audit` job executing Upgrade Assistant / workload updates / dependency audits / API Analyzer for the targeted project, storing artifacts under `artifacts/`.
- **REQ-003**: Build jobs must run `dotnet build`, `dotnet test`, `dotnet list package --vulnerable`, and container builds with .NET 10 base images (or Node 24 + Vite for UI).
- **REQ-004**: Publishing jobs must push to GHCR with Git SHA/tag naming.
- **REQ-005**: Workflow artifacts must include tooling outputs and validation reports for audit.
- **SEC-001**: Fail workflows if API Analyzer reports severity ≥ Medium or `dotnet list package --vulnerable` finds vulnerabilities.
- **CON-001**: All workflows triggered on PR and main branch (push) must enforce status checks referenced in branch protection.
- **GUD-001**: Use composite actions or reusable workflow templates where possible to reduce duplication (optional but encouraged).

## 2. Implementation Steps

### Implementation Phase 1 – Baseline Audit & Common Upgrades

- **GOAL-001**: Document current workflows, update core actions, and ensure .NET/Node toolchains are specified.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-000** | List existing workflows (`ls .github/workflows/*.yaml`) and capture summary in `artifacts/workflows/workflow-inventory.md` (name, service, triggers). | | |
| **TASK-001** | For each workflow, update `actions/checkout` to `@v4`, `actions/setup-dotnet` to `@v4`, and `docker/build-push-action` to `@v5` (where applicable). Document diffs in `artifacts/workflows/common-action-update.md`. | | |
| **TASK-002** | Ensure each .NET workflow installs SDK 10.0.100 (respecting `global.json`) via `actions/setup-dotnet@v4` with `dotnet-quality: ga`. | | |
| **TASK-003** | For `package-ui.yaml`, update to `actions/setup-node@v4` with `node-version: 24.x`, run `npm ci`, `npm run build`, `npm run test`, and set `CI=true`. | | |
| **TASK-004** | Add caching (`actions/cache@v3`) for NuGet (`~/.nuget/packages`) and npm (`~/.npm`) as appropriate. | | |
| **TASK-005** | Update container build steps to reference `.NET 10` images (if explicit tags used) and ensure `docker buildx` uses `--build-arg VERSION=$(git rev-parse HEAD)`. | | |

**Completion Criteria (Phase 1):** Inventory and action update artifacts exist; all workflows use current action versions and specify .NET 10 / Node 24 toolchains.

### Implementation Phase 2 – Tooling Audit & Validation Jobs

- **GOAL-002**: Embed tooling audit, build/test, and publish jobs that satisfy modernization requirements.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-006** | Add `tooling-audit` job to each workflow: run Upgrade Assistant, workload restore/update, `dotnet list ...`, API Analyzer, store outputs in workflow artifacts (`artifacts/upgrade-assistant/`, `artifacts/dependencies/`, `artifacts/api-analyzer/`), fail on warnings/vulnerabilities. | | |
| **TASK-007** | Add/adjust `build-and-test` job: `dotnet build -warnaserror`, `dotnet test --collect:"XPlat Code Coverage"`, `dotnet list package --vulnerable`, upload coverage reports; for UI, run `npm run lint/test/build`. | | |
| **TASK-008** | Add `docker-build-and-publish` job: build container with .NET 10 runtime, tag `ghcr.io/azure/reddog-<service>:$GITHUB_SHA`, push only on `main`. | | |
| **TASK-009** | Configure workflow-level `workflow_call` or `workflow_run` dependencies: `build-and-test` depends on `tooling-audit`; `docker-build-and-publish` depends on `build-and-test`. | | |
| **TASK-010** | Upload tooling artifacts using `actions/upload-artifact@v4` (naming convention `tooling-<service>-<runId>`). | | |
| **TASK-011** | Ensure status checks (`tooling-audit`, `build-and-test`, `docker-build-and-publish`) match branch protection rules defined in modernization strategy. | | |

### Implementation Phase 3 – Testing & Rollout

- **GOAL-003**: Validate workflows on feature branches and document success criteria.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-012** | For each service workflow, open a PR that triggers the pipeline; attach run URLs to `artifacts/workflows/workflow-validation.md` and confirm stages succeeded. | | |
| **TASK-013** | Execute manual QA checklist: verify tooling artifacts contain expected files, containers push to GHCR, statuses appear in PR. | | |
| **TASK-014** | Merge workflow PRs after approval; update `plan/cicd-modernization-strategy.md` status table if maintained. | | |

**Completion Criteria (Phase 3):** All workflows run successfully on PR and main; artifacts exist; branch protection gating is satisfied.

## 3. Alternatives

- **ALT-001**: Migrate to Azure DevOps pipelines — rejected (modernization strategy is GitHub-only).
- **ALT-002**: Use a single monolithic workflow for all services — deferred; current approach keeps per-service workflows for clarity.

## 4. Dependencies

- `plan/cicd-modernization-strategy.md` – overall CI/CD guidance.
- `plan/testing-validation-strategy.md` – tooling artifacts & validation criteria.
- `plan/modernization-strategy.md` – branch protection/status check requirements.
- Individual service implementation plans referencing workflow validation tasks.

## 5. Files

- `.github/workflows/package-*.yaml` (one per service + UI)
- `.github/workflows/promote-manifests.yaml`
- `artifacts/workflows/*` (inventory, action updates, validation logs)

## 6. Testing

- GitHub Actions runs (PR + push) for each workflow with `tooling-audit`, `build-and-test`, `docker-build-and-publish` jobs.
- Optional: `act` local runner to sanity-check changes before pushing.

## 7. Risks & Assumptions

- **RISK-001**: Tooling commands increase workflow runtime. *Mitigation*: Use caching, limit `tooling-audit` to PR + protected branches.
- **RISK-002**: Upgrade Assistant or API Analyzer may fail for projects lacking prerequisites. *Mitigation*: ensure repo contains `global.json`, required tools installed in job step.
- **ASSUMPTION-001**: Secrets for GHCR push already configured (`GHCR_TOKEN`).

## 8. Related Specifications / Further Reading

- `plan/cicd-modernization-strategy.md`
- `plan/testing-validation-strategy.md`
- `plan/modernization-strategy.md`
- `docs/research/dotnet-upgrade-analysis.md`
