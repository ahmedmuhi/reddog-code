---
title: "ADR-0009: Helm-Based Multi-Environment Deployment Strategy"
status: "Accepted"
date: "2025-11-09"
authors: "Red Dog Modernization Team"
tags: ["architecture", "decision", "helm", "multi-cloud", "deployment", "kubernetes"]
supersedes: ""
superseded_by: ""
---

# ADR-0009: Helm-Based Multi-Environment Deployment Strategy

## Status

**Accepted**

## Implementation Status

**Current State:** ⚪ Planned (Not Implemented)

**What's Working:**
- Decision documented with complete Helm chart structure
- Values file strategy designed (values-local.yaml, values-azure.yaml, values-aws.yaml, values-gke.yaml)
- Template examples created for Dapr components, service deployments

**What's Not Working:**
- charts/ directory doesn't exist in repository
- No Helm Chart.yaml files created
- No values files (values-local.yaml, values-azure.yaml, etc.) created
- No Helm templates for services or Dapr components
- Current deployment uses raw manifests in manifests/branch/ (not Helm charts)

**Evidence:**
- Repository search for "charts/" directory returns zero results
- manifests/branch/ contains raw Kubernetes YAML (pre-Helm approach)
- No Chart.yaml files exist for reddog or infrastructure charts

**Dependencies:**
- **Depends On:** ADR-0007 (Containerized infrastructure to deploy via Helm)
- **Blocks:** ADR-0008 (kind local dev needs Helm charts to deploy)
- **Supports:** Multi-environment deployment (local, AKS, EKS, GKE)

**Next Steps:**
1. Create charts/reddog/ directory with Chart.yaml for application services
2. Create charts/infrastructure/ directory for Dapr, Nginx, RabbitMQ, Redis
3. Create templates/ subdirectories with service deployments and Dapr components
4. Create values/values-local.yaml with localhost configuration
5. Create values/values-azure.yaml, values-aws.yaml, values-gke.yaml for cloud deployments
6. Test: `helm install reddog ./charts/reddog -f values/values-local.yaml`

## Context

Red Dog's modernization strategy requires deploying to four distinct environments with cloud-agnostic architecture (ADR-0007):
- **Local**: kind cluster (ADR-0008)
- **Azure**: Azure Kubernetes Service (AKS)
- **AWS**: Elastic Kubernetes Service (EKS)
- **GCP**: Google Kubernetes Engine (GKE)

**Key Constraints:**
- **DRY Principle**: Maintain single source of truth for Kubernetes manifests; avoid duplicating YAML across environments
- **Cloud-Agnostic Architecture**: Same application code and manifests must work across all platforms (ADR-0007)
- **Teaching/Demo Focus**: Instructors must demonstrate "deploy once, run anywhere" with minimal configuration changes
- **Environment-Specific Configuration**: Each environment has different infrastructure (Redis locally, Cosmos DB on Azure, DynamoDB on AWS, Firestore on GCP)
- **Dapr Component Variability**: Dapr state stores, pub/sub, and secret stores differ per environment
- **Production Parity**: Local development must use identical manifests as production (ADR-0008)

**Configuration Differences Across Environments:**

| Component | Local (kind) | Azure (AKS) | AWS (EKS) | GCP (GKE) |
|-----------|--------------|-------------|-----------|-----------|
| **State Store** | Redis (dapr init) | Azure Cosmos DB | AWS DynamoDB | GCP Firestore |
| **Pub/Sub** | Redis | RabbitMQ | RabbitMQ | RabbitMQ |
| **Database** | SQL Server container | SQL Server container | SQL Server container | SQL Server container |
| **Secret Store** | Kubernetes secrets | Azure Key Vault | AWS Secrets Manager | GCP Secret Manager |
| **Ingress** | Nginx (localhost:80) | Nginx (Azure LB) | Nginx (AWS NLB) | Nginx (GCP LB) |
| **DNS** | localhost | reddog.azure.example.com | reddog.aws.example.com | reddog.gcp.example.com |

**Current State (Before This ADR):**
- Kubernetes manifests exist in `manifests/branch/` for production
- No local development manifests (deleted November 2, 2025)
- No standardized approach for multi-environment deployments
- Infrastructure deployed via Flux GitOps with Helm releases

**Available Approaches:**

