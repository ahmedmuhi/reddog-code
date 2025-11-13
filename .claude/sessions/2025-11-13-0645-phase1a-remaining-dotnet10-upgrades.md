# Session: Phase 1A - Remaining .NET 10 Service Upgrades

**Started:** 2025-11-13 06:45 NZDT
**Status:** In Progress

## Overview

This session continues Phase 1A modernization work - completing the remaining 4 .NET 6.0 to .NET 10 upgrades. Following the successful upgrade of 5/9 services in the previous session, we now focus on the remaining services.

**Strategic Context:**
- Phase 1A: Upgrade ALL 9 .NET projects to .NET 10 LTS before polyglot migrations (Phase 1B)
- Previous session completed: OrderService, ReceiptGenerationService, AccountingService, AccountingModel, Bootstrapper
- This session targets: MakeLineService, LoyaltyService, VirtualWorker, VirtualCustomers

**Current State (Session Start):**
- ✅ .NET 10 SDK 10.0.100 (GA) installed
- ✅ kind cluster operational with optimized resources
- ✅ 5/9 services upgraded to .NET 10 (56% complete)
- ✅ Resource optimization complete (82% CPU reduction achieved)
- ✅ WSL2 memory optimization complete (4GB limit working well)
- ✅ Upgrade automation scripts ready to use

## Goals

### Primary Goals

1. **Upgrade MakeLineService to .NET 10**
   - Update target framework: net6.0 → net10.0
   - Update NuGet package dependencies to .NET 10-compatible versions
   - Build, test, and deploy to kind cluster
   - Verify 2/2 pod status (app + Dapr sidecar)

2. **Upgrade LoyaltyService to .NET 10**
   - Update target framework: net6.0 → net10.0
   - Update NuGet package dependencies
   - Build, test, and deploy
   - Verify pub/sub subscription working

3. **Upgrade VirtualWorker to .NET 10**
   - Update target framework: net6.0 → net10.0
   - Update NuGet package dependencies
   - Build, test, and deploy
   - Verify worker completing orders

4. **Upgrade VirtualCustomers to .NET 10**
   - Update target framework: net6.0 → net10.0
   - Update NuGet package dependencies
   - Build, test, and deploy
   - Verify customer order generation

### Secondary Goals

5. **Use Upgrade Automation Scripts**
   - Leverage upgrade-preflight.sh for pre-checks
   - Use upgrade-build-images.sh for image builds
   - Run upgrade-validate.sh for post-deployment validation
   - Document any script improvements needed

6. **Maintain Deployment Health**
   - Ensure all services remain healthy during upgrades
   - Verify 2/2 container counts (MANDATORY)
   - Test end-to-end pub/sub flow after each upgrade
   - Monitor resource usage stays within optimized limits

7. **Update Documentation**
   - Document any service-specific upgrade issues
   - Update modernization-strategy.md with completion status
   - Note any improvements to upgrade procedures

## Prerequisites Verified

✅ **.NET 10 SDK:** 10.0.100 (GA) - verified with `dotnet --version`
✅ **kind Cluster:** Operational - reddog-local cluster running
✅ **Dapr:** 1.16.2 running in Kubernetes mode
✅ **Previous Services:** 5/9 upgraded and healthy
✅ **Resource Optimization:** Complete (3.5 vCPU total, 4GB WSL2 memory)
✅ **Automation Scripts:** Available in `scripts/` directory
✅ **Upgrade Guide:** `docs/guides/dotnet10-upgrade-procedure.md` ready

## Reference Documents

- **Upgrade Guide:** `docs/guides/dotnet10-upgrade-procedure.md` (652 lines, comprehensive)
- **Modernization Strategy:** `plan/modernization-strategy.md` (Phase 1A section)
- **Previous Session:** `.claude/sessions/2025-11-11-1541-phase1a-orderservice-dotnet10-upgrade.md`
- **Automation Scripts:** `scripts/upgrade-preflight.sh`, `scripts/upgrade-validate.sh`, `scripts/upgrade-build-images.sh`

## Progress

### MakeLineService Upgrade
- Status: Not started
- Notes: TBD

### LoyaltyService Upgrade
- Status: Not started
- Notes: TBD

