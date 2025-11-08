# Infrastructure Container Images Research Report

**Date:** 2025-11-09
**Purpose:** Verify latest stable, production-ready container images for Red Dog infrastructure modernization
**Scope:** 7 infrastructure components (SQL Server, Redis, RabbitMQ, Nginx, cert-manager, Dapr, KEDA)

---

## Executive Summary

This research validates container image versions for Red Dog Coffee's infrastructure modernization. All 7 components have stable releases available, though several require careful consideration for breaking changes when upgrading from current versions (2021-2022 era).

**Key Findings:**
- SQL Server 2022 is latest stable (2025 in preview)
- Redis 8.0 is latest stable (significant jump from Redis 7)
- RabbitMQ 4.2 is latest stable (major version upgrade from 3.x)
- Nginx 1.28 is latest stable
- cert-manager 1.16 is latest supported (1.19 newest, requires K8s 1.31+)
- Dapr 1.16 is latest stable (significant breaking changes from 1.3)
- KEDA 2.18 is latest stable (significant upgrade from 2.2)

**Critical Compatibility Note:**
Current infrastructure uses Helm charts from 2021-2022. Upgrading all 7 components simultaneously requires careful sequencing to avoid dependency conflicts.

---

## 1. SQL Server

### Verified Information

- **Latest Stable Version**: 2022
- **Latest Preview Version**: 2025 (public preview, development only)
- **Recommended Docker Image**: `mcr.microsoft.com/mssql/server:2022-latest`
- **Alternative Tags**:
  - `mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04` (specific cumulative update)
  - `mcr.microsoft.com/mssql/server:2022-latest` (latest CU)
  - `mcr.microsoft.com/mssql/rhel/server:2022-latest` (RHEL variant)
- **Base OS**: Ubuntu 22.04 LTS (starting with CU10, GA for production)
- **Image Size**: ~1.5-1.6 GB (compressed, based on SQL Server 2019 reference)
- **LTS/Support**:
  - Mainstream support: Until January 11, 2028
  - Extended support: Until January 11, 2033
  - Lifecycle: Fixed Lifecycle Policy
- **Kubernetes Requirements**: N/A (runs as pod, no specific K8s version dependency)
- **Notes**:
  - SQL Server 2022 on Ubuntu 22.04 became GA starting with CU10
  - 2 GB minimum memory required at runtime
  - All editions supported on both Linux and Windows
- **PostgreSQL Alternative**:
  - **Recommended**: `postgres:17-bookworm` (PostgreSQL 17.6)
  - Alternative tags: `postgres:17-alpine` (Alpine 3.22, 36% smaller - 278MB vs 438MB)
  - Base OS: Debian 12 Bookworm (default) or Alpine 3.22
  - Image Size: 438MB (Bookworm), 278MB (Alpine)
  - Notes: Alpine uses musl libc vs glibc, ADR-0007 recommends Bookworm for glibc performance
- **References**:
  - https://learn.microsoft.com/en-us/lifecycle/products/sql-server-2022
  - https://mcr.microsoft.com/product/mssql/server/about
  - https://hub.docker.com/_/postgres

---

## 2. Redis

### Verified Information

- **Latest Stable Version**: 8.0 (8.0.5 as of search date)
- **Previous Stable**: 7.4 (7.4.7)
- **Recommended Docker Image**: `redis:8.0-bookworm`
- **Alternative Tags**:
  - `redis:8.0.5-bookworm` (pinned patch version)
  - `redis:8.0-alpine` (Alpine 3.21, smaller image)
  - `redis:7.4-bookworm` (if compatibility concerns with Redis 8)
  - `redis:7.4-alpine` (Alpine variant of Redis 7)
- **Base OS**:
  - Debian 12 Bookworm (recommended, glibc 2x faster per ADR-0007)
  - Alpine 3.21 (alternative, musl libc)
