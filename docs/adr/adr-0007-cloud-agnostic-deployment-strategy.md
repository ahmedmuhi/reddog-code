---
title: "ADR-0007: Cloud-Agnostic Deployment via Containerized Infrastructure"
status: "Accepted"
date: "2025-11-09"
authors: "Red Dog Modernization Team"
tags: ["architecture", "decision", "cloud-agnostic", "containers", "infrastructure", "portability"]
supersedes: ""
superseded_by: ""
---

# ADR-0007: Cloud-Agnostic Deployment via Containerized Infrastructure

## Status

**Accepted**

## Context

Red Dog's modernization strategy targets deployment across multiple cloud platforms (AKS, Container Apps, EKS, GKE) and teaching scenarios where students should learn portable, infrastructure-agnostic patterns.

**Key Constraints:**
- Multi-cloud deployment targets require identical application architecture across all platforms
- Teaching/demo focus requires clear narrative: "This app deploys anywhere without code changes"
- REQ-001 (from ADR-0002): Dapr provides cloud-agnostic service abstraction
- Students need local development environment that mirrors production behavior

**Current State:**
- Application uses Dapr pub/sub and state store components
- Historical migration (May 2021): Azure Service Bus → RabbitMQ for cloud-agnostic messaging
- Infrastructure dependencies: Message broker, state store, database

**Infrastructure Options:**

| Component | Cloud-Specific PaaS | Containerized Self-Hosted |
|-----------|-------------------|---------------------------|
| **Message Broker** | Azure Service Bus, AWS SQS, GCP Pub/Sub | RabbitMQ container |
| **State/Cache** | Azure Cache for Redis, AWS ElastiCache, GCP Memorystore | Redis container |
| **Database** | Azure SQL Database, AWS RDS, GCP Cloud SQL | SQL Server/PostgreSQL container |

**Problem:**
Using cloud-specific PaaS services creates vendor lock-in:
- Different APIs, connection patterns, and pricing models per cloud
- Application must be reconfigured for each cloud provider
- Local development requires cloud connectivity or emulators
- Teaching complexity: "On Azure use X, on AWS use Y, on GCP use Z"
- Violates cloud-agnostic architecture principle

**Historical Context:**
In May 2021 (commit `3d91853`), Red Dog migrated from Azure Service Bus (`pubsub.azure.servicebus`) to containerized RabbitMQ (`pubsub.rabbitmq`) specifically to achieve cloud-agnostic deployment. This decision proved the viability of containerized infrastructure for multi-cloud portability.

## Decision

**Adopt containerized infrastructure (self-hosted in Kubernetes) instead of cloud-specific PaaS services for all infrastructure dependencies.**

**Implementation:**
- **Message Broker (Pub/Sub)**: RabbitMQ container (`rabbitmq:4.1-management`)
- **State Store (Cache)**: Redis container (`redis:7-bookworm`)
- **Database**: SQL Server container (`mcr.microsoft.com/mssql/server:2022-latest` with Developer Edition)

**Local Development:**
- Docker Compose with same container images as production
- Dapr components configured for localhost infrastructure
- Zero cloud connectivity required for development/testing

**Production Deployment:**
- Kubernetes StatefulSets for RabbitMQ, Redis, and SQL Server (or PostgreSQL)
- Dapr components configured for Kubernetes service DNS
- Application code identical across local and production environments

**Rationale:**
- **PORT-001**: **Multi-Cloud Portability**: Same containers deploy to AKS, EKS, GKE, Container Apps, or on-premises Kubernetes without modification
- **PORT-002**: **Protocol-Based Abstraction**: Infrastructure uses standard protocols (AMQP for RabbitMQ, Redis protocol, SQL protocol) that work identically regardless of deployment location
- **PORT-003**: **Dapr Component Swapping**: Application uses Dapr APIs (`DaprClient`); only Dapr component YAML changes between environments (localhost vs K8s service DNS)
- **PORT-004**: **Production Parity**: Docker Compose local development uses identical containers as production StatefulSets, eliminating "works on my machine" issues
- **PORT-005**: **Teaching Clarity**: "This application runs on any Kubernetes cluster" - single, clear message. No cloud-specific service explanations.
- **PORT-006**: **Cost Efficiency**: Students run entire stack locally without cloud costs. Instructors demo on any cloud without vendor-specific accounts.