### VirtualWorker Upgrade
- Status: Not started
- Notes: TBD

### VirtualCustomers Upgrade
- Status: Not started
- Notes: TBD

---

## Session Updates

*(Updates will be added here as work progresses)*

---

### Update - 2025-11-13 07:56 NZDT

**Summary**: Completed bash script analysis and setup-local-dev.sh resource optimization updates

**Activities**:
1. Reviewed all 7 bash scripts in repository (1,094 lines total)
2. Identified that 4 upgrade scripts (845 lines) capture all learnings from previous session
3. Found setup-local-dev.sh was missing resource optimization checks
4. Created comprehensive plan and implemented updates to setup-local-dev.sh
5. Updated values-local.yaml.sample with Dapr sidecar resource configuration

**Git Changes**:
- Modified: scripts/setup-local-dev.sh (+108 lines: 158→266)
- Modified: values/values-local.yaml.sample (+11 lines)
- Modified: .claude/sessions/2025-11-11-1541-phase1a-orderservice-dotnet10-upgrade.md
- Added: .claude/sessions/2025-11-13-0645-phase1a-remaining-dotnet10-upgrades.md
- Current branch: master (commit: eefe3da)

**Todo Progress**: 7 completed, 0 in progress, 0 pending
- ✓ Completed: Review and analyze all bash scripts in repository
- ✓ Completed: Document findings and recommendations
- ✓ Completed: Create plan for setup-local-dev.sh updates
- ✓ Completed: Add WSL2 memory configuration check to setup-local-dev.sh
- ✓ Completed: Add Dapr sidecar resource validation to setup-local-dev.sh
- ✓ Completed: Update values-local.yaml.sample with Dapr resources
- ✓ Completed: Test the updated setup script

**Bash Script Analysis Results**:

**Excellent Scripts (4/7 - Capture all learnings):**
1. upgrade-preflight.sh (139 lines) - Pre-flight checks, prevents 4 recurring issues
2. upgrade-validate.sh (200 lines) - MANDATORY 2/2 container validation
3. upgrade-build-images.sh (206 lines) - Builds all tags at once, prevents stale images
4. upgrade-dotnet10.sh (300 lines) - Complete orchestrator, uses all other scripts

**Scripts Updated (1/7):**
5. setup-local-dev.sh (158→266 lines) - NOW includes resource optimization checks

**Simple Utility Scripts (2/7):**
6. status-local-dev.sh (58 lines) - Status checker, works well
7. teardown-local-dev.sh (33 lines) - Cluster cleanup, works well

**Key Finding**: The upgrade automation scripts directly implement all learnings from the previous session and are production-ready.

**Changes to setup-local-dev.sh**:

**1. Added WSL2 Memory Configuration Check (after line 35)**
- Detects WSL2 by checking `/proc/version` for "microsoft"
- Checks for .wslconfig at `C:\Users\<username>\.wslconfig`
- If missing: Warns about VMMEMWSL consuming 14GB+ RAM
- Provides complete .wslconfig template with 4GB limit
- Interactive pause: User must press Enter to continue
- If exists: Displays configured memory limit

**2. Added Dapr Sidecar Resource Validation (after line 90)**
- Parses values-local.yaml for `services.common.dapr.resources.limits.cpu`
- If missing: CRITICAL ERROR explaining 2000m default = 12 vCPU for 6 sidecars
- If set to 2000m: CRITICAL ERROR warning about performance impact
- Interactive prompt: User must type 'y' to continue if misconfigured
- If reasonable (≤500m): Shows green checkmark
- Provides complete configuration template in error messages

**3. Updated values-local.yaml.sample**
- Added `services.common.dapr.resources` section
- Included detailed comments:
  * "Without these explicit limits, Dapr defaults to 2000m (2 vCPU) per sidecar!"
  * "With 6 services using Dapr, this results in 12 vCPU consumed by sidecars alone."
  * "This will severely degrade performance on typical development machines."
- Ensures new users get correct configuration from the start

**Testing Results**:
- ✅ Bash syntax validation passed
- ✅ WSL2 detection working correctly (detected WSL2)
- ✅ .wslconfig check working (file found, 4GB limit extracted)
- ✅ Script now prevents the resource issues that "crippled" the machine in previous session