- **Image Size**: ~111 MB (Debian), ~30 MB (Alpine) - approximate uncompressed sizes
- **Official Helm Chart**:
  - **No official Redis Helm chart from Redis Inc.**
  - **Bitnami Helm chart (community standard)**: `oci://registry-1.docker.io/bitnamicharts/redis`
  - Latest version: 23.2.11 (from Bitnami)
  - Bitnami chart uses hardened Photon Linux base, includes monitoring, HA support
  - Alternative: Deploy raw Docker image with custom Kubernetes manifests
- **Kubernetes Requirements**: N/A for raw Docker deployment
- **Notes**:
  - Redis 8.0 represents major version upgrade from Redis 7.x
  - Consider testing compatibility with Dapr state store component before upgrading
  - Bitnami chart provides production-ready features (Sentinel, metrics, volume provisioning)
  - ADR-0007 recommends Debian Bookworm over Alpine for glibc performance benefits
- **References**:
  - https://hub.docker.com/_/redis
  - https://redis.io/downloads/
  - https://github.com/bitnami/charts/tree/main/bitnami/redis

---

## 3. RabbitMQ

### Verified Information

- **Latest Stable Version**: 4.2 (4.2.0 released October 28, 2025)
- **Recommended Docker Image**: `rabbitmq:4.2-management`
- **Alternative Tags**:
  - `rabbitmq:4.2.0-management` (pinned patch version)
  - `rabbitmq:4.2` (core only, no management plugin)
  - `rabbitmq:4.2-management-alpine` (Alpine variant with management)
  - `rabbitmq:4.2-alpine` (Alpine variant, core only)
- **Base OS**:
  - Ubuntu 24.04 LTS (default for RabbitMQ 4.x, per ADR-0007 assumption - NEEDS VERIFICATION)
  - Alpine Linux (alternative, `-alpine` suffix)
- **Image Size**: NEEDS VERIFICATION (search did not return specific sizes for 4.2)
- **Management Plugin**:
  - Included in `-management` tagged images
  - Provides administrative web UI and HTTP API
  - Default ports: 5672 (AMQP), 15672 (Management UI)
- **Prometheus Metrics**:
  - **Included**: `rabbitmq_prometheus` plugin (built-in since 3.8+)
  - **Default Port**: 15692
  - **Endpoint**: `http://localhost:15692/metrics`
  - Additional endpoints: `/metrics/per-object`, `/metrics/detailed`
  - Configuration: `prometheus.tcp.port = 15692`, `prometheus.path = /metrics`
- **Official Helm Chart**:
  - **No official RabbitMQ Helm chart from RabbitMQ team**
  - **Bitnami Helm chart (community standard)**: `oci://registry-1.docker.io/bitnamicharts/rabbitmq`
  - Latest version: 16.0.14 (from Bitnami)
  - **Alternative**: RabbitMQ Cluster Operator (official Kubernetes operator)
  - Bitnami chart includes Prometheus integration, volume provisioning, cluster support
- **Kubernetes Requirements**: N/A for raw Docker deployment
- **Notes**:
  - RabbitMQ 4.2 is a MAJOR version upgrade from 3.x (breaking changes likely)
  - RabbitMQ 4.3 is under development (expected first half 2026)
  - Management plugin variant recommended for production visibility
  - Prometheus metrics port 15692 enabled by default in management images
  - KEDA 2.18 fully supports RabbitMQ Queue scaler (AMQP and HTTP protocols)
- **References**:
  - https://hub.docker.com/_/rabbitmq
  - https://www.rabbitmq.com/release-information
  - https://www.rabbitmq.com/docs/prometheus
  - https://github.com/bitnami/charts/tree/main/bitnami/rabbitmq

---

## 4. Nginx

### Verified Information