## Scope

This ADR applies to **infrastructure containers** (third-party dependencies we consume):
- RabbitMQ, Redis, SQL Server, PostgreSQL, Nginx (UI runtime), message brokers, databases

This ADR does **NOT** apply to **application containers** (services we build):
- OrderService, AccountingService, MakeLineService, etc.
- See ADR-0003 for application container base image standardization (Ubuntu 24.04)

## Consequences

### Positive

- **POS-001**: **Zero Cloud Lock-In**: No dependency on Azure Service Bus, AWS SQS, Azure SQL Database, or cloud-specific services. Application architecture portable across all platforms.
- **POS-002**: **Kubernetes-Native Deployment**: All infrastructure runs in StatefulSets with persistent volumes. Standard K8s patterns apply to AKS, EKS, GKE identically.
- **POS-003**: **Local Development Without Cloud**: Students run complete microservices stack locally via Docker Compose. No Azure/AWS/GCP accounts required for development.
- **POS-004**: **Configuration Portability**: Dapr component YAML is only difference between environments. Application code (C#, Go, Python, Node.js) unchanged.
- **POS-005**: **Teaching Multi-Cloud Patterns**: Instructors demonstrate deployment to AKS, then EKS, then GKE without application changes. Proves cloud-agnostic architecture value.
- **POS-006**: **Official Image Performance**: Using official upstream images (RabbitMQ, Redis) provides glibc-optimized performance (2x faster than Alpine for Redis), vendor security patches, and production validation.
- **POS-007**: **Cost Predictability**: Infrastructure costs are compute/storage only (no PaaS service fees). Same pricing model across all clouds (K8s node hours + disk).
- **POS-008**: **Consistent Monitoring**: Prometheus metrics from RabbitMQ, Redis, and application containers in all environments. Single observability stack (Grafana) for local dev and production.

### Negative

- **NEG-001**: **Operational Overhead**: Self-hosted infrastructure requires managing StatefulSets, persistent volumes, backups, and upgrades. PaaS services offload this to cloud providers.
- **NEG-002**: **No Managed Service SLAs**: Unlike Azure Cache for Redis (99.9% SLA), self-hosted Redis SLA depends on Kubernetes cluster availability and backup strategy.
- **NEG-003**: **StatefulSet Complexity**: Requires understanding persistent volume claims, headless services, and stateful pod management. More complex than PaaS connection strings.
- **NEG-004**: **Database Licensing**: SQL Server Developer Edition free for dev/test but cannot be used in production. Must switch to Express (10GB limit) or paid licenses for production. PostgreSQL migration eliminates this concern.
- **NEG-005**: **Scaling Limitations**: Cloud PaaS services offer automatic scaling (e.g., Azure Cache for Redis scales to 1.2TB). Self-hosted Redis limited by Kubernetes node resources and manual scaling.
- **NEG-006**: **Security Hardening Required**: Self-hosted infrastructure requires manual TLS configuration, network policies, and secret management. PaaS services provide this by default.
- **NEG-007**: **Backup Strategy Complexity**: Must implement backup strategies for RabbitMQ (queue persistence), Redis (RDB/AOF snapshots), and SQL Server (automated backups). PaaS services provide point-in-time restore.

### Mitigations

- **MIT-001**: For production deployments requiring SLAs, consider managed Kubernetes add-ons (e.g., AKS Azure Cache for Redis integration) while keeping containerized option for other clouds.
- **MIT-002**: Use Bitnami Helm charts for RabbitMQ and Redis, which include production-ready configurations (TLS, persistence, backups, monitoring).
- **MIT-003**: Migrate from SQL Server to PostgreSQL to eliminate licensing concerns (fully open-source, no production restrictions).
- **MIT-004**: Teaching scenarios prioritize portability over operational simplicity. For production systems, re-evaluate PaaS vs self-hosted based on team expertise.

## Alternatives Considered

### Cloud-Specific PaaS Services

- **ALT-001**: **Description**: Use Azure Service Bus + Azure Cache for Redis + Azure SQL Database on Azure; AWS SQS + ElastiCache + RDS on AWS; GCP Pub/Sub + Memorystore + Cloud SQL on GCP.
- **ALT-002**: **Rejection Reason**: Violates cloud-agnostic architecture principle. Application must be reconfigured for each cloud (different connection strings, APIs, auth patterns). Teaching complexity: students learn cloud-specific services, not portable patterns. Historical evidence: Project migrated from Azure Service Bus to RabbitMQ in May 2021 for this exact reason.

### Hybrid Approach (PaaS for Managed Services, Containers for Apps)

- **ALT-003**: **Description**: Use cloud PaaS for infrastructure (Azure Service Bus, Azure Cache) but containerize application services. Leverage Dapr to abstract PaaS differences.
- **ALT-004**: **Rejection Reason**: Dapr can abstract some differences, but configuration still varies by cloud (e.g., Azure Service Bus topics vs AWS SQS queues have different semantics). Requires cloud accounts for local development (or emulators with feature gaps). Partially defeats portability goal.

### Emulators for Local Development, PaaS for Production

- **ALT-005**: **Description**: Use Azurite (Azure Storage emulator), LocalStack (AWS emulator) for local dev; PaaS services in production.
- **ALT-006**: **Rejection Reason**: Emulators have feature gaps and behavioral differences vs real services (Azure Cosmos DB emulator missing features, LocalStack free tier limited). "Works in dev, breaks in production" risk. Does not solve multi-cloud deployment problem (still locked to single cloud for production).

### Serverless Infrastructure (Managed Kubernetes Services)

- **ALT-007**: **Description**: Use managed Kubernetes services (AKS, EKS, GKE) with cluster autoscaling for infrastructure workloads.
- **ALT-008**: **Acceptance Reason**: This is the chosen approach. Managed Kubernetes provides infrastructure portability (same StatefulSets work everywhere) while offloading cluster management (node upgrades, control plane availability) to cloud providers. Best of both worlds.

## Implementation Notes

### Infrastructure Container Images

| Component | Official Image | Base OS | Size (Compressed) | Notes |
|-----------|---------------|---------|-------------------|-------|
| **RabbitMQ** | `rabbitmq:4.1-management` | Ubuntu 24.04 | 125 MB | Management plugin enables Prometheus metrics (port 15692) and web UI (port 15672) |
| **Redis** | `redis:7-bookworm` | Debian 12 Bookworm | 45 MB | glibc provides 2x performance vs Alpine (musl libc). Bitnami default. |
| **SQL Server** | `mcr.microsoft.com/mssql/server:2022-latest` | Ubuntu 22.04 | 1.5 GB | Use `MSSQL_PID=Developer` for free dev/test license |
| **PostgreSQL** | `postgres:17-bookworm` | Debian 12 Bookworm | 131 MB | Alternative to SQL Server with no licensing restrictions |
| **Nginx (UI)** | `nginx:1.27-bookworm` | Debian 12 Bookworm | 67 MB | Static site hosting for Vue.js UI |

**Image Selection Rationale:**
- **Official Upstream Images**: Use Docker Hub official images or Microsoft Container Registry (MCR) for vendor support and security patches
- **Debian/Ubuntu Preference**: glibc performance optimization (2x faster for Redis vs Alpine musl), official RabbitMQ support (Alpine not supported), alignment with ADR-0003 for .NET services
- **Management/Monitoring Variants**: RabbitMQ management plugin required for Prometheus metrics in Kubernetes production environments

### Docker Compose Configuration (Local Development)

**File**: `docker-compose.yml` (to be created)

```yaml
services:
  redis:
    image: redis:7-bookworm
    container_name: reddog-redis
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    command: redis-server --appendonly yes

  rabbitmq:
    image: rabbitmq:4.1-management
    container_name: reddog-rabbitmq
    hostname: reddog-rabbitmq  # RabbitMQ requires stable hostname
    ports:
      - "5672:5672"   # AMQP
      - "15672:15672" # Management UI
      - "15692:15692" # Prometheus metrics
    environment:
      - RABBITMQ_DEFAULT_USER=contosoadmin
      - RABBITMQ_DEFAULT_PASS=MyPassword123
    volumes:
      - rabbitmq-data:/var/lib/rabbitmq

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: reddog-sqlserver
    ports:
      - "1433:1433"
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=YourStrong!Passw0rd
      - MSSQL_PID=Developer  # Free for dev/test
    volumes:
      - sqlserver-data:/var/opt/mssql

volumes:
  redis-data:
  rabbitmq-data:
  sqlserver-data:
```

**Usage:**
```bash
# Start all infrastructure
docker compose up -d

# View logs
docker compose logs -f rabbitmq

# Access RabbitMQ Management UI
open http://localhost:15672  # user: contosoadmin, pass: MyPassword123

# Stop and clean up
docker compose down -v
```

### Kubernetes Production Deployment

**Deployment Strategy**: Use Helm charts for production-ready configurations (persistence, monitoring, backups).

**RabbitMQ (Bitnami Helm Chart)**:
```yaml
# manifests/branch/dependencies/rabbitmq/rabbitmq.yaml
apiVersion: helm.fluxcd.io/v1
kind: HelmRelease
metadata:
  name: rabbitmq
  namespace: rabbitmq
spec:
  releaseName: rabbitmq
  chart:
    repository: https://charts.bitnami.com/bitnami
    name: rabbitmq
    version: 16.0.14
  values:
    image:
      registry: docker.io
      repository: rabbitmq
      tag: 4.1-management  # Official image
    replicaCount: 3
    auth:
      username: contosoadmin
      existingPasswordSecret: rabbitmq-credentials
    persistence:
      enabled: true
      size: 8Gi
    metrics:
      enabled: true
      serviceMonitor:
        enabled: true
```

**Redis (Bitnami Helm Chart)**:
```yaml
# manifests/branch/dependencies/redis/redis.yaml
apiVersion: helm.fluxcd.io/v1
kind: HelmRelease
metadata:
  name: redis
  namespace: redis
spec:
  releaseName: redis
  chart:
    repository: https://charts.bitnami.com/bitnami
    name: redis
    version: 20.5.0
  values:
    image:
      registry: docker.io
      repository: redis
      tag: 7-bookworm  # Official image
    master:
      persistence:
        enabled: true
        size: 8Gi
    metrics:
      enabled: true
      serviceMonitor:
        enabled: true
```

**SQL Server (Manual StatefulSet)**:
```yaml
# manifests/branch/dependencies/sqlserver/statefulset.yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: sqlserver
  namespace: database
spec:
  serviceName: sqlserver
  replicas: 1
  selector:
    matchLabels:
      app: sqlserver
  template:
    metadata:
      labels:
        app: sqlserver
    spec:
      containers:
      - name: sqlserver
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
        - name: ACCEPT_EULA
          value: "Y"
        - name: MSSQL_SA_PASSWORD
          valueFrom:
            secretKeyRef:
              name: sqlserver-credentials
              key: password
        - name: MSSQL_PID
          value: "Developer"  # Change to Express or Standard for production
        ports:
        - containerPort: 1433
        volumeMounts:
        - name: data
          mountPath: /var/opt/mssql
  volumeClaimTemplates:
  - metadata:
      name: data
    spec:
      accessModes: ["ReadWriteOnce"]
      resources:
        requests:
          storage: 20Gi
```

### Dapr Component Configuration

**Local Development** (`components/reddog.pubsub.yaml`):
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: reddog.pubsub
spec:
  type: pubsub.rabbitmq
  version: v1
  metadata:
  - name: host
    value: "amqp://contosoadmin:MyPassword123@localhost:5672"
  - name: durable
    value: "true"
  - name: deletedWhenUnused
    value: "false"
```

**Kubernetes Production** (`manifests/branch/base/components/reddog.pubsub.yaml`):
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: reddog.pubsub
spec:
  type: pubsub.rabbitmq
  version: v1
  metadata:
  - name: host
    value: "amqp://contosoadmin:MyPassword123@rabbitmq.rabbitmq.svc.cluster.local:5672"
  - name: durable
    value: "true"
  - name: deletedWhenUnused
    value: "false"
```

**Application Code (Unchanged)**:
```csharp
// OrderService - publishes order events
await daprClient.PublishEventAsync("reddog.pubsub", "orders", orderSummary);

// MakeLineService - subscribes to order events
[Topic("reddog.pubsub", "orders")]
public async Task<ActionResult> ProcessOrder(OrderSummary order) { ... }
```

**Key Insight**: Only Dapr component YAML changes (`localhost:5672` vs `rabbitmq.rabbitmq.svc.cluster.local:5672`). Application code identical.

### Success Criteria

- **IMP-001**: All infrastructure services (RabbitMQ, Redis, SQL Server) start successfully in Docker Compose locally
- **IMP-002**: Dapr pub/sub flows work locally (OrderService → RabbitMQ → MakeLineService/LoyaltyService/ReceiptGenerationService/AccountingService)
- **IMP-003**: Dapr state operations work locally (MakeLineService ETag concurrency, LoyaltyService loyalty points)
- **IMP-004**: All services deploy to AKS using StatefulSets without application code changes
- **IMP-005**: Same Kubernetes manifests deploy to EKS and GKE without modification (prove portability)
- **IMP-006**: Performance tests show < 10% latency difference between containerized infrastructure and PaaS equivalents
- **IMP-007**: Prometheus metrics available from RabbitMQ (port 15692) and Redis in all environments

### Upgrade Path (Current Red Dog Infrastructure)

**Current State** (from git history):
- RabbitMQ: Bitnami Helm chart 8.20.2 from 2021 (likely RabbitMQ 3.8 or 3.9, EOL)
- Redis: Bitnami Helm chart (version TBD)

**Recommended Upgrades**:
1. **Dapr 1.3.0 → 1.16.0** (per modernization plan) - test AMQP compatibility first
2. **RabbitMQ 3.x → 4.1**: Upgrade Bitnami chart 8.20.2 → 16.0.14, test Dapr pubsub component
3. **Redis**: Upgrade to chart 20.5.0 with `redis:7-bookworm` image

**Migration Testing**:
- Test Dapr `pubsub.rabbitmq` component with RabbitMQ 4.1 (frame_max >= 8192 compatibility)
- Test Dapr `state.redis` component with Redis 7 (backward compatible)
- Verify ETag optimistic concurrency patterns still work (MakeLineService, LoyaltyService)

## References

- **REF-001**: Related ADR: `docs/adr/adr-0002-cloud-agnostic-configuration-via-dapr.md` (Dapr abstraction for multi-cloud portability)
- **REF-002**: Related ADR: `docs/adr/adr-0003-ubuntu-2404-base-image-standardization.md` (application container base images - does NOT apply to infrastructure)
- **REF-003**: Research Document: `docs/research/local-development-gap.md` (identified need for local dev environment)
- **REF-004**: Research Document: `docs/research/alpine-vs-ubuntu-redis-containers.md` (Redis image selection, Alpine 2x slower than Debian)
- **REF-005**: Research Document: `docs/research/docker-compose-vs-aspire-comparison.md` (local development tooling comparison)
- **REF-006**: Git Commit: `3d91853` (May 18, 2021) - Migration from Azure Service Bus to RabbitMQ for cloud-agnostic architecture
- **REF-007**: RabbitMQ Official Image: https://hub.docker.com/_/rabbitmq (management variant includes Prometheus metrics)
- **REF-008**: Redis Official Image: https://hub.docker.com/_/redis (7-bookworm recommended for glibc performance)
- **REF-009**: SQL Server Container: https://hub.docker.com/_/microsoft-mssql-server (Developer Edition free for dev/test)
- **REF-010**: Bitnami Helm Charts: https://github.com/bitnami/charts (production-ready RabbitMQ and Redis configurations)