| Approach | Pros | Cons |
|----------|------|------|
| **Raw Manifests + Kustomize** | Kubernetes-native, no external dependencies | Limited templating, complex overlays |
| **Helm Charts** | Industry standard, rich templating, values files | Learning curve for Helm syntax |
| **RADIUS (Microsoft)** | Modern platform engineering | No GCP support (ADR evaluation rejected) |
| **Pulumi/Terraform** | Multi-cloud provisioning | Overkill for application deployment |
| **Environment-Specific Directories** | Simple | Massive duplication, violates DRY |

## Decision

**Adopt Helm charts with environment-specific values files as the standard deployment mechanism for Red Dog Coffee across all four environments.**

**Implementation:**

```
charts/
├── reddog/                    # Main application chart
│   ├── Chart.yaml
│   ├── templates/             # Kubernetes manifests (identical across environments)
│   │   ├── order-service.yaml
│   │   ├── makeline-service.yaml
│   │   ├── loyalty-service.yaml
│   │   ├── accounting-service.yaml
│   │   ├── receipt-generation-service.yaml
│   │   ├── virtual-customers.yaml
│   │   ├── virtual-worker.yaml
│   │   ├── ui.yaml
│   │   └── dapr-components/   # Dapr component templates
│   │       ├── pubsub.yaml
│   │       ├── statestore-makeline.yaml
│   │       ├── statestore-loyalty.yaml
│   │       └── secretstore.yaml
│   └── values/                # Environment-specific configurations
│       ├── values-local.yaml
│       ├── values-azure.yaml
│       ├── values-aws.yaml
│       └── values-gcp.yaml
└── infrastructure/            # Infrastructure dependencies (Dapr, Nginx, etc.)
    └── Chart.yaml
```

**Deployment Commands:**

```bash
# Local (kind)
helm install reddog ./charts/reddog -f values/values-local.yaml

# Azure (AKS)
helm install reddog ./charts/reddog -f values/values-azure.yaml

# AWS (EKS)
helm install reddog ./charts/reddog -f values/values-aws.yaml

# GCP (GKE)
helm install reddog ./charts/reddog -f values/values-gcp.yaml
```

**Rationale:**

- **HELM-001: Single Source of Truth**: Application manifests defined once in `templates/`, reused across all environments
- **HELM-002: Environment Isolation**: Configuration differences isolated to values files; no manifest duplication
- **HELM-003: Industry Standard**: Helm is CNCF graduated project with widespread adoption (78% of Kubernetes users per 2024 survey)
- **HELM-004: Complements ADRs**: Works seamlessly with ADR-0002 (Dapr abstraction), ADR-0004 (Config API), ADR-0006 (env vars), ADR-0007 (containerized infrastructure)
- **HELM-005: Teaching Clarity**: "Same chart, different values" reinforces cloud-agnostic architecture concept
- **HELM-006: Deployment-Time vs Runtime Separation**: Helm handles deployment-time decisions (which Dapr component); Dapr handles runtime abstraction (how services connect)
- **HELM-007: Extensibility**: Easy to add new environments (on-premises, additional clouds) by creating new values files
- **HELM-008: GitOps Compatible**: Flux, ArgoCD, and other GitOps tools have native Helm support

## Consequences

### Positive

- **POS-001: DRY Compliance**: Zero manifest duplication; 99% of YAML identical across environments
- **POS-002: Configuration Clarity**: All environment differences visible in one file (`values-{env}.yaml`)
- **POS-003: Easy Environment Additions**: Adding on-premises or additional cloud requires only new values file
- **POS-004: Consistent Deployments**: Same `helm install` command works locally, in CI/CD, and for manual deployments
- **POS-005: Version Control**: Helm chart versions enable rollbacks and release management
- **POS-006: Dependency Management**: Helm handles dependencies (Dapr, Nginx Ingress) via `Chart.yaml` requirements
- **POS-007: Templating Power**: Conditional logic, loops, and functions reduce manifest complexity
- **POS-008: Testing Support**: `helm template` enables offline validation and CI/CD preview
- **POS-009: Namespace Isolation**: Helm releases support multiple deployments in same cluster (dev, staging, prod namespaces)
- **POS-010: Community Charts**: Can leverage Bitnami charts for infrastructure (RabbitMQ, Redis) via subchart dependencies

### Negative

