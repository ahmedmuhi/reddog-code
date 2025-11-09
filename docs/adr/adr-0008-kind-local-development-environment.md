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

## Implementation Status

**Current State:** ⚪ Planned (Not Implemented)

**What's Working:**
- Decision documented with complete kind cluster configuration
- Setup scripts designed (scripts/setup-local-dev.sh, scripts/teardown-local-dev.sh)
- kind-config.yaml specification created in ADR

**What's Not Working:**
- kind-config.yaml file doesn't exist in repository root
- Setup/teardown scripts don't exist in scripts/ directory
- Helm charts don't exist (charts/ directory missing - blocked by ADR-0009)
- No local development workflow documented in CLAUDE.md yet

**Evidence:**
- Repository search for "kind-config.yaml" returns zero results
- scripts/ directory may not exist or lacks setup-local-dev.sh
- charts/ directory doesn't exist (confirmed by ADR-0009 status)

**Dependencies:**
- **Depends On:** ADR-0009 (Helm charts must be created first for local deployment)
- **Depends On:** ADR-0007 (Containerized infrastructure provides services to deploy)
- **Blocks:** Local development workflow for all team members
- **Blocks:** Testing Helm charts before cloud deployment

**Next Steps:**
1. Create charts/ directory with infrastructure and application Helm charts (ADR-0009 first)
2. Create kind-config.yaml in repository root with port mappings (80/443 for Ingress)
3. Create scripts/setup-local-dev.sh with kind cluster creation + Dapr + Nginx + Helm deployment
4. Create scripts/teardown-local-dev.sh for cleanup
5. Update CLAUDE.md with "Local Development" section referencing kind setup
6. Test end-to-end: kind cluster → Helm install → curl http://localhost/api/orders

## Context

Red Dog's modernization strategy requires a local development environment that mirrors production deployments across multiple cloud platforms (AKS, EKS, GKE). On November 2, 2025, the project removed Docker Compose and VS Code dev containers (commit `ecca0f5`), creating a local development gap.

**Key Constraints:**
- **Production Parity Required**: Local environment must match cloud Kubernetes deployments to prevent "works on my machine" issues
- **Teaching/Demo Focus**: Instructors need to demonstrate identical deployments across local, Azure, AWS, and GCP environments
- **Cloud-Agnostic Architecture**: Local development must support the same manifests used in production (ADR-0007: Containerized Infrastructure)
- **Multi-Environment Testing**: Developers must validate Helm charts, Dapr components, and Kubernetes manifests locally before cloud deployment
- **No Cloud Connectivity Required**: Students and developers should run the complete stack offline without Azure/AWS/GCP accounts

**Previous Setup (Deleted November 2, 2025):**
- `.devcontainer/docker-compose.yml` - VS Code dev container + SQL Server 2019
- `.vscode/tasks.json` - Dapr sidecar management for 9 services
- `.vscode/launch.json` - Debug configurations for individual services
- `manifests/local/branch/` - Dapr component configs for localhost

**Local Development Challenges:**
- Docker Compose does not mimic Kubernetes networking (no service discovery, no sidecars, no Ingress)
- VS Code dev containers create IDE lock-in (students must use VS Code + GitHub Codespaces)
- Different architecture between local (Docker Compose) and production (Kubernetes) creates cognitive overhead for learners
- No way to test Helm charts, StatefulSets, or Kubernetes-native features locally

**Available Approaches:**

| Approach | Pros | Cons |
|----------|------|------|
| **Docker Compose** | Lightweight, fast startup, familiar | Different from production, no K8s features |
| **kind (Kubernetes-in-Docker)** | Production parity, runs K8s in Docker | Slightly heavier, requires Docker Desktop/Podman |
| **minikube** | Feature-rich, supports multiple drivers | Slower startup, VM overhead |
| **k3s/k3d** | Lightweight K8s, fast | Less feature parity with AKS/EKS/GKE |
| **Rancher Desktop** | GUI-friendly, bundles K8s | Heavy, opinionated tooling |
| **Remote Cloud Cluster** | Exact production environment | Requires cloud account, costs money, slow iteration |

## Decision

**Adopt kind (Kubernetes-in-Docker) as the standard local development platform for Red Dog Coffee.**

