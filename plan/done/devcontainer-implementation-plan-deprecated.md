# Red Dog Development Container Implementation Plan

**Date:** 2025-11-09
**Status:** ~~Draft~~ **DEPRECATED**
**Version:** 1.0
**Related ADRs:** ADR-0008 (kind local dev), ADR-0009 (Helm), ADR-0010 (Nginx Ingress)
**Research Reference:** `docs/research/development-containers-comprehensive-guide-2025.md`

---

**⚠️ DEPRECATION NOTICE - 2025-11-10**

This implementation plan is **NO LONGER ACTIVE** and is preserved for historical reference only.

**Decision:** Dev containers have been removed from the project after implementation and testing revealed technical blockers:
- kind has bugs when running inside containers (nested Docker complexity - [GitHub Issue #3695](https://github.com/kubernetes-sigs/kind/issues/3695))
- k3d lacks production parity with AKS/EKS/GKE (removes cloud providers, uses SQLite vs etcd)
- Native kind setup on WSL2/macOS/Linux is simpler and more reliable for teaching Kubernetes fundamentals

**Current Approach:** Follow ADR-0008 for local development using native kind clusters.

**Status:** Archived for reference - do not implement. See `.claude/sessions/2025-11-10-1030-dev-container-local-implementation.md` for full exploration history.

---

## Executive Summary

This plan outlines the phased reintroduction of development containers to the Red Dog project, leveraging 2025 best practices for polyglot microservices development. The implementation complements (not replaces) the kind-based local development environment defined in ADR-0008.

**Key Principle:** Development containers provide the **development environment** (language runtimes, IDEs, tools), while kind clusters provide the **runtime environment** (where services actually run).

### Goals

1. **5-Minute Developer Onboarding:** New developers productive immediately via "Reopen in Container"
2. **100% Environment Parity:** Same development environment across all team members and CI/CD
3. **Polyglot Support:** Single dev container with .NET 10, Go, Python, Node.js, and tooling for all languages
4. **kind Integration:** Dev container works seamlessly with kind cluster deployment (ADR-0008)
5. **CI/CD Alignment:** Same container image used locally and in GitHub Actions

### Non-Goals

- ❌ Replace kind cluster with Docker Compose for service orchestration
- ❌ Run microservices inside the dev container (services run in kind per ADR-0008)
- ❌ Change existing ADR decisions (complement, not contradict)

### Success Metrics

| Metric | Current | Target |
|--------|---------|--------|
| New developer setup time | 2+ hours | < 10 minutes |
| Environment consistency issues | Frequent | Zero |
| CI/dev environment parity | ~70% | 100% |
| Supported platforms | Manual setup | Windows, macOS, Linux, Codespaces |

---

## Prerequisites

### Required Software

**All Developers:**
- Docker Desktop 4.26+ (Windows/macOS) OR Docker CE 24.0+ (Linux)
- VS Code 1.94+
- VS Code Dev Containers extension (`ms-vscode-remote.remote-containers`)
- Git 2.40+

**Additional Tools (Installed via dev container):**
- .NET 10 SDK
- Go 1.23
- Python 3.12
- Node.js 24
- kind 0.20+
- kubectl 1.28+
- Helm 3.12+
- Dapr CLI 1.16+

### System Requirements

**Minimum:**
- 8GB RAM
- 50GB free disk space
- 2 CPU cores

**Recommended:**
- 16GB RAM
- 100GB free disk space
- 4+ CPU cores

### Knowledge Prerequisites

**Developers should understand:**
- Basic Docker concepts (containers, images, volumes)
- VS Code basics (command palette, extensions)
- Git workflow (clone, commit, push)

**Nice to have:**
- Kubernetes basics (for kind cluster usage)
- Dapr fundamentals (for microservices development)

### Permissions & Access

- Read/write access to Red Dog repository
- Ability to install Docker Desktop (business license if applicable: $5-9/month per user)
- Optional: GitHub Codespaces access (free tier: 60 hours/month)

---

## Architecture Overview

### Development Workflow Architecture

```
┌─────────────────────────────────────────────────────────────┐
│ Developer Machine (Host)                                     │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ VS Code (Host)                                        │  │
│  │  • UI and editor                                      │  │
│  │  • Remote connection to container                     │  │
│  └───────────────────────────────────────────────────────┘  │
│                        ↓                                     │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ Development Container (Docker)                        │  │
│  │  ┌─────────────────────────────────────────────────┐  │  │
│  │  │ Ubuntu 24.04                                     │  │  │
│  │  │  • .NET 10 SDK                                   │  │  │
│  │  │  • Go 1.23                                       │  │  │
│  │  │  • Python 3.12                                   │  │  │
│  │  │  • Node.js 24                                    │  │  │
│  │  │  • kind CLI                                      │  │  │
│  │  │  • kubectl, Helm, Dapr CLI                       │  │  │
│  │  │  • Project files (mounted from host)             │  │  │
│  │  │  • VS Code Server                                │  │  │
│  │  └─────────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────────┘  │
│                        ↓                                     │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ kind Cluster (Kubernetes-in-Docker)                   │  │
│  │  ┌─────────────────────────────────────────────────┐  │  │
│  │  │ Kubernetes Control Plane                         │  │  │
│  │  │  • Dapr 1.16 (system services)                   │  │  │
│  │  │  • Nginx Ingress Controller                      │  │  │
│  │  │  • Application Services:                         │  │  │
│  │  │    - OrderService (pod)                          │  │  │
│  │  │    - MakeLineService (pod)                       │  │  │
│  │  │    - LoyaltyService (pod)                        │  │  │
│  │  │    - AccountingService (pod)                     │  │  │
│  │  │    - UI (pod)                                    │  │  │
│  │  │  • Infrastructure:                               │  │  │
│  │  │    - Redis (pod)                                 │  │  │
│  │  │    - SQL Server (pod)                            │  │  │
│  │  │    - RabbitMQ (pod)                              │  │  │
│  │  └─────────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────────┘  │
│                        ↑                                     │
│                    Port Forwarding                           │
│                    (80, 443, etc.)                           │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Key Concepts

**Development Container:**
- Provides consistent development tools and runtimes
- Includes all language SDKs (.NET, Go, Python, Node.js)
- Includes Kubernetes tooling (kind, kubectl, Helm)
- Does NOT run application services

**kind Cluster:**
- Runs inside Docker on the host machine
- Hosts all microservices as Kubernetes pods
- Provides production-parity environment
- Deployed via Helm charts (ADR-0009)

**Separation of Concerns:**
- Dev Container = **Development environment** (edit code, run builds, manage kind)
- kind Cluster = **Runtime environment** (run services, test integrations)

---

## Phased Implementation

### Phase 1: Foundation (Week 1 - Estimated 8-12 hours)

**Objective:** Create minimal working dev container with .NET 10 and kind support

**Status:** Not Started  
**Duration:** 8-12 hours  
**Risk Level:** Low

#### Phase 1 Deliverables

1. **Basic Dev Container Configuration**
   - `.devcontainer/devcontainer.json` - Main configuration
   - `.devcontainer/Dockerfile` - Custom base image (optional, can use pre-built)
   - `.devcontainer/scripts/postCreateCommand.sh` - Setup automation

2. **Development Tooling**
   - .NET 10 SDK installed
   - kind, kubectl, Helm installed
   - Dapr CLI installed
   - VS Code extensions configured

3. **Documentation**
   - `.devcontainer/README.md` - Setup guide
   - Updated `CLAUDE.md` - Reference dev container setup

#### Phase 1 Implementation Steps

##### Step 1.1: Create Dev Container Directory Structure

```bash
mkdir -p .devcontainer/scripts
touch .devcontainer/devcontainer.json
touch .devcontainer/Dockerfile
touch .devcontainer/scripts/postCreateCommand.sh
chmod +x .devcontainer/scripts/postCreateCommand.sh
```

##### Step 1.2: Create `devcontainer.json` (Minimal Configuration)

**File:** `.devcontainer/devcontainer.json`

```json
{
  "name": "Red Dog Development",
  "build": {
    "dockerfile": "Dockerfile",
    "context": ".",
    "args": {
      "VARIANT": "10.0-noble"
    }
  },

  // VS Code customizations
  "customizations": {
    "vscode": {
      "extensions": [
        "ms-dotnettools.csharp",
        "ms-dotnettools.csdevkit",
        "ms-kubernetes-tools.vscode-kubernetes-tools",
        "ms-azuretools.vscode-dapr",
        "ms-vscode.makefile-tools",
        "eamodio.gitlens"
      ],
      "settings": {
        "dotnet.defaultSolution": "/workspace/RedDog.sln",
        "[csharp]": {
          "editor.defaultFormatter": "ms-dotnettools.csharp",
          "editor.formatOnSave": true
        }
      }
    }
  },

  // Port forwarding (for kind cluster access)
  "forwardPorts": [
    80,     // Nginx Ingress HTTP
    443,    // Nginx Ingress HTTPS
    5100,   // OrderService (direct access)
    8080    // UI (direct access)
  ],

  // Container setup
  "remoteUser": "vscode",
  "postCreateCommand": "bash .devcontainer/scripts/postCreateCommand.sh",

  // Features (instead of manual installation)
  "features": {
    "ghcr.io/devcontainers/features/docker-in-docker:2": {
      "version": "latest",
      "enableNonRootDocker": "true"
    }
  }
}
```

##### Step 1.3: Create Dockerfile

**File:** `.devcontainer/Dockerfile`

```dockerfile
# Use official .NET dev container base image
FROM mcr.microsoft.com/devcontainers/dotnet:1-10.0-noble

# Install kind, kubectl, Helm
RUN curl -Lo ./kind https://kind.sigs.k8s.io/dl/v0.20.0/kind-linux-amd64 \
    && chmod +x ./kind \
    && mv ./kind /usr/local/bin/kind

RUN curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl" \
    && chmod +x kubectl \
    && mv kubectl /usr/local/bin/

RUN curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash

# Install Dapr CLI
RUN wget -q https://raw.githubusercontent.com/dapr/cli/master/install/install.sh -O - | /bin/bash

# Set working directory
WORKDIR /workspace

# Configure Docker-in-Docker (kind needs Docker)
# This is handled by the docker-in-docker feature
```

##### Step 1.4: Create Post-Create Command Script

**File:** `.devcontainer/scripts/postCreateCommand.sh`

```bash
#!/bin/bash
set -e

echo "🚀 Setting up Red Dog development environment..."

# Restore .NET packages
echo "📦 Restoring .NET packages..."
dotnet restore RedDog.sln 2>/dev/null || echo "⚠️  No .NET solution found (expected during initial setup)"

# Verify tooling
echo "✅ Verifying installed tools..."
echo "  .NET version: $(dotnet --version)"
echo "  kind version: $(kind version)"
echo "  kubectl version: $(kubectl version --client --short 2>/dev/null || echo 'not connected')"
echo "  Helm version: $(helm version --short)"
echo "  Dapr version: $(dapr version --client 2>/dev/null || echo 'CLI only')"

echo ""
echo "✨ Development environment ready!"
echo ""
echo "📝 Next steps:"
echo "   1. Create kind cluster: ./scripts/setup-local-dev.sh (when ADR-0008 implemented)"
echo "   2. Build services: dotnet build RedDog.sln"
echo "   3. Deploy to kind: helm install reddog ./charts/reddog -f values/values-local.yaml"
echo ""
```

##### Step 1.5: Create Dev Container README

**File:** `.devcontainer/README.md`

```markdown
# Red Dog Development Container

## Quick Start

1. **Install Docker Desktop** (Windows/macOS) or Docker CE (Linux)
2. **Install VS Code** and the "Dev Containers" extension
3. **Open project in VS Code**
4. **Reopen in Container:** Press F1 → "Dev Containers: Reopen in Container"
5. **Wait for setup** (first time: 3-5 minutes)
6. **Start developing!**

## What's Included

### Language Runtimes
- .NET 10 SDK (LTS)
- (Future: Go 1.23, Python 3.12, Node.js 24)

### Kubernetes Tools
- kind (Kubernetes-in-Docker)
- kubectl (Kubernetes CLI)
- Helm (Package manager)

### Development Tools
- Dapr CLI
- Git
- GitHub CLI

### VS Code Extensions
- C# Dev Kit
- Kubernetes
- Dapr
- GitLens

## Architecture

The dev container provides the **development environment** (tools, SDKs, IDE).  
Services run in a **kind cluster** (per ADR-0008), not in the dev container.

```
Dev Container (this) → provides tools
   ↓
kind Cluster → runs services
```

## Common Tasks

### Build Services
```bash
dotnet build RedDog.sln
```

### Create kind Cluster
```bash
# When ADR-0008 implementation is complete:
./scripts/setup-local-dev.sh
```

### Deploy to kind
```bash
# When Helm charts exist (ADR-0009):
helm install reddog ./charts/reddog -f values/values-local.yaml
```

### Access Services
```bash
# UI
http://localhost

# OrderService API
http://localhost/api/orders
```

## Troubleshooting

### Container Build Fails
```bash
# Rebuild container
F1 → "Dev Containers: Rebuild Container"

# View build logs
F1 → "Dev Containers: Show Container Log"
```

### kind Cluster Won't Start
```bash
# Check Docker is running
docker ps

# Check kind cluster exists
kind get clusters

# Recreate cluster
kind delete cluster --name reddog-local
kind create cluster --config kind-config.yaml
```

### Port Already in Use
Edit `.devcontainer/devcontainer.json` and change `forwardPorts` to alternative ports.

### Slow Performance (macOS/Windows)
Allocate more resources to Docker Desktop:
- Settings → Resources → Memory: 8GB+
- Settings → Resources → CPUs: 4+

## File Structure

```
.devcontainer/
├── devcontainer.json          # Main configuration
├── Dockerfile                 # Custom base image
├── README.md                  # This file
└── scripts/
    └── postCreateCommand.sh   # Setup automation
```

## Support

- Documentation: [ADR-0008 (kind local dev)](../docs/adr/adr-0008-kind-local-development-environment.md)
- Dev Containers Guide: [Research Document](../docs/research/development-containers-comprehensive-guide-2025.md)
- Issues: Open GitHub issue with `devcontainer` label
```

##### Step 1.6: Update CLAUDE.md

Add section to `CLAUDE.md` under "Common Development Commands":

```markdown
### Development Container Setup (Recommended)

**Quick Start:**
1. Install Docker Desktop and VS Code
2. Install "Dev Containers" extension in VS Code
3. Open project in VS Code
4. Press F1 → "Dev Containers: Reopen in Container"
5. Wait for setup (first time: 3-5 minutes)

**What's Included:**
- .NET 10 SDK, kind, kubectl, Helm, Dapr CLI
- All necessary VS Code extensions
- Consistent environment across team

**Documentation:** See `.devcontainer/README.md` for details.
```

#### Phase 1 Validation Steps

**Test Locally:**

1. **Build the dev container:**
   ```bash
   # From VS Code
   F1 → "Dev Containers: Rebuild Container"
   ```

2. **Verify tooling:**
   ```bash
   # Inside dev container terminal
   dotnet --version    # Should show 10.0.x
   kind version        # Should show v0.20.0
   kubectl version --client
   helm version
   dapr version
   ```

3. **Build .NET solution:**
   ```bash
   dotnet restore RedDog.sln
   dotnet build RedDog.sln
   ```

4. **Verify VS Code extensions loaded:**
   - C# extension active (syntax highlighting works)
   - Kubernetes extension shows in sidebar
   - Dapr extension shows in sidebar

**Success Criteria:**

- ✅ Dev container builds without errors (< 5 minutes)
- ✅ All tools installed and working
- ✅ .NET solution builds successfully
- ✅ VS Code extensions loaded
- ✅ Documentation accurate and complete

#### Phase 1 Rollout Strategy

**Week 1:**
- Day 1-2: Create dev container configuration
- Day 3: Test locally, iterate on issues
- Day 4: Create documentation
- Day 5: Team review, commit to repository

**Rollout:**
1. Commit to feature branch `feature/devcontainer-phase1`
2. Test in CI (GitHub Actions)
3. Team member validation (2-3 developers)
4. Merge to main branch
5. Announce to team via docs update

---

### Phase 2: Polyglot Support (Week 2 - Estimated 6-8 hours)

**Objective:** Add Go, Python, Node.js support for polyglot development

**Status:** Not Started (depends on Phase 1)  
**Duration:** 6-8 hours  
**Risk Level:** Low

#### Phase 2 Deliverables

1. **Additional Language Runtimes**
   - Go 1.23 installed
   - Python 3.12 installed
   - Node.js 24 installed

2. **Language-Specific Tooling**
   - Go extensions (gopls, Go test)
   - Python extensions (Pylance, Ruff)
   - Node.js extensions (ESLint, Prettier)
   - Vue.js extensions (Vetur, Vue Language Features)

3. **Updated Setup Scripts**
   - Python dependency installation (pip install)
   - Node.js dependency installation (npm install)
   - Go module verification

#### Phase 2 Implementation Steps

##### Step 2.1: Update `devcontainer.json` with Features

```json
{
  "name": "Red Dog Polyglot Development",
  "build": {
    "dockerfile": "Dockerfile",
    "context": "."
  },

  // Add language features
  "features": {
    "ghcr.io/devcontainers/features/docker-in-docker:2": {},
    "ghcr.io/devcontainers/features/go:1": {
      "version": "1.23"
    },
    "ghcr.io/devcontainers/features/python:1": {
      "version": "3.12",
      "installTools": true
    },
    "ghcr.io/devcontainers/features/node:1": {
      "version": "24"
    }
  },

  "customizations": {
    "vscode": {
      "extensions": [
        // .NET
        "ms-dotnettools.csharp",
        "ms-dotnettools.csdevkit",
        
        // Go
        "golang.go",
        
        // Python
        "ms-python.python",
        "ms-python.vscode-pylance",
        "charliermarsh.ruff",
        
        // Node.js / JavaScript
        "dbaeumer.vscode-eslint",
        "esbenp.prettier-vscode",
        
        // Vue.js
        "Vue.volar",
        
        // General
        "ms-kubernetes-tools.vscode-kubernetes-tools",
        "ms-azuretools.vscode-dapr",
        "eamodio.gitlens"
      ],
      "settings": {
        // .NET
        "[csharp]": {
          "editor.defaultFormatter": "ms-dotnettools.csharp",
          "editor.formatOnSave": true
        },
        
        // Go
        "[go]": {
          "editor.defaultFormatter": "golang.go",
          "editor.formatOnSave": true
        },
        
        // Python
        "[python]": {
          "editor.defaultFormatter": "charliermarsh.ruff",
          "editor.formatOnSave": true
        },
        
        // JavaScript/Vue
        "[javascript]": {
          "editor.defaultFormatter": "esbenp.prettier-vscode",
          "editor.formatOnSave": true
        },
        "[vue]": {
          "editor.defaultFormatter": "Vue.volar"
        }
      }
    }
  },

  "forwardPorts": [
    80, 443,      // Nginx Ingress
    5100, 5180,   // OrderService
    5200, 5280,   // MakeLineService
    5300, 5380,   // ReceiptGenerationService
    5400, 5480,   // LoyaltyService
    5700, 5780,   // AccountingService
    8080          // UI
  ]
}
```

##### Step 2.2: Update Post-Create Command

**File:** `.devcontainer/scripts/postCreateCommand.sh`

```bash
#!/bin/bash
set -e

echo "🚀 Setting up Red Dog polyglot development environment..."

# .NET Setup
echo "📦 Restoring .NET packages..."
dotnet restore RedDog.sln 2>/dev/null || echo "⚠️  No .NET solution found"

# Node.js Setup (UI)
if [ -d "RedDog.UI" ]; then
  echo "📦 Installing UI dependencies..."
  cd RedDog.UI
  npm install --silent
  cd ..
fi

# Python Setup (when Python services exist)
if [ -f "receipt-service/requirements.txt" ]; then
  echo "📦 Installing Python dependencies..."
  pip install -r receipt-service/requirements.txt
fi

# Go Setup (when Go services exist)
if [ -d "makeline-service" ]; then
  echo "📦 Verifying Go modules..."
  cd makeline-service
  go mod download
  cd ..
fi

# Verify all tools
echo "✅ Verifying polyglot environment..."
echo "  .NET version: $(dotnet --version)"
echo "  Go version: $(go version | awk '{print $3}')"
echo "  Python version: $(python3 --version | awk '{print $2}')"
echo "  Node.js version: $(node --version)"
echo "  npm version: $(npm --version)"
echo "  kind version: $(kind version)"
echo "  kubectl version: $(kubectl version --client --short 2>/dev/null || echo 'not connected')"
echo "  Helm version: $(helm version --short)"
echo "  Dapr version: $(dapr version --client 2>/dev/null || echo 'CLI only')"

echo ""
echo "✨ Polyglot development environment ready!"
echo ""
echo "📝 Supported languages:"
echo "   • .NET 10 (C#)"
echo "   • Go 1.23"
echo "   • Python 3.12"
echo "   • Node.js 24"
echo "   • Vue.js 3"
echo ""
```

#### Phase 2 Validation Steps

1. **Rebuild container with new features:**
   ```bash
   F1 → "Dev Containers: Rebuild Container"
   ```

2. **Verify all language runtimes:**
   ```bash
   dotnet --version     # 10.0.x
   go version           # go1.23.x
   python3 --version    # Python 3.12.x
   node --version       # v24.x.x
   ```

3. **Test language-specific builds:**
   ```bash
   # .NET
   dotnet build RedDog.sln
   
   # Node.js (UI)
   cd RedDog.UI && npm run build
   
   # Go (when services exist)
   cd makeline-service && go build
   
   # Python (when services exist)
   cd receipt-service && python -m pytest
   ```

4. **Verify VS Code language servers:**
   - Open .cs file → C# IntelliSense works
   - Open .go file → Go IntelliSense works
   - Open .py file → Python IntelliSense works
   - Open .vue file → Vue IntelliSense works

**Success Criteria:**

- ✅ All language runtimes installed and working
- ✅ All language-specific extensions loaded
- ✅ IntelliSense working for all languages
- ✅ Format-on-save working for all languages
- ✅ Build/test commands work for all languages

---

### Phase 3: kind Cluster Integration (Week 3 - Estimated 10-12 hours)

**Objective:** Integrate dev container with kind cluster creation and management

**Status:** Not Started (depends on ADR-0008 implementation)  
**Duration:** 10-12 hours  
**Risk Level:** Medium

**Prerequisites:**
- Phase 2 complete
- ADR-0008 implementation complete (kind-config.yaml, Helm charts exist)

#### Phase 3 Deliverables

1. **Automated kind Cluster Management**
   - Scripts to create/delete kind cluster from dev container
   - Helm chart deployment automation
   - Health check scripts

2. **Development Workflows**
   - VS Code tasks for common operations
   - Debugging configurations for services in kind
   - Port forwarding automation

3. **Integration Documentation**
   - How to run services in kind from dev container
   - Debugging guide
   - Troubleshooting guide

#### Phase 3 Implementation Steps

##### Step 3.1: Create kind Helper Scripts

**File:** `.devcontainer/scripts/setup-kind-cluster.sh`

```bash
#!/bin/bash
set -e

echo "🔧 Setting up kind cluster for Red Dog..."

# Check if cluster exists
if kind get clusters | grep -q "reddog-local"; then
  echo "⚠️  Cluster 'reddog-local' already exists"
  read -p "Delete and recreate? (y/N): " -n 1 -r
  echo
  if [[ $REPLY =~ ^[Yy]$ ]]; then
    kind delete cluster --name reddog-local
  else
    echo "Using existing cluster"
    exit 0
  fi
fi

# Create kind cluster
echo "📦 Creating kind cluster..."
kind create cluster --config kind-config.yaml

# Wait for cluster to be ready
echo "⏳ Waiting for cluster to be ready..."
kubectl wait --for=condition=Ready nodes --all --timeout=120s

# Install Dapr
echo "🔗 Installing Dapr..."
helm repo add dapr https://dapr.github.io/helm-charts/
helm repo update
helm install dapr dapr/dapr \
  --namespace dapr-system \
  --create-namespace \
  --version 1.16.0 \
  --wait

# Install Nginx Ingress
echo "🌐 Installing Nginx Ingress..."
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/kind/deploy.yaml
kubectl wait --namespace ingress-nginx \
  --for=condition=ready pod \
  --selector=app.kubernetes.io/component=controller \
  --timeout=90s

# Deploy infrastructure (when Helm charts exist)
if [ -d "charts/infrastructure" ]; then
  echo "🗄️  Deploying infrastructure..."
  helm install reddog-infra ./charts/infrastructure \
    -f values/values-local.yaml \
    --wait
fi

# Deploy application (when Helm charts exist)
if [ -d "charts/reddog" ]; then
  echo "🚀 Deploying Red Dog application..."
  helm install reddog ./charts/reddog \
    -f values/values-local.yaml \
    --wait
fi

echo ""
echo "✅ kind cluster ready!"
echo ""
echo "📝 Access services:"
echo "   UI: http://localhost"
echo "   API: http://localhost/api/orders"
echo ""
echo "📝 Useful commands:"
echo "   kubectl get pods              # View all pods"
echo "   kubectl logs <pod-name>       # View logs"
echo "   helm list                     # View releases"
echo ""
```

**File:** `.devcontainer/scripts/teardown-kind-cluster.sh`

```bash
#!/bin/bash
set -e

echo "🗑️  Tearing down kind cluster..."

if kind get clusters | grep -q "reddog-local"; then
  kind delete cluster --name reddog-local
  echo "✅ Cluster deleted"
else
  echo "⚠️  No cluster named 'reddog-local' found"
fi
```

##### Step 3.2: Create VS Code Tasks

**File:** `.vscode/tasks.json`

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "kind: Create Cluster",
      "type": "shell",
      "command": "bash .devcontainer/scripts/setup-kind-cluster.sh",
      "problemMatcher": [],
      "presentation": {
        "reveal": "always",
        "panel": "new"
      }
    },
    {
      "label": "kind: Delete Cluster",
      "type": "shell",
      "command": "bash .devcontainer/scripts/teardown-kind-cluster.sh",
      "problemMatcher": []
    },
    {
      "label": "Build: All Services",
      "type": "shell",
      "command": "dotnet build RedDog.sln",
      "group": {
        "kind": "build",
        "isDefault": true
      },
      "problemMatcher": "$msCompile"
    },
    {
      "label": "Helm: Install Red Dog",
      "type": "shell",
      "command": "helm install reddog ./charts/reddog -f values/values-local.yaml",
      "problemMatcher": [],
      "dependsOn": ["kind: Create Cluster"]
    },
    {
      "label": "Helm: Upgrade Red Dog",
      "type": "shell",
      "command": "helm upgrade reddog ./charts/reddog -f values/values-local.yaml",
      "problemMatcher": []
    },
    {
      "label": "kubectl: Get Pods",
      "type": "shell",
      "command": "kubectl get pods -A",
      "problemMatcher": []
    },
    {
      "label": "kubectl: Get Services",
      "type": "shell",
      "command": "kubectl get services -A",
      "problemMatcher": []
    }
  ]
}
```

##### Step 3.3: Update Dev Container Configuration

Add to `.devcontainer/devcontainer.json`:

```json
{
  // ... existing config ...
  
  "postCreateCommand": "bash .devcontainer/scripts/postCreateCommand.sh",
  
  // Start kind cluster on container start (optional)
  // "postStartCommand": "bash .devcontainer/scripts/setup-kind-cluster.sh",
  
  // Mount Docker socket for kind
  "mounts": [
    "source=/var/run/docker.sock,target=/var/run/docker.sock,type=bind"
  ],
  
  // Ensure Docker-in-Docker works
  "privileged": false,
  "init": true
}
```

##### Step 3.4: Create Integration Documentation

**File:** `.devcontainer/KIND_INTEGRATION.md`

```markdown
# kind Cluster Integration with Dev Container

## Overview

This dev container is configured to create and manage kind (Kubernetes-in-Docker) clusters for running Red Dog microservices locally.

## Quick Start

### Create kind Cluster

**Option 1: VS Code Task**
1. Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on macOS)
2. Select "Tasks: Run Task"
3. Select "kind: Create Cluster"

**Option 2: Command Line**
```bash
bash .devcontainer/scripts/setup-kind-cluster.sh
```

### Deploy Services

Once cluster is created:

```bash
# Deploy with Helm (when charts exist)
helm install reddog ./charts/reddog -f values/values-local.yaml

# Or use VS Code task:
# Ctrl+Shift+P → "Tasks: Run Task" → "Helm: Install Red Dog"
```

### Access Services

```bash
# UI
http://localhost

# OrderService API
http://localhost/api/orders

# Direct pod access (for debugging)
kubectl port-forward pod/<pod-name> 5100:5100
```

## Common Tasks

### View Running Pods
```bash
kubectl get pods -A

# Or use VS Code task:
# Ctrl+Shift+P → "Tasks: Run Task" → "kubectl: Get Pods"
```

### View Logs
```bash
# Specific service
kubectl logs -f deployment/order-service

# All containers in a pod
kubectl logs -f pod/<pod-name> --all-containers
```

### Rebuild and Redeploy Service

```bash
# 1. Build new image
dotnet publish RedDog.OrderService -c Release

# 2. Build Docker image
docker build -t order-service:latest -f RedDog.OrderService/Dockerfile .

# 3. Load image into kind
kind load docker-image order-service:latest --name reddog-local

# 4. Restart deployment
kubectl rollout restart deployment/order-service
```

### Delete Cluster

```bash
bash .devcontainer/scripts/teardown-kind-cluster.sh
```

## Debugging in kind

### Attach Debugger to Pod

1. **Forward debugger port:**
   ```bash
   kubectl port-forward pod/order-service-xxx 5000:5000
   ```

2. **Attach VS Code debugger:**
   - Press F5
   - Select ".NET Core Attach"
   - Select process in port-forwarded pod

### Remote Debugging Configuration

Add to `.vscode/launch.json`:

```json
{
  "name": ".NET Core Attach (kind)",
  "type": "coreclr",
  "request": "attach",
  "processId": "${command:pickRemoteProcess}",
  "pipeTransport": {
    "pipeCwd": "${workspaceFolder}",
    "pipeProgram": "kubectl",
    "pipeArgs": ["exec", "-i", "<pod-name>", "--"],
    "debuggerPath": "/vsdbg/vsdbg"
  }
}
```

## Troubleshooting

### Cluster Won't Start

**Check Docker:**
```bash
docker ps  # Should show containers
```

**Check kind cluster:**
```bash
kind get clusters
kind get nodes --name reddog-local
```

**Recreate cluster:**
```bash
bash .devcontainer/scripts/teardown-kind-cluster.sh
bash .devcontainer/scripts/setup-kind-cluster.sh
```

### Services Won't Deploy

**Check Helm charts exist:**
```bash
ls charts/reddog
ls charts/infrastructure
```

If missing, see ADR-0009 for Helm chart creation.

**Check Helm releases:**
```bash
helm list -A
```

### Port Conflicts

**Check port usage:**
```bash
# macOS/Linux
lsof -i :80

# Windows
netstat -ano | findstr :80
```

**Change ports in kind-config.yaml:**
```yaml
extraPortMappings:
- containerPort: 80
  hostPort: 8080  # Use alternative port
```

### Performance Issues

**Allocate more resources to Docker Desktop:**
- Settings → Resources → Memory: 8GB+
- Settings → Resources → CPUs: 4+

**Use named volumes for better performance:**
See `.devcontainer/docker-compose.yml` (if using Compose)
```

#### Phase 3 Validation Steps

1. **Create kind cluster from dev container:**
   ```bash
   bash .devcontainer/scripts/setup-kind-cluster.sh
   ```

2. **Verify cluster health:**
   ```bash
   kubectl get nodes
   kubectl get pods -n dapr-system
   kubectl get pods -n ingress-nginx
   ```

3. **Deploy application (when Helm charts exist):**
   ```bash
   helm install reddog ./charts/reddog -f values/values-local.yaml
   kubectl get pods
   ```

4. **Access services:**
   ```bash
   curl http://localhost
   curl http://localhost/api/orders
   ```

5. **Test VS Code tasks:**
   - Run "kind: Create Cluster" task
   - Run "kubectl: Get Pods" task
   - Verify output in terminal

**Success Criteria:**

- ✅ kind cluster creates successfully from dev container
- ✅ Dapr and Nginx Ingress install correctly
- ✅ Services deploy and run in kind
- ✅ Services accessible via localhost
- ✅ VS Code tasks work correctly
- ✅ Documentation accurate and helpful

---

### Phase 4: CI/CD Integration (Week 4 - Estimated 6-8 hours)

**Objective:** Ensure dev container works in GitHub Actions for CI/CD parity

**Status:** Not Started (depends on Phase 1-3)  
**Duration:** 6-8 hours  
**Risk Level:** Low

#### Phase 4 Deliverables

1. **GitHub Actions Workflow**
   - Build and test in dev container
   - Use same container image as local development
   - Cache container layers for performance

2. **Pre-built Container Images**
   - Publish dev container to GHCR
   - Version tagging strategy
   - Automated builds on Dockerfile changes

3. **CI/CD Documentation**
   - How CI uses dev container
   - How to update dev container
   - Troubleshooting CI failures

#### Phase 4 Implementation Steps

##### Step 4.1: Create GitHub Actions Workflow

**File:** `.github/workflows/devcontainer-ci.yml`

```yaml
name: Dev Container CI

on:
  push:
    branches: [main, develop]
    paths:
      - '.devcontainer/**'
      - '.github/workflows/devcontainer-ci.yml'
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Build and run in dev container
        uses: devcontainers/ci@v0.3
        with:
          imageName: ghcr.io/${{ github.repository }}/devcontainer
          cacheFrom: ghcr.io/${{ github.repository }}/devcontainer:cache
          push: filter
          
          runCmd: |
            # Verify tools installed
            dotnet --version
            go version
            python3 --version
            node --version
            kind version
            kubectl version --client
            helm version
            dapr version
            
            # Build .NET solution
            dotnet restore RedDog.sln
            dotnet build RedDog.sln -c Release
            
            # Build UI (when it exists)
            if [ -d "RedDog.UI" ]; then
              cd RedDog.UI
              npm install
              npm run build
              cd ..
            fi
            
            echo "✅ All builds successful in dev container"

      - name: Upload build artifacts
        if: success()
        uses: actions/upload-artifact@v3
        with:
          name: build-artifacts
          path: |
            **/bin/Release/**
            RedDog.UI/dist/**
```

##### Step 4.2: Pre-build and Publish Dev Container

**File:** `.github/workflows/devcontainer-publish.yml`

```yaml
name: Publish Dev Container

on:
  push:
    branches: [main]
    paths:
      - '.devcontainer/**'
  workflow_dispatch:

jobs:
  publish:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Log in to GHCR
        uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Build and push dev container
        uses: devcontainers/ci@v0.3
        with:
          imageName: ghcr.io/${{ github.repository }}/devcontainer
          imageTag: |
            latest
            ${{ github.sha }}
          cacheFrom: ghcr.io/${{ github.repository }}/devcontainer:cache
          push: always
```

##### Step 4.3: Use Pre-built Image Locally

Update `.devcontainer/devcontainer.json`:

```json
{
  "name": "Red Dog Development",
  
  // Option 1: Build locally (default for new contributors)
  "build": {
    "dockerfile": "Dockerfile",
    "context": "."
  },
  
  // Option 2: Use pre-built image (faster, uncomment to use)
  // "image": "ghcr.io/your-org/reddog-code/devcontainer:latest",
  
  // ... rest of config
}
```

#### Phase 4 Validation Steps

1. **Trigger CI workflow:**
   - Make change to `.devcontainer/Dockerfile`
   - Commit and push
   - Check GitHub Actions runs successfully

2. **Verify pre-built image:**
   - Check GHCR for published image
   - Pull image locally: `docker pull ghcr.io/.../devcontainer:latest`
   - Use pre-built image in dev container

3. **Test CI parity:**
   - Build passes locally → should pass in CI
   - Build fails locally → should fail in CI (same error)

**Success Criteria:**

- ✅ CI builds dev container successfully
- ✅ Tests run in CI using dev container
- ✅ Pre-built images published to GHCR
- ✅ Local dev can use pre-built images
- ✅ 100% parity between local and CI environments

---

### Phase 5: Optimization & Advanced Features (Week 5 - Estimated 8-10 hours)

**Objective:** Optimize performance, add advanced features, multi-configuration support

**Status:** Not Started (optional enhancements)  
**Duration:** 8-10 hours  
**Risk Level:** Low

#### Phase 5 Deliverables

1. **Performance Optimizations**
   - Named volumes for dependencies (node_modules, .nuget, .cache)
   - Layer caching strategies
   - Resource limit configurations

2. **Multi-Configuration Support**
   - Frontend-only dev container (lighter, faster)
   - Backend-only dev container
   - Full-stack dev container (default)

3. **Advanced Features**
   - GitHub Codespaces prebuilds
   - Docker Compose for infrastructure services
   - Secret management integration

#### Phase 5 Implementation Steps

##### Step 5.1: Add Docker Compose for Performance

**File:** `.devcontainer/docker-compose.yml`

```yaml
version: '3.8'

services:
  dev:
    build:
      context: ..
      dockerfile: .devcontainer/Dockerfile
    
    volumes:
      # Bind mount source code (cached for performance)
      - ..:/workspace:cached
      
      # Exclude .git from sync (performance)
      - /workspace/.git
      
      # Named volumes for dependencies (fast)
      - reddog-node-modules:/workspace/RedDog.UI/node_modules
      - reddog-dotnet-nuget:/home/vscode/.nuget
      - reddog-dotnet-cache:/home/vscode/.cache/dotnet
      - reddog-go-cache:/go/pkg
    
    # Keep container running
    command: sleep infinity
    
    # Environment variables
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      DOTNET_CLI_TELEMETRY_OPTOUT: "1"
    
    # Access to Docker daemon for kind
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    
    # Network for future multi-container setups
    networks:
      - reddog

# Optional: Infrastructure services (Redis, SQL Server)
# Uncomment when needed for local testing without kind
#
#   redis:
#     image: redis:7-alpine
#     ports:
#       - "6379:6379"
#     networks:
#       - reddog
#
#   sqlserver:
#     image: mcr.microsoft.com/mssql/server:2022-latest
#     environment:
#       ACCEPT_EULA: "Y"
#       SA_PASSWORD: "YourStrong!Passw0rd"
#     ports:
#       - "1433:1433"
#     networks:
#       - reddog

volumes:
  reddog-node-modules:
  reddog-dotnet-nuget:
  reddog-dotnet-cache:
  reddog-go-cache:

networks:
  reddog:
    driver: bridge
```

Update `.devcontainer/devcontainer.json`:

```json
{
  "name": "Red Dog Development",
  
  // Use Docker Compose
  "dockerComposeFile": "docker-compose.yml",
  "service": "dev",
  "workspaceFolder": "/workspace",
  
  // ... rest of config
}
```

##### Step 5.2: Create Role-Based Configurations

**File:** `.devcontainer/devcontainer-frontend.json` (Frontend only)

```json
{
  "name": "Red Dog Frontend",
  "image": "mcr.microsoft.com/devcontainers/javascript-node:1-24",
  
  "customizations": {
    "vscode": {
      "extensions": [
        "Vue.volar",
        "dbaeumer.vscode-eslint",
        "esbenp.prettier-vscode"
      ]
    }
  },
  
  "forwardPorts": [8080],
  
  "postCreateCommand": "cd RedDog.UI && npm install"
}
```

**File:** `.devcontainer/devcontainer-backend.json` (Backend only)

```json
{
  "name": "Red Dog Backend",
  "image": "mcr.microsoft.com/devcontainers/dotnet:1-10.0-noble",
  
  "features": {
    "ghcr.io/devcontainers/features/go:1": { "version": "1.23" },
    "ghcr.io/devcontainers/features/python:1": { "version": "3.12" },
    "ghcr.io/devcontainers/features/docker-in-docker:2": {}
  },
  
  "customizations": {
    "vscode": {
      "extensions": [
        "ms-dotnettools.csharp",
        "golang.go",
        "ms-python.python",
        "ms-kubernetes-tools.vscode-kubernetes-tools"
      ]
    }
  },
  
  "postCreateCommand": "dotnet restore RedDog.sln"
}
```

**Usage:**
```bash
# Open with specific config
F1 → "Dev Containers: Open Folder in Container..."
→ Select configuration file
```

##### Step 5.3: Add GitHub Codespaces Prebuilds

**File:** `.devcontainer/devcontainer.json` (add codespaces config)

```json
{
  // ... existing config ...
  
  // GitHub Codespaces settings
  "codespaces": {
    "prebuild": {
      "enabled": true
    },
    "dotfiles": {
      "repository": "",
      "installCommand": ""
    }
  }
}
```

**File:** `.github/workflows/codespaces-prebuild.yml`

```yaml
name: Codespaces Prebuild

on:
  push:
    branches: [main]
    paths:
      - '.devcontainer/**'
  schedule:
    # Weekly rebuild
    - cron: '0 0 * * 0'
  workflow_dispatch:

jobs:
  prebuild:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4
      
      - name: Trigger Codespaces prebuild
        uses: actions/github-script@v7
        with:
          script: |
            await github.rest.codespaces.preFlightWithRepoForAuthenticatedUser({
              owner: context.repo.owner,
              repo: context.repo.repo
            });
```

##### Step 5.4: Resource Limits and Security

**File:** `.devcontainer/devcontainer.json` (add resource limits)

```json
{
  // ... existing config ...
  
  // Security settings
  "privileged": false,
  "init": true,
  "capAdd": [],
  "securityOpt": ["no-new-privileges"],
  
  // Resource limits (Docker Compose)
  "runArgs": [
    "--cpus=4",
    "--memory=8g"
  ],
  
  // Environment variables (non-sensitive only)
  "containerEnv": {
    "ASPNETCORE_ENVIRONMENT": "Development",
    "DOTNET_CLI_TELEMETRY_OPTOUT": "1"
  },
  
  // Mounts (read-only where appropriate)
  "mounts": [
    "source=${localEnv:HOME}${localEnv:USERPROFILE}/.ssh,target=/home/vscode/.ssh,readonly",
    "source=${localEnv:HOME}${localEnv:USERPROFILE}/.gitconfig,target=/home/vscode/.gitconfig,readonly"
  ]
}
```

#### Phase 5 Validation Steps

1. **Test performance improvements:**
   ```bash
   # Time initial container build
   time: F1 → "Dev Containers: Rebuild Container"
   
   # Compare with/without named volumes
   # Should see 50%+ improvement with volumes
   ```

2. **Test role-based configurations:**
   ```bash
   # Test frontend-only
   F1 → "Dev Containers: Open Folder in Container..."
   → Select "devcontainer-frontend.json"
   
   # Verify only Node.js tools present
   node --version  # Should work
   dotnet --version  # Should NOT exist
   ```

3. **Test Codespaces prebuild:**
   - Open repository in Codespaces
   - Should start in < 30 seconds (vs 3-5 minutes)
   - Verify all tools pre-installed

**Success Criteria:**

- ✅ Container startup time reduced by 50%+
- ✅ Named volumes improve dependency install speed
- ✅ Role-based configs work correctly
- ✅ Codespaces prebuilds functional
- ✅ Resource limits prevent runaway processes
- ✅ Security hardening in place

---

## Testing and Validation

### Comprehensive Test Plan

#### Unit Tests (Per Phase)

**Phase 1 Tests:**
- [ ] Dev container builds successfully
- [ ] .NET 10 SDK installed correctly
- [ ] kind, kubectl, Helm installed
- [ ] VS Code extensions load
- [ ] .NET solution builds

**Phase 2 Tests:**
- [ ] Go runtime installed
- [ ] Python runtime installed
- [ ] Node.js runtime installed
- [ ] All language extensions load
- [ ] IntelliSense works for all languages

**Phase 3 Tests:**
- [ ] kind cluster creates from dev container
- [ ] Dapr installs correctly
- [ ] Nginx Ingress installs correctly
- [ ] Services deploy to kind
- [ ] Services accessible via localhost

**Phase 4 Tests:**
- [ ] CI builds dev container
- [ ] Tests run in CI using dev container
- [ ] Pre-built images published
- [ ] Local dev uses pre-built images

**Phase 5 Tests:**
- [ ] Named volumes improve performance
- [ ] Role-based configs work
- [ ] Codespaces prebuilds functional
- [ ] Resource limits enforced

#### Integration Tests

**End-to-End Workflow:**
1. Clone repository
2. Open in VS Code
3. Reopen in container (first time: 3-5 minutes)
4. Create kind cluster
5. Deploy services
6. Access UI and APIs
7. Make code changes
8. Rebuild and redeploy

**Expected Timeline:**
- First-time setup: 5-10 minutes
- Subsequent starts: 30-60 seconds
- Code change → redeploy: 2-3 minutes

#### Platform Tests

**Windows:**
- [ ] Docker Desktop integration works
- [ ] WSL2 integration works
- [ ] Port forwarding works
- [ ] File sync works (cached volumes)

**macOS:**
- [ ] Docker Desktop integration works
- [ ] File sync performance acceptable
- [ ] Port forwarding works
- [ ] Resource limits respected

**Linux:**
- [ ] Docker CE works
- [ ] Podman compatibility (optional)
- [ ] Native performance confirmed

**GitHub Codespaces:**
- [ ] Codespace starts quickly
- [ ] All tools available
- [ ] Port forwarding works
- [ ] Performance acceptable

### Acceptance Criteria

#### Minimum Viable Product (MVP)

**Phase 1 Complete:**
- ✅ Dev container with .NET 10 and kind tools
- ✅ Documentation complete
- ✅ At least 2 team members successfully using it

**Full Implementation (Phase 1-3):**
- ✅ Polyglot support (.NET, Go, Python, Node.js)
- ✅ kind cluster integration
- ✅ CI/CD parity
- ✅ Team-wide adoption

#### Quality Gates

**Before Phase Completion:**
1. All unit tests pass
2. Documentation reviewed and approved
3. At least one team member validates
4. Performance benchmarks met

**Before Production:**
1. All integration tests pass
2. All platforms tested (Windows, macOS, Linux)
3. CI/CD integration validated
4. Security review complete

---

## Rollout Strategy

### Pilot Program (Week 1-2)

**Participants:** 2-3 early adopters

**Goals:**
- Validate dev container setup
- Identify issues early
- Gather feedback

**Success Criteria:**
- All pilot users can develop successfully
- No major blockers identified
- Positive feedback on experience

### Team Rollout (Week 3-4)

**Communication:**
1. **Announcement:** Email/Slack about new dev container
2. **Documentation:** Link to `.devcontainer/README.md`
3. **Training:** Optional 30-minute walkthrough session
4. **Support:** Dedicated Slack channel for questions

**Migration Plan:**
- Existing developers can continue current workflow
- New developers should use dev container
- Gradual migration over 2-4 weeks

**Support Plan:**
- Dedicated "dev container champion" for troubleshooting
- Office hours (2x per week for 2 weeks)
- FAQ document updated based on questions

### Adoption Metrics

**Track Weekly:**
- Number of developers using dev container
- Setup time (average)
- Issues reported
- Time to first successful build

**Target Metrics (Week 4):**
- 80%+ team adoption
- < 10 minute average setup time
- < 2 issues per developer
- 100% success rate for builds

---

## Maintenance Plan

### Regular Maintenance Tasks

#### Monthly

**Update Base Images:**
```bash
# Check for updates
docker pull mcr.microsoft.com/devcontainers/dotnet:1-10.0-noble

# Test locally
F1 → "Dev Containers: Rebuild Container"

# If working, commit and push
git add .devcontainer/
git commit -m "chore: update dev container base image"
```

**Review Extensions:**
- Check for deprecated extensions
- Update extension versions if needed
- Remove unused extensions

#### Quarterly

**Security Audit:**
- Scan base image for vulnerabilities: `docker scan`
- Review mounted volumes and permissions
- Update security best practices

**Performance Review:**
- Measure container startup time
- Measure build times
- Optimize if degraded

#### As Needed

**Dependency Updates:**
- Update language runtimes (when new LTS released)
- Update tooling (kind, kubectl, Helm)
- Update VS Code extensions

**Configuration Changes:**
- Add new services (update port forwarding)
- Add new languages (update features)
- Add new tools (update Dockerfile)

### Version Management

**Semantic Versioning for Dev Container:**

```
v1.0.0 - Initial .NET 10 support
v1.1.0 - Add Go, Python, Node.js
v1.2.0 - Add kind integration
v2.0.0 - Breaking change (e.g., change base image)
```

**Tag Images in GHCR:**
```bash
ghcr.io/org/repo/devcontainer:latest
ghcr.io/org/repo/devcontainer:1.2.0
ghcr.io/org/repo/devcontainer:1
```

### Breaking Changes Policy

**When Making Breaking Changes:**

1. **Announcement:** Give 2-week notice before change
2. **Migration Guide:** Document upgrade steps
3. **Backwards Compatibility:** Maintain old config for 1 month
4. **Support:** Extended office hours during transition

**Example Breaking Change:**
- Upgrading .NET 10 → .NET 12
- Changing base image
- Removing major feature

### Troubleshooting Runbook

**Common Issues:**

| Issue | Cause | Solution |
|-------|-------|----------|
| Container won't build | Dockerfile syntax error | Check Docker logs, fix Dockerfile |
| Slow performance | Insufficient Docker resources | Allocate more RAM/CPU in Docker Desktop |
| Port conflicts | Another service using port | Change ports in `devcontainer.json` |
| Extensions won't load | Extension incompatibility | Remove problematic extension, report issue |
| kind cluster fails | Docker not running | Start Docker, recreate cluster |
| File changes not syncing | Volume mount issue | Use `:cached` flag, restart container |

**Escalation Path:**
1. Check `.devcontainer/README.md` troubleshooting section
2. Search GitHub issues
3. Ask in team Slack channel
4. Open GitHub issue with `devcontainer` label
5. Contact dev container champion

---

## Security Considerations

### Threat Model

**Threats:**
1. **Compromised base image** - Malicious code in Microsoft image
2. **Exposed secrets** - Credentials in devcontainer.json
3. **Container escape** - Attacker breaks out of container
4. **Host access** - Unauthorized access to host filesystem
5. **Supply chain attack** - Malicious VS Code extension

**Mitigations:**
1. Use official Microsoft images, verify signatures
2. Never commit secrets, use environment variables
3. Run as non-root, no privileged mode
4. Limit mounts to read-only where possible
5. Review extensions before adding, use trusted sources

### Security Checklist

**Configuration Review:**
- [ ] Base image from trusted source (mcr.microsoft.com)
- [ ] Image version pinned (not `latest`)
- [ ] Non-root user configured (`remoteUser: vscode`)
- [ ] No privileged mode (`privileged: false`)
- [ ] No extra capabilities (`capAdd: []`)
- [ ] Minimal mounts (only what's needed)
- [ ] Read-only mounts where applicable
- [ ] No secrets in `devcontainer.json`
- [ ] No secrets in environment variables
- [ ] Extensions from trusted publishers only

**Runtime Security:**
- [ ] Docker Desktop up to date
- [ ] VS Code up to date
- [ ] Dev Containers extension up to date
- [ ] Base image scanned for vulnerabilities
- [ ] No exposed sensitive ports
- [ ] Network isolation configured

### Secret Management

**DO NOT:**
- ❌ Hardcode secrets in `devcontainer.json`
- ❌ Commit `.env` files with real secrets
- ❌ Mount sensitive directories (`.aws`, `.azure`)

**DO:**
- ✅ Use environment variables (not committed)
- ✅ Use secret management tools (Vault, etc.)
- ✅ Use GitHub Codespaces secrets (for Codespaces)
- ✅ Document secret setup in README

**Example:**

```bash
# .env.local (gitignored)
DATABASE_PASSWORD=secret123
API_KEY=abc123

# Load in dev container
export $(cat .env.local | grep -v '^#' | xargs)
```

---

## Performance Optimization

### Benchmark Targets

| Metric | Target | Current (estimate) |
|--------|--------|-------------------|
| Initial container build | < 5 min | N/A (not yet built) |
| Container startup | < 60 sec | N/A |
| .NET restore | < 30 sec | N/A |
| .NET build | < 2 min | N/A |
| UI npm install | < 60 sec | N/A |
| UI build | < 30 sec | N/A |

### Optimization Strategies

**Layer Caching:**
```dockerfile
# Dockerfile - optimize layer order
FROM mcr.microsoft.com/devcontainers/dotnet:1-10.0-noble

# Install tools (changes rarely)
RUN apt-get update && apt-get install -y jq

# Install kind, kubectl, Helm (changes rarely)
RUN curl -Lo ./kind ... && mv ./kind /usr/local/bin/

# Copy solution file only (for restore caching)
COPY *.sln .
RUN dotnet restore

# Copy everything else (changes frequently)
COPY . .
```

**Named Volumes:**
```yaml
# docker-compose.yml
volumes:
  # Fast named volumes for dependencies
  - reddog-node-modules:/workspace/RedDog.UI/node_modules
  - reddog-dotnet-nuget:/home/vscode/.nuget
  - reddog-go-cache:/go/pkg
```

**Parallel Operations:**
```bash
# postCreateCommand.sh
(dotnet restore RedDog.sln) &
(cd RedDog.UI && npm install) &
wait
```

**Resource Allocation:**
```json
// devcontainer.json
"runArgs": [
  "--cpus=4",      // Use 4 CPU cores
  "--memory=8g"    // Use 8GB RAM
]
```

---

## Alignment with Existing ADRs

### ADR-0008: kind Local Development

**Alignment:**
- ✅ Dev container includes kind, kubectl, Helm
- ✅ Scripts to create/manage kind cluster
- ✅ Dev container does NOT run services (kind does)
- ✅ Port forwarding configured for kind cluster access

**Relationship:**
- Dev container = **development tooling**
- kind cluster = **runtime environment**
- They work together, not in conflict

### ADR-0009: Helm Multi-Environment Deployment

**Alignment:**
- ✅ Dev container includes Helm 3.12+
- ✅ Scripts to deploy Helm charts to kind
- ✅ Same Helm charts used locally and in production
- ✅ `values-local.yaml` used for kind deployment

**Relationship:**
- Dev container provides Helm CLI
- Helm deploys to kind cluster
- Same workflow as production (just different values file)

### ADR-0010: Nginx Ingress Controller

**Alignment:**
- ✅ Scripts install Nginx Ingress to kind
- ✅ Port 80/443 forwarded for Nginx access
- ✅ Same Ingress configuration as production

**Relationship:**
- Dev container manages kind cluster
- kind cluster runs Nginx Ingress
- Localhost access via port forwarding

### Summary: No Conflicts

This dev container implementation **complements** existing ADRs:
- Provides tooling for kind (ADR-0008)
- Provides Helm CLI for deployments (ADR-0009)
- Supports Nginx Ingress in kind (ADR-0010)
- Does NOT replace kind with Docker Compose
- Does NOT change deployment strategy

---

## GitHub Codespaces Support

### Configuration

Dev container automatically works with GitHub Codespaces:

```json
// devcontainer.json includes:
{
  "codespaces": {
    "prebuild": {
      "enabled": true
    }
  }
}
```

### Usage

**Open in Codespaces:**
1. Go to repository on GitHub
2. Click "Code" → "Codespaces" → "Create codespace"
3. Codespace starts in < 30 seconds (with prebuild)
4. Full development environment ready

**Advantages:**
- No local Docker Desktop needed
- Accessible from any device (browser, iPad, VS Code)
- Consistent hardware (not limited by laptop specs)
- Free tier: 60 hours/month

**Limitations:**
- Requires internet connection
- Network latency for file operations
- Cost beyond free tier: $0.36/hour

### Prebuild Strategy

**Trigger Prebuilds:**
- On push to main (`.devcontainer/**` changes)
- Weekly schedule (keep fresh)
- Manual trigger via workflow

**Benefits:**
- Codespace starts in 30 seconds (vs 3-5 minutes)
- All dependencies pre-installed
- Container layers cached

---

## Documentation

### Required Documentation

1. **`.devcontainer/README.md`** - Quick start, troubleshooting
2. **`.devcontainer/KIND_INTEGRATION.md`** - kind cluster usage
3. **`CLAUDE.md` update** - Reference dev container setup
4. **This document** - Comprehensive implementation plan

### Training Materials

**30-Minute Walkthrough:**
1. Introduction (5 min) - What is dev container, why use it
2. Setup (5 min) - Install Docker, VS Code, reopen in container
3. Demo (10 min) - Create kind cluster, deploy services, make changes
4. Troubleshooting (5 min) - Common issues, where to get help
5. Q&A (5 min)

**Self-Service Resources:**
- Video recording of walkthrough
- FAQ document
- Troubleshooting runbook
- Office hours schedule

---

## Success Criteria

### Phase-Specific Criteria

See individual phase sections above for detailed criteria.

### Project-Wide Criteria

**Technical:**
- ✅ Dev container builds successfully on all platforms
- ✅ 100% team can develop using dev container
- ✅ CI/CD uses same container image
- ✅ kind cluster integration works seamlessly
- ✅ Performance targets met

**Business:**
- ✅ Developer onboarding time reduced by 90%+
- ✅ "Works on my machine" issues eliminated
- ✅ Positive team feedback (survey: 4+/5 average)
- ✅ No increase in infrastructure costs

**Adoption:**
- ✅ 80%+ team adoption within 4 weeks
- ✅ 100% new developers use dev container
- ✅ Documentation complete and maintained

---

## Risk Assessment

### Risks and Mitigations

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Docker Desktop licensing costs | Medium | Low | Use Podman on Linux, free tier for others |
| Performance issues (macOS/Windows) | High | Medium | Use named volumes, allocate more resources |
| Team resistance to change | Medium | Medium | Pilot program, training, gradual rollout |
| CI/CD integration failures | Low | High | Test early, use devcontainers/ci action |
| Breaking changes to base images | Low | Medium | Pin versions, test before updating |
| Security vulnerabilities | Low | High | Monthly scans, use official images only |

---

## Conclusion

This implementation plan provides a comprehensive, phased approach to reintroducing development containers to the Red Dog project. The plan:

1. **Aligns with existing ADRs** - Complements kind, Helm, Nginx strategies
2. **Supports polyglot development** - .NET, Go, Python, Node.js, Vue.js
3. **Provides clear phases** - Each phase is independent and testable
4. **Includes validation** - Comprehensive testing and acceptance criteria
5. **Addresses risks** - Security, performance, adoption challenges

**Recommended Next Steps:**

1. **Review and approve** this plan with team
2. **Start Phase 1** (Foundation) - 8-12 hours of work
3. **Pilot with 2-3 developers** - Gather feedback
4. **Iterate and improve** - Based on pilot feedback
5. **Roll out to team** - Gradual adoption over 2-4 weeks

**Estimated Total Effort:**
- Phase 1: 8-12 hours
- Phase 2: 6-8 hours
- Phase 3: 10-12 hours (depends on ADR-0008 implementation)
- Phase 4: 6-8 hours
- Phase 5: 8-10 hours (optional)
- **Total: 38-50 hours** (5-7 days of focused work)

**Timeline:**
- Week 1: Phase 1 + pilot
- Week 2: Phase 2 + iteration
- Week 3: Phase 3 (requires ADR-0008 complete)
- Week 4: Phase 4 + team rollout
- Week 5: Phase 5 (optional optimizations)

---

**Document Status:** Draft  
**Last Updated:** 2025-11-09  
**Next Review:** After Phase 1 completion  
**Owner:** Red Dog Modernization Team