- **NEG-001: Helm Learning Curve**: Developers must learn Helm syntax (values files, template functions, helpers)
- **NEG-002: Tool Dependency**: Helm CLI becomes required tool (in addition to kubectl)
- **NEG-003: Templating Complexity**: Complex Go template syntax can be difficult to debug
- **NEG-004: Values File Management**: Must maintain 4+ values files in sync (risk of configuration drift)
- **NEG-005: Chart Versioning Overhead**: Requires semantic versioning and release management discipline

### Mitigations

- **MIT-001: Helm Training**: Include Helm basics in teaching materials (1-2 hour module on charts, values, templates)
- **MIT-002: Linting**: Use `helm lint` and `helm template` in CI/CD to catch errors before deployment
- **MIT-003: Schema Validation**: Define `values.schema.json` to validate values files against expected schema
- **MIT-004: Documentation**: Create `charts/reddog/README.md` documenting all values file parameters
- **MIT-005: Default Values**: Define sensible defaults in `values.yaml` to minimize required overrides

## Relationship to Existing ADRs

This ADR complements but does NOT conflict with existing architectural decisions:

| ADR | Concern Layer | Purpose | Helm Role |
|-----|---------------|---------|-----------|
| **ADR-0002: Dapr Abstraction** | Runtime | How services connect (Dapr abstracts Redis vs Cosmos) | Helm chooses WHICH Dapr component to deploy |
| **ADR-0004: Config API** | Runtime | Application settings (feature flags, app config) | Helm does NOT manage app config (Dapr handles it) |
| **ADR-0006: Env Vars** | Runtime | Infrastructure config (connection strings, endpoints) | Helm sets env var VALUES from values files |
| **ADR-0007: Containerized Infrastructure** | Deployment | Cloud-agnostic infrastructure (RabbitMQ, Redis, SQL Server) | Helm deploys infrastructure containers |
| **ADR-0008: kind Local Dev** | Deployment | Local Kubernetes environment | Helm deploys to kind using values-local.yaml |

**Key Distinction:**
- **Deployment-Time (Helm)**: Decides WHICH resources to deploy (Cosmos DB component vs Redis component)
- **Runtime (Dapr)**: Abstracts HOW services connect (application code unchanged)
- **Application Config (Config API)**: Feature flags and business logic settings (not infrastructure)

**Example: State Store Configuration**

**Helm values-azure.yaml (deployment-time):**
```yaml
dapr:
  stateStore:
    type: azure.cosmosdb
    endpoint: https://reddog.cosmos.azure.com
    database: reddog
```

**Dapr component template (generated by Helm):**
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: statestore
spec:
  type: state.azure.cosmosdb
  metadata:
  - name: url
    value: {{ .Values.dapr.stateStore.endpoint }}
  - name: database
    value: {{ .Values.dapr.stateStore.database }}
