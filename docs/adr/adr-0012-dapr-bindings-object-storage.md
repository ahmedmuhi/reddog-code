---
title: "ADR-0012: Dapr Bindings for Object Storage"
status: "Accepted"
date: "2025-11-11"
authors: "Red Dog Modernization Team"
tags: ["architecture", "decision", "dapr", "bindings", "storage", "cloud-native"]
supersedes: ""
superseded_by: ""
---

# ADR-0012: Dapr Bindings for Object Storage

## Status

**Decision:** Accepted  
**Implementation:** 🟢 Local development implemented, 🔵 Production rollout planned

## Context

Several Red Dog services need to write and later read binary artifacts such as receipt PDFs and other documents. These have different characteristics from transactional data and messaging:

- Potentially large objects (MB–GB).
- Write-heavy, latency-tolerant workloads.
- Strong durability requirements in production.
- Native cloud services (Azure Blob Storage, S3, GCS) provide lifecycle management, encryption, and CDN integration.

At the same time, we want:

- Application code to remain cloud-agnostic and polyglot.
- Local development to work without cloud dependencies.
- Operations to use native cloud storage in production rather than self-hosted object stores.

The architectural choice is how Red Dog services should access object storage, and whether we standardise on cloud-native storage, self-hosted (e.g. MinIO), or a mixture.

## Decision

1. **Use Dapr output bindings as the abstraction for all application-level object storage.**

   - Application code must interact with object storage **only** via Dapr bindings, not via cloud SDKs or raw HTTP APIs.
   - Each logical use case gets a stable binding name (e.g. `receipt-storage` for receipt PDFs).

2. **Use ephemeral local storage for development and cloud-native blob storage in higher environments.**

   - **Local development:**
     - Dapr `localstorage` binding backed by an ephemeral filesystem path (e.g. Kubernetes `emptyDir` or local directory).
     - Suitable for demos and functional testing; no durability guarantees.
   - **Production / non-prod cloud environments:**
     - Azure: binding configured against Azure Blob Storage.
     - AWS: binding configured against S3.
     - GCP: binding configured against Cloud Storage.
     - Component types and metadata follow Dapr’s official bindings for each provider.

3. **Keep the binding contract stable across environments.**

   - The binding **name**, supported operations (e.g. `create`, optional `get`/`delete`), and basic metadata contract (object key, content type, optional path/prefix) are consistent.
   - Environment-specific details (connection strings, buckets, containers, regions) are handled in Dapr component configuration and secrets, not in application code.

## Consequences

### Positive

- **Cloud-agnostic application code:** All services call the same Dapr binding name and operations regardless of cloud provider.
- **Operationally simple in production:** Use fully managed blob storage services; no MinIO or other self-hosted object storage to deploy, scale, or back up.
- **Feature-rich storage:** Can use provider features such as lifecycle policies, encryption-at-rest, versioning, and CDN integration where needed.
- **Local developer experience:** Local storage works with Docker / Kubernetes using simple ephemeral volumes, no cloud accounts required.

### Negative

- **Data-layer portability:** Moving between cloud providers still requires data migration between blob stores; changing only Dapr components is not enough.
- **Behaviour differences:** Local ephemeral storage has weaker durability and consistency characteristics than production blob storage; some bugs may only surface in non-local environments.
- **Provider-specific configuration:** Dapr component YAML must be maintained per cloud (different metadata fields, auth mechanisms).

### Mitigations

- Treat application code as portable, and accept that **data** migration between providers is a deliberate, manual operation.
- Use at least one non-local environment wired to real blob storage for integration and performance testing.
- Document component configuration patterns and binding contracts centrally (see associated Knowledge Item).

## Implementation Notes (High-Level)

- **Binding naming:**
  - `receipt-storage` for receipt PDFs and related artifacts.
  - Additional bindings for other document domains must follow the same pattern and be documented before use.

- **Local development pattern (illustrative only):**
  - Dapr `localstorage` binding with `rootPath` mapped to a writable path inside the app container.
  - In Kubernetes, use an `emptyDir` (or equivalent) and Dapr’s volume-mount annotation / container volume mounts to provide that path.
  - Security context (e.g. `fsGroup`) must ensure the Dapr sidecar and app container can both write to the path.

- **Cloud environments pattern (illustrative only):**
  - Each environment defines a Dapr binding component named `receipt-storage` pointing at the appropriate blob storage backend.
  - Provider-specific details (account, bucket/container name, region, credentials) are configured via metadata and secret references in the component.
  - CI/CD or Helm values control which component variant is deployed in each environment.

Detailed binding contracts, example component YAML, and environment mappings are maintained in the associated Knowledge Item.

## References

- `docs/adr/adr-0002-cloud-agnostic-configuration-via-dapr.md` (Dapr abstraction principle)
- `docs/adr/adr-0007-cloud-agnostic-deployment-strategy.md` (infrastructure container and cloud portability rationale)
- `knowledge/dapr-object-storage-bindings-ki.md` (binding contract and implementation patterns)
- `docs/research/dapr-volume-mounts-configuration.md` (volume-mount implementation details for local storage)
