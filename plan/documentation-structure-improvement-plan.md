# Documentation Structure Improvement Plan

**Date:** 2025-11-09
**Status:** Proposed (Awaiting Approval)
**Objective:** Create a unified, navigable documentation structure with clear implementation status tracking

## Problem Statement

The Red Dog repository suffers from scattered, disconnected documentation:

1. **No central navigation**: ADRs, web-api-standards.md, CLAUDE.md, and plan/ directory are disconnected
2. **CLAUDE.md growing too long**: Only trusted source but doesn't reference plan/ directory
3. **web-api-standards.md falling off track**: Not properly integrated with ADRs
4. **ADRs lack cross-references**: Readers can't see relationships between decisions
5. **Configuration confusion**: Spread across 4 ADRs (0002, 0004, 0006, 0009) without clear hierarchy
6. **No implementation status**: ADRs show target state, not what's actually implemented
7. **No ADR overview**: Developers don't know which ADR to read for what

## Solution Overview

Create a **centralized ADR hub** (docs/adr/README.md) that serves as the single source of truth for navigating all architectural decisions, with bidirectional cross-references to related documentation.

**Key Principle:** One new file (docs/adr/README.md), updates to existing files only. No scattered documents.

## Deliverables

- **1 new file:** `docs/adr/README.md` (ADR navigation hub)
- **13 modified files:**
  - `CLAUDE.md` (add documentation map)
  - `docs/standards/web-api-standards.md` (add ADR references)
  - All 11 ADRs (add implementation status + cross-references)

## 8-Phase Implementation Plan

---

### Phase 1: Create ADR Overview & Navigation Hub

**File:** `docs/adr/README.md`

**Content sections:**

1. **Introduction**
   - Purpose of this hub
   - Link to ADR template and process
   - How to read this document

2. **Implementation Status Legend**
   ```markdown
   🟢 **Implemented** - Fully working in current codebase
   🟡 **In Progress** - Partially implemented, work ongoing
   🔵 **Accepted** - Decision made, implementation planned
   ⚪ **Planned** - Under consideration
   ```

3. **ADR Index by Category**
   - **Core Platform Decisions**
     - 🔵 ADR-0001: .NET 10 LTS Adoption
     - 🟢 ADR-0002: Cloud-Agnostic Configuration via Dapr
     - 🔵 ADR-0003: Ubuntu 24.04 Base Image Standardization

   - **Configuration & Secrets**
     - 🟢 ADR-0002: Cloud-Agnostic Configuration via Dapr (Secret Store)
     - ⚪ ADR-0004: Dapr Configuration API Standardization
     - 🔵 ADR-0006: Infrastructure Configuration via Environment Variables

   - **Deployment & Infrastructure**
     - ⚪ ADR-0008: kind Local Development Environment
     - ⚪ ADR-0009: Helm Multi-Environment Deployment
     - ⚪ ADR-0010: Nginx Ingress Controller (Cloud-Agnostic)

   - **Operational Standards**
     - 🔵 ADR-0005: Kubernetes Health Probe Standardization
     - ⚪ ADR-0011: OpenTelemetry Observability Standard

   - **Multi-Cloud Strategy**
     - 🔵 ADR-0007: Cloud-Agnostic Deployment Strategy

4. **Configuration Architecture Overview**

   **The 4-Layer Configuration Hierarchy:**

   ```
   Layer 1: Deployment-Time (Helm)
   ├── values-aks.yaml        → Azure-specific infrastructure
   ├── values-aks-aca.yaml    → Azure Container Apps variant
   ├── values-eks.yaml        → AWS-specific infrastructure
   └── values-gke.yaml        → GCP-specific infrastructure

   Layer 2: Infrastructure (Environment Variables)
   ├── SERVICE_PORT           → Runtime binding addresses
   ├── DAPR_HTTP_PORT         → Dapr sidecar configuration
   └── ASPNETCORE_ENVIRONMENT → Deployment mode (Dev/Prod)

   Layer 3: Application (Dapr Configuration API)
   ├── Business rules         → Feature flags, thresholds
   ├── Application behavior   → Retry counts, timeouts
   └── Dynamic updates        → Runtime reconfiguration

   Layer 4: Secrets (Dapr Secret Store)
   ├── Azure Key Vault        → AKS deployments
   ├── AWS Secrets Manager    → EKS deployments
   └── GCP Secret Manager     → GKE deployments
   ```

