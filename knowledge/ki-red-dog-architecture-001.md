---
id: KI_REDDOG_ARCHITECTURE_001
title: Red Dog Target Architecture and Service Boundaries
tags:
  - architecture
  - microservices
  - dapper
  - database-per-service
  - polyglot
last_updated: 2025-11-09
source_sessions:
  - .claude/sessions/2025-11-01-0838.md
source_plans:
  - plan/modernization-strategy.md
confidence: high
status: active
owner: "Ahmed Muhi"
notes: >
  Captures the long-lived target architecture for Red Dog Coffee and how
  services communicate and own data. Applies across language migrations.
---

# Summary

This Knowledge Item describes the target architecture for Red Dog Coffee once
modernisation phases (e.g. .NET 10 and language migrations) are complete. It
defines service boundaries, data ownership, and cross-service communication
patterns that should remain stable even as implementation details change.

It applies to all Red Dog services (OrderService, MakeLineService,
AccountingService, LoyaltyService, ReceiptGenerationService, UI) regardless of language.

## Key Facts

- **FACT-001**: Red Dog is a polyglot microservices system, with services
  implemented in .NET, Go, Python, and Node.js at the target state.
- **FACT-002**: The system uses a **Database-per-Service** pattern:
  each service owns its own data store and is the only service allowed to
  access that store directly.
- **FACT-003**: Dapr is the standard mechanism for cross-service communication:
  - Pub/Sub for event-driven flows (e.g. `orders` topic),
  - Service Invocation for synchronous HTTP/gRPC calls,
  - State API for key–value storage where applicable.
- **FACT-004**: The UI (Vue) calls backend services via HTTP APIs; it does not
  access any database directly.
- **FACT-005**: AccountingService owns the SQL database and exposes data to
  other services and the UI only via its API surface and/or Dapr messages.
- **FACT-006**: MakeLineService and LoyaltyService use Dapr state stores
  (e.g. Redis) rather than owning SQL databases.
- **FACT-007**: Red Dog is designed so that swapping runtime environments
  (AKS/EKS/GKE) does not change application-level architecture; only platform
  manifests change.

## Constraints

- **CON-001**: No service may query or modify another service’s database.
  Cross-service access to SQL/NoSQL stores is forbidden; only the owning
  service may talk to its database.
- **CON-002**: Cross-service communication must use Dapr primitives
  (pub/sub, service invocation, state, bindings). Direct HTTP calls around
  Dapr to pod IPs or load balancers are not permitted for internal traffic.
- **CON-003**: New services must declare a clear ownership boundary for
  their data; shared mutable data stores are not allowed.
- **CON-004**: The UI must communicate only with well-defined HTTP APIs
  (e.g. AccountingService) and never directly with Dapr or backing stores.
- **CON-005**: Language choice for a service must not violate the above
  boundaries; polyglot is allowed only within the Dapr/DB-per-service model.

## Patterns & Recommendations

- **PAT-001**: When introducing a new service, choose language based on
  workload:
  - .NET: complex business logic, SQL-heavy services (e.g. AccountingService).
  - Go: high-throughput, concurrent workloads (e.g. MakeLineService).
  - Python: data processing, document generation, scripting (e.g. ReceiptGenerationService).
  - Node.js: event-driven and I/O-heavy operations (e.g. LoyaltyService).
- **PAT-002**: Use Dapr pub/sub for events representing business facts
  (e.g. OrderCreated, OrderCompleted). Do not model these as direct HTTP calls.
- **PAT-003**: When another service needs data owned by AccountingService,
  expose it through AccountingService’s HTTP API or Dapr service invocation,
  never by granting DB access.
- **PAT-004**: Use Dapr bindings for integration with external systems
  (queues, storage, etc.) to keep the core architecture portable across clouds.
- **PAT-005**: For teaching and demos, keep the architectural diagrams and
  narrative aligned with this KI; code examples may vary by language, but
  boundaries must remain consistent.

## Risks & Open Questions

- **RISK-001**: Ad-hoc “quick fixes” that bypass Dapr or query another
  service’s database will erode the architecture and make the system harder
  to reason about and migrate.
- **RISK-002**: During incremental migrations (e.g. while some services are
  still .NET 6.0), the code may accidentally drift away from the target
  architecture if changes are not checked against this KI.
- **OPEN-001**: Long-term choice of state store technology (Redis vs other
  Dapr-supported stores) may evolve, but the database-per-service principle
  should remain intact.
- **OPEN-002**: Whether some read-only reporting views might be shared later
  (e.g. through materialised views or a data warehouse) is undecided; any
  such solution must not break `CON-001`.

## Sources & Provenance

- **SRC-001**: `docs/standards/web-api-standards.md` – “Red Dog Architecture Overview”
- **SRC-002**: `plan/modernization-strategy.md` – Modernization phases and target state
- **SRC-003**: ADRs related to platform & deployment:
  - `adr/adr-0001-dotnet10-lts-adoption.md`
  - `adr/adr-0007-cloud-agnostic-deployment-strategy.md`
