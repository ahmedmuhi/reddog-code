# Red Dog Polyglot Development Container

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
- Go 1.23
- Python 3.12
- Node.js 24 + npm

### Kubernetes Tools
- kind (Kubernetes-in-Docker)
- kubectl (Kubernetes CLI)
- Helm (Package manager)

### Development Tools
- Dapr CLI
- Git
- GitHub CLI

### VS Code Extensions
- C# Dev Kit (.NET)
- Go (gopls)
- Python (Pylance, Ruff)
- ESLint & Prettier (JavaScript/Node.js)
- Vue.js (Volar)
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
http://localhost:8080

# OrderService
http://localhost:5100
```

## Troubleshooting

### Container Build Fails
```bash
# Rebuild from scratch
F1 → "Dev Containers: Rebuild Container Without Cache"
```

### kind Cluster Issues
```bash
# Check cluster
kind get clusters

# Recreate cluster
kind delete cluster --name reddog-local
kind create cluster --config kind-config.yaml
```

### Port Already in Use
Edit `.devcontainer/devcontainer.json` and change `forwardPorts` to alternative ports.

### Slow Performance (macOS/Windows)

**Why does this require 8GB RAM and 50GB disk?**

The dev container itself is lightweight (~2GB), but the complete development environment includes:

- **Docker Desktop:** ~1-2GB base memory
- **kind cluster:** ~2-4GB (runs Kubernetes control plane + worker nodes)
- **Dev container:** ~1-2GB (language runtimes, tools)
- **Running services:** ~1-2GB (when deployed to kind)
- **Build artifacts & caches:** ~20-30GB disk (Docker images, .NET packages, Go modules, npm packages)

**Resource Allocation:**

Allocate more resources to Docker Desktop if performance is slow:
- Settings → Resources → Memory: 8GB minimum, 16GB recommended
- Settings → Resources → CPUs: 2 minimum, 4+ recommended
- Settings → Resources → Disk: 50GB minimum, 100GB recommended

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