**Implementation:**
- Developers install Docker Desktop (Windows/macOS) or Podman (Linux) as container runtime
- Create kind cluster with port mappings for Nginx Ingress (80/443)
- Deploy same Helm charts used in production with environment-specific values (`values-local.yaml`)
- Use same Dapr components, StatefulSets, and Ingress configurations as cloud deployments
- Students learn real Kubernetes (kubectl, Helm, Dapr) without cloud accounts

**Cluster Configuration:**
```yaml
kind: Cluster
apiVersion: kind.x-k8s.io/v1alpha4
name: reddog-local
nodes:
- role: control-plane
  extraPortMappings:
  - containerPort: 80
    hostPort: 80
    protocol: TCP
  - containerPort: 443
    hostPort: 443
    protocol: TCP
  kubeadmConfigPatches:
  - |
    kind: InitConfiguration
    nodeRegistration:
      kubeletExtraArgs:
        node-labels: "ingress-ready=true"
```

**Local Development Workflow:**
```bash
# 1. Create kind cluster
kind create cluster --config kind-config.yaml

# 2. Install infrastructure (Dapr, Nginx Ingress)
helm install dapr dapr/dapr --namespace dapr-system --create-namespace
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/kind/deploy.yaml

# 3. Deploy Red Dog application
helm install reddog ./charts/reddog -f values/values-local.yaml

# 4. Access services
curl http://localhost/api/orders
open http://localhost  # UI
```

**Rationale:**
- **PROD-001: Identical Architecture**: Same Kubernetes manifests work locally and in AKS/EKS/GKE without modification
- **PROD-002: No Mental Shift**: Students learn Kubernetes once; knowledge transfers to cloud deployments
- **PROD-003: Testing Parity**: Helm charts, Dapr components, Ingress rules, StatefulSets tested locally before cloud deployment
- **PROD-004: Teaching Clarity**: "This is a real Kubernetes cluster on your laptop" - no Docker Compose approximations or simplifications
- **PROD-005: Industry Standard**: kind is the official Kubernetes SIG project for local testing (used by Kubernetes CI/CD)
- **PROD-006: Cloud-Agnostic**: kind runs identically on Windows, macOS, Linux (via Docker Desktop or Podman)
- **PROD-007: Dapr Compatibility**: Dapr sidecars inject into pods exactly as in production (no localhost workarounds)
- **PROD-008: Ingress Support**: Nginx Ingress Controller works via port mappings (same as AKS/EKS/GKE)

## Consequences

### Positive

- **POS-001: Production Parity**: Local environment matches cloud exactly - no "works locally but fails in AKS" surprises
- **POS-002: Kubernetes Learning**: Students gain real Kubernetes experience (kubectl, Helm, manifests) instead of Docker Compose knowledge
- **POS-003: Multi-Cloud Validation**: Developers test same manifests that deploy to Azure, AWS, and GCP
- **POS-004: Helm Chart Development**: Local iteration on Helm charts with immediate feedback (no cloud deployment latency)
- **POS-005: Dapr Development**: Sidecar injection, service invocation, and pub/sub work identically to production
- **POS-006: Offline Development**: Complete stack runs locally without internet or cloud accounts
- **POS-007: CI/CD Testing**: GitHub Actions can use kind for integration tests (same environment as developers)
- **POS-008: Cost Savings**: No cloud resource costs for development/testing (Azure/AWS/GCP only for final validation)
- **POS-009: Ingress Testing**: Path-based routing (`/api/orders`, `/`) works locally via localhost:80
- **POS-010: StatefulSet Testing**: SQL Server, Redis, RabbitMQ deploy as StatefulSets (same as production)

### Negative

- **NEG-001: Docker Desktop Requirement**: Windows/macOS developers need Docker Desktop (free for personal/education, $5-9/month for business)
- **NEG-002: Resource Overhead**: kind cluster uses ~2GB RAM vs ~500MB for Docker Compose (acceptable trade-off for laptops with 8GB+ RAM)
- **NEG-003: Startup Time**: kind cluster creation takes 30-60 seconds vs 5-10 seconds for Docker Compose (one-time cost)
- **NEG-004: Complexity for Beginners**: Kubernetes concepts (pods, deployments, services) steeper learning curve than Docker Compose
- **NEG-005: Windows WSL2 Dependency**: Windows users must enable WSL2 (Windows 10 build 19041+ or Windows 11)
- **NEG-006: Port 80/443 Conflicts**: Nginx Ingress requires ports 80/443 available (conflicts with local web servers like IIS, Apache)

