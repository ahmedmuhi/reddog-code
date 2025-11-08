# Docker Compose & Infrastructure Container Analysis for Red Dog Coffee

## Executive Summary

This codebase contains **ONE Docker Compose file** (deleted) and **Kubernetes Helm/Static manifests** defining infrastructure containers. The project has evolved from Docker Compose-based local development (2021-2025) to **Kubernetes-only deployment** (2025-present).

---

## Docker Compose File History

### Current Status: DELETED ❌
**File:** `.devcontainer/docker-compose.yml`
**Status:** Deleted on 2025-11-02
**Deletion Commit:** `ecca0f5` (Ahmed Muhi)

### Evolution Timeline

#### 1. INITIAL CREATION (2021-06-14)
**Commit:** `6066105a703903ffe311077b1b430497098ab879`
**Author:** Lynn Orrell
**Date:** 2021-06-14 15:07:48
**Change:** `A` (Added)

**Services Defined:**
```yaml
version: '3.7'
services:
  app:                    # VS Code dev container
    build: ./
    environment:
      DAPR_NETWORK: dapr-dev-container
    volumes:
      - /var/run/docker.sock:/var/run/docker-host.sock
      - ..:/workspace:cached
    entrypoint: /usr/local/share/docker-init.sh
    command: sleep infinity
  # NOTE: Database NOT included in this version
networks:
  default:
    name: dapr-dev-container
```

**Purpose:** Development container for VS Code/GitHub Codespaces with Docker socket forwarding for local Dapr development

---

#### 2. LOCAL DAPR COMPONENTS ADDED (2022-01-12)
**Commit:** `7dc5cf5b97fbd53b64bf2c1e646c496c35f7e88b`
**Author:** asofio
**Date:** 2022-01-12 23:03:47
**Change:** `M` (Modified)

**Addition:**
```yaml
  db:
    image: "mcr.microsoft.com/mssql/server:2019-latest"
    environment:
      MSSQL_SA_PASSWORD: "pass@word1"
      ACCEPT_EULA: "Y"
```

**Containers Now Defined (2):**
1. **app** - VS Code development container
2. **db** - SQL Server 2019 (for local development)

**Context:** Added local Dapr configuration files in `manifests/local/branch/`:
- `reddog.pubsub.yaml`
- `reddog.secretstore.yaml`
- `reddog.state.makeline.yaml`
- `reddog.state.loyalty.yaml`
- `reddog.binding.receipt.yaml`
- `reddog.binding.virtualworker.yaml`

---

#### 3. SQL SERVER RELIABILITY IMPROVEMENTS (2022-05-10)
**Commit:** `99beec3b87413455b6025a88ac0ef73b3973b461`
**Author:** Lynn Orrell
**Date:** 2022-05-10 11:55:56
**Change:** `M` (Modified)

**Updates:**
```yaml
  db:
    image: "mcr.microsoft.com/mssql/server:2019-latest"
    environment:
      MSSQL_SA_PASSWORD: "pass@word1"
      ACCEPT_EULA: "Y"
    container_name: reddog-sql-server      # NEW
    restart: on-failure                     # NEW
```

**Related Changes:**
- Updated dev container to install EF Core tools
- Added REST client VS Code extension
- Updated to .NET 6
- Made MakeLineService and LoyaltyService etag-aware
- Pre-compiling EF models

**Docker Compose Containers (2):** No change in count
1. **app** - VS Code container
2. **db** - SQL Server 2019

---

#### 4. DELETION / DEVCONTAINER REMOVAL (2025-11-02)
**Commit:** `ecca0f5f859d5ba75d7d7bb805a0406ace473eaa`
**Author:** Ahmed Muhi
**Date:** 2025-11-02 07:12:00
**Change:** `D` (Deleted)

**Commit Message:**
```
Phase 1: Remove devcontainer and unused manifest directories

- Removed .devcontainer/ (not using devcontainers)
- Removed manifests/local/ (no local development focus)
- Removed manifests/corporate/ (no Arc scenarios)
- Removed docs/ (contained outdated local-dev.md)
```

