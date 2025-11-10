# Tooling Installation and Preparation

**Session Start:** 2025-11-10 14:33 NZDT

---

## Session Overview

This session focuses on implementing Phase 0 of the Testing & Validation Strategy: Prerequisites & Setup. The goal is to prepare the local development environment with all necessary tools, verify installations, create artifact directories, and establish readiness for .NET 10 upgrade work.

**Context:** Following the decision to remove dev containers and use native kind clusters, we need to ensure the local WSL2/macOS/Linux environment has all tools installed for modernization work.

---

## Goals

### Primary Goals:

1. **Install and Verify .NET 10 SDK**
   - Confirm `.NET SDK 10.0.100` (per `global.json`)
   - Verify via `dotnet --version` and `dotnet --list-sdks`

2. **Install .NET Upgrade Tools**
   - Install `upgrade-assistant` global tool
   - Install `Microsoft.DotNet.ApiCompat.Tool` global tool
   - Verify installations

3. **Install Kubernetes Development Tools**
   - Install `kind` (Kubernetes-in-Docker)
   - Install `kubectl` CLI
   - Install `Helm` package manager
   - Verify installations and versions

4. **Verify Dapr CLI**
   - Confirm `Dapr CLI v1.16.0+` installed
   - Required for integration smoke tests

5. **Verify Node.js and npm**
   - Confirm `Node.js 24.x` and `npm 10+`
   - Required for Vue.js UI build/test pipeline

6. **Create Artifact Directories**
   - `artifacts/upgrade-assistant/`
   - `artifacts/api-analyzer/`
   - `artifacts/dependencies/`
   - `artifacts/performance/`

7. **Run Environment Verification Checklist**
   - Execute verification script to confirm all prerequisites
   - Document any gaps or issues

### Secondary Goals:

8. **Document Local Development Setup**
   - Update CLAUDE.md with native tool installation steps
   - Create quick start guide for future developers

---

## Progress

### Update - 2025-11-10 14:33 NZDT

**Summary:** Session started - Tooling installation and preparation for Phase 0

**Goals Defined:**
- .NET 10 SDK verification
- Upgrade tools installation (upgrade-assistant, ApiCompat)
- Kubernetes tools installation (kind, kubectl, Helm)
- Dapr CLI verification
- Node.js/npm verification
- Artifact directory creation
- Environment verification checklist

**Reference Documents:**
- `plan/testing-validation-strategy.md` - Phase 0 requirements
- `docs/research/dotnet-upgrade-analysis.md` - Tooling workflow details

**Current Status:**
- Starting environment verification
- Will check existing installations first
- Then install missing tools
- Finally run verification checklist

**Next Steps:**
- Check current tool installations
- Identify what's already installed vs what's missing
- Install missing tools
- Create artifact directories
- Run verification checklist

---

## Issues and Solutions

(To be documented as issues arise)

---

## Key Decisions

(To be documented as decisions are made)

---

## References

- [Testing & Validation Strategy](../plan/testing-validation-strategy.md) - Phase 0: Prerequisites & Setup
- [.NET Upgrade Analysis](../docs/research/dotnet-upgrade-analysis.md) - Tooling workflow (lines 94-119)
- [ADR-0008: kind Local Development Environment](../docs/adr/adr-0008-kind-local-development-environment.md)

---