**Benefits**:
- Prevents 12 vCPU allocation to Dapr sidecars (82% reduction opportunity)
- Prevents 14GB WSL2 memory consumption (71% reduction opportunity)
- Clear, actionable warnings at the right moment (before cluster creation)
- Non-blocking for users with correct configuration
- Maintains backward compatibility

**Issues Encountered**: None

**Solutions Implemented**:
- WSL2 check: Non-blocking warning with interactive pause
- Dapr check: Hard failure with interactive prompt (y/N) to prevent accidents

---

### Update - 2025-11-13 13:18 NZDT (VirtualWorker Upgrade Complete)

**Summary:** VirtualWorker now runs on .NET 10 with minimal hosting, OpenTelemetry, and ADR-0005 health probes; container + Helm assets aligned with GA toolchains.

**Activities:**
1. Retargeted `RedDog.VirtualWorker.csproj` to `net10.0`, enabled nullable/implicit usings, and refreshed Microsoft.Extensions/Dapr dependencies.
2. Replaced `Startup.cs` with a minimal `Program.cs` that wires options, OpenTelemetry, health checks, and Dapr client DI.
3. Swapped Dockerfile base images to `sdk:10.0.100` / `aspnet:10.0`, added explicit restore/publish, and kept `global.json` intact.
4. Updated Helm template + values with ADR-0005 probes, env vars (`ASPNETCORE_URLS`, `DAPR_HTTP_PORT`), and consistent labels/app IDs.
5. Ran `./scripts/upgrade-build-images.sh VirtualWorker`, `helm upgrade ... --wait`, `./scripts/upgrade-validate.sh VirtualWorker`, and worker smoke tests; all passed.

**Key Files:**
- `RedDog.VirtualWorker/Program.cs`, `RedDog.VirtualWorker/RedDog.VirtualWorker.csproj`, `RedDog.VirtualWorker/Dockerfile`
- `charts/reddog/templates/virtual-worker.yaml`, `values/values-local.yaml*`
- `scripts/upgrade-validate.sh` (console-worker awareness)

**Status:** ✅ VirtualWorker deployed (2/2 pods, Dapr subscriptions active)

---

### Update - 2025-11-13 14:10 NZDT (VirtualCustomers Upgrade + Validation)

**Summary:** Final Phase 1A workload upgraded. VirtualCustomers now packages asynchronous worker logic with GA SDK/runtime, Helm integration, and enhanced automation.

**Activities:**
1. Upgraded csproj + Program.cs to .NET 10, added OpenTelemetry, DI-hosted `VirtualCustomersWorker`, and removed Serilog/blocking code.
2. Ensured Dockerfile restores before publish and uses GA images without deleting `global.json`.
3. Added `Configuration/VirtualCustomerOptions.cs`, Helm annotations, env-driven options, and exec-based probes (`pgrep dotnet`) for background-worker health.
4. Fixed `scripts/upgrade-build-images.sh` to rely on Docker exit codes/`--progress plain`; hardened `scripts/upgrade-validate.sh` for console workloads and probe-threshold logic.
5. Ran `./scripts/upgrade-build-images.sh VirtualCustomers`, `helm upgrade ... --wait`, `./scripts/upgrade-validate.sh VirtualCustomers`, and `scripts/run-virtualcustomers-smoke.sh`; all successful (new pod clean, 0 restarts).

**Artifacts / Logs:**
- `/tmp/virtualcustomers-smoke.log`
- Helm revision 13 (kind cluster)
- Session references to GA image digests for sdk/runtime

**Status:** ✅ VirtualCustomers deployed (2/2 pods; Dapr + smoke validation green)

---

### Update - 2025-11-13 14:35 NZDT (Documentation & Planning Wrap-Up)

**Summary:** Phase 1A officially closed. Implementation guides moved to `plan/done/`, modernization strategy + session log updated, and repo ready for commit.

**Activities:**
1. Marked VirtualWorker & VirtualCustomers implementation guides as completed, added completion summaries, and moved both to `plan/done/`.
2. Updated `plan/modernization-strategy.md` to reflect 9/9 upgrades complete, refreshed progress table, documented prevention outcomes, and corrected guide paths.
3. Logged upgrade/validation details in this session file, including automation script improvements.
4. Prepared for commit: ensure helm/templates, values, scripts, plans, and code changes staged together.