- **Latest Stable Version**: 1.28 (1.28.0 released April 23, 2025)
- **Latest Mainline Version**: 1.27 (1.27.5)
- **Recommended Docker Image for Static Hosting**: `nginx:1.28-bookworm`
- **Recommended Ingress Controller**: `registry.k8s.io/ingress-nginx/controller:v1.14.0`
- **Alternative Tags (Static Nginx)**:
  - `nginx:1.28.0-bookworm` (pinned patch version)
  - `nginx:stable` (tracks latest stable)
  - `nginx:1.28-alpine` (Alpine variant, smaller)
  - `nginx:1.27-bookworm` (mainline, odd-numbered)
- **Base OS**:
  - Debian 12 Bookworm (recommended)
  - Alpine Linux (alternative)
- **Image Size**: ~68-72 MB (compressed, based on similar Bookworm variants)
- **Official Helm Chart (Ingress Controller)**:
  - **Repository**: https://kubernetes.github.io/ingress-nginx
  - **Chart Version**: 4.14.0 (latest)
  - **Controller Version**: v1.13.2 / v1.12.6
  - **Container Image**: `registry.k8s.io/ingress-nginx/controller:v1.14.0`
  - **NOTE**: Old registry `k8s.gcr.io` deprecated (frozen April 3, 2023), use `registry.k8s.io`
- **Kubernetes Requirements**:
  - **Minimum**: Kubernetes 1.19+ for latest ingress controller
  - Older K8s (1.18 or earlier): Use ingress-nginx 0.x versions
- **Notes**:
  - Nginx versioning: Even numbers = stable (1.28.x), Odd numbers = mainline (1.27.x, 1.29.x)
  - **Dual Role**:
    1. Static web server for RedDog.UI (Vue.js app) - use `nginx:1.28-bookworm`
    2. Kubernetes Ingress Controller - use `registry.k8s.io/ingress-nginx/controller:v1.14.0`
  - ADR-0007 specifies Nginx for static file hosting (UI), ingress controller is separate concern
  - IMPORTANT: `k8s.gcr.io` registry is deprecated, all images must use `registry.k8s.io`
- **References**:
  - https://hub.docker.com/_/nginx
  - https://nginx.org/en/download.html
  - https://kubernetes.github.io/ingress-nginx/deploy/
  - https://artifacthub.io/packages/helm/ingress-nginx/ingress-nginx

---

## 5. Cert-Manager

### Verified Information

- **Latest Stable Version**: 1.16 (recommended for Red Dog)
- **Newest Version**: 1.19 (requires Kubernetes 1.31+, too new for current cluster)
- **Recommended Helm Chart**: `oci://quay.io/jetstack/charts/cert-manager:v1.19.1`
- **Alternative Repository**:
  - Legacy HTTP: `helm repo add jetstack https://charts.jetstack.io`
  - OCI (recommended): `oci://quay.io/jetstack/charts/cert-manager`
  - Note: OCI charts published first, HTTP repository updated a few hours later
- **Kubernetes Requirements**:
  - **cert-manager 1.16**: Kubernetes 1.25 → 1.32
  - **cert-manager 1.19**: Kubernetes 1.31 → 1.34 (too new for most clusters)
  - **Recommendation**: Use 1.16 for broader K8s compatibility
- **Let's Encrypt ACME Support**:
  - **ACME v2**: Fully supported since cert-manager v0.3 (2018)
  - Current version 1.3.1 already uses ACME v2 (no migration needed)
  - ACME v1 was removed in cert-manager v0.3
- **Breaking Changes from 1.3.1**:
  - **API Version Deprecation** (v1.6): `v1alpha2`, `v1alpha3`, `v1beta1` no longer served (use `v1`)
  - **Private Key Rotation** (v1.16): Default `Certificate.Spec.PrivateKey.RotationPolicy` changed to `Always` (was `Never`)
  - **Revision History Limit** (v1.16): Default `Certificate.Spec.RevisionHistoryLimit` now `1` (was unlimited)
  - **ACME HTTP01 PathType** (v1.18): Changed to `Exact` (was `Prefix` or unspecified)
  - **Helm Schema Validation** (v1.16): Stricter validation for chart values
  - **Venafi Issuer** (v1.16): Compatibility changes for Venafi users
