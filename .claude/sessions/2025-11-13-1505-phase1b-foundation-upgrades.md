# Session: Phase 1B Foundations - Platform & Tooling Upgrades

**Started:** 2025-11-13 15:05 NZDT
**Status:** In Progress

## Overview

With Phase 1A complete (all nine .NET workloads now on .NET 10), this session focuses on preparing the broader platform for Phase 1B polyglot migrations. The goal is to modernize every supporting component—tooling, infrastructure services, and CI/CD—so future language migrations happen on a fully upgraded foundation (Dapr/KEDA/Helm, Vue 3 UI, Redis/SQL tuning, GitHub workflows, etc.).

## Objectives

1. **Assess Outstanding Plans**
   - Review the active documents under `plan/` (CI/CD modernization, Dapr 1.16→latest, KEDA 2.18, infrastructure containers, Vue 3 migration, storage/state refactors).
   - Capture dependencies and ordering so upgrades happen without conflicting rollouts.

2. **Define Environment Readiness Checklist**
   - Document the exact versions/toolchains required before Phase 1B (Dapr, KEDA, nginx ingress, Redis, SQL Server, CI workflows, Vue tooling, smoke/validation scripts).
   - Identify automation gaps (e.g., scripts for Vue/Node/Go builds) and log them as TODOs in this session.

3. **Kick Off First Foundation Upgrade**
   - Select the highest-impact plan (likely `plan/upgrade-dapr-1.16-implementation-1.md` or `plan/upgrade-infrastructure-containers-implementation-1.md`).
   - Create/refresh an implementation checklist and begin execution (pre-flight, Helm changes, validation, documentation updates).

## Reference Documents

- `plan/modernization-strategy.md` (Phase 1B and infrastructure sections)
- `plan/testing-validation-strategy.md` (smoke/validation expectations)
- Active plans in `plan/`: CICD modernization, Dapr upgrade, KEDA upgrade, infrastructure containers, Vue 3 migration, storage/state refactors
- `.claude/sessions/2025-11-13-0645-phase1a-remaining-dotnet10-upgrades.md` (Phase 1A closure summary)

## Initial Tasks

- [ ] Inventory every active plan and note current status + required tooling.
- [ ] Draft an “environment readiness” checklist (versions, scripts, validation steps) and store it in this session log.
- [ ] Decide which upgrade (Dapr vs. CI/CD vs. Vue 3) unblocks the most downstream work; prepare its detailed execution plan.

---

_Updates will be appended below as work proceeds._

### Update - 2025-11-13 15:25 NZDT

**Summary:** Cleanly split the legacy “Upgrade Dapr 1.16” plan so the core runtime work can proceed now, while the deferred cloud/workload-identity scope moved into a new companion plan.

**Actions:**
1. Trimmed `plan/upgrade-dapr-1.16-implementation-1.md` to cover Phases 1–4 (prep, Helm upgrade, component refresh, deployment annotations) plus Phase 7 validation; added a scope note pointing to the follow-up work.
2. Created `plan/dapr-cloud-hardening-implementation-1.md` to capture the deferred service invocation header fixes and Azure/AWS/GCP workload identity tasks (former Phases 5–6), keeping them visible but decoupled from the runtime upgrade.
3. Committed changes (9987a98) so both plans are version-controlled and aligned with our Phase 1B goals.

**Next Steps:**
- Fill out a tactical execution checklist for the trimmed Dapr runtime plan (who/when/commands) so we can start Phase 1 tasks immediately.
- Update the “Initial Tasks” bullets with owners/dates once we inventory the remaining platform plans.

### Update - 2025-11-13 15:50 NZDT

**Summary:** Completed Phase 1 prep work for the Dapr runtime upgrade. All evidence is stored under `artifacts/dapr-upgrade/2025-11-13/`.