**Final Docker Compose File (Before Deletion):**
```yaml
version: '3.7'
services:
  app:
    build: 
      context: .
      dockerfile: Dockerfile
      args:
        variant: 3.1
    environment:
      DAPR_NETWORK: dapr-dev-container
    init: true
    volumes:
      - /var/run/docker.sock:/var/run/docker-host.sock 
      - ..:/workspace:cached
    entrypoint: /usr/local/share/docker-init.sh
    command: sleep infinity
    
  db:
    image: "mcr.microsoft.com/mssql/server:2019-latest"
    environment:
      MSSQL_SA_PASSWORD: "pass@word1"
      ACCEPT_EULA: "Y"
    container_name: reddog-sql-server
    restart: on-failure
    
networks: 
  default:
    name: dapr-dev-container
```

---

## Current Infrastructure Containers (Kubernetes Deployment)

### Location: `/manifests/branch/dependencies/`

The project now defines infrastructure via **Kubernetes Helm Releases** and **Static Deployments**. These are the **7 infrastructure containers** identified in the modernization phase:

#### 1. DAPR (Distributed Application Runtime)
**File:** `/manifests/branch/dependencies/dapr/dapr.yaml`
**Type:** Helm Release
**Version:** 1.3.0 (outdated - target: 1.16+)

```yaml
apiVersion: helm.fluxcd.io/v1
kind: HelmRelease
metadata:
  name: dapr
  namespace: dapr-system
spec:
  releaseName: dapr
  chart:
    repository: https://dapr.github.io/helm-charts/
    name: dapr
    version: 1.3.0
```

**Purpose:** Service invocation, pub/sub, state management, bindings, secrets

---

#### 2. REDIS (State/Pub-Sub Store)
**File:** `/manifests/branch/dependencies/redis/redis.yaml`
**Type:** Helm Release
**Version:** 15.0.0 (Bitnami chart, equiv Redis 7.x)

```yaml
apiVersion: helm.fluxcd.io/v1
kind: HelmRelease
metadata:
  name: redis
  namespace: redis
spec:
  releaseName: redis
  targetNamespace: redis
  chart:
    repository: https://marketplace.azurecr.io/helm/v1/repo
    name: redis
    version: 15.0.0
  values:
    auth:
      password: MyPassword123
    master:
      podSecurityContext:
        enabled: true
        fsGroup: 2000
```

**Purpose:** 
- State store for MakeLineService (order queue)
- State store for LoyaltyService (loyalty points)
- Pub/Sub broker for order messaging

---

#### 3. SQL SERVER (Relational Database)
**File:** `/manifests/branch/dependencies/sql/sql-server.yaml`
**Type:** Kubernetes Deployment (Static)
**Version:** 2019-latest

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mssql-deployment
spec:
  replicas: 1
  containers:
  - name: mssql
    image: mcr.microsoft.com/mssql/server:2019-latest
    ports:
    - containerPort: 1433
    env:
    - name: MSSQL_PID
      value: "Developer"
    - name: ACCEPT_EULA
      value: "Y"
    - name: SA_PASSWORD
      valueFrom:
        secretKeyRef:
          name: mssql
          key: SA_PASSWORD
    volumeMounts:
    - name: mssqldb
      mountPath: /var/opt/mssql
  volumes:
  - name: mssqldb
    persistentVolumeClaim:
      claimName: mssql-data
---
apiVersion: v1
kind: Service
metadata:
  name: mssql-deployment
spec:
  type: LoadBalancer
  ports:
  - protocol: TCP
    port: 1433
```

**Purpose:** 
- Persistent storage for AccountingService (sales analytics)
- Database: "reddogdemo"
- Provisioned via EF Core migrations in Bootstrapper

---

#### 4. RABBITMQ (Message Broker)
**File:** `/manifests/branch/dependencies/rabbitmq/rabbitmq.yaml`
**Type:** Helm Release
**Version:** 8.20.2 (RabbitMQ 3.x era, target: 4.2.0)

```yaml
apiVersion: helm.fluxcd.io/v1
kind: HelmRelease
metadata:
  name: rabbitmq
  namespace: rabbitmq
spec:
  releaseName: rabbitmq
  targetNamespace: rabbitmq
  chart:
    repository: https://marketplace.azurecr.io/helm/v1/repo
    name: rabbitmq
    version: 8.20.2
  values:
    replicaCount: 3
    service:
      type: LoadBalancer
    auth:
      username: contosoadmin
      password: MyPassword123