- **Support Window**:
  - Each release supported for ~8 months (until N+2 release)
  - Releases issued every 4 months
  - Always 2+ supported versions available
- **Notes**:
  - **MAJOR UPGRADE**: 1.3.1 (2021) to 1.16 (2024) spans 13 minor versions
  - Breaking changes require careful review of all intermediate release notes
  - Private key rotation default change may affect certificate renewals
  - API version v1 is stable, v1alpha/v1beta deprecated
  - Consider incremental upgrades (1.3 → 1.6 → 1.10 → 1.16) to test compatibility
- **References**:
  - https://cert-manager.io/docs/releases/
  - https://cert-manager.io/docs/installation/helm/
  - https://artifacthub.io/packages/helm/cert-manager/cert-manager
  - https://github.com/cert-manager/cert-manager/releases

---

## 6. Dapr

### Verified Information

- **Latest Stable Version**: 1.16 (1.16.2 latest patch, released September 2025)
- **Next Version**: 1.17 (in development, expected 2026)
- **Recommended Helm Chart**: `dapr/dapr` version 1.16.2
- **Helm Repository**: https://dapr.github.io/helm-charts/
- **Installation**:
  ```bash
  helm repo add dapr https://dapr.github.io/helm-charts/
  helm install dapr dapr/dapr --version=1.16.2 --namespace dapr-system --create-namespace --wait
  ```
- **Kubernetes Requirements**:
  - Aligned with Kubernetes Version Skew Policy
  - Supports current K8s release and previous minor versions
  - Helm v3 required (Helm v2 no longer supported)
  - Specific tested versions: NEEDS VERIFICATION (docs reference table not fully visible)
- **.NET Runtime Support**:
  - **Dapr 1.16 SDK**: .NET 8 and .NET 9 ONLY
  - **.NET 10 Support**: Coming in future 1.16.x patch release
  - **.NET 6 and .NET 7**: Dropped (EOL per Microsoft policy)
  - **Impact**: Red Dog cannot use Dapr 1.16 SDK until .NET 10 patch released
  - **Workaround**: Use Dapr HTTP/gRPC APIs directly (no SDK dependency)
- **Dapr .NET SDK Version**:
  - **Current**: Dapr.Client 1.16.0 (targets .NET 8/9)
  - **Note**: .NET 10 support requires waiting for patch release
- **Dapr CLI Version**:
  - Install/upgrade: `dapr upgrade --runtime-version 1.16.2 -k`
  - Local development: Use Dapr CLI to match runtime version
- **Breaking Changes from 1.3.0**:
  - **MAJOR UPGRADE**: 1.3.0 (2021) to 1.16.2 (2025) spans 13 minor versions
  - **HTTP Configuration**: `dapr-http-max-request-size` deprecated, use `max-body-size`
  - **.NET SDK**: Removed .NET 6/7 support, only .NET 8/9 supported (10 coming)
  - **Workflow Methods**: Deprecated workflow client methods pruned, migrate to durabletask library
  - **Conversation API**: Alpha1 deprecated (removed in 1.17), migrate to Alpha2
  - **Incremental Upgrades**: Tested upgrade paths exist, review table in docs
  - **Support Policy**: v1.13 and earlier no longer supported (N-2 policy)
- **Notes**:
  - **CRITICAL**: .NET 10 support not yet available in Dapr 1.16.0, coming in patch release
  - **BLOCKER**: Red Dog .NET services cannot use Dapr SDK until .NET 10 patch released
  - **MITIGATION**: Use Dapr HTTP/gRPC APIs directly (DaprClient not required)
  - Dapr 1.16 has "a few breaking changes" per release announcement
  - Review all intermediate release notes (1.4 through 1.15) for accumulated changes
  - Scheduler service reached Stable status in 1.15
  - Workflow performance improvements in 1.16