### Mitigations

- **MIT-001: Docker Desktop Alternatives**: Linux users can use Podman (fully open-source, no licensing) via `KIND_EXPERIMENTAL_PROVIDER=podman`
- **MIT-002: Resource Limits**: Configure kind with resource limits (1 CPU, 2GB RAM) for low-spec laptops
- **MIT-003: Pre-Built Clusters**: Provide `kind-cluster.yaml` config and setup scripts for one-command cluster creation
- **MIT-004: Teaching Progression**: Introduce Kubernetes concepts gradually (start with simple deployments, build to StatefulSets and Ingress)
- **MIT-005: Port Alternatives**: Document alternative port mappings (8080/8443) for developers with port conflicts
- **MIT-006: Quick Start Guide**: Create `docs/local-development.md` with step-by-step setup instructions

## Alternatives Considered

### Docker Compose (Rejected)

- **ALT-001: Description**: Use Docker Compose to orchestrate SQL Server, Redis, RabbitMQ, and application containers locally
- **ALT-002: Rejection Reason**:
  - Different architecture from production creates "works locally, fails in cloud" problems
  - No Kubernetes features (Ingress, StatefulSets, sidecars) available for testing
  - Students learn Docker Compose patterns that don't transfer to Kubernetes
  - Teaching overhead: "Locally it works like X, but in production it works like Y"
  - Defeats cloud-agnostic architecture goal (ADR-0007) by introducing local-only setup
  - Research document (`docs/research/docker-compose-infrastructure-history.md`) confirmed this gap led to removal

### minikube (Considered but Rejected)