5. **Configuration Decision Tree**

   **"Where should I put this setting?"**

   ```
   START: I need to configure...
   │
   ├─❓ Is it a secret? (connection string, API key, password)
   │  └─ YES → Use Dapr Secret Store (ADR-0002)
   │           ├─ Define in secrets.yaml component
   │           └─ Reference via DaprClient.GetSecretAsync()
   │
   ├─❓ Is it infrastructure-specific? (cloud resource endpoints, region)
   │  └─ YES → Use Helm values file (ADR-0009)
   │           ├─ values-aks.yaml for Azure
   │           ├─ values-eks.yaml for AWS
   │           └─ values-gke.yaml for GCP
   │
   ├─❓ Is it a runtime address? (port, URL, Dapr setting)
   │  └─ YES → Use Environment Variable (ADR-0006)
   │           ├─ Define in Kubernetes deployment YAML
   │           └─ Read via Environment.GetEnvironmentVariable()
   │
   └─❓ Is it a business rule or feature flag?
      └─ YES → Use Dapr Configuration API (ADR-0004)
               ├─ ⚠️ NOT IMPLEMENTED YET
               ├─ Define in configuration.yaml component
               └─ Subscribe via DaprClient.GetConfiguration()
   ```

6. **Role-Based Reading Guides**

   **For Developers (Writing Service Code):**
   - Start with: ADR-0002 (Secrets), ADR-0006 (Environment Variables)
   - Then read: ADR-0005 (Health Checks), ADR-0011 (Observability)
   - Reference: web-api-standards.md (HTTP API conventions)

   **For Platform Operators (Deploying Services):**
   - Start with: ADR-0009 (Helm), ADR-0008 (Local Dev)
   - Then read: ADR-0007 (Multi-Cloud Strategy), ADR-0010 (Ingress)
   - Reference: CLAUDE.md (Common Development Commands)

   **For Decision Makers (Understanding Architecture):**
   - Start with: ADR-0001 (.NET 10 Adoption), ADR-0007 (Cloud-Agnostic Strategy)
   - Then read: plan/modernization-strategy.md (8-phase roadmap)
   - Reference: CLAUDE.md (Current Development Status)

7. **Related Documentation**
   - [Web API Standards](../standards/web-api-standards.md)
   - [Modernization Strategy](../../plan/modernization-strategy.md)
   - [Testing & Validation Strategy](../../plan/testing-validation-strategy.md)
   - [CLAUDE.md Development Guide](../../CLAUDE.md)

8. **Maintenance Guidelines**
   - When creating new ADR: Update category index, add cross-references
   - When changing implementation status: Update dashboard and dependent ADRs
   - When adding configuration: Follow decision tree, document in relevant ADR

---

### Phase 2: Add Implementation Status to All ADRs

**Files:** All 11 ADR files (adr-0001 through adr-0011)

**For each ADR, add after "Status" section:**

```markdown
## Implementation Status

**Current State:** [Planned | In Progress | Implemented]

**What's Working:**
- [List concrete evidence of implementation]
- [Example: "Secret store configured in manifests/branch/base/components/secrets.yaml"]

**What's Not Working:**
- [List gaps or blockers]
- [Example: "Services still target .NET 6.0, not .NET 10"]

**Evidence:**
- File locations showing implementation
- Code references (file_path:line_number)
- Configuration examples

**Dependencies:**
- [List ADRs this depends on]
- [List ADRs that depend on this]

**Next Steps:**
- [If not implemented: What needs to happen?]
- [If in progress: What's the current blocker?]
```

**Specific updates for key ADRs:**

- **ADR-0001 (.NET 10 LTS):**
  - Status: 🔵 Accepted (Not Implemented)
  - Working: global.json specifies .NET 10 SDK
  - Not working: All .csproj files target net6.0
  - Blocker: Testing strategy prerequisite (plan/testing-validation-strategy.md)

- **ADR-0002 (Dapr Secret Store):**
  - Status: 🟢 Implemented
  - Working: secrets.yaml in manifests/branch/base/components/
  - Evidence: DaprClient.GetSecretAsync() calls in service code

- **ADR-0004 (Dapr Configuration API):**
  - Status: ⚪ Planned (NOT Implemented)
  - Working: Nothing
  - Not working: Zero GetConfiguration() calls in codebase
  - Blocker: Configuration component not created

- **ADR-0008 (kind Local Dev):**
  - Status: ⚪ Planned
  - Not working: kind-config.yaml doesn't exist, charts/ directory doesn't exist
  - Dependency: ADR-0009 (Helm charts must be created first)

- **ADR-0009 (Helm Deployment):**
  - Status: ⚪ Planned
  - Not working: charts/ directory doesn't exist, no values-*.yaml files

- **ADR-0011 (OpenTelemetry):**
  - Status: ⚪ Planned
  - Not working: Services use Serilog 4.1.0, no OpenTelemetry packages
  - Blocker: .NET 10 upgrade prerequisite