**Next Steps:**
- Commit + push VirtualWorker/VirtualCustomers upgrades + doc updates.
- Begin planning for Phase 1B (polyglot migrations) leveraging new GA baselines.

### Update - 2025-11-13 10:59 NZDT

**Summary**: MakeLineService upgrade completed end-to-end with production-grade fixes, new options/DI patterns, successful validation + smoke tests, and automation hardening.

**Activities**:
1. Updated `RedDog.MakeLineService` project to net10.0, enabled nullable + implicit usings, adopted C# 14, replaced Serilog/Swashbuckle with OpenTelemetry + Scalar, and wired new strongly-typed options, Dapr-sidecar health checks, and programmatic pub/sub subscription (`/dapr/makeline/orders`).
2. Introduced `Configuration/DaprOptions.cs`, `Configuration/CorsOptions.cs`, `HealthChecks/DaprSidecarHealthCheck.cs`, and the new `IMakelineQueueProcessor` service to encapsulate Redis/Dapr operations used by both controllers and Dapr subscriptions (primary-constructor DI everywhere).
3. Fixed Docker + global.json guardrails for GA .NET 10, modernized the Helm chart (env vars + ADR-0005 probe paths), and repaired `scripts/upgrade-build-images.sh` so BuildKit output stops causing false failures.
4. Corrected `charts/reddog/templates/dapr-components/statestore-makeline.yaml` (scopes now align with `makelineservice` app-id) and improved `scripts/upgrade-validate.sh` label handling + numeric parsing so deployment health checks work reliably.
5. Executed full automation: `upgrade-build-images.sh`, `helm upgrade`, `kubectl rollout status`, `upgrade-validate.sh` (pass), then ran the functional smoke test per the upgrade guide (`kubectl port-forward svc/makelineservice 15200:80` + `curl /orders/Redmond` → HTTP 200 / empty array).

**Observations / Fixes**:
- Program.cs now defers DAPR_HTTP_PORT validation until configuration is available, auto-defaults to 3500 for Development, and binds CORS via `IOptions<CorsOptions>` instead of manual snapshots.
- Validation script previously assumed label `app=<service>-service`; updated to kebab-case converter so MakeLineService pods resolve correctly, and replaced brittle `grep|wc` math with `grep -ci`.
- Dapr state component used scoped `make-line-service`; since app-id is `makelineservice`, state requests via HTTP saw `ERR_STATE_STORE_NOT_CONFIGURED`. Helm template now emits the configured app-id to keep component scopes and pods aligned.
- Functional smoke testing uncovered port contention on 5200 (host). Rebinding to 15200 keeps local port forwards deterministic while verifying REST paths.

**Artifacts / Evidence**:
- `.image-manifest-MakeLineService.txt` refreshed with all 4 tags (net10, net10-test, local, latest).
- `./scripts/upgrade-validate.sh MakeLineService` (revamped) passes with zero warnings.
- `curl http://localhost:15200/orders/Redmond` returns `[]`, confirming GET pipeline works against Redis state store after scope fix.

**Next Steps**:
- Update modernization docs (Phase 1A progress) and prepare commit with all MakeLineService + tooling changes.
- Repeat net10 upgrade workflow for LoyaltyService once MakeLineService changes are merged.

### Update - 2025-11-13 11:13 NZDT

**Summary**: Captured upgrade lessons in docs, added helper tooling, and created official MakeLine smoke-test script so future services avoid today’s pitfalls.

**Activities**:
1. Updated `docs/guides/dotnet10-upgrade-procedure.md` with automation guardrails (no stdout parsing, defer env validation, naming alignment) and documented the new port-forward helper usage.
2. Added a deployment naming matrix + port-forward guidance to `plan/modernization-strategy.md`, so Helm/Kubernetes/Dapr identifiers stay synchronized.
3. Introduced `scripts/find-open-port.sh` plus enhanced `scripts/upgrade-validate.sh` to select free ports automatically and run a Dapr state read/write round-trip (component map currently covers MakeLine + Loyalty).
4. Created `scripts/run-dapr-makeline-smoke.sh` (curling both direct HTTP and Dapr service invocation), updated all planning docs to reference it, and ran the script successfully against kind cluster (`storeId=Redmond`).