```

**Application code (unchanged across environments):**
```csharp
// MakeLineService - saves order state
await daprClient.SaveStateAsync("statestore", orderId, orderData);
```

**Separation of Concerns:**
- Helm decides to deploy Cosmos DB component (Azure) vs Redis component (local)
- Dapr provides runtime abstraction (`SaveStateAsync` works with both)
- Application code knows nothing about Cosmos DB or Redis

## Alternatives Considered

### Kustomize Overlays (Rejected)

- **ALT-001: Description**: Use Kustomize with base manifests + environment overlays
- **ALT-002: Rejection Reason**:
  - Limited templating capabilities (no loops, conditionals, functions)
  - Overlays become complex for significant environment differences (state stores, DNS, secrets)
  - Less expressive than Helm for Dapr component generation
  - Helm is more widely adopted in CNCF ecosystem (78% vs 34% for Kustomize)

### RADIUS (Evaluated and Rejected)

- **ALT-003: Description**: Use RADIUS for unified deployment across clouds
- **ALT-004: Rejection Reason**:
  - No GCP support (Azure/AWS only) - defeats 4-cloud goal
  - Pre-1.0 maturity (v0.52.0, CNCF Sandbox)
  - Adds complexity (RADIUS CLI + Bicep) without eliminating Helm/kubectl
  - See `docs/research/RADIUS-evaluation-2025.md` for full analysis

### Environment-Specific Manifest Directories (Rejected)

- **ALT-005: Description**: Create separate manifest directories per environment
```
manifests/
├── local/     # Duplicate manifests for local
├── azure/     # Duplicate manifests for Azure
├── aws/       # Duplicate manifests for AWS
└── gcp/       # Duplicate manifests for GCP
```
- **ALT-006: Rejection Reason**:
  - Massive manifest duplication (99% identical content across 4 directories)
  - Violates DRY principle
  - High maintenance burden (changes must be applied 4 times)
  - Teaching confusion ("Why are there 4 copies of the same manifest?")

### Terraform Kubernetes Provider (Rejected)

- **ALT-007: Description**: Use Terraform to deploy Kubernetes resources
- **ALT-008: Rejection Reason**:
  - Terraform better suited for infrastructure provisioning (clusters, VPCs) than application deployment
  - Helm is Kubernetes-native standard for application deployment
  - Terraform Kubernetes provider has limited templating vs Helm
  - Would still need Helm for dependency management (Dapr, Nginx charts)

### GitOps with Flux/ArgoCD Only (Partial Adoption)

- **ALT-009: Description**: Use Flux HelmRelease resources pointing to Helm charts
- **ALT-010: Acceptance Reason**: This is the PRODUCTION deployment strategy
- **ALT-011: Clarification**: Flux/ArgoCD consume Helm charts (this ADR); they don't replace Helm. Developers still create Helm charts; Flux automates deployment.

## Implementation Notes

### Chart.yaml Structure

**File**: `charts/reddog/Chart.yaml`

```yaml
apiVersion: v2
name: reddog
description: Red Dog Coffee - Cloud-Agnostic Microservices Demo
type: application
version: 1.0.0  # Chart version (SemVer)
appVersion: "2.0.0"  # Application version

maintainers:
- name: Red Dog Modernization Team

dependencies:
- name: dapr
  version: 1.16.0
  repository: https://dapr.github.io/helm-charts/
  condition: dapr.enabled
- name: ingress-nginx
  version: 4.10.0
  repository: https://kubernetes.github.io/ingress-nginx
  condition: ingress.enabled
```

### values.yaml (Defaults)

**File**: `charts/reddog/values.yaml`

```yaml
# Default values for Red Dog Coffee
# Override these in environment-specific files (values-local.yaml, etc.)

global:
  environment: local
  domain: localhost

# Dapr configuration
dapr:
  enabled: true
  stateStore:
    type: redis
    host: localhost:6379
  pubsub:
    type: redis
    host: localhost:6379
  secretStore:
    type: kubernetes

# Ingress configuration
ingress:
  enabled: true
  className: nginx
  tls: false
  annotations: {}

# Database configuration
database:
  type: sqlserver
  host: sqlserver.database.svc.cluster.local
  port: 1433
  database: reddog

# Service-specific configurations
orderService:
  replicaCount: 1
  image:
    repository: ghcr.io/ahmedmuhi/reddog/order-service
    tag: latest
  resources:
    requests:
      cpu: 100m
      memory: 128Mi
    limits:
      cpu: 500m
      memory: 512Mi

# ... (repeat for other services)
```

### values-local.yaml (Local Development)

**File**: `charts/reddog/values/values-local.yaml`

```yaml
global:
  environment: local
  domain: localhost

dapr:
  stateStore:
    type: redis
    host: localhost:6379  # Dapr init provides Redis
  pubsub:
    type: redis
    host: localhost:6379
  secretStore:
    type: kubernetes  # Local secrets via kubectl

ingress:
  enabled: true
  className: nginx
  tls: false
  annotations: {}

database:
  host: sqlserver.database.svc.cluster.local
  connectionString: "Server=sqlserver,1433;Database=reddog;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true"
```

### values-azure.yaml (Azure AKS)

**File**: `charts/reddog/values/values-azure.yaml`

```yaml
global:
  environment: azure
  domain: reddog.azure.example.com

dapr:
  stateStore:
    type: azure.cosmosdb
    endpoint: https://reddog.documents.azure.com:443/
    database: reddog
    masterKey: secretRef:cosmosdb-key
  pubsub:
    type: rabbitmq
    host: amqp://rabbitmq.rabbitmq.svc.cluster.local:5672
  secretStore:
    type: azure.keyvault
    vaultName: reddog-kv

ingress:
  enabled: true
  className: nginx
  tls: true
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod
    service.beta.kubernetes.io/azure-dns-label-name: reddog