---

### Phase 3: Integrate web-api-standards.md with ADRs

**File:** `docs/standards/web-api-standards.md`

**Updates:**

1. **Add ADR references throughout document:**

   ```markdown
   ## 3. Configuration Management

   **See Configuration Decision Tree in [ADR Overview](../adr/README.md#configuration-decision-tree)**

   - Secrets: Use Dapr Secret Store (ADR-0002)
   - Infrastructure: Use Helm values (ADR-0009)
   - Runtime: Use Environment Variables (ADR-0006)
   - Business Rules: Use Dapr Configuration API (ADR-0004) ⚠️ NOT IMPLEMENTED

   ## 5. Health Checks

   **See [ADR-0005: Kubernetes Health Probe Standardization](../adr/adr-0005-kubernetes-health-probe-standardization.md)**

   All services MUST implement:
   - `/healthz` - Overall health
   - `/livez` - Liveness probe
   - `/readyz` - Readiness probe

   ## 9. Observability

   **See [ADR-0011: OpenTelemetry Observability Standard](../adr/adr-0011-opentelemetry-observability-standard.md)**

   Quick reference:
   - Use native OpenTelemetry OTLP exporters (NOT Serilog)
   - Export to: Console (dev), Azure Monitor (AKS), CloudWatch (EKS), Cloud Logging (GKE)
   ```

2. **Add "Related ADRs" section at end of document:**

   ```markdown
   ## Related Architectural Decisions

   This standard is supported by the following ADRs:

   - [ADR-0002: Cloud-Agnostic Configuration via Dapr](../adr/adr-0002-cloud-agnostic-configuration-via-dapr.md) - Secret management
   - [ADR-0004: Dapr Configuration API](../adr/adr-0004-dapr-configuration-api-standardization.md) - Application configuration ⚠️ NOT IMPLEMENTED
   - [ADR-0005: Kubernetes Health Probes](../adr/adr-0005-kubernetes-health-probe-standardization.md) - Health check endpoints
   - [ADR-0006: Infrastructure Configuration](../adr/adr-0006-infrastructure-configuration-via-environment-variables.md) - Environment variables
   - [ADR-0011: OpenTelemetry Observability](../adr/adr-0011-opentelemetry-observability-standard.md) - Logging, tracing, metrics

   For complete architectural context, see [ADR Overview](../adr/README.md).
   ```

---

### Phase 4: Embed Configuration Decision Tree in ADR Hub

**Already included in Phase 1** (docs/adr/README.md Section 5)

No separate file needed. Configuration decision tree is embedded directly in the ADR overview for single-source-of-truth access.

---

### Phase 5: Update CLAUDE.md with Documentation Map

**File:** `CLAUDE.md`

**Add new section after "Current Development Status":**

```markdown
## Documentation Map

This repository uses structured documentation to separate concerns:

### 📋 Quick Reference (You Are Here)
- **CLAUDE.md** - Development guide, current status, common commands

### 🏗️ Architectural Decisions
- **[ADR Overview](docs/adr/README.md)** - Start here to navigate all ADRs
  - Implementation status dashboard
  - Configuration decision tree
  - Role-based reading guides
- **Individual ADRs** in `docs/adr/` - Detailed decision records

### 📐 Implementation Standards
- **[Web API Standards](docs/standards/web-api-standards.md)** - HTTP API conventions
  - Cross-references to supporting ADRs
  - Target state for modernized services

### 📝 Planning Documents
- **[Modernization Strategy](plan/modernization-strategy.md)** - 8-phase roadmap
- **[Testing & Validation Strategy](plan/testing-validation-strategy.md)** - Test baseline
- **[Documentation Improvement Plan](plan/documentation-structure-improvement-plan.md)** - This plan

### 🔍 Navigation Tips

**I need to understand a decision:** → [ADR Overview](docs/adr/README.md)
**I need to implement an API:** → [Web API Standards](docs/standards/web-api-standards.md)
**I need to configure something:** → [Configuration Decision Tree](docs/adr/README.md#configuration-decision-tree)
**I need to know what's implemented:** → [ADR Implementation Status](docs/adr/README.md#implementation-status-legend)
**I need to know what's next:** → [Modernization Strategy](plan/modernization-strategy.md)
```

**Update existing "Architectural Decisions" section:**

```markdown
### Architectural Decisions:
- **[ADR Overview](docs/adr/README.md)** - Navigation hub for all architectural decisions
  - Category-based index (Core Platform, Configuration, Deployment, Operational)
  - Implementation status tracking
  - Configuration decision tree
- **`docs/adr/`** - Individual Architectural Decision Records
  - `adr-0001-dotnet10-lts-adoption.md` - .NET 10 LTS adoption rationale
  - ... [list continues]
```