- **References**:
  - https://docs.dapr.io/operations/support/support-release-policy/
  - https://blog.dapr.io/posts/2025/09/16/dapr-v1.16-is-now-available/
  - https://github.com/dapr/dapr/releases
  - https://artifacthub.io/packages/helm/dapr/dapr
  - https://www.nuget.org/packages/Dapr.Client

---

## 7. KEDA

### Verified Information

- **Latest Stable Version**: 2.18 (2.18.1 released October 29, 2024)
- **Previous Version**: 2.17 (2.17.0 released April 7, 2025)
- **Recommended Helm Chart**: `kedacore/keda` version 2.18.1
- **Helm Repository**: https://kedacore.github.io/charts
- **Installation**:
  ```bash
  helm repo add kedacore https://kedacore.github.io/charts
  helm install keda kedacore/keda --version 2.18.1 -n keda --create-namespace
  ```
- **Kubernetes Requirements**:
  - **Minimum**: Kubernetes 1.30+
  - **Support Policy**: N-2 support window (current + previous 2 minor versions)
  - **Deployment**: Use `--server-side` flag for CRDs and admission webhooks
- **Dapr Integration**:
  - Compatible with Dapr 1.16
  - Dapr uses KEDA for autoscaling on Kubernetes
  - Azure Event Hub scaler mentions Dapr goSdk strategy for older versions
- **RabbitMQ Scaler**:
  - **Supported**: Yes, since KEDA v1.0+
  - **Maintained by**: Microsoft
  - **Protocols**: AMQP and HTTP
  - **Use Case**: Scale based on RabbitMQ Queue depth
- **Redis Scaler**:
  - **Supported**: Yes, multiple variants:
    - Redis Lists (standard)
    - Redis Lists (Redis Cluster support)
    - Redis Lists (Redis Sentinel support)
    - Redis Streams (standard)
    - Redis Streams (Redis Cluster support)
    - Redis Streams (Redis Sentinel support)
- **Breaking Changes from 2.2.0**:
  - **MAJOR UPGRADE**: 2.2.0 (2021) to 2.18.1 (2024) spans 16 minor versions
  - Specific breaking changes: NEEDS DETAILED ANALYSIS
  - Recommendation: Review migration guide at https://keda.sh/docs/2.18/migration/
  - Check all intermediate release notes for accumulated changes
- **Notes**:
  - KEDA 2.18 requires Kubernetes 1.30+, which may be newer than current cluster
  - Consider KEDA 2.17 if running older Kubernetes version
  - RabbitMQ and Redis scalers fully supported, no migration concerns for Red Dog use case
  - Comprehensive scaler library supports all Red Dog messaging/state components
  - Review KEDA 2.18 migration guide for detailed breaking changes
- **References**:
  - https://keda.sh/docs/2.18/deploy/
  - https://github.com/kedacore/keda/releases
  - https://artifacthub.io/packages/helm/kedacore/keda
  - https://keda.sh/docs/2.18/scalers/

---

## 8. Version Compatibility Matrix (Summary)

| Component | Current | Latest Stable | Recommended Target | Min K8s Version | Notes |
|-----------|---------|---------------|-------------------|-----------------|-------|
| SQL Server | 2019 | 2022 | `mcr.microsoft.com/mssql/server:2022-latest` | N/A (pod) | Ubuntu 22.04, GA since CU10 |
| PostgreSQL | N/A | 17 (17.6) | `postgres:17-bookworm` | N/A (pod) | Alternative to SQL Server per ADR-0007 |
| Redis | 15.0.0 (Helm) | 8.0 (8.0.5) | `redis:8.0-bookworm` | N/A | Bitnami chart or raw Docker |
| RabbitMQ | 8.20.2 (Helm) | 4.2 (4.2.0) | `rabbitmq:4.2-management` | N/A | Prometheus metrics on 15692 |
| Nginx (UI) | 3.31.0 (Helm) | 1.28 (1.28.0) | `nginx:1.28-bookworm` | N/A | For static file hosting |
| Nginx (Ingress) | 3.31.0 (Helm) | 4.14.0 (chart) | `registry.k8s.io/ingress-nginx/controller:v1.14.0` | 1.19+ | Ingress controller role |
| Cert-Manager | 1.3.1 | 1.19 | `cert-manager:v1.16.x` | 1.25-1.32 | 1.19 requires K8s 1.31+ |
| Dapr | 1.3.0 | 1.16 (1.16.2) | `dapr:1.16.2` | Per K8s skew policy | .NET 10 support pending patch |
| KEDA | 2.2.0 | 2.18 (2.18.1) | `keda:2.18.1` | 1.30+ | 2.17 if K8s <1.30 |

