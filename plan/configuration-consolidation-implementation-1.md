---
goal: Consolidate Configuration Sprawl and Establish Single Source of Truth
version: 1
date_created: 2025-11-25
last_updated: 2025-11-25
owner: Ahmed Muhi / Red Dog Modernization Team
status: 'Planned'
tags: [architecture, configuration, refactor, technical-debt, helm, dapr]
---

# Configuration Consolidation Implementation Plan

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

This plan addresses the critical configuration sprawl across the Red Dog repository that currently fragments configuration management across 9+ locations with severe duplication, inconsistent naming conventions, and mixed concerns. The current state violates ADR-0002 (cloud-agnostic Dapr components), blocks ADR-0004 (Dapr Configuration API) implementation, and creates significant developer productivity and security risks.

## How to Use This Plan

**Execution Approach:**
- **Phase 1 (Dapr Component Unification)** must complete before Phases 4 and 5 can begin (hard dependency)
- **Phases 2 (Environment Values) and 3 (Kustomize Elimination)** can run in parallel with Phase 1 for most tasks
- **Phase 4 (Infrastructure Consolidation)** and **Phase 5 (Dapr Configuration API)** require Phase 1 completion
- **Phase 6 (Documentation)** can be drafted incrementally throughout, finalized at end

**Parallel Workstreams:**
See "Execution Notes: Dependencies and Parallelism" in Section 2 for detailed parallel workstream opportunities. Up to four concurrent workstreams are possible during Phases 1-3.

**Key Decision Points:**
This plan requires four critical architectural decisions before execution can proceed:
1. App ID and namespace naming conventions (Phase 1, TASK-003)
2. Cloud secret injection pattern: ESO vs CSI vs CI/CD (Phase 2, TASK-023c)
3. Cloud configuration backend strategy (Phase 5, TASK-047b)
4. Configuration versioning approach (Phase 5, TASK-052a)

**Validation Strategy:**
Each phase has explicit completion criteria and mapped tests. Run tests incrementally (TEST-001 through TEST-009) as phases complete rather than batching all validation at the end.

## 1. Requirements & Constraints

### Functional Requirements

- **REQ-001**: All Dapr components MUST be defined in exactly ONE canonical location per deployment environment
- **REQ-002**: Application service naming (app IDs) MUST be consistent across all Dapr component scopes
- **REQ-002a**: Kubernetes namespace conventions MUST be standardized across all environments (app ID + namespace form service identity)
- **REQ-003**: Configuration MUST be separated by concern: secrets, infrastructure config, application config, deployment manifests
- **REQ-004**: The same application container images MUST deploy to local/kind, AKS, EKS, and GKE using only values file changes
- **REQ-005**: Configuration changes MUST NOT require application code changes or rebuilds

### Architectural Constraints

- **CON-001**: MUST comply with ADR-0002 (cloud-agnostic configuration via Dapr)
- **CON-002**: MUST comply with ADR-0004 (Dapr Configuration API for application config, across ALL environments)
- **CON-002a**: Configuration backend MUST be cloud-appropriate (not Redis in production unless explicitly justified)
- **CON-003**: MUST comply with ADR-0006 (infrastructure config via environment variables)
- **CON-004**: MUST comply with ADR-0009 (Helm-based multi-environment deployment)
- **CON-005**: MUST comply with ADR-0013 (secret management strategy - no secrets in Git)
- **CON-006**: MUST NOT introduce cloud-provider-specific code paths or SDKs into application services
- **CON-007**: Deployment tool (Helm) MUST be the single source of truth for manifest generation
- **CON-012**: Chart defaults (`charts/reddog/values.yaml`) MUST contain only stable, environment-agnostic defaults; environment-specific configuration (endpoints, replicas, resource sizes, feature flags) MUST live in repo `/values/values-*.yaml` files only
- **CON-013**: Infrastructure chart (`charts/infrastructure`) is for local/demo/dedicated cluster scenarios where Redis, SQL Server, RabbitMQ run in-cluster. For cloud environments using managed PaaS (Azure SQL, RDS, Cloud SQL, Azure Cache for Redis), infrastructure is external and provisioned separately; values files contain only endpoints and secret references, never deploy in-cluster infrastructure
- **CON-014**: Configuration values stored in Dapr Configuration backends MUST be versioned and rollback-capable to prevent production incidents from bad configuration deployments

### Security Requirements

- **SEC-001**: Secrets MUST NOT be committed to Git in any form (no raw values in values files)
- **SEC-002**: Secret references MUST be isolated from non-secret configuration
- **SEC-003**: Database passwords and connection strings MUST use Kubernetes Secret references, never inline values
- **SEC-004**: All secret handling MUST follow ADR-0013 patterns (gitignored values files for local, managed secret stores for cloud)
- **SEC-005**: Application services MUST NEVER have direct access to infrastructure connection strings or credentials. Only Dapr components (state stores, pub/sub, bindings, secret stores) backed by Kubernetes Secrets or cloud secret stores may hold connection strings. Applications access infrastructure solely through Dapr APIs

### Migration Constraints

- **CON-008**: Migration MUST be incremental and testable at each phase
- **CON-009**: Existing local development workflows MUST remain functional throughout migration
- **CON-010**: Each phase MUST have clear rollback capability
- **CON-011**: Migration MUST NOT introduce breaking changes to running environments without explicit coordination

### Quality Guidelines

- **GUD-001**: Configuration structure MUST be self-documenting with clear naming and organization
- **GUD-002**: Sample/template files MUST be clearly marked and contain ONLY placeholders, never real values
- **GUD-003**: Documentation MUST be updated synchronously with structural changes
- **GUD-004**: All Helm charts MUST pass `helm lint` and `helm template` validation
- **GUD-005**: When deciding where configuration belongs: if it varies by environment (local vs cloud, dev vs prod), it goes in `/values/values-*.yaml`; if it's a sensible default that works everywhere, it goes in `charts/reddog/values.yaml`

### Architectural Patterns

- **PAT-001**: Follow Helm best practices with strict separation: `charts/reddog/values.yaml` contains stable, environment-agnostic defaults only (never environment-specific endpoints, sizes, or flags); `/values/values-*.yaml` contains environment-specific overrides only (local/azure/aws/gcp). This ensures charts remain portable and environments remain explicit
- **PAT-002**: Use Helm chart dependencies for external infrastructure (RabbitMQ, cert-manager, etc.)
- **PAT-003**: Leverage Helm templating for Dapr components with environment-specific backend parameterization
- **PAT-004**: Separate infrastructure components (databases, message brokers) from application services in chart structure
- **PAT-004a**: Infrastructure deployment strategy varies by environment: local/kind deploys `charts/infrastructure` with in-cluster Redis, SQL Server, RabbitMQ; cloud environments (AKS/EKS/GKE) use external managed PaaS services (Azure SQL, RDS, Cloud SQL, Azure Cache, Amazon MQ, Cloud Pub/Sub) provisioned outside Helm, referenced via endpoints in values files
- **PAT-005**: Use explicit app ID naming in Dapr components following kebab-case convention (`order-service`, not `orderservice`)
- **PAT-005a**: Standardize namespace conventions: use single application namespace (e.g., `reddog`) for all application services and components; reserve system namespaces (e.g., `reddog-system`, `dapr-system`) for infrastructure only
- **PAT-005b**: Dapr component scope: decide and document whether components are namespace-scoped (apply to all apps in namespace) or app-scoped (apply only to apps listed in `scopes`). Default to app-scoped for explicit control
- **PAT-006**: Implement escape hatch pattern in all Helm templates to allow environment-specific configuration without chart rewrites
  - Pod-level: `{{ with .Values.<service>.podSpec }}{{ toYaml . | nindent N }}{{ end }}`
  - Container-level: `{{ with .Values.<service>.containerSpec }}{{ toYaml . | nindent N }}{{ end }}`
  - Dapr components: `{{ with .Values.dapr.<component>.extraMetadata }}{{ toYaml . | nindent N }}{{ end }}`
  - Use `with` guard to make escape hatches optional (no ugly empty blocks in rendered YAML)