**Artifacts / Notes:**
- `dapr-components-backup.tgz` – tarball of the current Helm-rendered component templates (acts as rollback point).
- `sidecar-annotations.md` – dump of every deployment’s `dapr.io/*` annotations (even though replicas are scaled to 0, the specs retain annotations).
- `runtime-version.txt` + `helm-list.txt` – confirmed the control-plane is already on 1.16.2 GA across operator/sentry/injector/placement; no unknown Helm releases.
- `kubernetes-version.yaml` + `dapr-cli-version.txt` – verified cluster/server at v1.34 and CLI at 1.16.3, satisfying the plan’s prereqs.
- `component-names.md` – proves there are no duplicate component names in `charts/reddog/templates/dapr-components/`.
- `invocation-audit.txt` – `rg` snapshot of all `InvokeMethodAsync` usage (currently DaprClient-based, so HTTP Content-Type fixes are deferred to the new cloud hardening plan).

**Checklist Impact:** Phase 1 of the Dapr runtime plan is now complete; ready to tackle the Helm upgrade steps.

### Update - 2025-11-13 16:05 NZDT

**Summary:** Executed Phase 2 (Helm upgrade) for the Dapr runtime plan.

**Details:**
- `helm repo update dapr` (repo already present) and captured current values in `artifacts/dapr-upgrade/2025-11-13/helm-current-values.yaml`.
- `helm upgrade dapr dapr/dapr --namespace dapr-system --version 1.16.2 --wait --timeout 5m` succeeded (revision 2); control-plane pods remained healthy (see `control-plane-pods.json`).
- Verified operator, sentry, injector, placement, and scheduler pods are still on 1.16.2 GA images; recorded `kubectl get pods -n dapr-system -o wide` output for traceability.

**Next Steps:** Start Phase 3—update component manifests/annotations in Helm charts and prep the rollout/restart scripts.

### Update - 2025-11-13 16:40 NZDT

**Summary:** Completed Phase 3 deliverables (component schema refresh + deployment annotations) for the Dapr runtime plan.

**Details:**
- Refreshed every component template under `charts/reddog/templates/dapr-components/` (pubsub, state stores, bindings, secret store) and added a brand-new `Configuration` resource; all use values-driven scopes so app IDs stay in sync with Helm.
- Added `dapr.io/config`, `dapr.io/enable-metrics`, and `dapr.io/metrics-port` annotations to every Dapr-enabled workload (services + bootstrapper job) so the new configuration + telemetry settings apply uniformly.
- Introduced `dapr.configuration` settings in `values/values-local.yaml` (+ sample) to keep tracing/feature toggles declarative.
- Applied the updated manifests live via `helm template ... --show-only templates/dapr-components/* | kubectl apply -f -` (captured in `artifacts/dapr-upgrade/2025-11-13/components.txt`), and created the `reddog.configuration` resource for use by the new annotations.

**Next Steps:** Phase 7 validation once we scale the services back up (will require a Helm upgrade or `kubectl rollout restart` when we re-enable replicas). For now, replicas remain scaled to zero to keep resource usage low.

### Update - 2025-11-13 17:25 NZDT

**Summary:** Attempted to re-enable the workloads (Phase 7 prep) but hit a Dapr sidecar issue. Helm upgrade now waits on pods that never become Ready because `daprd` crashes while fetching its identity from Sentry.

**What happened:**
- Scaled infrastructure (Redis + SQL Server) back up and kicked off `helm upgrade reddog … --wait --timeout 5m`. The first run failed because the new `virtualworker.cron` component wasn’t managed by Helm; after deleting it (and the Configuration object) we re-ran the upgrade.
- After the kind control-plane restart, pods redeployed but every Dapr-enabled workload sits at `0/2` with the sidecar in `CrashLoopBackOff`. Logs show `failed to retrieve the initial identity: error establishing connection to sentry: dial tcp 10.96.56.61:443: connect: connection refused`.
- Because the sidecars never go Ready, Helm keeps timing out (`context deadline exceeded`) and the release is stuck between revisions (history now shows rev 15 `pending-upgrade`).