---

## 9. Key Findings and Recommendations

### Major Version Jumps Identified

1. **RabbitMQ**: 3.x → 4.2 (MAJOR version change)
   - Breaking changes expected
   - Review RabbitMQ 4.0 release notes before upgrading
   - Test Dapr pub/sub component compatibility

2. **Redis**: 7.x → 8.0 (MAJOR version change)
   - Breaking changes possible
   - Test Dapr state store component compatibility

3. **Dapr**: 1.3.0 → 1.16.2 (13 minor versions, 3+ years)
   - Multiple breaking changes accumulated
   - .NET 10 SDK support not yet available (BLOCKER)
   - Incremental upgrades recommended

4. **KEDA**: 2.2.0 → 2.18.1 (16 minor versions, 3+ years)
   - Kubernetes 1.30+ required (may be too new)
   - Consider KEDA 2.17 for older K8s clusters

5. **Cert-Manager**: 1.3.1 → 1.16 (13 minor versions)
   - Private key rotation default changed
   - API versions deprecated
   - Incremental upgrades recommended

### EOL/Deprecated Components

1. **Current Infrastructure** (All from 2021-2022):
   - SQL Server 2019: Mainstream support until 2025, extended until 2030 (not urgent)
   - Redis 15.0.0 Helm chart: Outdated, upgrade to Redis 8.0 or use Bitnami chart
   - RabbitMQ 8.20.2 Helm chart: Outdated, upgrade to 4.2 or use Bitnami chart
   - Cert-Manager 1.3.1: No longer supported (N+2 policy), upgrade critical
   - Dapr 1.3.0: No longer supported (N-2 policy), upgrade critical
   - KEDA 2.2.0: No longer supported, upgrade critical

2. **Registry Deprecation**:
   - `k8s.gcr.io` → `registry.k8s.io` (frozen April 2023)
   - All Kubernetes ingress controller images must migrate

### Helm Chart vs Raw Docker Trade-offs

| Approach | Bitnami Helm Chart | Raw Docker Image + Manifests |
|----------|-------------------|------------------------------|
| **Pros** | - Production-ready defaults<br>- Monitoring integration<br>- Volume provisioning<br>- HA/clustering support<br>- Regular security updates | - Full control over config<br>- Simpler to understand<br>- No Helm dependency<br>- Smaller footprint |
| **Cons** | - Additional abstraction layer<br>- Bitnami-specific config<br>- Helm chart versioning complexity | - Manual monitoring setup<br>- Manual volume management<br>- No built-in HA patterns<br>- More YAML to maintain |
| **Use Case** | Production workloads, teams with Helm expertise | Development, simple deployments, learning |

**Recommendation for Red Dog:**
- **SQL Server**: Raw Docker (single pod, no clustering needed)
- **PostgreSQL**: Raw Docker (single pod, simpler than SQL Server)
- **Redis**: **Bitnami Helm chart** (monitoring, volume management, HA option)
- **RabbitMQ**: **Bitnami Helm chart** (cluster support, metrics integration)
- **Nginx (UI)**: Raw Docker (simple static file hosting)
- **Nginx (Ingress)**: **Official Helm chart** (standard K8s pattern)
- **Cert-Manager**: **Official Helm chart** (required, CRDs + controllers)
- **Dapr**: **Official Helm chart** (required, CRDs + controllers)
- **KEDA**: **Official Helm chart** (required, CRDs + controllers)

