---
title: "ADR-0008: kind (Kubernetes-in-Docker) for Local Development Environment"
status: "Accepted"
date: "2025-11-09"
authors: "Red Dog Modernization Team"
tags: ["architecture", "decision", "local-development", "kubernetes", "kind", "docker"]
supersedes: ""
superseded_by: ""
---

# ADR-0008: kind (Kubernetes-in-Docker) for Local Development Environment

## Status

**Accepted**

This ADR records a **stable architectural decision**. Concrete scripts, cluster configs, and tool versions are maintained in plans and docs, not in this file.

---

## Context

Red Dog’s modernization strategy targets Kubernetes-based deployments on:

- Azure Kubernetes Service (AKS),
- AWS Elastic Kubernetes Service (EKS),
- Google Kubernetes Engine (GKE),
- and other CNCF-compatible clusters.

Related ADRs establish key foundations:

- **ADR-0001** – .NET 10 LTS runtime baseline for services.  
- **ADR-0002** – Cloud-agnostic integration via Dapr components.  
- **ADR-0007** – Preference for containerized infrastructure to support multi-cloud portability.

Previously, local development relied on Docker Compose and VS Code dev containers. That setup diverged from production in several important ways:

- No Kubernetes concepts (Deployments, Services, Ingress, sidecars, StatefulSets).
- Dapr sidecars and annotations behaved differently from cluster deployments.
- Helm charts, Dapr components, and Kubernetes manifests could not be validated locally.
- The teaching narrative had to explain “local (Compose) vs production (Kubernetes)” as two distinct models.

As part of removing Docker Compose and devcontainers, there is now a **local development gap**: we need a way to develop and test locally that matches the Kubernetes- and Dapr-centric architecture used in all environments.

---

## Decision

**Adopt _kind_ (Kubernetes-in-Docker) as the standard local development environment for Red Dog.**

More precisely:

1. Local development for Red Dog must run on a **real Kubernetes cluster** created with kind, not Docker Compose.
2. The **same Kubernetes manifests and Helm charts** used for AKS/EKS/GKE deployments must be deployable to the local kind cluster with only environment-specific values (for example `values-local.yaml`).
3. Local workloads (Red Dog services, Dapr sidecars, and containerized infrastructure per ADR-0007) should run inside the kind cluster, behind the same Ingress and Dapr patterns as in cloud environments.
4. Local development should not depend on any cloud account; the entire stack must run offline on a developer machine.

This ADR does **not** prescribe:

- exact kind / Kubernetes / Dapr / Helm versions,
- specific cluster topology (number of nodes, resource limits),
- concrete script names or command sequences.

Those details are expected to evolve and are managed in implementation plans and documentation.

---

## Rationale

### Rationale-001 – Production parity

Red Dog’s production architecture is Kubernetes-first:

- Services are deployed as Pods and Deployments.
- Dapr sidecars are injected and configured via annotations.
- Infrastructure (RabbitMQ, Redis, database) is deployed as StatefulSets (ADR-0007).
- Ingress routing is standardized (ADR-0010).

Using kind locally allows developers and students to work with **the same primitives**:

- A Pod with a service container and a Dapr sidecar behaves the same locally and in AKS/EKS/GKE.
- The same Ingress resources and DNS names can be used everywhere, subject to environment values.
- StatefulSets, Services, ConfigMaps, and Secrets behave the same across environments.

This significantly reduces “works on my machine, fails in AKS/EKS/GKE” scenarios.

### Rationale-002 – Single mental model for teaching

Red Dog is a teaching and demo platform. A core goal is to show:

> “This microservices system can be deployed to any Kubernetes-compatible cloud without changing the application code.”

If local development uses a different orchestration model (Docker Compose), the learning story becomes:

- “Locally we use Compose, but in production we use Kubernetes and Dapr.”

Using kind means:

- learners interact with **one** deployment model (Kubernetes + Dapr),
- examples and exercises translate directly from local to cloud clusters,
- workshop material can say “this is exactly the same as AKS/EKS/GKE, just on your laptop.”

### Rationale-003 – Dapr and component validation

ADR-0002 and ADR-0007 rely heavily on Dapr:

- Sidecar injection,
- Component configuration for state, pub/sub, bindings, secret stores,
- Service invocation via `DaprClient`.

A kind cluster lets us:

- test Dapr sidecars, components, and annotations exactly as in cloud environments,
- validate component wiring for Redis/RabbitMQ/secret stores locally,
- debug apps and sidecars together using the same tools and manifests.

This is not possible in a pure Compose setup without re-creating large parts of the K8s+Dapr control plane manually.

### Rationale-004 – Multi-cloud validation and portability

Because the same charts and manifests are expected to deploy to AKS, EKS, GKE, and the local kind cluster:

- local testing becomes an early **multi-cloud validation step**,
- any Kubernetes portability issues (storage classes, Ingress differences, etc.) surface earlier,
- the cloud-agnostic story from ADR-0002 and ADR-0007 is reinforced by practice.

### Rationale-005 – Offline development and cost control

A local kind cluster:

- does not require any cloud resources or accounts,
- allows fully offline development (e.g., on student laptops without internet),
- keeps dev/test cloud costs predictable and focused on integration testing, not inner-loop development.

---

## Consequences

### Positive Consequences

- **POS-001 – Realistic local environment**  
  Developers work against a real Kubernetes API server and node, not an approximation. Issues related to pods, services, Ingress, sidecars, and StatefulSets are caught locally.

- **POS-002 – Unified toolchain**  
  The same tools (`kubectl`, Helm, Dapr CLI) are used locally and in cloud environments, simplifying onboarding and documentation.

- **POS-003 – Stronger alignment with ADR-0001 / ADR-0002 / ADR-0007**  
  - ADR-0001: same container images and runtime baseline run locally and in cloud.  
  - ADR-0002: Dapr patterns are exercised in every environment.  
  - ADR-0007: containerized infra (RabbitMQ, Redis, database) is deployed the same way locally and in Kubernetes clusters.

- **POS-004 – Better testing for Helm charts and manifests**  
  Developers can iterate on Helm values, component manifests, and Kubernetes resources locally before pushing changes to shared clusters.

- **POS-005 – Teaching value**  
  Workshops and examples can honestly claim: “you are running the same architecture on your laptop that you will deploy to AKS/EKS/GKE.”

### Negative Consequences

- **NEG-001 – Higher local resource usage**  
  Running a Kubernetes control plane and multiple pods is heavier than running individual containers under Docker Compose. This may limit the experience on very low-spec machines.

- **NEG-002 – Increased conceptual complexity**  
  New contributors must understand basic Kubernetes concepts (Pods, Deployments, Services, Ingress, Namespaces) to use the local environment effectively.

- **NEG-003 – Dependency on container runtime**  
  Developers must install and maintain Docker Desktop or an equivalent container runtime (e.g., Podman). Licensing or platform constraints may apply in some organizations.

- **NEG-004 – Port conflicts and local networking**  
  Exposing Ingress via common ports (such as 80/443) can conflict with other software on developer machines and may require alternative port mappings.

These negatives are accepted because they align local development with the long-term Kubernetes-first architecture and teaching goals of Red Dog.

---

## Alternatives Considered

### Alternative 1 – Docker Compose for Local, Kubernetes for Cloud

**Description:**  
Use Docker Compose for local development (as previously done), while keeping Kubernetes for test and production clusters.

**Reasons Rejected:**

- Significant architectural drift between local and cloud environments (no Ingress, no sidecars, different networking).
- Dapr sidecar patterns, annotations, and component configuration cannot be validated locally in the same way.
- Teaching story becomes more complex: two deployment models to explain.
- Past experience in this repo showed the divergence created friction and confusion, which led to the removal of the Compose-based dev setup.

---

### Alternative 2 – minikube

**Description:**  
Use minikube as the local Kubernetes environment.

**Reasons Rejected:**

- Heavier setup and, in many cases, slower startup than kind.
- Additional VM/driver configuration on some platforms.
- The project’s needs are closer to “Kubernetes in Docker for testing the same manifests”, which aligns more naturally with kind’s design and usage in upstream Kubernetes CI.

---

### Alternative 3 – k3s / k3d

**Description:**  
Use k3s (and/or k3d) as a lightweight Kubernetes distribution.

**Reasons Rejected:**

- Different defaults (for example Ingress controller choice) can diverge from the Nginx-based approach standardized elsewhere.
- Slightly different cluster composition from managed K8s (AKS/EKS/GKE) may hide portability issues.
- Smaller ecosystem and less explicit alignment with the upstream Kubernetes CI story compared to kind.

---

### Alternative 4 – Remote Cloud Development Clusters

**Description:**  
Developers use shared or per-developer AKS/EKS/GKE clusters for daily development.

**Reasons Rejected:**

- Requires cloud accounts and credentials for all contributors, including students.
- Introduces cloud costs for each developer and for every test cluster.
- Slower inner loop (deployment and feedback depend on network latency and cloud provisioning).
- No offline development path.

---

## Implementation Notes (Non-Normative)

- This ADR deliberately does **not** fix:
  - the exact kind / Kubernetes / Dapr / Helm versions,
  - the shape of `kind` configuration files,
  - the names of setup scripts or Helm releases.
- Those details are expected to evolve and are captured in:
  - local development setup documentation, and
  - implementation plans that introduce or modify the local kind environment.

The architectural invariant is:

> Local Red Dog development uses a kind-based Kubernetes cluster that runs the same services, Dapr sidecars, and containerized infrastructure patterns as cloud environments, using the same manifests and Helm charts with environment-specific values only.

---

## References

- ADR-0001 – .NET 10 LTS Adoption  
- ADR-0002 – Cloud-Agnostic Configuration via Dapr  
- ADR-0007 – Cloud-Agnostic Deployment via Containerized Infrastructure  
- ADR-0010 – Nginx Ingress Controller for Cloud-Agnostic Traffic Routing  
- Repository history removing Docker Compose/devcontainers for local development