```

**Purpose:** 
- Message broker for order pub/sub pipeline (added May 2021, replacing Azure Service Bus)
- Replicated deployment (3 replicas for HA)
- Management UI available

---

#### 5. NGINX (Ingress Controller)
**File:** `/manifests/branch/dependencies/nginx/nginx.yaml`
**Type:** Helm Release
**Version:** 3.31.0 (old - target: latest)

```yaml
apiVersion: helm.fluxcd.io/v1
kind: HelmRelease
metadata:
  name: nginx-ingress
  namespace: nginx-ingress
spec:
  releaseName: nginx-ingress
  chart:
    repository: https://kubernetes.github.io/ingress-nginx
    name: ingress-nginx
    version: 3.31.0
  values:
    controller:
      service:
        type: LoadBalancer
        annotations: 
          service.beta.kubernetes.io/azure-dns-label-name: "paas-vnext-workshop"
      replicaCount: 2
```

**Purpose:** 
- HTTP/HTTPS ingress routing
- Load balancer for UI and API traffic
- DNS label for Azure Load Balancer

---

#### 6. CERT-MANAGER (TLS Certificate Management)
**File:** `/manifests/branch/dependencies/cert-manager/cert-manager.yaml`
**Type:** Helm Release
**Version:** 1.3.1 (outdated - target: 1.19+)

```yaml
apiVersion: helm.fluxcd.io/v1
kind: HelmRelease
metadata:
  name: cert-manager
  namespace: cert-manager
spec:
  releaseName: cert-manager
  chart:
    repository: https://charts.jetstack.io
    name: cert-manager
    version: 1.3.1
  values:
    installCRDs: true
```

**Purpose:** 
- TLS certificate provisioning and renewal
- Integration with ClusterIssuer for Let's Encrypt

**Related File:** `/manifests/branch/dependencies/yaml/cluster-issuer.yaml` (ClusterIssuer CRD configuration)

---

#### 7. KEDA (Kubernetes Event Driven Autoscaling)
**File:** `/manifests/branch/dependencies/keda/keda.yaml`
**Type:** Helm Release
**Version:** 2.2.0 (outdated - target: 2.18.1)

```yaml
apiVersion: helm.fluxcd.io/v1
kind: HelmRelease
metadata:
  name: keda
  namespace: keda
spec:
  releaseName: keda
  chart:
    repository: https://kedacore.github.io/charts
    name: keda
    version: 2.2.0
```

**Purpose:** 
- Event-driven autoscaling for services
- Scales microservices based on custom metrics (e.g., queue depth)

---

## Supporting Infrastructure Components

### PersistentVolumeClaim (SQL Server Storage)
**File:** `/manifests/branch/dependencies/sql/pvc.yaml`
- SQL Server data persistence
- Named: `mssql-data`

### Namespaces
**File:** `/manifests/branch/dependencies/yaml/namespaces.yaml`
- Creates: `reddog-retail` (application services)
- Creates: `dapr-system` (Dapr system namespace)
- Creates: `redis`, `rabbitmq`, `nginx-ingress`, `cert-manager`, `keda`

### Cluster Issuer (TLS)
**File:** `/manifests/branch/dependencies/yaml/cluster-issuer.yaml`
- Let's Encrypt integration for certificate provisioning

---

## Local Development Components (Deleted)

**Location:** `/manifests/local/branch/` (DELETED in ecca0f5)

Before deletion, these Dapr component definitions were used for local dev:

```yaml
# reddog.pubsub.yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: reddog.pubsub
  namespace: reddog-retail
spec:
  type: pubsub.redis
  version: v1
  metadata:
  - name: redisHost
    value: dapr_redis_dapr-dev-container:6379
scopes:
  - order-service
  - make-line-service
  - loyalty-service
  - receipt-generation-service
  - accounting-service
auth:
  secretStore: reddog.secretstore
