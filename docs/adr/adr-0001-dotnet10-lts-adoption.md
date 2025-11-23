---
title: "ADR-0001: Adopt .NET 10 LTS for OrderService and Future .NET Services"
status: "Accepted"
date: "2025-11-02"
authors: "Red Dog Modernization Team"
tags: ["architecture", "decision", "dotnet", "lts", "modernization"]
supersedes: ""
superseded_by: ""
---

# ADR-0001: Adopt .NET 10 LTS for OrderService and Future .NET Services

## Status

**Accepted**

## Implementation

This ADR records the architectural decision to adopt .NET 10 LTS as the target framework for Red Dog’s .NET services.

Implementation details and current status (migration steps, test coverage, Docker updates, CI changes) are tracked in:

- `plan/orderservice-dotnet10-upgrade.md`
- `plan/modernization-strategy.md`
- Relevant `.claude/sessions/*` logs

As of 2025-11-23:

- All 13 `.csproj` files in this repo target `<TargetFramework>net10.0</TargetFramework>`.
- `global.json` pins the SDK to `10.0.100`.
- The .NET 10 SDK (`10.0.100`) is installed on the development environment used for this audit.

This ADR does not attempt to stay in sync with day-to-day implementation status beyond this high-level note.

## Context

Red Dog’s .NET services (notably OrderService and AccountingService) originally ran on .NET 6.0, which reached End-of-Life (EOL) on 2024-11-12. That created security and support risk for any long-lived demo or production-like deployment.

We needed to choose a forward .NET target for:

- OrderService and AccountingService (the .NET services being retained in the longer-term polyglot architecture).
- Short- to medium-term stability (3+ years of support).
- Alignment with:
  - Dapr 1.16+,
  - Ubuntu 24.04 container base images,
  - Teaching goals (showing “modern .NET” patterns, minimal hosting, OpenAPI, OpenTelemetry, etc.).

The polyglot target architecture (see `KI-REDDOG-ARCHITECTURE-001`) still expects some services to move to Go/Python/Node.js. This ADR covers an intermediate step: stabilising retained .NET services on a current LTS before language migrations.

### LTS vs STS

Microsoft’s support model distinguishes:

- **LTS (Long-Term Support)** – 3-year support window after GA.
- **STS (Standard-Term Support)** – 24-month support window after GA (STS now has 24 months of support, but is still not intended as a long-term production baseline). :contentReference[oaicite:0]{index=0}

At the time of this decision:

- **.NET 6** – EOL November 2024 (unsupported) ❌
- **.NET 7** – EOL May 2024 (unsupported) ❌
- **.NET 8** – LTS, EOL November 2026 (3-year window from GA) :contentReference[oaicite:1]{index=1}  
- **.NET 9** – STS, supported for ~24 months from GA, with EOL aligned to November 2026 under the updated STS policy :contentReference[oaicite:2]{index=2}  
- **.NET 10** – LTS, expected 3-year support window (to ~November 2028) as the next LTS after .NET 8. :contentReference[oaicite:3]{index=3}  

We wanted a framework that:

- Avoids being “almost EOL” immediately,
- Avoids needing another major migration in a short time,
- Matches the “latest stable LTS” stance we use elsewhere (Node.js, Dapr, etc.).

## Decision

Adopt **.NET 10 LTS** as the target framework for all Red Dog .NET services that remain .NET-based in the target architecture, specifically:

- OrderService
- AccountingService
- Supporting .NET background workers and test projects

Key points:

- .NET 10 LTS is the **baseline** for any .NET code we keep.
- New .NET services must also target .NET 10 unless a future ADR explicitly changes the baseline.
- This is an **intermediate step** in the modernization roadmap; later ADRs and plans describe which services move off .NET entirely to Go/Python/Node.js.

## Consequences

### Positive

- **POS-001 – Extended support horizon**  
  We get a ~3-year support window (to ~November 2028) instead of being constrained by .NET 8/9’s 2026 EOL. This reduces the likelihood of another major framework upgrade during the timeframe we want Red Dog to be a stable teaching/demo platform.

- **POS-002 – Performance and runtime improvements**  
  .NET 10 brings JIT and GC improvements over .NET 6 (and intermediate versions). Public benchmarks indicate single-digit to low double-digit percentage improvements in throughput and reduced GC pauses for typical web workloads. We expect modest but meaningful gains in latency and resource usage for OrderService/AccountingService. :contentReference[oaicite:4]{index=4}  

- **POS-003 – Security and support**  
  We remain on a supported, patched runtime with access to security fixes and CVEs for the full LTS window.