---

### Phase 6: Add Implementation Tracking Dashboard

**File:** `docs/adr/README.md` (update from Phase 1)

**Add "Implementation Dashboard" section after ADR Index:**

```markdown
## Implementation Dashboard

Track progress across all architectural decisions:

| Category | Total ADRs | Implemented | In Progress | Planned |
|----------|-----------|-------------|-------------|---------|
| Core Platform | 3 | 0 | 0 | 3 |
| Configuration | 3 | 1 (ADR-0002) | 0 | 2 |
| Deployment | 3 | 0 | 0 | 3 |
| Operational | 2 | 0 | 0 | 2 |
| **TOTAL** | **11** | **1 (9%)** | **0 (0%)** | **10 (91%)** |

**Completion Milestones:**

- ✅ **Phase 0 (Cleanup):** Completed 2025-11-02
  - Removed .devcontainer, manifests/local, manifests/corporate, CorporateTransferService

- ⚠️ **Phase 1A (.NET 10 Upgrade):** Blocked
  - Blocker: Testing strategy implementation required
  - ADRs affected: ADR-0001, ADR-0003, ADR-0011

- ⚪ **Phase 1B (Polyglot Migration):** Not Started
  - Prerequisites: Phase 1A completion
  - ADRs affected: All operational standards (0005, 0011)

- ⚪ **Phase 2 (Local Development):** Not Started
  - Prerequisites: Phase 1A completion
  - ADRs affected: ADR-0008, ADR-0009, ADR-0010

**Critical Path:**
1. Implement testing strategy (plan/testing-validation-strategy.md)
2. Execute .NET 10 upgrade (ADR-0001)
3. Build Helm charts (ADR-0009)
4. Create kind local dev (ADR-0008)
5. Implement remaining operational standards (ADR-0005, ADR-0011)
```

---

### Phase 7: Add Quick Reference Cards

**File:** `docs/adr/README.md` (update from Phase 1)

**Add "Quick Reference Cards" section at end:**

```markdown
## Quick Reference Cards

### Configuration Quick Reference

| I need to... | Use | ADR | Status |
|--------------|-----|-----|--------|
| Store a secret | Dapr Secret Store | ADR-0002 | 🟢 Implemented |
| Set service port | Environment Variable | ADR-0006 | 🔵 Accepted |
| Deploy to Azure | Helm values-aks.yaml | ADR-0009 | ⚪ Planned |
| Add feature flag | Dapr Configuration API | ADR-0004 | ⚪ NOT IMPLEMENTED |

### Deployment Quick Reference

| Environment | Tool | Configuration | Status |
|-------------|------|---------------|--------|
| Local Dev | kind + Helm | values-local.yaml | ⚪ Planned (ADR-0008) |
| Azure (AKS) | Helm | values-aks.yaml | ⚪ Planned (ADR-0009) |
| Azure (ACA) | Helm | values-aks-aca.yaml | ⚪ Planned (ADR-0009) |
| AWS (EKS) | Helm | values-eks.yaml | ⚪ Planned (ADR-0009) |
| GCP (GKE) | Helm | values-gke.yaml | ⚪ Planned (ADR-0009) |

### Observability Quick Reference

| Component | Technology | Export Target | Status |
|-----------|-----------|---------------|--------|
| Logging | OpenTelemetry OTLP | Console (dev), Cloud (prod) | ⚪ Planned (ADR-0011) |
| Tracing | OpenTelemetry OTLP | Dapr tracing config | ⚪ Planned (ADR-0011) |
| Metrics | OpenTelemetry OTLP | Prometheus/Cloud | ⚪ Planned (ADR-0011) |
| Current | Serilog 4.1.0 | Console only | 🟢 Implemented (Legacy) |

**Note:** Current services use Serilog. ADR-0011 prescribes migration to native OpenTelemetry.

### Health Check Quick Reference

| Endpoint | Purpose | Use Case | Status |
|----------|---------|----------|--------|
| `/healthz` | Overall health | Manual health checks | 🔵 Accepted (ADR-0005) |
| `/livez` | Liveness | Kubernetes restart decision | 🔵 Accepted (ADR-0005) |
| `/readyz` | Readiness | Kubernetes traffic routing | 🔵 Accepted (ADR-0005) |

**Current State:** MakeLineService and AccountingService implement `/health`. Migration to ADR-0005 endpoints not yet started.
```

---

### Phase 8: Add Maintenance Plan