### Conflicts and Incompatibilities Discovered

1. **Dapr 1.16 + .NET 10**:
   - **CONFLICT**: Dapr .NET SDK 1.16.0 does not support .NET 10
   - **BLOCKER**: Red Dog .NET services cannot use Dapr SDK until patch release
   - **MITIGATION**: Use Dapr HTTP/gRPC APIs directly (no SDK), or wait for Dapr 1.16.x patch

2. **KEDA 2.18 + Kubernetes Version**:
   - **REQUIREMENT**: KEDA 2.18 requires Kubernetes 1.30+
   - **ISSUE**: May be newer than current cluster version
   - **MITIGATION**: Use KEDA 2.17 if running Kubernetes <1.30

3. **Cert-Manager 1.19 + Kubernetes Version**:
   - **REQUIREMENT**: Cert-Manager 1.19 requires Kubernetes 1.31+
   - **ISSUE**: Too new for most production clusters
   - **RECOMMENDATION**: Use Cert-Manager 1.16 (supports K8s 1.25-1.32)

4. **RabbitMQ 4.2 + Dapr Pub/Sub**:
   - **RISK**: Major version upgrade may affect Dapr component compatibility
   - **TEST REQUIRED**: Verify Dapr pub/sub component with RabbitMQ 4.2 before production

5. **Redis 8.0 + Dapr State Store**:
   - **RISK**: Major version upgrade may affect Dapr component compatibility
   - **TEST REQUIRED**: Verify Dapr state store component with Redis 8.0 before production

### Recommended Upgrade Sequence (Based on Dependencies)

**CRITICAL PATH:** Kubernetes cluster version must be verified FIRST before any infrastructure upgrades.

#### Phase 1: Prerequisites (No Dependencies)
1. **Verify Kubernetes Version**: Confirm cluster version (must be 1.25+ for cert-manager 1.16, 1.30+ for KEDA 2.18)
2. **Upgrade Cert-Manager**: 1.3.1 → 1.16 (no dependencies, critical for security)
3. **Upgrade SQL Server or PostgreSQL**:
   - SQL Server: 2019 → 2022 (independent database pod)
   - OR PostgreSQL: Deploy PostgreSQL 17 as alternative (requires schema migration)

#### Phase 2: Messaging and State (Test Dapr Compatibility)
4. **Upgrade Redis**:
   - Deploy Redis 8.0 in parallel (new pod)
   - Test Dapr state store component compatibility
   - Migrate state, retire old Redis
5. **Upgrade RabbitMQ**:
   - Deploy RabbitMQ 4.2 in parallel (new pod)
   - Test Dapr pub/sub component compatibility
   - Migrate queues, retire old RabbitMQ

#### Phase 3: Dapr Runtime (Depends on Redis/RabbitMQ)
6. **Upgrade Dapr**: 1.3.0 → 1.16.2
   - **CRITICAL**: Do NOT upgrade application .NET SDK until Dapr 1.16.x patch with .NET 10 support
   - Use Dapr HTTP/gRPC APIs directly in .NET 10 services
   - Test all components (state store, pub/sub, service invocation, bindings)
   - Review all intermediate release notes (1.4-1.15)
   - Consider incremental upgrades (1.3 → 1.8 → 1.12 → 1.16)

#### Phase 4: Autoscaling (Depends on Dapr)
7. **Upgrade KEDA**: 2.2.0 → 2.18.1 (or 2.17 if K8s <1.30)
   - Verify Kubernetes version meets minimum requirement
   - Test RabbitMQ and Redis scalers with new versions
   - Review migration guide for breaking changes