database:
  host: sqlserver.database.svc.cluster.local
  connectionString: secretRef:sqlserver-connection-string
```

### Template Example: Dapr State Store Component

**File**: `charts/reddog/templates/dapr-components/statestore-makeline.yaml`

```yaml
{{- if .Values.dapr.enabled }}
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: statestore-makeline
  namespace: {{ .Release.Namespace }}
spec:
  type: state.{{ .Values.dapr.stateStore.type }}
  version: v1
  metadata:
  {{- if eq .Values.dapr.stateStore.type "redis" }}
  - name: redisHost
    value: {{ .Values.dapr.stateStore.host }}
  - name: redisPassword
    value: ""
  {{- else if eq .Values.dapr.stateStore.type "azure.cosmosdb" }}
  - name: url
    value: {{ .Values.dapr.stateStore.endpoint }}
  - name: database
    value: {{ .Values.dapr.stateStore.database }}
  - name: masterKey
    secretKeyRef:
      name: cosmosdb-credentials
      key: masterKey
  {{- else if eq .Values.dapr.stateStore.type "aws.dynamodb" }}
  - name: table
    value: reddog-makeline-state
  - name: region
    value: {{ .Values.dapr.stateStore.region }}
  {{- else if eq .Values.dapr.stateStore.type "gcp.firestore" }}
  - name: type
    value: {{ .Values.dapr.stateStore.projectId }}
  - name: project_id
    value: {{ .Values.dapr.stateStore.projectId }}
  {{- end }}
{{- end }}
```

### Deployment Workflow

**Local Development:**
```bash
# Create kind cluster
kind create cluster --config kind-config.yaml

# Deploy Red Dog with local values
helm install reddog ./charts/reddog -f values/values-local.yaml

# Verify deployment
kubectl get pods
curl http://localhost/api/orders
```

**Azure AKS:**
```bash
# Get AKS credentials
az aks get-credentials --resource-group reddog --name reddog-aks

# Deploy Red Dog with Azure values
helm install reddog ./charts/reddog -f values/values-azure.yaml

# Verify deployment
kubectl get pods
curl https://reddog.azure.example.com/api/orders
```

### Testing and Validation

**Lint Helm Chart:**
```bash
helm lint ./charts/reddog
```

**Dry-Run / Template Preview:**
```bash
helm template reddog ./charts/reddog -f values/values-local.yaml > preview.yaml
```

**CI/CD Integration:**
```yaml
# GitHub Actions example
- name: Lint Helm Chart
  run: helm lint ./charts/reddog

- name: Validate Values Files
  run: |
    helm template reddog ./charts/reddog -f values/values-local.yaml
    helm template reddog ./charts/reddog -f values/values-azure.yaml
    helm template reddog ./charts/reddog -f values/values-aws.yaml
    helm template reddog ./charts/reddog -f values/values-gcp.yaml
```

## References

- **REF-001**: Related ADR: `docs/adr/adr-0002-cloud-agnostic-configuration-via-dapr.md` (Dapr runtime abstraction)
- **REF-002**: Related ADR: `docs/adr/adr-0004-dapr-configuration-api-standardization.md` (Application config via Dapr)
- **REF-003**: Related ADR: `docs/adr/adr-0006-infrastructure-configuration-via-environment-variables.md` (Env vars for infrastructure)
- **REF-004**: Related ADR: `docs/adr/adr-0007-cloud-agnostic-deployment-strategy.md` (Containerized infrastructure)
- **REF-005**: Related ADR: `docs/adr/adr-0008-kind-local-development-environment.md` (kind cluster for local dev)
- **REF-006**: Related ADR: `docs/adr/adr-0010-nginx-ingress-controller.md` (Nginx Ingress works with Helm)
- **REF-007**: Research Document: `docs/research/RADIUS-evaluation-2025.md` (RADIUS rejected, Helm chosen)
- **REF-008**: Research Document: `docs/research/dev-container-alternatives-2025.md` (Helm + kind recommended)
- **REF-009**: Helm Official Documentation: https://helm.sh/docs/
- **REF-010**: Helm Best Practices: https://helm.sh/docs/chart_best_practices/
- **REF-011**: CNCF Survey 2024: 78% Helm adoption in Kubernetes deployments
