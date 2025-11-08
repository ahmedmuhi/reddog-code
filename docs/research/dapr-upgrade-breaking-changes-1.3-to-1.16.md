# Dapr Upgrade Analysis: Breaking Changes and Requirements (1.3.0 → 1.16.2)

**Research Date**: November 9, 2025
**Target Upgrade Path**: Dapr 1.3.0 (2021) → Dapr 1.16.2 (2024/2025)
**Scope**: HTTP/gRPC APIs (no .NET SDK - we'll use HTTP APIs directly)
**Red Dog Version Spans**: 13 minor versions (1.3 → 1.16)

---

## RESEARCH METHODOLOGY

**Official Sources Consulted**:
- docs.dapr.io official breaking changes documentation
- Dapr GitHub releases (v1.3 through v1.16)
- Dapr blog announcements for each major release
- Dapr Helm chart documentation
- Component reference specifications
- Azure Container Apps Dapr documentation

**Search Strategy**: Comprehensive review of all 13 minor versions, focusing on:
1. HTTP/gRPC API changes (not .NET SDK)
2. Component YAML specification changes
3. Kubernetes annotations and sidecar configuration
4. Deprecations and removal timeline (2-release rule)
5. Component compatibility (RabbitMQ, Redis, Secret stores, Bindings)

---

## CRITICAL DECISION: WORKAROUND FOR .NET 10

**Decision Already Made**: Use Dapr HTTP/gRPC APIs directly, bypassing the .NET SDK:
- **Why**: Dapr .NET SDK 1.16.2 does NOT support .NET 10 (only .NET 8/9)
- **When Fixed**: Upstream will eventually support .NET 10 in a future SDK release
- **Impact**: All Red Dog services will use Dapr HTTP APIs instead of DaprClient SDK
- **Status**: No blocking issues identified - HTTP APIs are stable and fully backward compatible

---

## 1. CRITICAL BREAKING CHANGES

### BC-001: Service Invocation - Default Content-Type Header Removed

**Deprecation Announced**: Dapr 1.7.0 (April 2022)
**Breaking Change Effective**: Dapr 1.9.0 (October 2022)
**Affected**: OrderService, LoyaltyService, MakeLineService (all service-to-service calls)

**What Changed**:
- **Before (1.3 - 1.8)**: Dapr auto-added `Content-Type: application/json` header if not explicitly provided
- **After (1.9+)**: No default content-type header is added; explicit `Content-Type` header required in all HTTP requests

**Impact on Red Dog**:
- All HTTP requests to other Dapr services must explicitly include `Content-Type: application/json` header
- Requests without this header may be rejected or misinterpreted by receiving services

**HTTP API Endpoint (unchanged)**:
```
POST http://localhost:3500/v1.0/invoke/<app-id>/method/<method-name>
```

**Migration Required**:
- Review all service-to-service invocations in Red Dog services
- Ensure every HTTP request to other services includes: `Content-Type: application/json` header
- Services currently using .NET SDK automatic header injection will break if migrated to HTTP APIs without this fix

**Status**: Migration required before .NET 10 upgrade

---

### BC-002: gRPC Service Invocation (legacy `invoke` method) - Deprecated

**Deprecation Announced**: Dapr 1.9.0 (October 2022)
**Removal Target**: Dapr 1.10.0+ (planned for removal)
**Affected**: Services using legacy gRPC service invocation (not HTTP-based)

**What Changed**:
- Old gRPC service invocation method (using `invoke` method on gRPC service) is deprecated
- Replacement: gRPC proxy mode (invoke via HTTP proxy, not gRPC-to-gRPC)

**Impact on Red Dog**:
- **Low Risk**: Red Dog primarily uses HTTP API for service invocation
- No identified usage of legacy gRPC `invoke` method

**Migration Path**:
- If any gRPC invocations exist, migrate to HTTP proxy mode: `/v1.0/invoke/<app-id>/method/<method>`

---

### BC-003: Actor Reminders - Scheduler Becomes Default (v1.15)

**Breaking Change**: Dapr 1.15.0 (February 2025)
**Affected**: Services using actor reminders (VirtualWorker uses actors indirectly)

**What Changed**:
- **Before (1.3 - 1.14)**: Actor reminders stored in state store (Placement service backend)
- **After (1.15+)**: Actor reminders now managed by Scheduler service by default
- **Feature Flag**: `SchedulerReminders` default is `true` (can be disabled if needed)

**Automatic Migration**:
- Dapr 1.15 automatically migrates existing actor reminders when control plane upgrades
- One-time operation per actor type
- Each replica migrates reminders for actor types it manages

**Critical Data Loss Risk** ⚠️:
- Upgrading from v1.14 to v1.15 **removes the existing Scheduler data directory**
- This causes **loss of all jobs and actor reminders** if SchedulerReminders was previously enabled
- If you need to preserve reminders, create etcd snapshot before upgrade

**Impact on Red Dog**:
- **VirtualWorker**: Simulates order completion using actor reminders - requires testing
- **Recommendation**: Test actor reminder behavior thoroughly before production upgrade

**Migration**: Can disable with configuration flag if needed for backward compatibility

---

### BC-004: Component Uniqueness Enforcement (v1.13)

**Breaking Change**: Dapr 1.13.0 (March 2024)
**Affected**: All component definitions (pubsub, state, secrets, bindings)

**What Changed**:
- Components must now have **unique names across ALL component types**
- **Before (1.3 - 1.12)**: Could have same name in different types (e.g., `reddog` pubsub and `reddog` state store)
- **After (1.13+)**: Name must be globally unique across all components

**Example**: If you have:
```yaml
# This would now fail in 1.13+
---
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: reddog  # ✓ pubsub
spec:
  type: pubsub.rabbitmq
---
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: reddog  # ✗ ERROR: duplicate name with pubsub above
spec:
  type: state.redis
```

**Required Fix**:
- Rename state/secret/binding components to have unique names
- Example: `reddog-pubsub`, `reddog-state`, `reddog-secrets`, `reddog-bindings`

**Impact on Red Dog**:
- Current YAML files in `manifests/local/branch/` and `manifests/` likely use consistent naming
- Review and update all component manifests if naming conflicts exist

---

### BC-005: NATS Streaming Component - Removed (v1.13)

**Deprecation Announced**: Dapr 1.11.0 (June 2023)
**Removal Effective**: Dapr 1.13.0 (March 2024)
**Affected**: Applications using NATS Streaming as pub/sub

**What Changed**:
- NATS Streaming component (type: `pubsub.nats-streaming`) completely removed
- Upstream NATS Streaming project deprecated June 2023; no security fixes available
- Replacement: NATS JetStream (type: `pubsub.nats`)

**Impact on Red Dog**:
- **No Impact**: Red Dog uses RabbitMQ pub/sub, not NATS Streaming

---

### BC-006: Workflow API - Major Changes (v1.11)

**Breaking Changes**: Dapr 1.11.0 (June 2023)
**Affected**: Services using Dapr Workflows (not used in Red Dog)

**What Changed**:
- Instance ID handling changed (removed from path, made optional query parameter)
- Instance ID constraints: only letters, numbers, dashes, underscores; < 64 characters
- Workflow input now comes from raw HTTP payload (no JSON parsing)
- Response format changes (metadata fields repositioned)

**Impact on Red Dog**:
- **No Impact**: Red Dog doesn't use Dapr Workflows

---

### BC-007: Max Request Size Flag Rename (v1.16)

**Deprecation Announced**: Dapr 1.14.0 (July 2024)
**Breaking Change**: Dapr 1.16.0 (September 2025)
**Affected**: Kubernetes deployments with request size limits

**What Changed**:
- **Old Flag**: `--dapr-http-max-request-size` (deprecated)
- **New Flag**: `--max-body-size` (replacement)
- **Old Annotation**: `dapr.io/http-max-request-size` (deprecated)
- **New Annotation**: `dapr.io/max-body-size` (replacement)

**Default**: 4MB (unchanged)

**Impact on Red Dog**:
- If deployment uses custom max request size limits, update flags/annotations
- Most deployments use default 4MB - no action needed

**Migration Path**:
```yaml
# OLD (deprecated)
dapr.io/http-max-request-size: "10"

# NEW (v1.16+)
dapr.io/max-body-size: "10"
```

---

## 2. DEPRECATIONS (Still Supported, Will Be Removed)

### DEP-001: Workflow APIs (Alpha1 Endpoints)

**Current Status**: Alpha1 endpoints `/v1.0-alpha1/workflows/*` deprecated in v1.15
**Scheduled Removal**: Dapr 1.17.0 (2025 or later)
**Replacement**: GA Workflow API (endpoint changes)
**Red Dog Impact**: None (not using workflows)

---

### DEP-002: .NET SDK Workflow Methods

**Current Status**: Deprecated in Dapr SDK 1.16
**Status**: Existing workflow client wrapper removed from SDK
**Recommendation**: Use durabletask library directly instead of Dapr SDK wrapper
**Red Dog Impact**: None (using HTTP APIs)

---

### DEP-003: Go SDK Workflow Methods

**Current Status**: Removed in Dapr 1.16 (no longer available)
**Replacement**: Use `dapr/durabletask-go` library directly
**Red Dog Impact**: None (Red Dog is .NET, not Go)

---

## 3. HTTP/gRPC API CHANGES

### Service Invocation API

**Endpoint Format**:
```
HTTP Methods: GET, POST, PUT, DELETE, PATCH
POST http://localhost:3500/v1.0/invoke/<app-id>/method/<method-name>
```

**Stability**: **VERY STABLE** - unchanged since v1.3 through v1.16

**Headers**:
- **Required in v1.9+**: `Content-Type: application/json` (must be explicit)
- **Optional**: `Dapr-Message-TTL` (request lifetime)

**Changes Introduced**:

| Feature | Introduced | Impact |
|---------|-----------|--------|
| HTTPEndpoint/FQDN support | v1.10+ | Can invoke non-Dapr HTTP endpoints via Dapr |
| Auto-attached headers | v1.16 | Dapr adds headers to service invocations |
| Namespace support (FQDN format) | v1.3+ | Full FQDN format for cross-namespace calls |

**Status for Red Dog**: ✓ NO CHANGES REQUIRED - HTTP service invocation API stable, except add explicit `Content-Type` header (BC-001)

---

### Pub/Sub Publishing API

**Endpoint Format**:
```
POST http://localhost:3500/v1.0/publish/<pubsub-name>/<topic-name>
```

**Stability**: **VERY STABLE** - unchanged since v1.3 through v1.16

**CloudEvents Format**:
- **V1.3 - V1.16**: Always uses CloudEvents v1.0 specification
- **No Changes**: CloudEvents format remains consistent

**Capabilities**:
- Raw payload publishing (without CloudEvents wrapper) - supported since v1.2
- CloudEvents structured format - default behavior
- Custom CloudEvents attributes - supported

**New Features**:
- Dead letter topic support - available in modern versions
- Message TTL configuration - available

**Status for Red Dog**: ✓ NO CHANGES REQUIRED - Pub/sub API stable across all versions

---

### Pub/Sub Subscription API

**Programmatic Subscriptions** (HTTP POST to `/dapr/subscribe`):
- Dapr 1.x uses declarative component-based subscriptions
- HTTP subscription endpoint format unchanged

**Declarative Subscriptions** (YAML components):
- Component type: `pubsub.rabbitmq`
- No breaking changes to subscription specification

**Status for Red Dog**: ✓ NO CHANGES REQUIRED - Subscription mechanism unchanged

---

### State Management API

**Basic Endpoints**:
```
GET  http://localhost:3500/v1.0/state/<store-name>/<key>
POST http://localhost:3500/v1.0/state/<store-name> (save)
DELETE http://localhost:3500/v1.0/state/<store-name>/<key>
```

**Stability**: **STABLE** - core API unchanged, enhancements added

**Changes**:

| Feature | Version | Details |
|---------|---------|---------|
| Transaction API | v1.3+ | Multi-state atomic operations supported |
| Query API | v1.5+ (Alpha) | New: `/v1.0-alpha1/state/<store>/query` endpoint |
| Bulk operations | v1.4+ | Batch state operations supported |
| TTL support | v1.3+ | State expiration supported |
| ETag/Versioning | v1.3+ | Optimistic concurrency control |

**Query API Breaking Change** (v1.7):
- Value field removed from key name in query results
- Only affects applications using state query feature (MakeLineService uses this)

**Status for Red Dog**:
- ✓ Basic state operations stable
- ⚠️ Query API change if used by MakeLineService - verify query usage

---

### Secrets API

**Endpoint Format**:
```
GET http://localhost:3500/v1.0/secrets/<store-name>/<secret-name>
GET http://localhost:3500/v1.0/secrets/<store-name> (bulk)
```

**Stability**: **STABLE** - no changes since v1.3

**Features**:
- Single secret retrieval - unchanged
- Bulk secret retrieval - unchanged
- Multiple secret stores per app - unchanged

**Status for Red Dog**: ✓ NO CHANGES REQUIRED

---

### Configuration API

**Status**: **INTRODUCED in v1.5** (ADR-0004 mandates this)

**Timeline**:
- v1.5: Configuration API first introduced (Preview/Alpha)
- v1.11: Stabilized and became production-ready
- v1.16: Fully stable

**Endpoint Format**:
```
GET http://localhost:3500/v1.0/configuration/<store-name>?keys=<comma-separated-keys>
POST http://localhost:3500/v1.0/configuration/<store-name>/subscribe (WebSocket)
```

**New in v1.16**: Consistent Configuration Store endpoints available

**Impact on Red Dog**:
- Currently may not use Configuration API
- **ADR-0004 mandates adoption** - plan to implement Configuration API for application settings
- Easy adoption path - simple REST endpoints

**Status**: ✓ API stable, ready for implementation

---

### Bindings (Output) API

**Endpoint Format**:
```
POST http://localhost:3500/v1.0/bindings/<binding-name>
```

**Stability**: **STABLE** - unchanged since v1.3

**Metadata Changes**: Component-specific metadata evolved, but HTTP API unchanged

**Input Bindings**: HTTP POST to app endpoints from Dapr (component-based, not HTTP API)

**Status for Red Dog**: ✓ NO CHANGES REQUIRED

---

## 4. COMPONENT SPECIFICATION CHANGES

### Component Manifest Version

**Current**: `apiVersion: dapr.io/v1alpha1` (unchanged since v1.0)
**Status**: Stable across all versions 1.3 → 1.16
**No Breaking Changes**: Component manifest format stable

**Note**: The `.spec.version: v1` field indicates component implementation version (not manifest API version)

---

### PubSub RabbitMQ Component

**Component Type**: `pubsub.rabbitmq`
**Component Implementation Version**: `v1` (stable)

**Metadata Fields (Core)** - Stable across v1.3 → v1.16:
- `host` - RabbitMQ server address
- `durable` - Queue durability
- `autoAck` - Auto-acknowledgment behavior
- `requeueInFailure` - Retry behavior on failure
- `exchangeKind` - Exchange type (direct, fanout, topic)
- `prefetchCount` - Message prefetch limit

**New Metadata** (added over time):
- `enableDeadLetter` - Dead letter queue support (v1.9+)
- `maxRetries` - Retry configuration (v1.10+)
- `connectionUri` - Alternative to host/username/password (v1.10+)

**RabbitMQ Version Compatibility**:
- **Dapr 1.16** supports **RabbitMQ 3.x and 4.x**
- **RabbitMQ 4.2.0** uses Khepri as default metadata store (backward compatible)
- **Status**: ✓ RabbitMQ 4.2 compatible with Dapr 1.16

**Breaking Changes in RabbitMQ Component**: None identified

---

### State Store - Redis Component

**Component Type**: `state.redis`
**Component Implementation Version**: `v1` (stable)

**Metadata Fields** - Stable across all versions:
- `redisHost` or `redisUri` - Connection string
- `redisPassword` - Authentication
- `logLevel` - Logging
- `maxRetries` - Retry behavior
- `maxConcurrentConnections` - Connection pooling

**New Features Over Time**:
- TTL support (v1.3+) - `expirationInSeconds` metadata
- Query API (v1.5+) - Requires RedisSearch and RedisJSON modules
- Cluster support (v1.6+) - Multiple node support
- Sentinel support (v1.9+) - High availability
- EntraID authentication (v1.11+) - Azure managed identity

**Redis Version Compatibility** ⚠️:
- **Dapr 1.16 supports Redis 6.x only**
- **Redis 7.x and 8.x NOT SUPPORTED**
- **Critical Issue**: Red Dog upgrading Redis to 8.0.5 is incompatible with Dapr 1.16

**Required Action**:
- **DO NOT upgrade Redis beyond 6.x** if using Dapr 1.16
- Keep Redis at 6.x LTS version
- OR upgrade Dapr beyond 1.16 once Redis 7/8 support is added upstream

**Breaking Changes**: None in Dapr component, but Redis version limitation is critical

---

### Secret Store - Azure Key Vault Component

**Component Type**: `secretstores.azure.keyvault`
**Stability**: Stable across v1.3 → v1.16

**Metadata Fields** - Consistent:
- `vaultName` - Key Vault name
- `azureTenantId` - Azure tenant
- `azureClientId` - Service principal/managed identity
- `azureClientSecret` - Service principal password (deprecated in favor of workload identity)

**Workload Identity Support** (v1.11+):
- Azure Workload Identity now preferred over secrets
- Dapr 1.16 includes advanced workload identity federation support
- SPIFFE identity integration for cross-cloud authentication

**Breaking Changes**: None

---

### Secret Store - AWS Secrets Manager Component

**Component Type**: `secretstores.aws.secretmanager`
**Stability**: Stable across versions

**Metadata Fields**:
- `region` - AWS region
- `endpoint` - Custom endpoint (optional)
- IAM authentication via IRSA or service account

**Workload Identity Support** (v1.16):
- AWS IRSA (IAM Roles for Service Accounts) now fully supported
- Dapr 1.16 adds ServiceAccount annotation support for IRSA configuration
- SPIFFE identity integration

**Breaking Changes**: None

---

### Secret Store - GCP Secret Manager Component

**Component Type**: `secretstores.gcp.secretmanager`
**Stability**: Stable across versions

**Metadata Fields**:
- `projectId` - GCP project
- `serviceAccountKey` - Service account JSON key

**Workload Identity Support** (v1.16):
- GCP Workload Identity federation now supported
- SPIFFE identity integration for zero-trust authentication

**Breaking Changes**: None

---

### Output Binding - S3 Component

**Component Type**: `bindings.aws.s3`
**Stability**: Stable

**Metadata**: Consistent across versions
- `bucket` - S3 bucket name
- `region` - AWS region
- `accessKey` / `secretKey` - IAM credentials

**Workload Identity**: IRSA support (v1.16+)

**Breaking Changes**: None

---

### Output Binding - Azure Blob Storage Component

**Component Type**: `bindings.azure.blobstorage`
**Stability**: Stable

**Metadata Fields**:
- `storageAccount` - Azure Storage account name
- `storageAccountKey` - Account key (deprecated)
- `containerName` - Container name

**Workload Identity**: Entra ID support (v1.16+)

**Breaking Changes**: None

---

### Output Binding - GCS Component

**Component Type**: `bindings.gcp.bucket`
**Stability**: Stable

**Metadata**:
- `bucket` - GCS bucket name
- `projectId` - GCP project
- `serviceAccountKey` - Service account JSON

**Workload Identity**: Workload Identity federation (v1.16+)

**Breaking Changes**: None

---

## 5. KUBERNETES SIDECAR CONFIGURATION CHANGES

### Sidecar Annotations

**Core Annotations** - STABLE across v1.3 → v1.16:

| Annotation | Purpose | Status |
|-----------|---------|--------|
| `dapr.io/enabled: "true"` | Enable Dapr sidecar injection | ✓ Unchanged |
| `dapr.io/app-id` | Application identifier | ✓ Unchanged |
| `dapr.io/app-port` | Application port | ✓ Unchanged |
| `dapr.io/app-protocol` | Protocol (http, grpc, h2c) | ✓ Unchanged |
| `dapr.io/config` | Configuration resource name | ✓ Unchanged |
| `dapr.io/log-level` | Sidecar log level | ✓ Unchanged |

**Deprecated Annotations**:

| Annotation | Deprecated | Replacement | Status |
|-----------|-----------|------------|--------|
| `dapr.io/http-max-request-size` | v1.14 | `dapr.io/max-body-size` | Removed in v1.17 (preview) |
| `dapr.io/app-ssl` | v1.11 | Use TLS configuration | Removed in v1.13 |

**New Annotations** (v1.16):
- `dapr.io/max-body-size` - Replaces deprecated `http-max-request-size`
- ServiceAccount annotations for workload identity (IRSA, Entra ID)

**Status for Red Dog**: ✓ NO CHANGES to core annotations required

---

### Sidecar Ports

**Default Ports** - UNCHANGED since v1.3:
- HTTP port: `3500` (Dapr HTTP APIs)
- gRPC port: `50001` (Dapr gRPC APIs)
- Metrics port: `9090` (Prometheus metrics)
- Health port: `3501` (health probes - added v1.6+)

**Status**: ✓ No port configuration changes needed

---

### Sidecar Environment Variables

**Common Environment Variables** - Stable:
- `DAPR_HTTP_PORT=3500`
- `DAPR_GRPC_PORT=50001`
- `DAPR_LOG_LEVEL=info`

**New Variables** (v1.16):
- Workload identity related variables (if using federation)
- OpenTelemetry configuration variables (optional)

**Status for Red Dog**: ✓ No required env var changes

---

## 6. HELM CHART UPGRADE PATH

### Current vs Target Versions

| Item | Current (1.3.0) | Target (1.16.2) | Gap |
|------|----------------|-----------------|-----|
| Dapr Runtime | 1.3.0 | 1.16.2 | 13 minor versions |
| Helm Chart Version | ~1.3.0 | 1.16.2 | Same as runtime |
| CRD API Version | v1alpha1 | v1alpha1 | Unchanged |

---

### Upgrade Sequence

**Option 1: Direct Upgrade (1.3.0 → 1.16.2)**

Dapr supports **direct upgrades across multiple versions** if following the 2-release deprecation rule:
- ✓ Direct 1.3 → 1.16 upgrade possible
- Recommend testing in pre-production environment first

**Option 2: Incremental Upgrade (safest)**

For production stability, upgrade incrementally:
1. 1.3.0 → 1.6.x (stable point)
2. 1.6.x → 1.9.x (past major service invocation changes)
3. 1.9.x → 1.12.x (before actor changes)
4. 1.12.x → 1.15.x (before scheduler default)
5. 1.15.x → 1.16.2 (latest)

**Recommendation**: Direct upgrade (1.3 → 1.16) with thorough testing

---

### CRD Update Requirements

**Custom Resource Definitions** must be updated manually (Helm doesn't manage CRD updates):

```bash
# Update CRDs to v1.16.2
kubectl replace -f https://raw.githubusercontent.com/dapr/dapr/v1.16.2/charts/dapr/crds/components.yaml
kubectl replace -f https://raw.githubusercontent.com/dapr/dapr/v1.16.2/charts/dapr/crds/configuration.yaml
kubectl replace -f https://raw.githubusercontent.com/dapr/dapr/v1.16.2/charts/dapr/crds/subscription.yaml
kubectl apply -f https://raw.githubusercontent.com/dapr/dapr/v1.16.2/charts/dapr/crds/resiliency.yaml
kubectl apply -f https://raw.githubusercontent.com/dapr/dapr/v1.16.2/charts/dapr/crds/httpendpoints.yaml
```

**Important**: Use `replace` for existing CRDs (components, configuration, subscription) and `apply` for new CRDs (resiliency, httpendpoints)

---

### Helm Values Changes

**Certificates**: Existing certificates automatically preserved (since v1.0.0+)

**No Breaking Helm Values Changes**: All existing `values.yaml` settings remain compatible

**New Optional Values** (v1.16):
- OpenTelemetry exporter configuration (optional)
- Workload identity configurations (optional)
- High availability settings (optional)

**Upgrade Command**:
```bash
# Add/update Dapr Helm repository
helm repo add dapr https://dapr.github.io/helm-charts/
helm repo update

# Upgrade to v1.16.2
helm upgrade dapr dapr/dapr \
  --version 1.16.2 \
  --namespace dapr-system \
  --values your-values.yaml \
  --wait

# Restart application deployments to pick up new sidecar
kubectl rollout restart deployment/<app-name> -n <namespace>
```

---

## 7. KUBERNETES COMPATIBILITY

### Version Support Matrix

| Component | Minimum | Recommended | Tested with |
|-----------|---------|-------------|------------|
| Kubernetes | 1.24+ | 1.30+ | 1.26-1.30 |
| Dapr | 1.16.2 | 1.16.2 | Current stable |
| Red Dog Target | N/A | 1.30+ | AKS, EKS, GKE |

**Kubernetes 1.30 Compatibility**: ✓ **YES** - Dapr 1.16.2 supports Kubernetes 1.30+

**Important**: Dapr follows Kubernetes Version Skew Policy, supporting multiple consecutive versions

---

## 8. WORKLOAD IDENTITY CONFIGURATION

### Azure Workload Identity (Enhanced in v1.16)

**Feature**: OIDC-based authentication to Azure services without long-lived secrets

**Configuration** (v1.16):
1. **Enable Dapr Sentry with JWT/OIDC**:
   ```yaml
   # Dapr configuration resource
   apiVersion: dapr.io/v1alpha1
   kind: Configuration
   metadata:
     name: dapr-config
   spec:
     mtls:
       enabled: true
       workloadCertTTL: 24h
     features:
       - name: SchedulerReminders
         enabled: true
   # Add Sentry configuration for OIDC (v1.16+)
   ```

2. **Create Federated Identity Credential** in Azure Entra ID:
   - Subject: `<dapr-namespace>:dapr:system:dapr-sentry` (SPIFFE ID)
   - Issuer: `https://<k8s-cluster>/identity/dapr/<namespace>`
   - Audience: `api://AzureADTokenExchange`

3. **Component Configuration**:
   ```yaml
   apiVersion: dapr.io/v1alpha1
   kind: Component
   metadata:
     name: azure-secrets
   spec:
     type: secretstores.azure.keyvault
     metadata:
       - name: vaultName
         value: "my-keyvault"
       - name: azureTenantId
         value: "..."  # tenant ID from Entra
       - name: azureClientId
         value: "..."  # client/app ID
       # REMOVED: azureClientSecret (use workload identity instead)
   ```

**Status**: ✓ Fully supported in v1.16, recommended approach

---

### AWS IRSA (IAM Roles for Service Accounts)

**Feature**: Kubernetes ServiceAccount assumes IAM role for AWS API access

**Configuration** (v1.16):
```bash
# 1. Create IAM role for service account
# 2. Create Kubernetes ServiceAccount with annotation
kubectl annotate serviceaccount <sa-name> \
  eks.amazonaws.com/role-arn=arn:aws:iam::<account>:role/<role-name>

# 3. Component uses IRSA automatically
```

**Component Configuration**:
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: aws-secrets
spec:
  type: secretstores.aws.secretmanager
  metadata:
    - name: region
      value: us-east-1
    # IAM credentials from IRSA - no explicit credentials needed
```

**Status**: ✓ Fully supported in v1.16 with ServiceAccount annotation support (new in v1.16)

---

### GCP Workload Identity Federation

**Feature**: Kubernetes ServiceAccount exchanges token for GCP service account

**Configuration** (v1.16):
```bash
# 1. Create Workload Identity Pool and Provider (federation)
# 2. Create Kubernetes ServiceAccount
# 3. Link SA to GCP service account via trust relationship
```

**Status**: ✓ Fully supported in v1.16 via SPIFFE/OIDC

---

### mTLS Configuration

**Status v1.3 - v1.16**: Enabled by default

**Changes**:
- v1.15+: Certificate rotation at half session lifespan (security improvement)
- v1.16: Enhanced with workload identity federation support

**Configuration**: Unchanged since v1.3

---

## 9. OBSERVABILITY AND OPENTELEMETRY

### OpenTelemetry Integration (New in v1.16)

**Previous Approach** (v1.3 - v1.15):
- Direct Prometheus metrics scraping
- Jaeger/Zipkin distributed tracing via configuration

**New Approach** (v1.16):
- **OTLP Exporter** support for unified observability
- OpenTelemetry Protocol (OTLP) for metrics, traces, logs

**Configuration Example** (v1.16):
```yaml
apiVersion: dapr.io/v1alpha1
kind: Configuration
metadata:
  name: dapr-config
spec:
  telemetry:
    # OTLP Exporter configuration
    exporters:
      - type: otlp
        metadata:
          - name: endpoint
            value: "http://otel-collector:4317"
          - name: insecure
            value: "true"
```

**Supported Backends**:
- OpenTelemetry Collector
- Jaeger (via OTLP)
- Grafana Loki (via OTLP)
- Prometheus (via OTLP remote write)

**Status**: ✓ Backward compatible - existing Prometheus setup continues to work

---

### Metrics Changes

**Metrics Endpoint** (unchanged): `:9090/metrics`

**New Metrics** (v1.16):
- Workflow execution metrics (if using workflows)
- Scheduler job metrics (if using actor reminders)
- W3C Baggage propagation metrics

**Deprecated Metrics**: None identified

---

### Tracing Changes

**Tracing Configuration** (v1.16):
- OTLP exporter support (new)
- Sampling rate configuration (unchanged)
- Custom headers for traces (new)

**W3C Baggage Support** (v1.16):
- Custom key-value pairs propagate across services
- Improves observability without distributed tracing

---

## 10. AZURE CONTAINER APPS - MANAGED DAPR

### Current Dapr Version in Container Apps

**As of April 2025**:
- **Current Version**: 1.13.6-msft.1 (not 1.16.2)
- **Status**: Dapr 1.16 not yet available in Azure Container Apps
- **Release Timeline**: Unknown - on "best effort" basis

**Versioning Format**:
- `<oss-version>-msft.<azure-specific-patch>`
- Example: `1.13.6-msft.1` means compatible with OSS 1.13.6, plus Azure modifications

**Key Limitation**: Custom version selection NOT supported
- Automatic updates enabled
- No way to stay on specific versions
- Updates rolled out to regions progressively

### Component Configuration Differences

**Container Apps Components**: Slightly different than self-managed Dapr
- Component specs mostly compatible
- Some metadata options may vary (Container Apps-specific)
- Configuration storage defaults may differ

### Migration Consideration

**If deploying to Container Apps**:
- Dapr 1.16 features not available until Microsoft releases it
- Plan to work with 1.13.6-msft.1 or newer when available
- Ensure Red Dog code compatibility with both versions

**Recommendation**: Test in self-managed Kubernetes first, migrate to Container Apps after Dapr 1.16 support available

---

## 11. RISKS AND MITIGATION

### RISK-001: Redis 8.0 Incompatibility with Dapr 1.16

**Likelihood**: HIGH (critical infrastructure component)
**Impact**: HIGH (complete state management failure)
**Severity**: CRITICAL

**Issue**: Red Dog plans to upgrade Redis to 8.0.5, but Dapr 1.16 only supports Redis 6.x

**Mitigation Options**:

1. **Keep Redis at 6.x** (Recommended for quick upgrade)
   - Redis 6.x LTS stable and secure
   - Dapr 1.16 fully compatible
   - Action: Don't upgrade Redis beyond 6.x

2. **Wait for Dapr Redis 7/8 Support**
   - Monitor Dapr GitHub for Redis 7/8 support PR
   - Upgrade to Dapr 1.17+ when available
   - Timeline: Unknown (not in 1.16)

3. **Use Different State Store** (Not recommended)
   - Replace Redis with PostgreSQL or CosmosDB
   - Requires major refactoring
   - Higher cost and complexity

**Recommended Action**: Upgrade Dapr 1.16 with Redis 6.x. Plan for future Redis 7/8 support in later Dapr versions.

---

### RISK-002: Actor Reminder Data Loss on 1.14→1.15 Upgrade

**Likelihood**: MEDIUM (if actors are used)
**Impact**: HIGH (loss of scheduled work)
**Severity**: HIGH

**Issue**: Upgrading to 1.15 removes Scheduler data directory, losing all actor reminders

**Mitigation**:
1. **Create etcd snapshot before upgrade** (if using embedded scheduler):
   ```bash
   etcdctl snapshot save backup.db
   ```

2. **Test VirtualWorker actor reminders** in staging environment first

3. **Enable SchedulerReminders before upgrade** to ensure feature flag is known

4. **Schedule upgrade during maintenance window** with backup plan

---

### RISK-003: Service Invocation Content-Type Header Changes

**Likelihood**: HIGH (affects all service-to-service calls)
**Impact**: HIGH (requests rejected if no Content-Type header)
**Severity**: MEDIUM (easy to fix, but widespread impact)

**Issue**: Since v1.9, default Content-Type header no longer added; must be explicit

**Mitigation**:
1. **Audit all HTTP service invocation code**
2. **Add explicit `Content-Type: application/json` header** to all inter-service calls
3. **Test thoroughly** - requests may fail silently or return 415 errors
4. **Create test suite** verifying Content-Type handling

---

### RISK-004: Component Name Uniqueness Enforcement (v1.13+)

**Likelihood**: MEDIUM (depends on component naming)
**Impact**: MEDIUM (components fail to load)
**Severity**: MEDIUM (easy to detect and fix)

**Issue**: Components must have globally unique names across all types

**Mitigation**:
1. **Audit all component definitions** in `manifests/` directories
2. **Check for naming conflicts** (same name, different types)
3. **Rename components if needed** to ensure uniqueness
4. **Test all components** load successfully

---

### RISK-005: Workflow API Breaking Changes (if workflows used in future)

**Likelihood**: LOW (Red Dog doesn't currently use workflows)
**Impact**: HIGH (if adopted)
**Severity**: LOW (future planning)

**Issue**: Workflow API changed significantly in v1.11

**Mitigation**:
- If future versions of Red Dog use workflows, review v1.11 changes
- Use GA Workflow API (v1.0, not alpha)
- Test workflow sample applications with target Dapr version

---

## 12. TESTING REQUIREMENTS

### TEST-001: Service Invocation with Content-Type Headers

**Purpose**: Verify all inter-service calls include Content-Type header
**Affected Services**: OrderService, MakeLineService, LoyaltyService, AccountingService

**Test Cases**:
- [ ] OrderService invokes MakeLineService (pub/sub)
- [ ] VirtualWorker invokes MakeLineService (GET order status)
- [ ] AccountingService retrieves data (if using invocation)
- [ ] Each request includes `Content-Type: application/json`

**Success Criteria**: All invocations succeed with 200/201/204 responses

**Testing Method**:
```bash
# Verify header in logs or use curl
curl -X POST \
  -H "Content-Type: application/json" \
  http://localhost:3500/v1.0/invoke/service-id/method/endpoint
```

---

### TEST-002: Pub/Sub Message Publishing and Subscription

**Purpose**: Verify RabbitMQ pub/sub continues to work
**Affected Services**: OrderService (publisher), MakeLineService, LoyaltyService, ReceiptGenerationService, AccountingService (subscribers)

**Test Cases**:
- [ ] OrderService publishes order message
- [ ] MakeLineService receives and processes order
- [ ] LoyaltyService receives and updates loyalty points
- [ ] ReceiptGenerationService receives and generates receipt
- [ ] AccountingService receives and records transaction
- [ ] RabbitMQ component loads successfully
- [ ] CloudEvents format preserved

**Success Criteria**: All subscribers receive messages within expected time

---

### TEST-003: Redis State Store Operations

**Purpose**: Verify Redis 6.x state operations work correctly
**Affected Services**: MakeLineService, LoyaltyService

**Test Cases**:
- [ ] Save state to Redis (MakeLineService order queue)
- [ ] Retrieve state from Redis
- [ ] Update state with ETag (optimistic concurrency)
- [ ] Delete state
- [ ] TTL expiration works
- [ ] Query API works (if used by MakeLineService)

**Success Criteria**: All state operations succeed

**Note**: Ensure Redis 6.x is used, NOT 7.x or 8.x

---

### TEST-004: Secret Store Access

**Purpose**: Verify secret retrieval from all clouds works
**Affected Components**: Azure Key Vault, AWS Secrets Manager, GCP Secret Manager

**Test Cases**:
- [ ] Azure Key Vault secret retrieval
- [ ] AWS Secrets Manager secret retrieval
- [ ] GCP Secret Manager secret retrieval
- [ ] Workload identity federation (v1.16 feature) if configured

**Success Criteria**: Secrets retrieved successfully from all configured stores

---

### TEST-005: Kubernetes Sidecar Injection and Annotations

**Purpose**: Verify Dapr sidecar correctly injected and configured

**Test Cases**:
- [ ] Pod has Dapr sidecar container
- [ ] Port 3500 (HTTP) accessible
- [ ] Port 50001 (gRPC) accessible
- [ ] Metrics available on port 9090
- [ ] Health check endpoints available

**Success Criteria**: Sidecar fully functional in each pod

---

### TEST-006: Actor Reminders (VirtualWorker)

**Purpose**: Verify actor reminder functionality after migration

**Test Cases**:
- [ ] VirtualWorker creates actor reminders
- [ ] Reminders fire at expected times
- [ ] Orders complete within expected timeframe
- [ ] No reminder data loss after upgrade

**Success Criteria**: All reminders execute as expected

**Migration Validation**:
- [ ] Existing reminders migrated to Scheduler (if present)
- [ ] New reminders created in Scheduler

---

### TEST-007: Configuration API (ADR-0004 Adoption)

**Purpose**: Verify new Configuration API works (if implemented)

**Test Cases**:
- [ ] Retrieve single configuration key
- [ ] Retrieve multiple keys
- [ ] Subscribe to configuration changes
- [ ] Configuration store component loads

**Success Criteria**: Configuration API endpoints return expected values

---

### TEST-008: Kubernetes Upgrade Path

**Purpose**: Verify safe upgrade from 1.3.0 to 1.16.2

**Test Cases**:
- [ ] CRDs updated successfully
- [ ] Helm chart upgraded without errors
- [ ] No pod restarts during upgrade
- [ ] All application pods restart successfully
- [ ] All services functional after upgrade

**Success Criteria**: Zero-downtime upgrade to 1.16.2

---

## 13. RECOMMENDED UPGRADE SEQUENCE

### Phase 1: Pre-Upgrade Validation (Week 1)

1. **Audit Current Configuration**
   - [ ] Review all YAML component manifests
   - [ ] Check for component name conflicts
   - [ ] Verify Redis version (must be 6.x)
   - [ ] Document current Dapr settings

2. **Code Audit**
   - [ ] Verify all service invocations include explicit `Content-Type: application/json` headers
   - [ ] Check for any deprecated API usage
   - [ ] Review actor reminder usage (VirtualWorker)

3. **Infrastructure Planning**
   - [ ] Ensure Kubernetes 1.26+ (confirm version)
   - [ ] Plan upgrade window (recommend off-peak hours)
   - [ ] Ensure backup of critical state (Redis, databases)

### Phase 2: Staging Environment Upgrade (Week 2)

1. **Deploy Staging Environment**
   - [ ] Deploy Red Dog to staging with Dapr 1.3.0
   - [ ] Verify all services operational

2. **Upgrade Dapr**
   ```bash
   # Update CRDs first
   kubectl replace -f https://raw.githubusercontent.com/dapr/dapr/v1.16.2/charts/dapr/crds/components.yaml
   kubectl replace -f https://raw.githubusercontent.com/dapr/dapr/v1.16.2/charts/dapr/crds/configuration.yaml
   kubectl replace -f https://raw.githubusercontent.com/dapr/dapr/v1.16.2/charts/dapr/crds/subscription.yaml
   kubectl apply -f https://raw.githubusercontent.com/dapr/dapr/v1.16.2/charts/dapr/crds/resiliency.yaml
   kubectl apply -f https://raw.githubusercontent.com/dapr/dapr/v1.16.2/charts/dapr/crds/httpendpoints.yaml

   # Update Helm repository
   helm repo add dapr https://dapr.github.io/helm-charts/
   helm repo update

   # Upgrade Dapr
   helm upgrade dapr dapr/dapr \
     --version 1.16.2 \
     --namespace dapr-system \
     --wait
   ```

3. **Restart Application Pods**
   ```bash
   kubectl rollout restart deployment -n reddog-staging
   ```

4. **Run Full Test Suite**
   - [ ] Execute TEST-001 through TEST-008 (all tests)
   - [ ] Load testing (verify performance)
   - [ ] Chaos testing (verify resilience)

5. **Validate Results**
   - [ ] All tests pass
   - [ ] No performance degradation
   - [ ] All services operational

### Phase 3: Production Upgrade (Week 3)

1. **Pre-Upgrade Backup**
   ```bash
   # Backup Redis
   redis-cli --rdb /backup/redis-$(date +%Y%m%d).rdb

   # Backup databases
   # (commands specific to your DB setup)
   ```

2. **Upgrade Production Dapr**
   ```bash
   # Same commands as staging, but on production namespace
   # Update CRDs, upgrade Helm, restart pods
   ```

3. **Monitor During Upgrade**
   - [ ] Watch Dapr sidecar logs
   - [ ] Monitor application logs
   - [ ] Check metrics for errors
   - [ ] Track latency/throughput

4. **Post-Upgrade Validation**
   - [ ] All pods running successfully
   - [ ] All services responding
   - [ ] No error spikes in logs
   - [ ] Performance metrics stable

5. **Gradual Rollout** (if using canary/blue-green)
   - [ ] Upgrade 10% of pods
   - [ ] Verify no errors for 1 hour
   - [ ] Upgrade 25%, 50%, 100% in stages

### Phase 4: Post-Upgrade (Week 4+)

1. **Documentation**
   - [ ] Update runbooks with Dapr 1.16 procedures
   - [ ] Document any configuration changes
   - [ ] Update troubleshooting guide

2. **Monitor for Issues**
   - [ ] Watch logs for 48+ hours
   - [ ] Track metrics for 1 week
   - [ ] Collect performance baseline

3. **Plan Next Steps**
   - [ ] .NET 10 upgrade (post-Dapr 1.16 upgrade)
   - [ ] Configuration API implementation (ADR-0004)
   - [ ] Workload identity federation adoption

---

## 14. REFERENCES

### Official Dapr Documentation
- Breaking Changes & Deprecations: https://docs.dapr.io/operations/support/breaking-changes-and-deprecations/
- Kubernetes Upgrade Guide: https://docs.dapr.io/operations/hosting/kubernetes/kubernetes-upgrade/
- Release Policy: https://docs.dapr.io/operations/support/support-release-policy/
- API Reference: https://docs.dapr.io/reference/api/
- Component Reference: https://docs.dapr.io/reference/components-reference/

### Release Notes
- Dapr 1.16.0 Release: https://blog.dapr.io/posts/2025/09/16/dapr-v1.16-is-now-available/
- Dapr 1.15.0 Release: https://blog.dapr.io/posts/2025/02/27/dapr-v1.15-is-now-available/
- GitHub Releases: https://github.com/dapr/dapr/releases

### Component Documentation
- RabbitMQ Pub/Sub: https://docs.dapr.io/reference/components-reference/supported-pubsub/setup-rabbitmq/
- Redis State Store: https://docs.dapr.io/reference/components-reference/supported-state-stores/setup-redis/
- Azure Key Vault: https://docs.dapr.io/reference/components-reference/supported-secret-stores/setup-azure-keyvault/
- Configuration Stores: https://docs.dapr.io/reference/components-reference/supported-configuration-stores/

### Workload Identity
- Azure Workload Identity: https://docs.dapr.io/developing-applications/integrations/azure/azure-authentication/howto-wif/
- AWS IRSA Support: https://docs.dapr.io/developing-applications/integrations/aws/authenticating-aws/
- Workload Identity Overview: https://docs.dapr.io/developing-applications/integrations/

### Azure Container Apps
- Dapr in Container Apps: https://learn.microsoft.com/en-us/azure/container-apps/dapr-overview
- Release Notes: https://github.com/microsoft/azure-container-apps/issues/1478

### Helm Charts
- Dapr Helm Charts: https://github.com/dapr/helm-charts
- Artifact Hub: https://artifacthub.io/packages/helm/dapr/dapr

---

## SUMMARY: GO/NO-GO FOR DAPR 1.16.2 UPGRADE

### Critical Blockers: NONE ✓

### Warnings: 1
- **Redis 8.0 Incompatibility** - Keep Redis at 6.x or wait for Dapr 7/8 support

### Required Changes: 2
1. Add explicit `Content-Type: application/json` header to all service invocations (v1.9+)
2. Ensure component names are globally unique (v1.13+)

### Recommended: 1
- Implement Configuration API (ADR-0004 mandates this)

### Testing Required: Full integration test suite (8 test categories)

### **UPGRADE ASSESSMENT: GO ✓**

**Red Dog Coffee can upgrade to Dapr 1.16.2 with manageable effort:**
- No blocking technical issues
- Backward compatible HTTP APIs
- All building blocks stable
- Workload identity support added (security improvement)
- RabbitMQ 4.2 compatible
- Kubernetes 1.30+ compatible

**Prerequisites**:
1. Keep Redis at 6.x (critical)
2. Add Content-Type headers to service invocations
3. Verify component name uniqueness
4. Run full test suite in staging
5. Follow recommended upgrade sequence

**Timeline**: 3-4 weeks (1 week planning, 1 week staging, 1 week production, 1 week validation)

---

**Report Compiled**: November 9, 2025
**Research Scope**: Dapr versions 1.3.0 - 1.16.2
**Focus**: HTTP/gRPC APIs (no .NET SDK)
**Status**: Ready for implementation
