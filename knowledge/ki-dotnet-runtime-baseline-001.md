# KI-DOTNET-RUNTIME-BASELINE-001: .NET Runtime Selection Policy

## Summary

This Knowledge Item defines how Red Dog selects and manages .NET runtime versions for services, background workers, and related tools.  
It establishes a single LTS baseline for production workloads, clarifies when STS releases may be used, and describes how runtime choices interact with containers, CI, and modernization plans.

It applies to all .NET code in Red Dog (services, background workers, utilities, test projects) unless a future ADR explicitly defines an exception.

## Applies To

- Red Dog .NET services (OrderService, AccountingService, LoyaltyService, MakeLineService, ReceiptGenerationService, VirtualWorker, etc.)
- .NET background workers and tools in this repo
- .NET test projects
- Container images and CI pipelines that build/run .NET code

---

## Key Facts

1. **FACT-001:** Red Dog uses a **single LTS .NET version as the baseline** for all production .NET workloads at any given time.  
   - The current baseline is **.NET 10 LTS**, as defined by ADR-0001.

2. **FACT-002:** **LTS releases** are the default choice for production and long-lived demo workloads.  
   **STS releases** are allowed only for short-lived experiments and spikes, not as a long-term baseline.

3. **FACT-003:** Framework upgrades are planned to **skip intermediate versions** where feasible (e.g. .NET 6 → .NET 10 directly) rather than chaining multiple LTS/STS hops.

4. **FACT-004:** The chosen .NET LTS baseline determines the **container base images** for .NET services (e.g. `mcr.microsoft.com/dotnet/aspnet:<LTS>` and `mcr.microsoft.com/dotnet/sdk:<LTS>` on Ubuntu 24.04 for .NET 10).

5. **FACT-005:** Runtime decisions are recorded via ADRs (e.g. ADR-0001) and mirrored in:
   - `global.json` SDK version
   - `<TargetFramework>` (or `<TargetFrameworks>`) in `.csproj`
   - Dockerfiles and CI pipelines

6. **FACT-006:** Framework upgrades are treated as **deliberate modernization work**, not incidental refactors, and require a test baseline and a rollback strategy.

---

## Constraints

1. **CON-001:** No new **production** .NET service may be introduced on an **EOL or near-EOL** .NET version.  
   - “Near-EOL” means a support horizon that would expire before the expected lifetime of the service as a teaching/demo asset.

2. **CON-002:** STS releases **must not** be used as the primary runtime for long-lived production or demo services, unless a dedicated ADR explicitly justifies the exception.

3. **CON-003:** All production .NET projects in this repo (apps, workers, tests) **must target the current baseline LTS** unless:
   - They are part of a controlled migration (with a plan), or
   - An ADR explicitly documents why a different TFM is required.

4. **CON-004:** Container base images used for .NET services **must align** with the chosen LTS baseline (same major version).  
   Mixing incompatible runtime/container combinations (e.g. `net10.0` app on a `aspnet:8.0` base image) is not allowed.

5. **CON-005:** Framework upgrades **must not** be performed without:
   - A defined testing strategy (unit/integration/smoke tests), and
   - A documented rollback path (e.g. Git tag or branch for the previous baseline).

6. **CON-006:** `global.json` must pin the SDK version used for builds to a version compatible with the chosen LTS baseline.  
   CI and local development pipelines should respect this pin by default.

---

## Patterns and Recommendations

1. **PAT-001 – Choosing a new runtime baseline**

   When a new .NET LTS is released:

   - Evaluate its support window and compare to the current baseline.
   - Verify that key dependencies (Dapr SDK, OpenTelemetry, test frameworks, essential NuGet packages) support the new LTS.
   - Decide whether to adopt the new LTS via a dedicated ADR (e.g. “ADR-00XX: Adopt .NET 12 LTS”).
   - Prefer a direct upgrade from the current baseline to the new LTS, skipping intermediate versions where safe.

2. **PAT-002 – Creating a new .NET service**

   - Target the **current LTS baseline** (currently `.NET 10`) from the start.
   - Use the same container base images, SDK version (`global.json`), and CI patterns as existing services.
   - If the repo is mid-migration, follow the existing baseline and rely on the upgrade plan rather than introducing a new TFM.

3. **PAT-003 – Upgrading existing services**

   - Capture the decision in an ADR (if changing baselines) and the concrete steps in a plan (e.g. `plan/orderservice-dotnetXX-upgrade.md`).
   - Sequence work as:
     1. Establish/verify test baseline.
     2. Update `global.json` and project TFMs.
     3. Update NuGet packages and fix compile issues.
     4. Update Dockerfiles and CI.
     5. Run tests and performance sanity checks.
   - Use Git tags or branches for the pre-upgrade state as the rollback anchor.

4. **PAT-004 – Handling STS releases**

   - Use STS only for:
     - Short-lived spikes,
     - Local experiments,
     - Prototypes that are not part of the main Red Dog teaching/demo path.
   - If STS features are required for a future direction, document this in an ADR and plan to converge to the next LTS once available.

5. **PAT-005 – CI and tooling alignment**

   - Ensure CI pipelines use the same SDK version as `global.json`.
   - Avoid ad hoc `dotnet` installations in CI that diverge from the repo-pinned SDK.
   - Keep `Directory.Build.props` free of hidden TFM overrides unless explicitly documented.

---

## Risks and Open Questions

1. **RISK-001 – Framework drift across services**  
   If some services lag behind the agreed LTS while others advance, the system may accumulate:
   - Multiple incompatible TFMs,
   - More complex CI and container matrix,
   - Confusing teaching story.

2. **RISK-002 – Over-reliance on STS**  
   Using STS for “just one more” production scenario risks:
   - Surprise EOL pressure,
   - More frequent migration work,
   - Mixed messaging in teaching material about best practices.

3. **RISK-003 – Delayed upgrades**  
   Deferring upgrades for too long can:
   - Leave services on EOL runtimes,
   - Increase the size and risk of future migrations,
   - Make it harder to adopt ecosystem features (OpenTelemetry, Scalar, new Dapr SDKs).

4. **OPEN-001 – Future LTS cadence and container OS choices**  
   - Future ADRs may choose different base OS images (e.g. newer Ubuntu releases or other distros) while keeping the LTS rule intact.
   - This KI does not fix the OS choice; it fixes the principle that runtime + container baseline move together.

5. **OPEN-002 – Multi-targeting scenarios**  
   - Some tools or libraries may benefit from multi-targeting (e.g. `netstandard2.1` + `net10.0`).
   - Allowing multi-targeting for libraries is acceptable as long as:
     - The primary runtime for Red Dog services remains the agreed LTS, and
     - Any additional TFMs are documented and do not complicate deployment.

---

## Sources and Provenance

- **ADR-0001 – Adopt .NET 10 LTS for OrderService and Future .NET Services**  
- `plan/orderservice-dotnet10-upgrade.md` (implementation plan for the .NET 10 upgrade)  
- `plan/modernization-strategy.md` (overall modernization phases and runtime goals)  
- Microsoft .NET Support Policy and lifecycle documentation (LTS vs STS)  