**Current state:**
- Redis + SQL pods are healthy; app pods exist but all `daprd` containers crash when contacting Sentry. Control-plane pods (operator/placement/sentry/injector) look healthy, so the next step is to diagnose why workloads can’t reach `dapr-sentry` (suspect: Helm-added config/metrics annotations referencing a Configuration resource that wasn’t recreated, or the Sentry service remapped after the kind restart).
- No Phase 7 health/smoke tests have been run yet; blocker is stabilizing the workloads.

**Next Steps / Options:**
1. Add the new `Configuration` and `virtualworker.cron` components directly to the Helm chart (so they inherit release metadata), then perform a clean `helm rollback reddog 13` followed by `helm upgrade …` once the cluster is reachable. If Sentry errors persist, capture network traces (`kubectl exec`) to confirm connectivity to `dapr-sentry.dapr-system.svc:443`.
2. As a quick mitigation, temporarily remove the `dapr.io/config` annotation so workloads fall back to the stock Dapr settings, re-run Helm, and verify whether Sentry connections succeed; if yes, reintroduce the configuration after confirming the resource exists in-cluster.
3. After pods stabilize, rerun `helm upgrade … --wait` and proceed with `./scripts/upgrade-validate.sh` for each service plus the smoke scripts.

### Update - 2025-11-13 18:55 NZDT

**Summary:** Rebuilt the kind cluster, reinstalled Dapr/infrastructure charts, reloaded all local images, redeployed `reddog`, and finished Phase 7 validation (upgrade-validate on every workload, Dapr API curls, and the existing smoke scripts).

**Details:**
1. `kind delete cluster --name reddog-local` → `kind create cluster --config kind-config.yaml`.
2. `helm upgrade --install dapr ... --version 1.16.2`, `helm upgrade --install reddog-infra ...`, then sequential `kind load docker-image` for every `ghcr.io/ahmedmuhi/reddog-* :local` tag before `helm upgrade --install reddog ... --wait`.
3. Restarted all app deployments to clear historical probe failures, deleted cluster events, and reran `./scripts/upgrade-validate.sh` for Order/MakeLine/Loyalty/Accounting/ReceiptGeneration/VirtualWorker/VirtualCustomers (all green).
4. Captured Dapr API smoke tests in `artifacts/dapr-upgrade/2025-11-13/phase7-dapr-tests.txt` (health, invoke `orderservice`, pub/sub, state, binding, secret) and re-ran `scripts/run-dapr-makeline-smoke.sh` + `scripts/run-virtualcustomers-smoke.sh`.

**Next Steps:**
- Update the Dapr upgrade plan tracker (Phase 7 = complete) and modernization docs, then commit the rebuild/validation changes.
- Proceed to the next foundation plan (e.g., KEDA or CI/CD) once the docs reflect this successful runtime upgrade.

### Update - 2025-11-14 06:45 NZDT

**Summary:** Removed the legacy Flux v1 `HelmRelease` manifests and Kustomize overlays so the repo reflects our Helm-only workflow.

**Details:**
- Deleted all files under `manifests/branch/dependencies/` plus `manifests/branch/base|redmond/kustomization.yaml`; nothing in CI/CD referenced them and the cluster has no Flux CRDs.
- Updated `plan/upgrade-phase0-platform-foundation-implementation-1.md` to point at the Helm charts/scripts that now own infrastructure deployments.
- Updated `docs/research/keda-upgrade-reddog-plan.md` to describe the direct Helm installation procedure.

**Next Steps:** Continue the KEDA 2.18 upgrade work (Phase 1 checklist is complete, ready for Helm install).

### Update - 2025-11-14 07:05 NZDT

**Summary:** Installed and validated KEDA 2.18.1 via Helm, completing Phase 3–4 of the upgrade plan.