- **ALT-003: Description**: Use minikube for local Kubernetes cluster
- **ALT-004: Rejection Reason**:
  - Requires VM or containerized driver (more complex setup than kind)
  - Slower startup (90+ seconds vs kind's 30-60 seconds)
  - Less feature parity with AKS/EKS/GKE (custom networking, different CNI)
  - kind is official Kubernetes SIG project (better long-term support)

### k3s/k3d (Considered but Rejected)

- **ALT-005: Description**: Use k3s (lightweight Kubernetes) via k3d (k3s-in-Docker)
- **ALT-006: Rejection Reason**:
  - k3s uses Traefik Ingress by default (Red Dog uses Nginx Ingress)
  - Less feature parity with AKS/EKS/GKE (k3s removes some Kubernetes components)
  - Smaller community than kind for troubleshooting
  - kind better aligns with "real Kubernetes" teaching goal

### Remote Cloud Development Clusters (Rejected)

- **ALT-007: Description**: Developers use ephemeral AKS/EKS clusters for development
- **ALT-008: Rejection Reason**:
  - Requires cloud account (barrier for students)
  - Costs $50-100/month per developer (vs free local development)
  - Slow iteration (kubectl apply latency to cloud)
  - No offline development capability

### VS Code Dev Containers (Already Rejected)

- **ALT-009: Description**: Restore `.devcontainer/` setup with Docker Compose
- **ALT-010: Rejection Reason**:
  - Already evaluated and removed (commit `ecca0f5`, November 2, 2025)
  - IDE lock-in (requires VS Code + Codespaces)
  - Does not solve production parity problem (still uses Docker Compose)

## Implementation Notes

### Prerequisites

**Required Software:**
- Docker Desktop 4.0+ (Windows/macOS) OR Podman 4.0+ (Linux)
- kind 0.20.0+
- kubectl 1.28+
- Helm 3.12+

**Installation (Windows/macOS):**
```bash
# Install Docker Desktop (https://www.docker.com/products/docker-desktop)
# Install kind
brew install kind  # macOS
choco install kind  # Windows

# Install kubectl
brew install kubectl  # macOS
choco install kubernetes-cli  # Windows

# Install Helm
brew install helm  # macOS
choco install kubernetes-helm  # Windows
```

**Installation (Linux):**
```bash
# Install Podman
sudo apt install podman  # Ubuntu/Debian
sudo dnf install podman  # Fedora/RHEL

# Install kind
curl -Lo ./kind https://kind.sigs.k8s.io/dl/v0.20.0/kind-linux-amd64
chmod +x ./kind
sudo mv ./kind /usr/local/bin/kind

# Install kubectl
curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
chmod +x kubectl
sudo mv kubectl /usr/local/bin/

# Install Helm
curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash
```

### kind Cluster Configuration

**File**: `kind-config.yaml` (to be created in repository root)

```yaml
kind: Cluster
apiVersion: kind.x-k8s.io/v1alpha4
name: reddog-local
nodes:
- role: control-plane
  extraPortMappings:
  - containerPort: 80
    hostPort: 80
    protocol: TCP
  - containerPort: 443
    hostPort: 443
    protocol: TCP
  kubeadmConfigPatches:
  - |
    kind: InitConfiguration
    nodeRegistration:
      kubeletExtraArgs:
        node-labels: "ingress-ready=true"
```

### Setup Script

**File**: `scripts/setup-local-dev.sh` (to be created)

```bash
#!/bin/bash
set -e

echo "Creating kind cluster..."
kind create cluster --config kind-config.yaml

echo "Installing Dapr..."
helm repo add dapr https://dapr.github.io/helm-charts/
helm repo update
helm install dapr dapr/dapr --namespace dapr-system --create-namespace --version 1.16.0 --wait

echo "Installing Nginx Ingress Controller..."
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/kind/deploy.yaml
kubectl wait --namespace ingress-nginx \
  --for=condition=ready pod \
  --selector=app.kubernetes.io/component=controller \
  --timeout=90s

echo "Deploying infrastructure (Redis, RabbitMQ, SQL Server)..."
helm install reddog-infra ./charts/infrastructure -f values/values-local.yaml --wait

echo "Deploying Red Dog application..."
helm install reddog ./charts/reddog -f values/values-local.yaml --wait

echo "Local development environment ready!"
echo "Access UI: http://localhost"
echo "Access API: http://localhost/api/orders"
```

### Teardown Script

**File**: `scripts/teardown-local-dev.sh` (to be created)

```bash
#!/bin/bash
set -e

echo "Deleting kind cluster..."
kind delete cluster --name reddog-local

echo "Local environment removed."
```

### Verification

**Success Criteria:**
- `kubectl get nodes` shows 1 control-plane node in Ready status
- `kubectl get pods -n dapr-system` shows all Dapr pods Running
- `kubectl get pods -n ingress-nginx` shows nginx-controller Running
- `curl http://localhost/api/orders` returns HTTP 200 or 404 (API reachable)
- `open http://localhost` displays Red Dog UI

## References

- **REF-001**: Related ADR: `docs/adr/adr-0007-cloud-agnostic-deployment-strategy.md` (Containerized infrastructure for multi-cloud)
- **REF-002**: Related ADR: `docs/adr/adr-0009-helm-multi-environment-deployment.md` (Helm values for local/Azure/AWS/GCP)
- **REF-003**: Related ADR: `docs/adr/adr-0010-nginx-ingress-controller.md` (Nginx Ingress works on kind)
- **REF-004**: Research Document: `docs/research/docker-compose-infrastructure-history.md` (Docker Compose removal history)
- **REF-005**: Research Document: `docs/research/local-development-infrastructure-requirements.md` (Infrastructure components for local dev)
- **REF-006**: Research Document: `docs/research/dev-container-alternatives-2025.md` (Evaluated Tilt, Skaffold, kind)
- **REF-007**: Git Commit: `ecca0f5` (November 2, 2025) - Removed `.devcontainer/` and `manifests/local/`
- **REF-008**: Git Commit: `04d8b7f` (November 2, 2025) - Removed `.vscode/` configuration
- **REF-009**: kind Official Documentation: https://kind.sigs.k8s.io/
- **REF-010**: kind Ingress Guide: https://kind.sigs.k8s.io/docs/user/ingress/
- **REF-011**: Kubernetes Official kind Documentation: https://kubernetes.io/docs/tasks/tools/#kind