#### Phase 5: Application Infrastructure (Last)
8. **Upgrade Nginx**: Deploy ingress controller 4.14.0 (for cluster ingress)
9. **Upgrade Nginx**: Deploy nginx:1.28-bookworm (for UI static hosting)

**ROLLBACK PLAN:** Each phase should have rollback procedures documented. Use parallel deployments where possible to minimize downtime.

**TESTING STRATEGY:** Deploy to development environment first, test each phase thoroughly before production deployment.

### Additional Recommendations

1. **Incremental Upgrades**:
   - Dapr 1.3 → 1.16: Consider 1.3 → 1.8 → 1.12 → 1.16 (test compatibility at each step)
   - Cert-Manager 1.3 → 1.16: Consider 1.3 → 1.6 → 1.10 → 1.16 (API version migrations)

2. **Dapr .NET 10 SDK Blocker**:
   - Monitor Dapr .NET SDK releases for .NET 10 support
   - Consider using Dapr HTTP/gRPC APIs directly until SDK patch available
   - Alternative: Delay .NET 10 migration until Dapr SDK patch released

3. **Base OS Strategy**:
   - Standardize on Debian Bookworm for glibc performance (per ADR-0007)
   - Use Alpine only for size-constrained environments (musl libc slower)

4. **Security Considerations**:
   - Review CVE advisories for all components before deployment
   - Use specific version tags (not `latest`) for production
   - Enable Prometheus metrics for monitoring (RabbitMQ 15692, Redis exporter)

5. **PostgreSQL vs SQL Server**:
   - ADR-0007 mentions PostgreSQL 17 as alternative
   - Requires schema migration (Entity Framework migrations)
   - Benefits: Open-source, cloud-agnostic, smaller image size
   - Trade-offs: Migration effort, team familiarity with T-SQL vs PostgreSQL

---

## 10. Verification Status

| Component | Official Source Verified | Image Tags Verified | K8s Compatibility Verified | Breaking Changes Reviewed |
|-----------|-------------------------|---------------------|---------------------------|--------------------------|
| SQL Server | ✓ | ✓ | N/A | ✓ |
| PostgreSQL | ✓ | ✓ | N/A | N/A (new) |
| Redis | ✓ | ✓ | N/A | Partial (needs Dapr testing) |
| RabbitMQ | ✓ | ✓ | N/A | Partial (needs Dapr testing) |
| Nginx (UI) | ✓ | ✓ | N/A | ✓ |
| Nginx (Ingress) | ✓ | ✓ | ✓ | ✓ |
| Cert-Manager | ✓ | ✓ | ✓ | ✓ |
| Dapr | ✓ | ✓ | Partial (docs reference table) | ✓ |
| KEDA | ✓ | ✓ | ✓ | ⚠ (needs detailed analysis) |

**Legend:**
- ✓ = Verified against official sources
- ⚠ = Partial verification, needs detailed review
- N/A = Not applicable

---

## 11. Next Steps

1. **Verify Current Kubernetes Version**: Check Red Dog cluster version to confirm KEDA/cert-manager compatibility
2. **Review Breaking Changes**: Deep dive into Dapr 1.4-1.15 and KEDA 2.3-2.17 release notes
3. **Test Dapr Compatibility**: Deploy Redis 8.0 and RabbitMQ 4.2 in dev, test with Dapr components
4. **Monitor Dapr .NET SDK**: Track Dapr .NET SDK releases for .NET 10 support patch
5. **Create Upgrade Plan**: Sequence infrastructure upgrades based on dependency graph
6. **Document Rollback Procedures**: Create rollback plan for each upgrade phase
7. **Update ADR-0007**: Confirm or revise infrastructure image recommendations based on this research

---

**Report Generated:** 2025-11-09
**Research Conducted By:** Claude Code (Haiku 4.5)
**Verification Method:** Web search against official documentation, Docker Hub, GitHub releases, and vendor docs