**Details:**
- Added the `kedacore` Helm repo, refreshed indexes, and confirmed chart 2.18.1 availability before installing.
- Ran `helm upgrade --install keda kedacore/keda --namespace keda --create-namespace --version 2.18.1 --wait --timeout 5m`; release reached `deployed` on revision 1.
- Verified pods (`kubectl get pods -n keda`), operator image (`ghcr.io/kedacore/keda:2.18.1`), metrics API service availability, admission webhook, and CRD access (`kubectl get scaledobjects/triggerauthentication -A`).
- Confirmed `autoscaling/v2` appears in `kubectl api-versions`, logs show no operator errors, and a sample ScaledObject passes `kubectl apply --dry-run=client`.

**Next Steps:** Document the remaining optional Phase 5 scaler work and decide when to author real ScaledObjects for MakeLine + Loyalty.

### Update - 2025-11-14 07:55 NZDT

**Summary:** Captured the new secret management policy in ADR-0013 and aligned the quick-start docs with the current platform state.

**Details:**
- Authored `docs/adr/adr-0013-secret-management-strategy.md` (transport = Kubernetes Secrets, source = environment-specific providers) and linked it from `docs/adr/README.md`.
- Refreshed `AGENTS.md` / `CLAUDE.md` with the latest status (Phase 1A complete, KEDA installed) plus a new “Secrets (ADR-0013)” quick-start section.
- Reinforced that Helm/External Secrets must populate Kubernetes Secrets and that KEDA TriggerAuthentications will reference them.

**Next Steps:** Apply the policy to upcoming work (RabbitMQ/KEDA credentials, CI/CD modernization) and keep the session log updated as additional foundation upgrades land.

### Update - 2025-11-14 08:25 NZDT

**Summary:** Refactored the Dapr Helm components to support metadata-driven backends, enabling true “plug and play” for RabbitMQ/Cosmos/other providers.

**Details:**
- Extended every Dapr component template (pub/sub, both state stores, secret store, bindings) to accept an optional `.metadata` array with backward-compatible Redis defaults.
- Updated `values/values-local.yaml.sample` with metadata examples and added `values/values-azure.yaml.sample` showing RabbitMQ + Cosmos DB + Key Vault configuration tied to ADR-0013.
- Refreshed AGENTS/CLAUDE quick starts to point developers at the new pattern and reference the cloud sample file.

**Next Steps:** Create real cloud values files per environment and hook them into deployment scripts; wire the RabbitMQ/Cosmos credentials via Kubernetes Secrets/ESO so we can stand up KEDA ScaledObjects.

### Update - 2025-11-14 08:45 NZDT

**Summary:** Pre-created the RabbitMQ 4.2 cloud deployment assets so Phase 0 can execute quickly when we bring up managed clusters.

**Details:**
- Added `charts/external/rabbitmq/values-cloud.yaml` + README (Bitnami chart pinned to 4.2.0, TLS, Prometheus, Khepri flag).
- Documented the required Kubernetes Secrets in `docs/cloud/rabbitmq/rabbitmq-secrets.template.yaml` so operators can source credentials via ADR-0013.

**Next Steps:** When cloud environments are ready, copy the template, inject real secrets, and run the documented Helm command to stand up RabbitMQ before wiring Dapr/KEDA.

### Update - 2025-11-14 09:30 NZDT

**Summary:** Closed KEDA platform upgrade, created cloud autoscaling plan.

**Details:**
- Marked `plan/upgrade-keda-2.18-implementation-1.md` complete (Phases 1-4 done, KEDA 2.18.1 operational).
- Moved to `plan/done/`.
- Created `plan/keda-cloud-autoscaling-implementation-1.md` for cloud ScaledObjects work (MakeLine RabbitMQ scaler, TriggerAuthentication, Helm integration).

**Next Steps:** Cloud deployment work deferred. Focus on remaining Phase 1B foundations (CI/CD, infrastructure containers).
