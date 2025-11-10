---
title: "ADR-0012: Dapr Bindings for Object Storage"
status: "Implemented"
date: "2025-11-11"
authors: "Red Dog Modernization Team"
tags: ["architecture", "decision", "dapr", "bindings", "storage", "cloud-native"]
supersedes: ""
superseded_by: ""
---

# ADR-0012: Dapr Bindings for Object Storage

## Status

**Implemented** (Phase 0.5 - Local Development)

## Implementation Status

**Current State:** 🟢 Implemented (Local), 🔵 Accepted (Production)

**What's Working:**
- Local development: emptyDir volume + `dapr.io/volume-mounts-rw` + `fsGroup: 65532`
- Receipt generation service writes successfully to `/tmp/receipts/`
- Dapr localstorage binding operational (validated in Phase 0.5)

**What's Planned:**
- Production: Azure Blob Storage / AWS S3 / GCP Cloud Storage bindings
- Component swap via Helm values (local vs cloud configuration)

**Evidence:**
- charts/reddog/templates/receipt-generation-service.yaml:41 - volume-mounts-rw annotation
- charts/reddog/templates/dapr-components/binding-receipt.yaml:1 - localstorage binding
- docs/research/dapr-volume-mounts-configuration.md - Implementation research

## Context

Red Dog services require object storage for receipts and documents. Unlike message brokers or caches (covered by ADR-0007), object storage has different characteristics:
- Large blob storage (potentially GBs/TBs)
- Less latency-sensitive (async write, infrequent read)
- Cloud providers offer unique features: lifecycle policies, CDN integration, geo-replication

**Key Question:** Should we use cloud-native blob storage (Azure Blob, S3, GCS) or self-hosted MinIO for cloud-agnostic portability?

## Decision

**Use cloud-native blob storage for production, ephemeral local storage for development.**

**Local Development:**
- Dapr `bindings.localstorage` component
- Kubernetes emptyDir volume (ephemeral, pod-scoped)
- `dapr.io/volume-mounts-rw` annotation + `securityContext.fsGroup: 65532`

**Production (Cloud-Specific):**
- Azure deployments: `bindings.azure.blobstorage`
- AWS deployments: `bindings.aws.s3`
- GCP deployments: `bindings.gcp.bucket`

**Rationale:**
- Cloud blob storage offers better features (lifecycle management, CDN, geo-replication) than self-hosted alternatives
- Object storage less critical than messaging (ADR-0007's RabbitMQ decision) - acceptable to use cloud-native
- Dapr binding abstraction means application code identical across clouds
- Operational simplicity: No need to manage MinIO StatefulSets, backups, scaling

## Consequences

### Positive
- **Simpler Operations:** No self-hosted object storage to manage (MinIO StatefulSets, backups)
- **Better Features:** Cloud blob storage lifecycle policies, CDN integration, geo-replication
- **Cost Efficiency:** Pay-per-use pricing, no compute overhead for storage clusters
- **High Availability:** Cloud storage SLAs (99.9%+) without manual HA configuration

### Negative
- **Platform-Specific Components:** Different Dapr binding components per cloud (not fully cloud-agnostic)
- **Testing Gap:** Local emptyDir ephemeral storage differs from cloud blob persistence
- **Migration Complexity:** Moving between clouds requires Dapr component updates + data migration

### Mitigations
- Dapr binding API abstraction keeps application code portable
- For multi-cloud validation, consider MinIO as staging environment (not production)
- Document component configuration differences in Helm values

## Alternatives Considered

**MinIO (Self-Hosted S3-Compatible Storage):**
- Rejected: Adds operational complexity (StatefulSets, persistence, backups) for minimal portability gain
- Alternative use case: Staging/testing environment for S3 API validation (not production)

## Implementation

**Local Development (emptyDir + localstorage):**
```yaml
# Pod annotation
dapr.io/volume-mounts-rw: "receipts:/tmp/receipts"

# Pod securityContext
securityContext:
  fsGroup: 65532  # Match Dapr UID for write permissions

# Dapr component
type: bindings.localstorage
metadata:
  - name: rootPath
    value: /tmp/receipts
```

**Production (Cloud Blob Storage):**
```yaml
# Azure
type: bindings.azure.blobstorage
metadata:
  - name: storageAccount
    value: reddog-storage
  - name: container
    value: receipts

# AWS
type: bindings.aws.s3
metadata:
  - name: bucket
    value: reddog-receipts
  - name: region
    value: us-west-2

# GCP
type: bindings.gcp.bucket
metadata:
  - name: bucket
    value: reddog-receipts
```

## References

- ADR-0002: Cloud-Agnostic Configuration via Dapr (Dapr abstraction principle)
- ADR-0007: Cloud-Agnostic Deployment Strategy (infrastructure containers - does NOT cover object storage)
- docs/research/dapr-volume-mounts-configuration.md: Volume mount implementation research
