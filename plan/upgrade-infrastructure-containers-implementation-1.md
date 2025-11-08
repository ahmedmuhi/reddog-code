---
goal: "Upgrade Infrastructure Containers (SQL Server, Redis, RabbitMQ, Nginx) to Latest Stable Versions"
version: 1.0
date_created: 2025-11-09
last_updated: 2025-11-09
owner: "Red Dog Modernization Team"
status: 'Planned'
tags: [infrastructure, upgrade, phase-0, sql, redis, rabbitmq, nginx, containers]
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

This plan upgrades supporting infrastructure containers to latest stable versions: SQL Server 2019→2022, Redis (Helm 15.0.0 → Docker 6.2.14), RabbitMQ (Helm 8.20.2 → Docker 4.2.0), and Nginx (Helm 3.31.0 → Docker 1.28.0).

**Duration**: 1 week (within Phase 0)

## 1. Requirements & Constraints

- **REQ-001**: SQL Server 2022 (Ubuntu 22.04) or PostgreSQL 17 alternative
- **REQ-002**: Redis 6.2.14-bookworm (local dev only, cloud uses Cosmos DB/DynamoDB/Firestore)
- **REQ-003**: RabbitMQ 4.2.0-management (AMQP 0.9.1, Prometheus metrics)
- **REQ-004**: Nginx 1.28.0-bookworm (ingress + UI static hosting)
- **CON-001**: SQL Server 2022 requires persistent volume (8GB PVC)
- **CON-002**: Redis 6.2.14 is EOL (July 2024) - local dev only, NOT production
- **CON-003**: RabbitMQ 4.2 is major version jump from 3.x (test compatibility)

## 2. Implementation Steps

### Implementation Phase 1: SQL Server Upgrade

- **GOAL-001**: Upgrade SQL Server 2019 → 2022

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-101 | Backup AccountingService database | | |
| TASK-102 | Update `manifests/branch/dependencies/sql/sql-server.yaml` to use `mcr.microsoft.com/mssql/server:2022-latest` | | |
| TASK-103 | Apply changes: `kubectl apply -f manifests/branch/dependencies/sql/` | | |
| TASK-104 | Verify SQL Server pod is running | | |
| TASK-105 | Test AccountingService database connectivity | | |

### Implementation Phase 2: Redis Replacement (Local Dev Only)

- **GOAL-002**: Replace Redis Helm chart with Docker image 6.2.14

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-201 | Remove Redis Helm release: `helm uninstall redis -n redis` | | |
| TASK-202 | Create Redis deployment using `redis:6.2.14-bookworm` Docker image | | |
| TASK-203 | Update `docker-compose.yml` for local dev with Redis 6.2.14 | | |
| TASK-204 | Test Dapr state component connectivity to Redis | | |

### Implementation Phase 3: RabbitMQ Upgrade

- **GOAL-003**: Upgrade RabbitMQ Helm 8.20.2 → Docker 4.2.0-management

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-301 | Backup RabbitMQ `orders` queue configuration | | |
| TASK-302 | Update `manifests/branch/dependencies/rabbitmq/rabbitmq.yaml` to use `rabbitmq:4.2.0-management` | | |
| TASK-303 | Apply changes and verify RabbitMQ pod is running | | |
| TASK-304 | Test Dapr pub/sub component connectivity | | |
| TASK-305 | Verify Prometheus metrics endpoint (port 15692) | | |

### Implementation Phase 4: Nginx Upgrade

- **GOAL-004**: Upgrade Nginx for ingress and UI hosting

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-401 | Update `manifests/branch/dependencies/nginx/nginx.yaml` to use `nginx:1.28.0-bookworm` | | |
| TASK-402 | Apply changes and verify Nginx pod is running | | |
| TASK-403 | Test UI static site hosting (access UI via HTTPS) | | |
| TASK-404 | Test ingress routing to backend services | | |

## 3. Alternatives

- **ALT-001**: **Use PostgreSQL 17 Instead of SQL Server 2022** - Open source alternative, documented in ADR-0007
- **ALT-002**: **Keep Redis Helm Chart** - Rejected: Moving to Docker image for consistency, cloud uses managed stores anyway

## 4. Dependencies

- **DEP-001**: Dapr 1.16.2 (compatible with RabbitMQ 4.2, Redis 6.2.14)
- **DEP-002**: Kubernetes 1.30+ for container runtime

## 5. Files

- **FILE-001**: `manifests/branch/dependencies/sql/sql-server.yaml`
- **FILE-002**: `manifests/branch/dependencies/redis/redis.yaml` (local dev)
- **FILE-003**: `manifests/branch/dependencies/rabbitmq/rabbitmq.yaml`
- **FILE-004**: `manifests/branch/dependencies/nginx/nginx.yaml`
- **FILE-005**: `docker-compose.yml` (local dev infrastructure)

## 6. Testing

- **TEST-001**: SQL Server connectivity test
- **TEST-002**: Redis state read/write test (local dev)
- **TEST-003**: RabbitMQ pub/sub message flow test
- **TEST-004**: Nginx ingress routing test
- **TEST-005**: UI static site HTTPS access test

## 7. Risks & Assumptions

- **RISK-001**: RabbitMQ 4.2 AMQP compatibility - **Mitigation**: Research confirms Dapr supports RabbitMQ 4.2
- **RISK-002**: SQL Server data loss during upgrade - **Mitigation**: Backup database before upgrade
- **ASSUMPTION-001**: Redis 6.2.14 EOL acceptable for local dev (not production)

## 8. Related Specifications / Further Reading

- [Infrastructure Versions Verification](../docs/research/infrastructure-versions-verification.md)
- [Phase 0: Platform Foundation](./upgrade-phase0-platform-foundation-implementation-1.md)
