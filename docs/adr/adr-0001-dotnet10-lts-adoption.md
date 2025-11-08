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

## Context

Red Dog's .NET services currently run on .NET 6.0, which reached End-of-Life (EOL) on November 12, 2024. This poses significant security and support risks for production deployments. The modernization effort requires selecting a target .NET version for OrderService and AccountingService (the two .NET services being retained in the polyglot architecture).

**Key Constraints:**
- .NET 6.0 is EOL (no security patches)
- Services must support 3+ years of production use
- Target framework must align with Red Dog's teaching and demonstration objectives
- Must support Dapr 1.16 integration
- Docker base images must use Ubuntu 24.04 (Microsoft's new default as of October 30, 2025)
- Cannot use .NET Native AOT due to Dapr sidecar architecture requirements

**LTS vs STS Context:**
- **LTS (Long-Term Support)**: 3-year support window, production-grade stability
- **STS (Standard-Term Support)**: 18-month support window, latest features

**Available Options (as of November 2025):**
- .NET 6: EOL November 2024 ❌
- .NET 7: EOL May 2024 ❌
- .NET 8: LTS, EOL November 2026 (2 years remaining)
- .NET 9: STS, EOL May 2026 (6 months remaining)
- .NET 10: LTS, EOL November 2028 (3 years remaining) ✅

## Decision

**Adopt .NET 10 LTS** as the target framework for all Red Dog .NET services (OrderService, AccountingService).

**Rationale:**
- **Maximum Support Window**: 3 years (November 2028 EOL) vs .NET 8's 2 years or .NET 9's 6 months
- **Avoid Dual Migrations**: Upgrading to .NET 8 now would require another upgrade to .NET 10 or .NET 12 within 2 years
- **LTS Alignment**: Matches project strategy of targeting latest LTS versions (Node.js 24 LTS, Vue 3.5, Dapr 1.16)
- **Performance Gains**: .NET 10 includes JIT improvements (5-15% faster), GC enhancements (8-20% lower pause times), stack allocation optimizations
- **Modern Patterns**: Minimal hosting model, C# 14 features, native OpenAPI support (replaces Swashbuckle)
- **Ubuntu 24.04 Support**: Microsoft changed default base images to Ubuntu 24.04 "Noble Numbat" (October 30, 2025), aligning with .NET 10 lifecycle
- **Teaching Value**: Demonstrates latest LTS practices, future-proofs demo code for instructors

## Consequences

### Positive

- **POS-001**: **Extended Support**: 3-year support window until November 2028 eliminates need for intermediate upgrades
- **POS-002**: **Performance Improvements**: 5-15% faster API response times (JIT + GC), 10-20% reduced memory allocation, 8-20% lower GC pause times for order processing workloads
- **POS-003**: **Security Updates**: Access to latest .NET security patches and CVE fixes for 3 years
- **POS-004**: **Modern Development Experience**: C# 14 features (field-backed properties, null-conditional assignment, extension blocks), minimal hosting model, improved developer productivity
- **POS-005**: **Native OpenAPI Support**: Built-in `Microsoft.AspNetCore.OpenApi` eliminates Swashbuckle dependency, better performance and integration
- **POS-006**: **Ubuntu Base Image Alignment**: Default `mcr.microsoft.com/dotnet/aspnet:10.0` uses Ubuntu 24.04 with longer support than .NET release cycles
- **POS-007**: **Teaching Relevance**: Demonstrates current best practices for LTS selection, remains relevant for 3+ years of instructor-led workshops
- **POS-008**: **Ecosystem Maturity Timeline**: Waiting until January 2026 (GA + 2 months) allows NuGet package ecosystem to stabilize

### Negative

- **NEG-001**: **Ecosystem Maturity Risk**: .NET 10 released November 2025; some NuGet packages may not have updated versions immediately (mitigated by January 2026 implementation timeline)
- **NEG-002**: **Breaking Changes**: Migration from .NET 6 → 10 spans 4 major versions (6→7→8→9→10), requiring review of breaking changes across all versions
- **NEG-003**: **Team Learning Curve**: Minimal hosting model and C# 14 features require developer upskilling (mitigated by thorough documentation in `plan/orderservice-dotnet10-upgrade.md`)
- **NEG-004**: **Testing Burden**: 10 working days estimated for full upgrade, testing, and validation per service
- **NEG-005**: **Temporary Instability**: First 2 months post-.NET 10 GA may have undiscovered runtime bugs (mitigated by staging environment testing)

## Alternatives Considered

### .NET 8 LTS (Current LTS)

- **ALT-001**: **Description**: Upgrade to .NET 8 LTS (EOL November 2026), currently mature with stable ecosystem
- **ALT-002**: **Rejection Reason**: Only 2 years of remaining support (vs 3 years for .NET 10). Would require another major upgrade in 2026-2027, doubling migration effort and cost. Not strategically aligned with long-term modernization goals.

### .NET 9 STS (Latest Non-LTS)

- **ALT-003**: **Description**: Upgrade to .NET 9 STS (EOL May 2026), available immediately with mature package ecosystem
- **ALT-004**: **Rejection Reason**: Only 6 months of remaining support. STS releases are not recommended for production systems. Would force immediate upgrade to .NET 10 or .NET 11, creating unnecessary churn and risk.

### Stay on .NET 6 with Updated Dependencies

- **ALT-005**: **Description**: Remain on .NET 6, update NuGet packages to latest .NET 6-compatible versions
- **ALT-006**: **Rejection Reason**: .NET 6 reached EOL November 12, 2024. No security patches available, creating unacceptable security risk. Does not address modernization goals or technical debt reduction.

### Staged Migration (.NET 6 → .NET 8 → .NET 10)

- **ALT-007**: **Description**: Migrate to .NET 8 LTS first, then upgrade to .NET 10 in 12-18 months
- **ALT-008**: **Rejection Reason**: Double migration effort (20 working days vs 10 working days for direct migration). Team fatigue from multiple upgrades. .NET 8 EOL in November 2026 provides minimal breathing room. Direct .NET 6 → 10 migration more cost-effective.

## Implementation Notes

- **IMP-001**: **Timeline**: Wait until January 2026 (GA + 2 months) before implementation to allow NuGet package ecosystem to mature. This provides buffer for critical bug fixes and package compatibility updates.
- **IMP-002**: **Migration Strategy**: Follow 8-phase implementation plan documented in `plan/orderservice-dotnet10-upgrade.md` (Pre-Upgrade Analysis → Framework Upgrade → Minimal Hosting Model → Code Modernization → Testing → Docker Updates → Documentation → Release)
- **IMP-003**: **Rollback Plan**: Maintain .NET 6 baseline tag (`orderservice-net6-baseline`) for emergency rollback. Use blue/green deployment or canary releases for production migration.
- **IMP-004**: **Success Criteria**: Zero critical/high NuGet vulnerabilities (`dotnet list package --vulnerable`), performance equal or better than .NET 6 baseline (P95 latency, memory usage), Docker image size < 250 MB
- **IMP-005**: **Dockerfile Changes**: Use default `mcr.microsoft.com/dotnet/aspnet:10.0` runtime image (Ubuntu 24.04 Noble Numbat), `mcr.microsoft.com/dotnet/sdk:10.0` build image
- **IMP-006**: **Monitoring**: Track upgrade progress in `.claude/sessions/` logs, document breaking changes and resolutions in session files

## References

- **REF-001**: Related Plan: `plan/orderservice-dotnet10-upgrade.md` (comprehensive 8-phase implementation guide)
- **REF-002**: Related Plan: `plan/modernization-strategy.md` (Phase 1: .NET Modernization)
- **REF-003**: Microsoft Docs: [What's new in .NET 10](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview)
- **REF-004**: Microsoft Docs: [Breaking changes in .NET 10](https://learn.microsoft.com/en-us/dotnet/core/compatibility/10.0)
- **REF-005**: Microsoft Docs: [.NET support policy](https://dotnet.microsoft.com/platform/support/policy)
- **REF-006**: Session Log: `.claude/sessions/2025-11-02-1105-orderservice-dotnet10-refinement.md`
- **REF-007**: Microsoft Announcement: [Default .NET images changed to Ubuntu 24.04 (October 30, 2025)](https://github.com/dotnet/dotnet-docker/blob/main/documentation/supported-tags.md)