**Gaps Addressed**:
- Automation regressions from BuildKit log parsing.
- Missing naming guidance that caused inconsistent appIds.
- Absent Dapr smoke script referenced by the testing plan.
- Port contention for smoke tests on WSL/kind environments.
- All checks use simple grep/sed parsing (no external dependencies)

**Next Steps**:
1. Commit these changes
2. Begin .NET 10 upgrades for remaining 4 services:
   - LoyaltyService ✅ (completed 2025-11-13 11:40 NZDT)
   - VirtualWorker
   - VirtualCustomers

### Update - 2025-11-13 11:45 NZDT

**Summary**: LoyaltyService upgraded to .NET 10, deployed via Helm, validated with enhanced automation, and smoke-tested over both HTTP and Dapr invocation paths.

**Activities**:
1. Applied the same C# Pro patterns as MakeLineService (nullable net10.0 csproj, options binding, OpenTelemetry, OpenAPI/Scalar, `/dapr/loyalty/orders` endpoint, ADR-0005 health checks) and removed Startup.cs/Serilog.
2. Added new `Configuration/` option classes, `ILoyaltyStateService` for Redis/Dapr access with ETag retry logic, and a Dapr sidecar health check.
3. Updated Dockerfile to GA SDK/runtime images, Helm chart to `/healthz|/livez|/readyz` + env vars, and corrected `reddog.state.loyalty` scope to `makelineservice`.
4. Built images (`./scripts/upgrade-build-images.sh LoyaltyService`), rolled back stuck Helm rev, re-upgraded, then ran `./scripts/upgrade-validate.sh LoyaltyService` (state round-trip), followed by manual HTTP + Dapr curls using `scripts/find-open-port.sh`.

**Artifacts / Evidence**:
- `.image-manifest-LoyaltyService.txt`
- `./scripts/upgrade-validate.sh LoyaltyService` output in terminal history (state store check passed)
- `/tmp/loyalty-http.json` & `/tmp/loyalty-dapr.json` capture HTTP 200 payloads.

**Next Steps**:
- Archive LoyaltyService plan under `plan/done/`
- Update modernization strategy + documentation with new guardrails
- Prepare VirtualWorker/VirtualCustomers plans with GA-image + port-forward guidance.

### Update - 2025-11-13 12:22 NZDT

**Summary**: VirtualWorker upgraded to .NET 10 (minimal hosting, OpenTelemetry, ADR-0005), container images rebuilt on GA tags, deployed via Helm, and validated + smoke-tested using the `/orders` endpoint pattern.

**Activities**:
1. Upgraded csproj, Dockerfile, Program.cs, and controllers/services to match the modern MakeLine/Loyalty patterns (options binding, Dapr client usage, health checks, OpenAPI/Scalar). Removed Startup.cs, Serilog, and ProbesController.
2. Added `Configuration/*`, `Services/IVirtualWorkerService.cs`, and `HealthChecks/DaprSidecarHealthCheck.cs`; Helm chart now sets env vars and `/healthz|/livez|/readyz` probes for the worker deployment.
3. Built/uploaded images via `./scripts/upgrade-build-images.sh VirtualWorker` (GA SDK/runtime), rolled Helm back to rev 8 when it got stuck, then re-upgraded to rev 11.
4. Ran `./scripts/upgrade-validate.sh VirtualWorker` (2/2 pods, state round trip) and manual smoke tests: `POST /orders` via service ingress and `POST /v1.0/invoke/virtualworker/method/orders` via Dapr—both returned 200.

**Artifacts / Evidence**:
- `.image-manifest-VirtualWorker.txt`
- `./scripts/upgrade-validate.sh VirtualWorker` output
- `/tmp/vworker-http.json` and `/tmp/vworker-dapr.json` showing HTTP 200.

**Next Steps**:
- Archive VirtualWorker plan once documentation updates are finished
- Update modernization strategy to mark VirtualWorker as ✅ when we commit
- Begin VirtualCustomers upgrade after committing these changes.
