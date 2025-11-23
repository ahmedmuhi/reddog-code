---
id: KI-DAPR_OBJECT_STORAGE_BINDINGS-001
title: Dapr Object Storage Bindings
tags:
  - red-dog
  - architecture
  - dapr
  - bindings
  - object-storage
last_updated: 2025-11-22
source_sessions: []
source_plans: []
confidence: high
status: Active
owner: Red Dog Modernization Team
notes: >
  Captures the canonical contract and environment mappings for Dapr-based
  object storage across Red Dog services (receipts and similar artifacts).
---

# Summary

This Knowledge Item defines how Red Dog services use Dapr output bindings for object storage, including binding names, contracts, and environment-specific backends. It applies to any workload that needs to write or read binary artifacts such as receipts and documents. The goal is to keep application code cloud-agnostic while allowing production deployments to use native blob storage services on each cloud provider.

## Key Facts

- **FACT-001**: All application-level object storage in Red Dog (e.g. receipt PDFs) is accessed via Dapr **output bindings**, not via cloud-specific SDKs or raw storage APIs.
- **FACT-002**: The canonical binding name for receipt-related storage is **`receipt-storage`**; this name is consistent across all environments and clouds.
- **FACT-003**: Local development uses an ephemeral filesystem-backed binding (Dapr `localstorage` with an `emptyDir` or equivalent), while cloud environments use managed blob storage (Azure Blob Storage, AWS S3, or GCP Cloud Storage).
- **FACT-004**: The binding contract is based on Dapr binding operations (primarily `create`; optional `get`/`delete` if needed) plus metadata that includes at least an object key and content type.
- **FACT-005**: Object storage configuration (accounts, buckets, containers, credentials) is isolated in Dapr component YAML and secrets, not in application code.

## Constraints

- **CON-001**: Application services MUST NOT call provider-specific object storage SDKs directly (Azure Blob, S3, GCS) for workloads covered by this KI; they MUST go through the Dapr binding.
- **CON-002**: The Dapr binding name used by application code (e.g. `receipt-storage`) MUST be identical across environments (local, staging, production, all clouds).
- **CON-003**: Local development storage is considered **ephemeral** and MUST NOT be relied upon for durability, audit, or long-term retention.
- **CON-004**: Provider-specific configuration (e.g. bucket names, regions, credentials) MUST be expressed in component metadata and secret references, not hard-coded in services.
- **CON-005**: Object storage used under this pattern MUST follow the platform’s security baseline (encryption-at-rest enabled, private containers/buckets by default, and access via managed identities or secret stores).

## Patterns & Recommendations

- **PAT-001**: For each logical object storage use case (e.g. receipt PDFs), define a single Dapr binding name (e.g. `receipt-storage`) and use it consistently across all services that write/read that content.
- **PAT-002**: Use Dapr’s `localstorage` binding and an ephemeral volume (e.g. Kubernetes `emptyDir`, or a local directory mount in Docker) for local development to avoid cloud dependencies.
- **PAT-003**: In cloud environments, configure the same binding name to target the cloud’s native blob storage service:
  - Azure: Azure Blob Storage container.
  - AWS: S3 bucket.
  - GCP: Cloud Storage bucket.
- **PAT-004**: Define a simple, stable object key scheme in metadata (for example `storeId/orderId/receiptId.pdf`) so that objects are easy to locate and migrate if necessary.
- **PAT-005**: At minimum, include the following fields when invoking the binding for a `create` operation:
  - `data`: the binary content (e.g. PDF bytes).
  - `metadata.filename`: logical filename (e.g. `receipt-<orderId>.pdf`).
  - `metadata.contentType`: MIME type (e.g. `application/pdf`).
  - `metadata.key` or equivalent: the storage key/prefix.
- **PAT-006**: Treat cloud-specific lifecycle, retention, and replication settings as infrastructure concerns: configure them at the storage account/bucket level, not in application logic.
- **PAT-007**: For non-local tests that depend on persistence semantics (e.g. verifying objects can be retrieved days later), use an environment wired to real blob storage rather than local ephemeral volumes.

## Risks & Open Questions

### Risks

- **RISK-001**: Behaviour differences between ephemeral local storage and durable cloud blob storage can hide bugs until later environments (e.g. assumptions about persistence, latency, or error codes).
- **RISK-002**: Migrating between cloud providers requires bulk data migration between blob stores in addition to updating Dapr components; this can be operationally complex for large datasets.
- **RISK-003**: Misconfigured storage permissions (e.g. public containers, overly broad IAM roles) can expose sensitive artifacts such as receipts.

### Open Questions

- **OPEN-001**: Do we need standardised conventions for multi-tenant separation in keys (e.g. per-store vs per-environment segmentation), or can this remain per-service/domain?
- **OPEN-002**: For future workloads (e.g. large media files), do we need additional bindings (e.g. `media-storage`) or can they share `receipt-storage` with stricter key conventions?

## Source & Provenance

- Derived from ADR:
  - `docs/adr/adr-0012-dapr-bindings-for-object-storage.md`
- Related ADRs:
  - `docs/adr/adr-0002-cloud-agnostic-configuration-via-dapr.md`
  - `docs/adr/adr-0007-cloud-agnostic-deployment-strategy.md`
- Related research/implementation notes:
  - `docs/research/dapr-volume-mounts-configuration.md`