```

**Deleted Files:**
- `reddog.pubsub.yaml` (Redis pub/sub)
- `reddog.secretstore.yaml` (Local secret store)
- `reddog.state.loyalty.yaml` (Loyalty state)
- `reddog.state.makeline.yaml` (MakeLine queue state)
- `reddog.binding.receipt.yaml` (Receipt generation binding)
- `reddog.binding.virtualworker.yaml` (Worker completion binding)
- `secrets.json` (Local development secrets)

---

## Container Versions Over Time

### Docker Compose Era (2021-2025)
| Container | Version | Image | Notes |
|-----------|---------|-------|-------|
| VS Code Dev | 3.1 variant | .NET 3.1 → .NET 6 (Apr 2022) | Evolved over time |
| SQL Server | 2019-latest | mcr.microsoft.com/mssql/server | Unchanged throughout |

### Kubernetes Helm Era (2021-2025)
| Component | Current Version | Target Version | Status |
|-----------|-----------------|-----------------|--------|
| Dapr | 1.3.0 | 1.16.2 | Outdated |
| Redis | 15.0.0 (Helm) | 7.x or cloud-native (DynamoDB/Cosmos) | Outdated |
| SQL Server | 2019-latest | 2022-latest | Outdated |
| RabbitMQ | 8.20.2 (Helm) | 4.2.0 | Outdated |
| Nginx | 3.31.0 | Latest | Outdated |
| Cert-Manager | 1.3.1 | 1.19 | Outdated |
| KEDA | 2.2.0 | 2.18.1 | Outdated |

---

## Architectural Evolution

### Phase 1: Docker Compose (2021-2025)
**Approach:** VS Code Development Container + Docker Compose

**Services in Compose:**
- 1 development container (app)
- 1 SQL Server instance (db)

**Infrastructure (Separate Kubernetes):**
- 7 infrastructure containers (Dapr, Redis, RabbitMQ, SQL Server, Nginx, Cert-Manager, KEDA)

**Drawback:** Mismatch between local dev (Compose) and production (Kubernetes)

---

### Phase 2: Kubernetes-Only (2025-Present)
**Approach:** Removed Docker Compose, now Kubernetes-only

**Rationale (from commit ecca0f5):**
> "Phase 1: Remove devcontainer and unused manifest directories
> - Removed .devcontainer/ (not using devcontainers)
> - Removed manifests/local/ (no local development focus)"

**Current Status:**
- All deployment via Kubernetes manifests
- No local Docker Compose development environment
- Focus shifts to cloud-native/Kubernetes development

---

## Research Documents (Strategic Context)

### 1. Docker Compose vs .NET Aspire Comparison
**File:** `/home/user/reddog-code/docs/research/docker-compose-vs-aspire-comparison.md`
**Date:** 2025-11-09
**Status:** Comprehensive 471-line analysis

**Key Finding:** Docker Compose recommended for Red Dog Coffee because:
- Cloud-agnostic (works on AWS, Azure, GCP)
- Supports polyglot architecture (Go, Python, Node.js, .NET)
- Industry-standard patterns
- Production parity (same tool dev → prod)

**However:** Document is dated AFTER docker-compose.yml was deleted, suggesting:
- Research was conducted for future re-adoption
- Potential discussion to bring Docker Compose back
- Gap identified: No local development strategy after deletion

---

### 2. RADIUS vs Docker Compose Analysis
**File:** `/home/user/reddog-code/docs/research/radius-vs-docker-compose.md`
**Date:** 2025-11-09
**Status:** Comprehensive 782-line analysis

**Recommendation:** Stick with Docker Compose for local dev

**Key Quote:**
> "RADIUS is a platform engineering tool that helps platform teams build internal developer platforms (IDPs). It is not a direct Docker Compose replacement."

**For Red Dog Coffee:**
> "Stick with Docker Compose for local development while monitoring RADIUS for future adoption."

---

## Migration Patterns Identified

### Pattern 1: Docker Compose → Kubernetes (2021-2025)
1. Started with docker-compose.yml for local dev
2. Evolved to Kubernetes Helm manifests for infrastructure
3. Removed Compose entirely (ecca0f5, 2025-11-02)

### Pattern 2: Image Version Stagnation
- Dapr: 1.3.0 (released 2021) → needs 1.16.2
- Redis: 15.0.0 Helm (outdated) → needs 7.x or cloud-native
- Cert-Manager: 1.3.1 → needs 1.19
- KEDA: 2.2.0 → needs 2.18.1
- Nginx: 3.31.0 (2021) → latest

### Pattern 3: Infrastructure-as-Code Evolution
1. Manual Kubernetes manifests (early 2021)
2. Helm Releases via Flux (2021-2025)
3. Future: Cloud-native services (Dapr with DynamoDB/Cosmos, not Redis)

### Pattern 4: Local Development Gap
1. **Before (2021-2025):** Docker Compose + local Dapr components
2. **After (2025-present):** No local development environment defined
3. **Research suggests:** Gap needs addressing before Phase 1A (.NET upgrades)

---

## Critical Findings

### Finding 1: Local Development Infrastructure Removed
The project **deleted local development infrastructure** (manifests/local/) without providing a replacement. The research documents suggest this was deliberate but creates a gap:

**Problem:**
- Docker Compose infrastructure deleted (2025-11-02)
- No Docker Compose replacement provided
- No local development strategy documented

**Solution Options (per research docs):**
1. Restore Docker Compose + update to latest versions
2. Implement RADIUS + kind for Kubernetes-based local dev
3. Use .NET Aspire for .NET-focused development

### Finding 2: Version Drift
All infrastructure containers are 1-3 years outdated:
- **Impact:** Security vulnerabilities, missing features, compatibility issues
- **Scope:** 7 infrastructure components need upgrading
- **Status:** Phase 0 planning documents identify this as blocker for Phase 1A

### Finding 3: Docker Compose Decision Contradiction
1. **Decision (2025-11-02):** Deleted Docker Compose
2. **Analysis (2025-11-09):** Recommended Docker Compose
3. **Status:** Unresolved tension in modernization strategy

### Finding 4: No Production Docker Compose
**Important:** The deleted docker-compose.yml was for local VS Code development ONLY.
- Production deployments: Kubernetes Helm charts
- No production Docker Compose files found

---

## Summary Tables

### All Docker Compose Files Found
| Path | Status | Created | Deleted | Versions |
|------|--------|---------|---------|----------|
| `.devcontainer/docker-compose.yml` | DELETED | 2021-06-14 | 2025-11-02 | 3 versions |

### All Infrastructure Containers (Current Kubernetes)
| Name | Type | File | Chart Version | Target Version |
|------|------|------|----------------|-----------------|
| Dapr | Helm Release | dapr/dapr.yaml | 1.3.0 | 1.16.2 |
| Redis | Helm Release | redis/redis.yaml | 15.0.0 | 7.x cloud-native |
| SQL Server | K8s Deployment | sql/sql-server.yaml | 2019-latest | 2022-latest |
| RabbitMQ | Helm Release | rabbitmq/rabbitmq.yaml | 8.20.2 | 4.2.0 |
| Nginx | Helm Release | nginx/nginx.yaml | 3.31.0 | Latest |
| Cert-Manager | Helm Release | cert-manager/cert-manager.yaml | 1.3.1 | 1.19 |
| KEDA | Helm Release | keda/keda.yaml | 2.2.0 | 2.18.1 |

### Services/Containers Defined
#### In Deleted Docker Compose (Development)
- 1 VS Code development container
- 1 SQL Server 2019 instance

#### In Current Kubernetes (Production)
- 1 Dapr control plane (distributed across infrastructure)
- 1 Redis cluster (3+ replicas)
- 1 SQL Server instance (1 replica, stateful)
- 1 RabbitMQ cluster (3 replicas)
- 1 Nginx ingress controller (2 replicas)
- 1 Cert-Manager (system-wide)
- 1 KEDA operator (system-wide)

**Total: 7 unique infrastructure containers (distributed across multiple K8s resources)**

---

## Recommendations

### Immediate (Phase 0)
1. **Restore local development strategy:** Docker Compose with updated container versions
2. **Update all infrastructure versions:** Use target versions identified in Phase 0 planning
3. **Document the gap:** Add local dev setup guide to README

### Short-term (Phase 1A)
1. **Upgrade Dapr:** 1.3.0 → 1.16.2
2. **Upgrade Redis:** 15.0.0 Helm → 7.x (local) or DynamoDB/Cosmos (cloud)
3. **Upgrade SQL Server:** 2019 → 2022
4. **Upgrade RabbitMQ:** 8.20.2 → 4.2.0
5. **Upgrade KEDA:** 2.2.0 → 2.18.1
6. **Upgrade Cert-Manager:** 1.3.1 → 1.19

### Long-term
1. **Cloud-native state stores:** Replace Redis with Cosmos DB (Azure) / DynamoDB (AWS)
2. **Evaluate RADIUS:** When v1.0+ released and teaching curriculum expands
3. **Polyglot support:** Ensure Docker Compose works for Go, Python, Node.js services

---

**Analysis Date:** 2025-11-08
**Codebase Status:** git branch `claude/review-last-commit-011CUwHzhqmCoNKoY1dGbnzW`
**Last Analyzed Commit:** `ec91b30` (Phase 0 infrastructure planning)