**File:** `docs/adr/README.md` (update from Phase 1)

**Add "Maintaining This Hub" section at end:**

```markdown
## Maintaining This Hub

### When to Update This Document

✅ **Always update when:**
- Creating a new ADR (add to category index, update dashboard)
- Changing implementation status (update dashboard, milestones)
- Adding new configuration layer (update decision tree)
- Completing a modernization phase (update dashboard, milestones)

✅ **Review quarterly:**
- Implementation dashboard accuracy
- Cross-reference link validity
- Role-based reading guide relevance

### Update Checklist for New ADRs

When creating ADR-00XX:

1. ☐ Add to category index in README.md
2. ☐ Add implementation status section to ADR
3. ☐ Update implementation dashboard counts
4. ☐ Add cross-references to related ADRs
5. ☐ Update web-api-standards.md if relevant
6. ☐ Add to role-based reading guides if applicable
7. ☐ Update configuration decision tree if it affects configuration
8. ☐ Add to quick reference cards if it's a common operation

### Cross-Reference Validation

Run this check monthly to ensure links are valid:

```bash
# Check for broken ADR references
grep -r "ADR-[0-9]" docs/adr/README.md docs/standards/ CLAUDE.md | \
  grep -v ".md:.*adr-[0-9].*\.md" | \
  sort | uniq

# Check for orphaned ADRs (not referenced in README.md)
for adr in docs/adr/adr-*.md; do
  basename=$(basename "$adr")
  if ! grep -q "$basename" docs/adr/README.md; then
    echo "⚠️  Orphaned ADR: $basename"
  fi
done
```

### Ownership

- **ADR Hub (README.md):** Architecture team maintains
- **Individual ADRs:** Decision author maintains
- **Implementation Status:** Engineering team updates during sprints
- **Dashboard Metrics:** Updated by tech lead at phase boundaries
```

---

## Implementation Sequence

### Recommended Order

1. **Phase 1** (1-2 hours) - Create docs/adr/README.md with all sections
2. **Phase 2** (2-3 hours) - Add implementation status to all 11 ADRs
3. **Phase 5** (30 min) - Update CLAUDE.md with documentation map
4. **Phase 3** (1 hour) - Add ADR references to web-api-standards.md
5. **Phase 6** (30 min) - Refine implementation dashboard in README.md
6. **Phase 7** (30 min) - Add quick reference cards to README.md
7. **Phase 8** (30 min) - Add maintenance guidelines to README.md

**Total Estimated Time:** 6-8 hours

### Validation Steps

After implementation:

1. ✅ Navigate from CLAUDE.md → ADR README → Individual ADR → Back to CLAUDE.md
2. ✅ Verify all 11 ADRs have implementation status sections
3. ✅ Verify web-api-standards.md references relevant ADRs
4. ✅ Test configuration decision tree with real scenarios
5. ✅ Verify dashboard counts match actual ADR statuses
6. ✅ Check all cross-reference links resolve correctly

---

## Success Criteria

This plan succeeds when:

1. ✅ New developers can navigate to relevant ADRs in <2 clicks from CLAUDE.md
2. ✅ Configuration questions can be answered via decision tree without reading 4 ADRs
3. ✅ Implementation status is visible for every architectural decision
4. ✅ No duplicate or scattered documentation exists
5. ✅ CLAUDE.md references all key documentation without embedding it
6. ✅ ADRs cross-reference each other where decisions are related
7. ✅ web-api-standards.md clearly links to supporting ADRs

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| README.md becomes too long | Medium | Use collapsible sections, keep reference cards concise |
| ADR implementation status becomes stale | High | Add to sprint definition of done, quarterly review |
| Cross-references break when files move | Medium | Use relative paths, validate links in CI/CD |
| Decision tree doesn't cover edge cases | Low | Add "If none of the above" → Ask in team channel |
| Developers bypass hub and read ADRs directly | Low | That's fine! Hub is for navigation, not enforcement |

---

## Future Enhancements (Out of Scope)

- Automated dashboard generation from ADR frontmatter
- ADR dependency graph visualization
- Implementation status badges in ADR files
- Integration with project management tools
- ADR search functionality

---

## Approval and Execution

**Status:** Awaiting user approval

**Next Steps:**
1. User reviews this plan
2. User approves or requests changes
3. Execute phases 1-8 in recommended order
4. Validate against success criteria
5. Commit and push to repository
6. Update session notes with completion

**Questions for User:**
- Does the configuration decision tree cover all scenarios you encounter?
- Are the role-based reading guides aligned with your team structure?
- Should we add any additional quick reference cards?
- Any specific cross-references between ADRs you want to ensure are included?