## 2. Implementation Steps

### Execution Notes: Dependencies and Parallelism

**Hard Dependencies (MUST complete before dependent tasks):**
- Phase 1 (Dapr Component Unification) must complete before Phase 4 (Infrastructure Consolidation) and Phase 5 (Dapr Configuration API)
- Within Phase 1: TASK-002a/002b/003 (decide namespace, scope, and naming conventions) block TASK-004, TASK-005, TASK-006 (all component updates must use same conventions)
- Within Phase 2: TASK-023 (gitignore) must complete before creating any cloud values files to prevent accidental commits
- Within Phase 3: TASK-026a (audit patches) blocks TASK-026b-e (can't design escape hatches without knowing requirements)
- Within Phase 5: TASK-047b (backend decision) blocks TASK-047c (can't parameterize template without strategy)

**Soft Dependencies (Recommended order, but can run in parallel or out of sequence):**
- Phase 2 (Environment Values) and Phase 3 (Kustomize Elimination) can run partially in parallel with Phase 1
  - TASK-014 through TASK-022 (values structure) are independent of Phase 1 app ID changes
  - TASK-026 through TASK-027 (Kustomize audit) can start during Phase 1
- Phase 6 documentation tasks can be drafted incrementally throughout Phases 1-5, finalized at end

**Parallel Workstream Opportunities:**
- Workstream A: Phase 1 TASK-001 through TASK-009 (Dapr unification and local testing)
- Workstream B: Phase 2 TASK-014 through TASK-022 (values file restructure) — can run concurrently with Workstream A
- Workstream C: Phase 3 TASK-026 through TASK-027 (Kustomize audit) — can run concurrently with Workstreams A and B
- Workstream D: Phase 4 TASK-036 (appsettings audit) — can start after Phase 1 completes (depends on TASK-013), does not depend on Phase 3 Kustomize removal
- Synchronization point: Phase 2 TASK-023 (gitignore) and Phase 3 TASK-028 onwards require Phase 1 completion

---

### Implementation Phase 1: Dapr Component Unification

**Goal:** Eliminate Dapr component duplication and establish single source of truth in Helm charts

- GOAL-001: All Dapr components consolidated into `charts/reddog/templates/dapr-components/`
- GOAL-002: Consistent app ID naming across all Dapr component scopes
- GOAL-003: Remove `.dapr/components/` and `manifests/branch/base/components/` directories

| Task ID  | Description                                                                                     | Location                                                       | DependsOn | Status     | Date |
|----------|-------------------------------------------------------------------------------------------------|----------------------------------------------------------------|-----------|------------|------|
| TASK-001 | Audit all Dapr components: compare `.dapr/components/`, `manifests/branch/base/components/`, and `charts/reddog/templates/dapr-components/` | /plan/configuration-consolidation-implementation-1.md#Phase-1 |           | NotStarted |      |
| TASK-002 | Document app ID naming inconsistencies (orderservice vs order-service vs OrderService)          | /plan/configuration-consolidation-implementation-1.md#Phase-1 | TASK-001  | NotStarted |      |
| TASK-002a | Document namespace inconsistencies and decide canonical namespace convention (recommend: single `reddog` namespace for all application services and components) | /plan/configuration-consolidation-implementation-1.md#Phase-1 | TASK-001  | NotStarted |      |
| TASK-002b | Decide Dapr component scope strategy: namespace-scoped (components apply to all apps in namespace) vs app-scoped (components only apply to apps in `scopes` list). Document decision and rationale (recommend: app-scoped for explicit control) | /plan/configuration-consolidation-implementation-1.md#Phase-1 | TASK-001  | NotStarted |      |
| TASK-003 | Decide canonical app ID naming convention (recommend: kebab-case `order-service`)               | /plan/configuration-consolidation-implementation-1.md#Phase-1 | TASK-002,TASK-002a,TASK-002b  | NotStarted |      |
| TASK-004 | Update all Dapr component scopes and namespaces in `charts/reddog/templates/dapr-components/*.yaml` to use canonical app IDs and namespace convention | /charts/reddog/templates/dapr-components/                    | TASK-003  | NotStarted |      |
| TASK-005 | Update all application Dapr sidecar annotations in `charts/reddog/templates/*-service.yaml` to match canonical app IDs | /charts/reddog/templates/                                    | TASK-003  | NotStarted |      |
| TASK-006 | Update `values/values-local.yaml` to reflect new app ID naming in `services.*.dapr.appId` fields | /values/values-local.yaml                                    | TASK-003  | NotStarted |      |
| TASK-006a | Create state migration script to rename Redis keys from old app IDs to new app IDs              | /scripts/migrate-state-keys.sh                               | TASK-006  | NotStarted |      |
| TASK-006b | Document state migration strategy: local = flush/migrate, cloud = coordinate with data owners   | /plan/configuration-consolidation-implementation-1.md#Phase-1 | TASK-006a | NotStarted |      |
| TASK-007 | Test Helm template rendering: `helm template reddog ./charts/reddog -f values/values-local.yaml` | /charts/reddog/                                              | TASK-004,TASK-005,TASK-006 | NotStarted |      |
| TASK-008 | Deploy to local kind cluster and verify Dapr component discovery                                | Local kind cluster                                           | TASK-007  | NotStarted |      |
| TASK-009 | Verify inter-service communication works with new app IDs (order-service → make-line-service)   | Local kind cluster                                           | TASK-008  | NotStarted |      |
| TASK-010 | Archive `.dapr/components/` to `.dapr/components.deprecated/` with README explaining migration  | /.dapr/components/                                           | TASK-009  | NotStarted |      |
| TASK-011 | Archive `manifests/branch/base/components/` to `manifests/Archive/branch-components/` with migration README | /manifests/branch/base/components/                           | TASK-009  | NotStarted |      |
| TASK-012 | Update `scripts/setup-local-dev.sh` to remove references to `.dapr/components/` and any `dapr run` workflow instructions. Document that `kind + Helm` is the only supported local development workflow | /scripts/setup-local-dev.sh                                  | TASK-010  | NotStarted |      |
| TASK-013 | Update relevant Knowledge Items (KI-DAPR-COMPONENTS-CLOUD-AGNOSTIC-001) to reflect new structure, namespace conventions, component scope strategy, and explicit statement that `dapr run` workflow is no longer supported | /knowledge/dapr-components-cloud-agnostic-ki.md             | TASK-011  | NotStarted |      |

**Completion Criteria for Phase 1:**
- All Dapr components exist ONLY in `charts/reddog/templates/dapr-components/`
- All app IDs are consistent across Dapr components and service annotations
- All namespaces are standardized according to decided convention (TASK-002a)
- Component scope strategy (namespace-scoped vs app-scoped) is explicitly documented (TASK-002b)
- State migration strategy documented and tested (TASK-006a, TASK-006b)
- Local kind deployment works with new unified components
- TEST-001 passes (see section 6)
- **CRITICAL**: Verify MakeLineService and LoyaltyService can access their state after app ID change (or acknowledge state was intentionally flushed)
- Document state migration outcome in session notes and plan to include in `configuration-consolidation-completion-report.md` (Phase 6): whether state was migrated or flushed for each stateful service
- **`dapr run` workflow explicitly decommissioned**: No official documentation or scripts reference `dapr run` with `.dapr/components/` anymore; `kind + Helm` is the sole supported local development workflow

---

### Implementation Phase 2: Environment Values Separation

**Goal:** Separate concerns in values files and eliminate 370-line monolithic `values-local.yaml`

- GOAL-004: Configuration organized by concern: environment metadata, infrastructure, Dapr, application services, observability
- GOAL-005: Clear separation between what varies per environment vs what is shared
- GOAL-006: Template/sample files with clear placeholders for secrets

| Task ID  | Description                                                                                     | Location                                                       | DependsOn | Status     | Date |
|----------|-------------------------------------------------------------------------------------------------|----------------------------------------------------------------|-----------|------------|------|
| TASK-014 | Create `values/README.md` documenting values file structure, naming conventions, gitignore pattern (`values-*.yaml` ignored, `values-*.yaml.sample` committed), and escape hatch keys (`services.<name>.podSpec`, `services.<name>.containerSpec`, `dapr.<component>.extraMetadata`). Document exception path: if CI needs committed values, use explicitly named file like `values-ci.yaml` with no secrets and explicit gitignore exception | /values/README.md                                            | TASK-013  | NotStarted |      |
| TASK-015 | Create `values/values-local.yaml.sample` template with clear `CHANGEME` placeholders for secrets | /values/values-local.yaml.sample                            | TASK-014  | NotStarted |      |
| TASK-016 | Split `values-local.yaml` into logical sections with clear comments. Note: YAML anchors work only within a single file, not across `values-local.yaml` and `values-azure.yaml`. Decision: accept duplication of shared config across environment files OR move truly shared defaults into `charts/reddog/values.yaml` (preferred for environment-agnostic settings) | /values/values-local.yaml                                    | TASK-015  | NotStarted |      |
| TASK-017 | Extract infrastructure config (Redis, SQL Server, RabbitMQ) into dedicated values subsection   | /values/values-local.yaml                                    | TASK-016  | NotStarted |      |
| TASK-018 | Extract Dapr component config into dedicated subsection separate from infrastructure           | /values/values-local.yaml                                    | TASK-016  | NotStarted |      |
| TASK-019 | Extract application service config (images, replicas, resources) into dedicated subsection     | /values/values-local.yaml                                    | TASK-016  | NotStarted |      |
| TASK-020 | Create `values/values-azure.yaml` for AKS environment following new structure                   | /values/values-azure.yaml                                    | TASK-017,TASK-018,TASK-019 | NotStarted |      |
| TASK-021 | Create `values/values-aws.yaml` for EKS environment following new structure                     | /values/values-aws.yaml                                      | TASK-017,TASK-018,TASK-019 | NotStarted |      |
| TASK-022 | Create `values/values-gcp.yaml` for GKE environment following new structure                     | /values/values-gcp.yaml                                      | TASK-017,TASK-018,TASK-019 | NotStarted |      |
| TASK-023 | Add `.gitignore` entries to ensure `values/values-*.yaml` (without `.sample` suffix) are never committed. Pattern: ignore `values/values-*.yaml`, allow `values/values-*.yaml.sample`. Document exception: if CI needs committed values file, use explicit name like `values-ci.yaml` with gitignore exception `!values/values-ci.yaml`, ensure zero secrets in that file | /.gitignore                                                  | TASK-020,TASK-021,TASK-022 | NotStarted |      |
| TASK-023a | Document cloud secret injection architecture: ESO/CSI Driver → K8s Secrets → Dapr Secret Store | /plan/configuration-consolidation-implementation-1.md#Phase-2-Secret-Architecture | TASK-023  | NotStarted |      |
| TASK-023b | Define Workload Identity setup for AKS (Azure Workload Identity), EKS (IRSA), GKE (Workload Identity) | /plan/configuration-consolidation-implementation-1.md#Phase-2-Secret-Architecture | TASK-023a | NotStarted |      |
| TASK-023c | Specify External Secrets Operator configuration or CSI Driver choice per cloud provider        | /plan/configuration-consolidation-implementation-1.md#Phase-2-Secret-Architecture | TASK-023b | NotStarted |      |
| TASK-023d | Create placeholder values in `values-azure.yaml` for secret references (secretName/key pattern, NOT raw values) | /values/values-azure.yaml                                  | TASK-023c | NotStarted |      |
| TASK-023e | Document CI/CD pipeline secret injection pattern (GitHub Actions secrets → helm install --set) | /docs/deployment-pipeline-secrets.md                        | TASK-023c | NotStarted |      |
| TASK-024 | Test Helm template rendering for all environment values files                                   | /charts/reddog/                                              | TASK-020,TASK-021,TASK-022 | NotStarted |      |
| TASK-025 | Update `scripts/setup-local-dev.sh` to copy sample file if `values-local.yaml` doesn't exist   | /scripts/setup-local-dev.sh                                  | TASK-015  | NotStarted |      |

**Completion Criteria for Phase 2:**
- `values-local.yaml` structure is clear and sectioned by concern
- Sample files exist for all environments with zero secrets
- `.gitignore` prevents accidental secret commits
- **Cloud secret injection architecture documented** (TASK-023a through TASK-023e)
- **Clear decision made**: ESO vs CSI Driver vs CI/CD token substitution
- **Workload Identity setup documented** for AKS, EKS, GKE
- TEST-002 passes (see section 6)

---

### Implementation Phase 3: Kustomize Elimination

**Goal:** Remove Kustomize overlays and raw manifests, establish Helm as single deployment tool

- GOAL-007: All deployments use Helm charts exclusively
- GOAL-008: GitOps manifests (if any) reference Helm charts via `HelmRelease`, not raw YAML
- GOAL-009: Clear migration path documented for any remaining raw manifest consumers

| Task ID  | Description                                                                                     | Location                                                       | DependsOn | Status     | Date |
|----------|-------------------------------------------------------------------------------------------------|----------------------------------------------------------------|-----------|------------|------|
| TASK-026 | Audit usage of `manifests/branch/`, `manifests/cloud/`, `manifests/overlays/` directories      | /plan/configuration-consolidation-implementation-1.md#Phase-3 | TASK-025  | NotStarted |      |
| TASK-026a | Identify ALL Kustomize patches: strategic overlays, one-off tweaks, fields not in Helm templates | /plan/configuration-consolidation-implementation-1.md#Phase-3-Kustomize-Patches | TASK-026  | NotStarted |      |
| TASK-026b | Document escape hatch requirements: topologySpread, affinity, tolerations, nodeSelectors, etc. | /plan/configuration-consolidation-implementation-1.md#Phase-3-Kustomize-Patches | TASK-026a | NotStarted |      |
| TASK-026c | Add escape hatch templates to all service deployments: `{{ with .Values.<service>.podSpec }}`  | /charts/reddog/templates/*-service.yaml                      | TASK-026b | NotStarted |      |
| TASK-026d | Add escape hatch to Dapr components: `{{ with .Values.dapr.<component>.extraMetadata }}`       | /charts/reddog/templates/dapr-components/*.yaml              | TASK-026b | NotStarted |      |
| TASK-026e | Test escape hatch mechanism: inject topologySpreadConstraints via values and verify templating | /charts/reddog/                                              | TASK-026c,TASK-026d | NotStarted |      |
| TASK-027 | Identify any CI/CD pipelines or scripts referencing raw manifests under `manifests/`           | /.github/workflows/,/scripts/                                 | TASK-026  | NotStarted |      |
| TASK-028 | Create migration guide documenting Helm equivalents for Kustomize overlay patterns. Include explicit command mappings: "If you used `kubectl apply -k manifests/branch`, use this equivalent `helm upgrade --install reddog ./charts/reddog -f values/values-branch.yaml` command." Provide before/after examples for each overlay pattern | /docs/guides/kustomize-to-helm-migration.md                  | TASK-027  | NotStarted |      |
| TASK-029 | Update CI/CD workflows to use `helm upgrade --install` instead of `kubectl apply -k`           | /.github/workflows/                                          | TASK-028  | NotStarted |      |
| TASK-030 | Document GitOps adoption as separate future implementation plan (Flux/ArgoCD HelmRelease pattern). GitOps tooling requires clean configuration foundation, which this plan establishes. If GitOps is already in use, document migration path; otherwise, note that GitOps adoption is explicitly out of scope for this plan and will be addressed in a separate implementation plan after consolidation is complete | /docs/guides/gitops-adoption-roadmap.md                      | TASK-029  | NotStarted |      |
| TASK-031 | Test deployment to branch/dev environment using Helm chart instead of Kustomize                | Branch environment                                           | TASK-029  | NotStarted |      |
| TASK-032 | Archive `manifests/branch/` to `manifests/Archive/branch/` with README explaining migration    | /manifests/branch/                                           | TASK-031  | NotStarted |      |
| TASK-033 | Archive `manifests/overlays/` to `manifests/Archive/overlays/` with README explaining migration | /manifests/overlays/                                         | TASK-031  | NotStarted |      |
| TASK-034 | Update documentation references to Kustomize in ADRs and Knowledge Items                        | /docs/adr/,/knowledge/                                       | TASK-032,TASK-033 | NotStarted |      |
| TASK-035 | Remove `kustomization.yaml` files from manifests directories                                    | /manifests/                                                  | TASK-034  | NotStarted |      |

**Completion Criteria for Phase 3:**
- Zero Kustomize usage in active deployment workflows
- All environments deploy via Helm charts
- **ALL Kustomize patches identified and migrated or deprecated** (TASK-026a)
- **Escape hatch mechanism implemented** for pod-level and Dapr component customization (TASK-026c, TASK-026d)
- **At least one production-specific setting** testable via escape hatch (TASK-026e)
- Raw manifests archived with clear migration documentation
- Migration guide includes explicit old→new command mappings for all previous Kustomize workflows
- GitOps adoption (if desired) documented as separate future implementation plan, not part of Phase 3 scope
- TEST-003 passes (see section 6)

---

### Implementation Phase 4: Infrastructure Configuration Consolidation

**Goal:** Centralize infrastructure component configuration and eliminate appsettings.json sprawl

- GOAL-010: Infrastructure components (Redis, SQL Server, RabbitMQ) configured via Helm charts only
- GOAL-011: Application services use minimal appsettings.json (AllowedHosts only, per ADR-0006)
- GOAL-012: Clear separation: application services NEVER see infrastructure connection strings; only Dapr components (backed by K8s Secrets/secret stores) hold connection strings; apps access infrastructure solely via Dapr APIs

| Task ID  | Description                                                                                     | Location                                                       | DependsOn | Status     | Date |
|----------|-------------------------------------------------------------------------------------------------|----------------------------------------------------------------|-----------|------------|------|
| TASK-036 | Audit all `appsettings.json` and `appsettings.Development.json` files for content               | /RedDog.*/appsettings*.json                                   | TASK-013  | NotStarted |      |
| TASK-037 | Document which settings belong in Dapr Configuration vs environment variables vs neither. Enforce rule: connection strings and credentials NEVER exposed to application services, only to Dapr components via Secrets | /plan/configuration-consolidation-implementation-1.md#Phase-4 | TASK-036  | NotStarted |      |
| TASK-038 | Remove connection strings and infrastructure config from all `appsettings.json` files           | /RedDog.*/appsettings.json                                    | TASK-037  | NotStarted |      |
| TASK-039 | Verify Helm templates inject all necessary infrastructure env vars (DAPR_HTTP_PORT, etc.)      | /charts/reddog/templates/*-service.yaml                       | TASK-038  | NotStarted |      |
| TASK-040 | Consolidate Redis configuration in `charts/infrastructure/templates/redis.yaml`                 | /charts/infrastructure/templates/redis.yaml                   | TASK-039  | NotStarted |      |
| TASK-041 | Consolidate SQL Server configuration in `charts/infrastructure/templates/sqlserver.yaml`        | /charts/infrastructure/templates/sqlserver.yaml               | TASK-039  | NotStarted |      |
| TASK-042 | Consolidate RabbitMQ configuration in `charts/external/rabbitmq/` external chart dependency     | /charts/external/rabbitmq/                                    | TASK-039  | NotStarted |      |
| TASK-043 | Create `charts/infrastructure/values.yaml` with sensible defaults for all infrastructure components | /charts/infrastructure/values.yaml                          | TASK-040,TASK-041,TASK-042 | NotStarted |      |
| TASK-044 | Update `values/values-local.yaml` to reference infrastructure chart values, not duplicate them  | /values/values-local.yaml                                     | TASK-043  | NotStarted |      |
| TASK-045 | Test local deployment with consolidated infrastructure configuration                            | Local kind cluster                                            | TASK-044  | NotStarted |      |
| TASK-046 | Document infrastructure configuration patterns in new Knowledge Item                            | /knowledge/infrastructure-configuration-helm-ki.md            | TASK-045  | NotStarted |      |

**Completion Criteria for Phase 4:**
- Infrastructure config exists ONLY in Helm charts (for local) or external PaaS references (for cloud)
- No connection strings or credentials in appsettings.json or application environment variables
- SEC-005 enforced: applications have zero direct access to connection strings; only Dapr components hold them
- Local infrastructure deployable via `helm upgrade --install infrastructure ./charts/infrastructure`
- Cloud infrastructure strategy documented: external PaaS provisioned separately, values files contain only endpoints
- TEST-004 passes (see section 6)

---

### Implementation Phase 5: Dapr Configuration API Implementation

**Goal:** Implement ADR-0004 (Dapr Configuration API) now that infrastructure is consolidated

- GOAL-013: Application configuration separated from infrastructure configuration
- GOAL-014: Dapr Configuration components deployed and functional
- GOAL-015: Sample application service migrated to use Dapr Configuration API

| Task ID  | Description                                                                                     | Location                                                       | DependsOn | Status     | Date |
|----------|-------------------------------------------------------------------------------------------------|----------------------------------------------------------------|-----------|------------|------|
| TASK-047 | Create Dapr Configuration component for local environment (Redis-backed)                        | /charts/reddog/templates/dapr-components/configuration.yaml   | TASK-046  | NotStarted |      |
| TASK-047a | Research cloud-native configuration backends: Azure App Configuration, AWS AppConfig, GCP alternatives | /plan/configuration-consolidation-implementation-1.md#Phase-5-Cloud-Config-Backends | TASK-047  | NotStarted |      |
| TASK-047b | Decide cloud configuration backend strategy: native services vs Redis sync vs hybrid approach  | /plan/configuration-consolidation-implementation-1.md#Phase-5-Cloud-Config-Backends | TASK-047a | NotStarted |      |
| TASK-047c | Parameterize Dapr Configuration component template to support multiple backends via Helm values | /charts/reddog/templates/dapr-components/configuration.yaml   | TASK-047b | NotStarted |      |
| TASK-047d | Document configuration backend mapping: local=Redis, AKS=App Configuration, EKS=AppConfig, GKE=? | /plan/configuration-consolidation-implementation-1.md#Phase-5-Cloud-Config-Backends | TASK-047c | NotStarted |      |
| TASK-048 | Create sample application configuration in Redis for OrderService (maxOrderSize, storeId, etc.) | /values/values-local.yaml OR /scripts/seed-config.sh         | TASK-047  | NotStarted |      |
| TASK-049 | Add Dapr Configuration SDK to OrderService dependencies (.NET)                                  | /RedDog.OrderService/RedDog.OrderService.csproj               | TASK-048  | NotStarted |      |
| TASK-050 | Implement configuration retrieval in OrderService using DaprClient.GetConfigurationAsync()      | /RedDog.OrderService/Program.cs                               | TASK-049  | NotStarted |      |
| TASK-051 | Remove hardcoded business configuration from OrderService code (migrate to Dapr Config)         | /RedDog.OrderService/                                         | TASK-050  | NotStarted |      |
| TASK-052 | Test OrderService configuration retrieval in local kind cluster                                 | Local kind cluster                                            | TASK-051  | NotStarted |      |
| TASK-052a | Define configuration versioning strategy: decide whether to use key prefixes (v1/maxOrderSize), deployment-tied versions, or backend native versioning (Azure App Config labels). Document chosen approach for rollback capability | /plan/configuration-consolidation-implementation-1.md#Phase-5-Config-Versioning | TASK-052  | NotStarted |      |
| TASK-052b | Document configuration rollback procedure: how to revert to previous config version if bad value deployed, testing requirements before promoting config changes | /plan/configuration-consolidation-implementation-1.md#Phase-5-Config-Versioning | TASK-052a | NotStarted |      |
| TASK-052c | Explicitly defer Dapr Configuration API subscriptions (watch for config changes) to future implementation plan. Phase 5 implements read-only configuration retrieval only; subscriptions require additional complexity (reconnect logic, state management, graceful updates) | /plan/configuration-consolidation-implementation-1.md#Phase-5 | TASK-052  | NotStarted |      |
| TASK-053 | Document Dapr Configuration API patterns in existing KI-DAPR-CONFIGURATION-API-001              | /knowledge/dapr-configuration-api-ki.md                       | TASK-052,TASK-052a,TASK-052b,TASK-052c  | NotStarted |      |
| TASK-054 | Create migration plan for remaining services (MakeLine, Loyalty, Accounting, etc.)              | /plan/dapr-configuration-api-rollout-implementation-1.md      | TASK-053  | NotStarted |      |
| TASK-055 | Update ADR-0004 implementation status to "Completed (Read-Only)" once OrderService migrated. Document that subscriptions (config change watching) are deferred to separate implementation plan | /docs/adr/adr-0004-dapr-configuration-api-standardization.md  | TASK-054  | NotStarted |      |

**Completion Criteria for Phase 5:**
- Dapr Configuration component deployed and accessible in local environment
- **Cloud configuration backend strategy decided** (TASK-047a, TASK-047b, TASK-047d)
- **Configuration component template parameterized** for multi-cloud backends (TASK-047c)
- OrderService successfully retrieves application config via Dapr Configuration API
- **Configuration versioning and rollback strategy documented** (TASK-052a, TASK-052b)
- **Configuration subscriptions explicitly deferred** to separate future implementation plan (TASK-052c)
- Business config removed from code and environment variables
- ADR-0004 no longer blocked (read-only configuration retrieval implemented)
- **Clear path documented** for Azure App Configuration, AWS AppConfig, GCP alternatives
- TEST-005 passes (see section 6)

---

### Implementation Phase 6: Documentation and Validation

**Goal:** Comprehensive documentation updates and final validation across all environments

- GOAL-016: All ADRs updated to reflect new configuration architecture
- GOAL-017: Knowledge Items updated with current patterns
- GOAL-018: Deployment guides updated for all environments

| Task ID  | Description                                                                                     | Location                                                       | DependsOn | Status     | Date |
|----------|-------------------------------------------------------------------------------------------------|----------------------------------------------------------------|-----------|------------|------|
| TASK-056 | Update ADR-0002 to reflect unified Dapr component locations                                     | /docs/adr/adr-0002-cloud-agnostic-configuration-via-dapr.md   | TASK-055  | NotStarted |      |
| TASK-057 | Update ADR-0009 implementation status to "Completed" (Helm as single deployment tool)          | /docs/adr/adr-0009-helm-multi-environment-deployment.md       | TASK-055  | NotStarted |      |
| TASK-058 | Update ADR-0013 to reference new values file structure, secret handling patterns, and document official secret injection pattern chosen in TASK-023c (ESO/CSI/CI substitution). Specify which patterns are allowed in which environments | /docs/adr/adr-0013-secret-management-strategy.md              | TASK-055  | NotStarted |      |
| TASK-059 | Create new Knowledge Item documenting configuration architecture post-consolidation. Include diagram/textual map of configuration layers and data flow (Chart defaults → repo values → Secrets/Dapr Config → App). Document escape hatch keys for user-facing use: `services.<name>.podSpec`, `services.<name>.containerSpec`, `dapr.<component>.extraMetadata` | /knowledge/configuration-architecture-ki.md                   | TASK-056,TASK-057,TASK-058 | NotStarted |      |
| TASK-060 | Update `docs/adr/README.md` to highlight configuration-related ADRs                            | /docs/adr/README.md                                           | TASK-059  | NotStarted |      |
| TASK-061 | Update or create deployment guide for local/kind environment                                    | /docs/deployment-local.md                                     | TASK-059  | NotStarted |      |
| TASK-062 | Update or create deployment guide for AKS environment                                           | /docs/deployment-aks.md                                       | TASK-059  | NotStarted |      |
| TASK-063 | Update or create deployment guide for EKS environment                                           | /docs/deployment-eks.md                                       | TASK-059  | NotStarted |      |
| TASK-064 | Update or create deployment guide for GKE environment                                           | /docs/deployment-gke.md                                       | TASK-059  | NotStarted |      |
| TASK-065 | Run full integration test suite against local kind cluster                                     | Local kind cluster                                            | TASK-061  | NotStarted |      |
| TASK-066 | Run full integration test suite against AKS dev cluster (if available)                         | AKS dev cluster                                               | TASK-062  | NotStarted |      |
| TASK-067 | Create configuration consolidation completion report summarizing changes and benefits           | /plan/configuration-consolidation-completion-report.md        | TASK-065,TASK-066 | NotStarted |      |

**Completion Criteria for Phase 6:**
- All ADRs reflect current configuration architecture
- Deployment guides accurate and tested for all environments
- Integration tests pass in at least local and one cloud environment
- TEST-006 passes (see section 6)

---

## 3. Alternatives

### Alternative 1: Keep Both Helm and Kustomize

- **ALT-001**: Continue maintaining parallel Helm charts and Kustomize manifests, with clear boundaries for when to use each
- **Reason for rejection**: Increases maintenance burden, creates confusion about which is canonical, perpetuates duplication. ADR-0009 already established Helm as the standard. Keeping Kustomize contradicts the architectural decision and maintains the very sprawl this plan aims to eliminate.

### Alternative 2: Switch to Kustomize-Only

- **ALT-002**: Deprecate Helm entirely, migrate all configuration to Kustomize base + overlays
- **Reason for rejection**: Kustomize lacks templating power needed for complex Dapr component parameterization across environments. Helm better handles external dependencies (RabbitMQ, cert-manager, KEDA) and provides richer ecosystem integration. ADR-0009 already committed to Helm-based strategy.

### Alternative 3: Adopt GitOps-Driven Configuration (Flux/Argo-First)

- **ALT-003**: Implement Flux or ArgoCD with native GitOps patterns before consolidating configuration
- **Reason for rejection**: Putting cart before horse—GitOps tools need clean configuration to manage. Current sprawl makes GitOps implementation more complex, not simpler. Better to consolidate first, then adopt GitOps tooling cleanly in a future phase. GitOps adoption (Flux HelmRelease or ArgoCD Application resources) will be addressed in a separate implementation plan after this configuration consolidation is complete.

### Alternative 4: Microservices-Specific Configuration Files

- **ALT-004**: Each service maintains its own configuration file/directory with service-specific settings
- **Reason for rejection**: Violates ADR-0002 (Dapr abstraction) and ADR-0009 (Helm charts). Creates per-service configuration sprawl instead of solving it. Makes multi-service changes (like Dapr version upgrades) exponentially harder.

### Alternative 5: Configuration-as-Code with Pulumi/CDK

- **ALT-005**: Adopt Pulumi or AWS CDK for infrastructure and configuration management using TypeScript/Python
- **Reason for rejection**: Red Dog is a Kubernetes-first teaching sample. Helm is already adopted (ADR-0009) and provides Kubernetes-native patterns. Pulumi/CDK would be a major architectural pivot requiring rewriting existing charts and retraining contributors.

### Alternative 6: Gradual Migration Without Removing Legacy

- **ALT-006**: Build new consolidated structure alongside existing sprawl, never remove old files
- **Reason for rejection**: Leaves technical debt in place indefinitely. Developers will continue using "whatever works," perpetuating inconsistency. Plan explicitly archives (not deletes) old structures, preserving history while establishing clear direction forward.

## 4. Dependencies

- **DEP-001**: Helm 3.x (already in use, version recorded in `global.json` or scripts)
- **DEP-002**: Dapr 1.16.2 (current version per ADR-0002 and `values/values-local.yaml`)
- **DEP-003**: Kubernetes 1.28+ (supported across kind, AKS, EKS, GKE)
- **DEP-004**: kind v0.20.0+ for local development testing (per ADR-0008)
- **DEP-005**: External Helm charts:
  - `nginx-ingress` (charts/external/nginx-ingress/)
  - `rabbitmq` (charts/external/rabbitmq/)
  - `cert-manager` (charts/external/cert-manager/)
  - KEDA (version to be determined in separate plan)
- **DEP-006**: Redis 7.x for local state stores, pub/sub, and configuration (charts/infrastructure/)
- **DEP-006a**: Azure App Configuration (AKS environments, to be provisioned)
- **DEP-006b**: AWS AppConfig or AWS Systems Manager Parameter Store (EKS environments, to be provisioned)
- **DEP-006c**: GCP Secret Manager or Firebase Remote Config (GKE environments, to be researched in TASK-047a)
- **DEP-007**: SQL Server 2022 Developer Edition for local database (charts/infrastructure/)
- **DEP-008**: .NET 10 SDK for any code changes to services (per ADR-0001)
- **DEP-009**: Git and GitHub for version control and collaboration
- **DEP-010**: CI/CD pipeline (GitHub Actions or equivalent) for automated testing

## 5. Files

### Core Configuration Files

- **FILE-001**: `/values/values-local.yaml` — Local/kind environment configuration (currently 370 lines, to be restructured)
- **FILE-002**: `/values/values-local.yaml.sample` — Template for local configuration (to be created)
- **FILE-003**: `/values/values-azure.yaml` — AKS environment configuration (to be created)
- **FILE-004**: `/values/values-aws.yaml` — EKS environment configuration (to be created)
- **FILE-005**: `/values/values-gcp.yaml` — GKE environment configuration (to be created)
- **FILE-006**: `/values/README.md` — Documentation for values file structure (to be created)

### Helm Charts

- **FILE-007**: `/charts/reddog/Chart.yaml` — Main application chart definition
- **FILE-008**: `/charts/reddog/values.yaml` — Default values for application chart
- **FILE-009**: `/charts/reddog/templates/dapr-components/*.yaml` — Unified Dapr component templates (7 files)
- **FILE-010**: `/charts/reddog/templates/*-service.yaml` — Service deployment templates (7 files)
- **FILE-011**: `/charts/infrastructure/Chart.yaml` — Infrastructure chart definition
- **FILE-012**: `/charts/infrastructure/values.yaml` — Default values for infrastructure (to be enhanced)

### Components to Archive

- **FILE-013**: `/.dapr/components/*.yaml` — Local Dapr components (5 files, to be archived)
- **FILE-014**: `/manifests/branch/base/components/*.yaml` — Branch Dapr components (7 files, to be archived)
- **FILE-015**: `/manifests/branch/base/deployments/*.yaml` — Raw deployment manifests (to be archived)
- **FILE-016**: `/manifests/overlays/` — Kustomize overlays (entire directory to be archived)

### Application Configuration Files

- **FILE-017**: `/RedDog.OrderService/appsettings.json` — OrderService config (to be minimized)
- **FILE-018**: `/RedDog.MakeLineService/appsettings.json` — MakeLineService config (to be minimized)
- **FILE-019**: `/RedDog.LoyaltyService/appsettings.json` — LoyaltyService config (to be minimized)
- **FILE-020**: `/RedDog.AccountingService/appsettings.json` — AccountingService config (to be minimized)
- **FILE-021**: `/RedDog.ReceiptGenerationService/appsettings.json` — ReceiptGenerationService config (to be minimized)

### Scripts

- **FILE-022**: `/scripts/setup-local-dev.sh` — Local development setup script (to be updated)
- **FILE-023**: `/scripts/upgrade-dotnet10.sh` — .NET 10 upgrade script (references values files)
- **FILE-023a**: `/scripts/migrate-state-keys.sh` — State store key migration script for app ID changes (to be created for Phase 1)
- **FILE-024**: `/scripts/seed-config.sh` — Configuration seeding script (to be created for Phase 5)

### Documentation

- **FILE-025**: `/docs/adr/adr-0002-cloud-agnostic-configuration-via-dapr.md` — Dapr abstraction ADR (to be updated)
- **FILE-026**: `/docs/adr/adr-0004-dapr-configuration-api-standardization.md` — Dapr Config API ADR (to be updated)
- **FILE-027**: `/docs/adr/adr-0009-helm-multi-environment-deployment.md` — Helm deployment ADR (to be updated)
- **FILE-028**: `/docs/adr/adr-0013-secret-management-strategy.md` — Secret management ADR (to be updated)
- **FILE-028a**: `/docs/deployment-pipeline-secrets.md` — Cloud secret injection patterns for CI/CD (to be created in Phase 2)
- **FILE-029**: `/knowledge/dapr-components-cloud-agnostic-ki.md` — Dapr components KI (to be updated)
- **FILE-030**: `/knowledge/dapr-configuration-api-ki.md` — Dapr Configuration API KI (to be updated)

## 6. Testing

### Unit Tests (where applicable)

- **TEST-001**: Dapr component YAML validation
  - Validates all Dapr component files in `charts/reddog/templates/dapr-components/` are syntactically correct
  - Ensures all required metadata fields present (name, namespace, type, version)
  - Verifies app ID consistency across all component scopes
  - Mapped to: TASK-007, TASK-008

### Integration Tests

- **TEST-002**: Helm template rendering validation
  - Renders Helm charts with each environment values file (`values-local.yaml`, `values-azure.yaml`, `values-aws.yaml`, `values-gcp.yaml`)
  - Validates output YAML is valid Kubernetes manifests
  - Ensures no secret values appear in non-Secret Kubernetes resources (ConfigMaps, Deployments, Services, env literals, annotations)
  - Secret values may only appear in `kind: Secret` objects and must reference external sources (ESO/CSI/CI-injected), never inline raw secrets from committed values files
  - Validates that rendered Secret objects contain only references (secretName/key patterns) or ESO/CSI annotations, not raw secret strings
  - **Local dev exception**: For local/kind environment without ESO/CSI, secrets may be injected out-of-band via `kubectl create secret` or similar scripts after Helm deployment; TEST-002 passes if Helm templates contain no inline secrets, even if runtime secrets are created manually
  - Mapped to: TASK-024

- **TEST-003**: Local kind deployment end-to-end
  - Deploys infrastructure chart to local kind cluster
  - Deploys reddog chart to local kind cluster
  - Verifies all pods reach Ready state
  - Executes health checks on all services
  - Tests inter-service communication (OrderService → MakeLineService)
  - Validates Dapr components are discovered and accessible
  - Mapped to: TASK-008, TASK-009, TASK-031, TASK-045

- **TEST-004**: Infrastructure configuration isolation
  - Verifies no connection strings in appsettings.json files
  - Confirms infrastructure connectivity is configured via environment variables (Dapr ports/addresses, component names) with NO connection strings or credentials in those env vars
  - Validates SEC-005: applications have zero access to connection strings; only Dapr components hold them
  - Tests Redis, SQL Server, RabbitMQ accessibility via Dapr
  - Mapped to: TASK-038, TASK-039, TASK-045, SEC-005

- **TEST-005**: Dapr Configuration API functionality
  - Seeds test configuration in Redis for OrderService
  - Deploys OrderService with Dapr Configuration component
  - Verifies OrderService successfully retrieves config via DaprClient.GetConfigurationAsync()
  - Tests configuration update (manual update in backend, app restart or re-fetch shows new value)
  - Does NOT test subscriptions (out of scope for Phase 5 per TASK-052c)
  - Verifies configuration versioning/rollback strategy is documented and testable
  - Mapped to: TASK-052, TASK-052a, TASK-052b

- **TEST-006**: Multi-environment validation
  - Renders templates for all environments (local, Azure, AWS, GCP)
  - Validates environment-specific differences are contained to values files
  - Ensures no environment-specific code paths in templates
  - Mapped to: TASK-065, TASK-066

### Manual Testing Checklist

- Deploy to local kind cluster using `helm upgrade --install`
- Verify UI accessible and functional
- Place test order through UI, verify flows through all services
- Check Dapr dashboard shows correct components and app IDs
- Verify logs show no configuration-related errors
- Test configuration change workflow (update values, helm upgrade, verify pickup)

### Continuous Integration Tests

- **TEST-007**: Helm lint for all charts
  - Runs `helm lint ./charts/reddog` and `helm lint ./charts/infrastructure`
  - Fails build on any warnings or errors
  - Mapped to: GUD-004

- **TEST-008**: Values file schema validation
  - Validates structure of all `values/values-*.yaml.sample` files
  - Ensures required top-level keys present (global, dapr, infrastructure, services, observability)
  - Fails build on missing required sections
  - Implemented via Helm JSON schema (`values.schema.json`) or CUE/JSON Schema validator in CI

- **TEST-009**: Secret scanning
  - Runs `gitleaks`, `git-secrets`, or equivalent secret detection tool over entire repository
  - Scans for patterns matching API keys, passwords, certificates, cloud credentials
  - Fails build on any detected secrets
  - Mapped to: SEC-001, RISK-003, TASK-023

## 7. Risks & Assumptions

### Critical Risks

- **RISK-001**: Migration introduces service downtime or functional regression
  - **Impact**: High (production services affected)
  - **Probability**: Medium (thorough testing mitigates)
  - **Mitigation**: Incremental migration with testing at each phase, maintain ability to rollback to previous state, conduct migration in lower environments first

- **RISK-002**: Dapr app ID naming change causes STATE DATA LOSS
  - **Impact**: CRITICAL (permanent data loss in state stores)
  - **Probability**: HIGH (Dapr state keys are prefixed by app ID)
  - **Mitigation**: 
    - TASK-006a creates migration script to rename Redis keys (`RENAME orderservice||* order-service||*`)
    - For local: Acceptable to flush state and start fresh (development data only)
    - For cloud/shared environments: MUST coordinate with stakeholders, backup state stores, run migration script BEFORE deploying new app IDs
    - Test migration script in isolated environment before production
    - Document that MakeLineService and LoyaltyService use stateful Dapr components and are HIGH RISK

- **RISK-002b**: Dapr app ID naming change breaks inter-service communication
  - **Impact**: High (distributed system failure)
  - **Probability**: Medium (requires coordinated change)
  - **Mitigation**: Change app IDs in both Dapr components AND service annotations simultaneously (TASK-004, TASK-005), test extensively in local environment before cloud deployment

- **RISK-003**: Secret exposure during migration due to gitignore misconfiguration
  - **Impact**: Critical (security incident)
  - **Probability**: Low (explicit TASK-023 addresses this)
  - **Mitigation**: Add gitignore entries BEFORE creating environment-specific values files, use pre-commit hooks to scan for secrets, maintain sample files with placeholders only

- **RISK-003b**: Cloud secret injection mechanism misconfigured or incomplete
  - **Impact**: High (cloud environments non-functional)
  - **Probability**: MEDIUM-HIGH (complex multi-cloud authentication patterns)
  - **Mitigation**: 
    - TASK-023a through TASK-023e explicitly design and document secret injection architecture
    - Choose ONE of three patterns: (1) External Secrets Operator + Workload Identity, (2) CSI Driver + Workload Identity, (3) CI/CD token substitution
    - Document bootstrap secrets (how do we authenticate to Key Vault/Secrets Manager to fetch other secrets?)
    - Validate against ADR-0013 three-layer model (source → transport → consumption)
    - If mechanism not implemented or misconfigured, cloud deployments will fail at secret resolution

- **RISK-004**: Loss of working configuration during archive/deletion of legacy files
  - **Impact**: Medium (recovery effort required)
  - **Probability**: Low (files archived, not deleted)
  - **Mitigation**: Archive to `manifests/Archive/` and `.dapr/components.deprecated/` rather than delete, include README explaining migration path, commit archives before removing references

- **RISK-005**: ADR-0004 implementation complexity underestimated
  - **Impact**: Medium (schedule delay)
  - **Probability**: Medium (new API adoption)
  - **Mitigation**: Phase 5 starts with single service (OrderService) as pilot, create detailed patterns in Knowledge Item before broader rollout, separate implementation plan for full rollout (TASK-054)

- **RISK-005b**: Dapr Configuration backend incompatible or misconfigured for cloud environments
  - **Impact**: HIGH (ADR-0004 works locally but fails in cloud)
  - **Probability**: MEDIUM-HIGH (Dapr backend support varies by cloud, sync complexity)
  - **Mitigation**:
    - TASK-047a researches cloud-native configuration services (Azure App Configuration, AWS AppConfig, GCP alternatives) AND verifies Dapr native support
    - TASK-047b makes explicit decision: native cloud services vs Redis sync vs hybrid
    - TASK-047c parameterizes Helm template to swap backends per environment
    - Document trade-offs: native services (better features, cloud-specific) vs Redis everywhere (simpler, less capable)
    - Critical: Capture "supported by Dapr natively vs needs a sync job" explicitly in TASK-047a research doc
    - If backend not natively supported by Dapr, document sync strategy (cloud config service → Redis via sidecar or CronJob)

- **RISK-006**: External Helm chart version incompatibilities
  - **Impact**: Medium (deployment failures)
  - **Probability**: Medium (external dependencies)
  - **Mitigation**: Pin external chart versions in Chart.yaml dependencies, test infrastructure chart separately from application chart, document tested versions in DEP-005

- **RISK-006b**: Helm rigidity prevents environment-specific configurations that Kustomize allowed
  - **Impact**: High (production requirements unmet, forces chart rewrites)
  - **Probability**: MEDIUM-HIGH (Kustomize patches likely exist for production needs)
  - **Mitigation**:
    - TASK-026a explicitly audits ALL Kustomize patches to identify requirements
    - TASK-026b documents what needs escape hatches (topologySpread, affinity, tolerations, securityContext overrides, etc.)
    - TASK-026c/d implement escape hatch pattern: `{{ with .Values.<service>.podSpec }}{{ toYaml . | nindent N }}{{ end }}`
    - Establish convention: ALL production-specific config goes through escape hatches, NOT by editing templates
    - Document escape hatch usage in deployment guides so future patches don't require chart changes

- **RISK-007**: Team unfamiliarity with Helm templating causes errors
  - **Impact**: Medium (implementation delays, technical debt)
  - **Probability**: Medium (skills gap)
  - **Mitigation**: Provide Helm training/documentation, establish code review requirements for template changes, leverage existing Helm knowledge from charts that already work

### Assumptions

- **ASSUMPTION-001**: Local development will continue using kind (not Docker Compose or minikube)
  - **Basis**: ADR-0008 establishes kind as standard local environment
  - **Validation**: Review ADR-0008 and current scripts/setup-local-dev.sh

- **ASSUMPTION-002**: All target cloud environments support Helm-based deployments
  - **Basis**: AKS, EKS, and GKE all have mature Helm support
  - **Validation**: Confirmed by ADR-0009 and industry standard practices

- **ASSUMPTION-003**: No production deployments exist yet (or can tolerate maintenance window)
  - **Basis**: Red Dog is primarily a teaching/demo sample
  - **Validation**: Coordinate with repository maintainer before starting migration

- **ASSUMPTION-003b**: Cloud environments either do not exist or do not have established secret injection patterns requiring backward compatibility
  - **Basis**: ADR-0013 references ESO/CSI patterns but doesn't indicate they're implemented
  - **Validation**: Audit existing cloud deployments before TASK-023a to determine if secret injection already exists; if yes, document current pattern and decide whether to replace or extend
  - **Risk if wrong**: Existing cloud deployments may break if we change secret injection mechanism without migration plan

- **ASSUMPTION-004**: Database schema changes are out of scope for this plan
  - **Basis**: Focus is configuration architecture, not data model
  - **Validation**: Audit TASK-036 findings; escalate if DB config changes required

- **ASSUMPTION-005**: All services can tolerate STATE LOSS during Phase 1 app ID migration (local environment only)
  - **Basis**: Local development data is disposable; production/shared environments require explicit state migration strategy
  - **Validation**: Confirm with stakeholders before executing TASK-008; for non-local environments, MUST execute TASK-006a migration script
  - **Services with persistent state affected**: MakeLineService (reddog.state.makeline), LoyaltyService (reddog.state.loyalty)

- **ASSUMPTION-006**: Redis is acceptable backing store for Dapr Configuration API in local environment, but NOT for cloud
  - **Basis**: ADR-0004 mentions Redis as supported backend, already in use for state/pubsub; however, production needs versioning, audit trails, and managed services
  - **Validation**: 
    - Verify Dapr Configuration component Redis support in Dapr 1.16.2 docs (for local)
    - Research Dapr support for Azure App Configuration, AWS AppConfig, GCP alternatives (for cloud)
    - If cloud backend unsupported, plan sync mechanism or accept Redis for all environments (document trade-offs)
  - **Critical Decision**: Cloud configuration backend choice affects Phase 5 timeline and complexity

- **ASSUMPTION-007**: Kustomize patches can be migrated to Helm values OR deprecated
  - **Basis**: Audit in TASK-026a will identify ALL patches; TASK-026b determines migration strategy
  - **Validation**: If patches exist that CANNOT be expressed via Helm values OR escape hatches, plan must be revised to either:
    - Enhance Helm templates to support the use case
    - Deprecate the requirement (if it's a workaround for old infrastructure)
    - Keep minimal Kustomize post-processing (ONLY if absolutely necessary, violates ADR-0009)
  - **Critical Decision Point**: If complex Kustomize transformations exist (e.g., strategic merge patches, JSON patches, name/namespace transformations), Phase 3 timeline may need extension

- **ASSUMPTION-008**: Current container images are compatible with new configuration structure
  - **Basis**: Configuration changes are external to application code (per ADR-0002, ADR-0006)
  - **Validation**: Confirm no hardcoded configuration assumptions in application code during TASK-037

- **ASSUMPTION-009**: CI/CD pipelines can be updated without major refactoring
  - **Basis**: Moving from kubectl apply to helm upgrade is straightforward
  - **Validation**: Audit in TASK-027 will identify any complex pipeline logic requiring redesign

- **ASSUMPTION-010**: External chart dependencies (RabbitMQ, cert-manager, nginx-ingress, KEDA) will remain at current versions during migration
  - **Basis**: Upgrading dependencies and consolidating config are independent concerns
  - **Validation**: Note in plan that external chart upgrades are explicitly out of scope and should be separate implementation plans

## 8. Related Specifications / Further Reading

### Architectural Decision Records

- [ADR-0002: Cloud-Agnostic Configuration via Dapr Abstraction](/docs/adr/adr-0002-cloud-agnostic-configuration-via-dapr.md) — Establishes Dapr as abstraction layer, defines requirement for cloud-agnostic components
- [ADR-0004: Application Configuration via Dapr Configuration API](/docs/adr/adr-0004-dapr-configuration-api-standardization.md) — Defines application configuration strategy, currently blocked by infrastructure sprawl
- [ADR-0006: Infrastructure Configuration via Environment Variables](/docs/adr/adr-0006-infrastructure-configuration-via-environment-variables.md) — Defines infrastructure vs application config separation
- [ADR-0009: Helm-Based Multi-Environment Deployment Strategy](/docs/adr/adr-0009-helm-multi-environment-deployment.md) — Establishes Helm as deployment standard, currently incomplete
- [ADR-0013: Secret Management Strategy](/docs/adr/adr-0013-secret-management-strategy.md) — Defines three-layer secret model (source, transport, consumption)

### Knowledge Items

- [KI-DAPR-COMPONENTS-CLOUD-AGNOSTIC-001](/knowledge/dapr-components-cloud-agnostic-ki.md) — Facts and patterns for Dapr components across environments
- [KI-DAPR-CONFIGURATION-API-001](/knowledge/dapr-configuration-api-ki.md) — Facts and patterns for Dapr Configuration API usage
- [KI-DEPLOY-HELM-MULTI-ENVIRONMENT-001](/knowledge/deploy-helm-multi-environment-ki.md) — Helm deployment patterns and conventions

### Implementation Plans

- [Modernization Strategy](/plan/modernization-strategy.md) — Overall Red Dog modernization approach and timeline
- [Upgrade Phase 0: Platform Foundation](/plan/upgrade-phase0-platform-foundation-implementation-1.md) — Infrastructure baseline and prerequisites

### External Documentation

- [Helm Documentation](https://helm.sh/docs/) — Helm charts, values, templates, and best practices
- [Dapr Components Documentation](https://docs.dapr.io/reference/components-reference/) — Dapr component specifications and configuration
- [Dapr Configuration API](https://docs.dapr.io/developing-applications/building-blocks/configuration/) — Dapr Configuration building block reference
- [Kubernetes Configuration Best Practices](https://kubernetes.io/docs/concepts/configuration/) — Kubernetes-native config patterns
- [12-Factor App: Config](https://12factor.net/config) — Foundational principles for configuration management

### Repository Context

- [AGENTS.md](/AGENTS.md) — Repository contract defining workflow modes (PLANNING → EXECUTION → VERIFICATION)
- [Session Logs](/.claude/sessions/) — Historical context of configuration decisions and pain points
- [Scripts](/scripts/) — Automation scripts affected by configuration changes