- **POS-004 – Modern development model**  
  We can standardise on:
  - Minimal hosting model,
  - Latest C# features available in .NET 10,
  - Native OpenAPI support (`Microsoft.AspNetCore.OpenApi`, Scalar UI),
  - Cleaner integration with OpenTelemetry and other modern tooling.

- **POS-005 – Container and OS alignment**  
  We align with Microsoft’s current container guidance by using Ubuntu 24.04–based .NET 10 images (e.g. `mcr.microsoft.com/dotnet/aspnet:10.0`), which have OS support that comfortably covers the .NET 10 lifecycle. :contentReference[oaicite:5]{index=5}  

- **POS-006 – Teaching value**  
  Workshops and labs can be positioned as “current LTS .NET” rather than “legacy .NET 6”, which should remain relevant for multiple academic years.

### Negative

- **NEG-001 – Ecosystem maturity risk**  
  As a relatively new LTS, .NET 10 may temporarily lag .NET 8/9 in NuGet package support at the moment of adoption. This risk is mitigated by:
  - Deferring package choices to libraries with clear 10.0 support,
  - Using staging environments to validate upgrades.

- **NEG-002 – Large version jump**  
  Migrating from .NET 6 to .NET 10 spans several major releases. Even with good compatibility, we need to review cumulative breaking changes across 7/8/9/10 and run a meaningful test suite.

- **NEG-003 – Learning curve**  
  The minimal hosting model and new C# features require the team (and workshop participants) to learn updated idioms compared to legacy .NET 6 templates.

- **NEG-004 – Testing and validation cost**  
  The migration requires dedicated time for:
  - Updating project files and Dockerfiles,
  - Adjusting package versions,
  - Running and extending tests to validate behaviour.

- **NEG-005 – Early LTS churn**  
  Early in an LTS’s life, there is some risk of runtime or tooling issues that are resolved in the first few servicing updates. We mitigate this by:
  - Using CI builds and staging deployments,
  - Applying servicing updates promptly.

## Alternatives Considered

### .NET 8 LTS (Current LTS at decision time)

- **ALT-001 – Description**  
  Upgrade to .NET 8 LTS (EOL November 2026), which is mature and widely adopted.

- **ALT-002 – Why not chosen**  
  - Only ~2 years of remaining support compared to ~3 years for .NET 10.
  - Would likely force another upgrade to .NET 10/12 within the same timeframe we want Red Dog to be stable.
  - Does not align with the strategy of “latest LTS baseline” once .NET 10 is available and supported by our dependencies.

### .NET 9 STS

- **ALT-003 – Description**  
  Upgrade to .NET 9 STS. Under the updated STS policy, .NET 9 receives ~24 months of support, with an EOL currently aligned to November 2026 alongside .NET 8. :contentReference[oaicite:6]{index=6}  

- **ALT-004 – Why not chosen**  
  - STS is **not** the right baseline for long-lived production/demo workloads in this project.
  - We would still want to move to an LTS (.NET 10 or later) for long-term stability.
  - Choosing .NET 9 would add churn (STS → LTS) without clear benefit over going directly to .NET 10.

### Stay on .NET 6 with updated dependencies

- **ALT-005 – Description**  
  Remain on .NET 6 and simply update NuGet packages to their latest .NET 6–compatible versions.

- **ALT-006 – Why not chosen**  
  - .NET 6 is EOL and no longer receives security patches.
  - Keeping an EOL runtime contradicts the project’s teaching goals around secure, modern deployments.
  - Does not reduce technical debt; just postpones it.

### Staged migration (.NET 6 → 8 → 10)

- **ALT-007 – Description**  
  First move from .NET 6 → 8, then later from .NET 8 → 10.

- **ALT-008 – Why not chosen**  
  - Implies two major upgrades (extra cost and risk) instead of one.
  - Adds complexity to workshops and documentation (“we used to be on 8, now on 10”).
  - Direct 6 → 10 migration is feasible and more cost-effective.

## Notes on Implementation (High-Level)

The detailed execution (phases, commands, Docker changes, CI adjustments, test baselines) is maintained in the plan and session files, not here. At a high level:

- The repo is standardised on:
  - `global.json` → .NET SDK `10.0.100`,
  - All `.csproj` → `<TargetFramework>net10.0</TargetFramework>`.
- Container images are moving to `.NET 10` Ubuntu 24.04–based images.
- OpenTelemetry, Scalar/OpenAPI, and other modern features are planned and/or implemented on top of this baseline as described in:
  - `ADR-0011` (OpenTelemetry Observability Standard),
  - Web API standard and related Knowledge Items.

This ADR should be read as the **canonical record of the runtime choice and its rationale**. Operational details and current progress live in plans and sessions.
