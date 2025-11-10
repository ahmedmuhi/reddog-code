# Development Containers: Comprehensive Research Report 2025

**Date:** November 2025
**Research Focus:** Modern dev containers for polyglot microservices
**Context:** Red Dog Coffee microservices project (polyglot: .NET, Go, Python, Node.js, Vue.js)

## Executive Summary

Development Containers (Dev Containers) are containerized development environments that provide consistent, reproducible setups across teams and machines. They've evolved from a niche tool to industry standard practice in 2025, with major adoption by companies like Microsoft, Netflix, Spotify, and hundreds of open-source projects.

**Key Finding:** Dev containers are ideal for the Red Dog project's polyglot architecture, but require careful planning for security, performance, and CI/CD integration.

---

**⚠️ PROJECT-SPECIFIC NOTE - 2025-11-10**

While this research document provides comprehensive information about dev containers in general, the **Red Dog project has decided NOT to use dev containers** after implementation and testing revealed technical blockers.

**Reasons:**
- kind has bugs when running inside containers (nested Docker complexity - cluster recreation fails)
- k3d lacks production parity with AKS/EKS/GKE (removes cloud providers, uses SQLite)
- Native kind clusters on WSL2/macOS/Linux are simpler and more reliable for teaching authentic Kubernetes

**Current Approach:** Local development uses native kind clusters per ADR-0008.

**Status:** This document is preserved for educational purposes and future reference. See `.claude/sessions/2025-11-10-1030-dev-container-local-implementation.md` for full exploration history.

---

## Section 1: What Are Development Containers?

### Definition

A **Development Container** is a Docker container specifically configured to provide a fully functional development environment. It encapsulates:
- Operating system (typically Linux)
- Programming language runtimes and SDKs
- Build tools and package managers
- IDE extensions and configurations
- Pre-configured settings and workflows

The environment is defined in a standardized `devcontainer.json` file that can be committed to version control, ensuring every developer has identical setups.

### Core Concept

Development containers separate development environment configuration from the host machine. Instead of "install Node 24, Python 3.12, .NET 10, and 15 VS Code extensions locally," developers simply:
1. Clone the repository
2. Click "Reopen in Container" in VS Code
3. Work in a fully provisioned environment identical to teammates

### Relationship to Docker/Containers

**Dev Containers are NOT:**
- Production containers (no runtime optimization, includes heavy development tools)
- Regular Docker containers (enhanced with IDE integration and metadata)
- Replacement for Docker Compose (though they work together well)

**Dev Containers ARE:**
- Enhanced Docker containers with IDE-specific metadata
- Defined by the open Development Container Specification (containers.dev)
- Compatible with VS Code, JetBrains IDEs, GitHub Codespaces, and Azure DevOps
- A lightweight alternative to VMs for development environments

### Key Technologies Involved

| Technology | Role |
|------------|------|
| **Docker** | Container runtime and image building |
| **devcontainer.json** | Configuration file (JSON) defining the environment |
| **VS Code Dev Containers Extension** | IDE integration enabling seamless container workflows |
| **Development Container Specification** | Open standard (containers.dev) defining the format |
| **Docker Compose** | Optional orchestration for multi-container setups |
| **Features** | Reusable, shareable installation scripts for common tools |
| **Templates** | Pre-built starter configurations for popular tech stacks |
| **GitHub Codespaces** | Cloud-hosted dev containers with integrated workflows |

---

## Section 2: How They Work Technically

### Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│ Your Computer (Host Machine)                             │
├─────────────────────────────────────────────────────────┤
│  ┌────────────────────────────────────────────────────┐ │
│  │ VS Code / IDE                                      │ │
│  │  ├─ Dev Containers Extension                       │ │
│  │  └─ Remote Explorer UI                             │ │
│  └────────────────────────────────────────────────────┘ │
│                        ↓ (Docker API)                   │
│  ┌────────────────────────────────────────────────────┐ │
│  │ Docker Desktop / Docker CE                         │ │
│  └────────────────────────────────────────────────────┘ │
│                        ↓                                 │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ Dev Container (Docker)                                   │
├─────────────────────────────────────────────────────────┤
│  ┌────────────────────────────────────────────────────┐ │
│  │ Ubuntu 24.04 (or other base image)                 │ │
│  ├─ .NET 10 SDK / Go 1.23 / Python 3.12 / Node 24   │ │
│  ├─ Git, GitHub CLI, build tools                      │ │
│  ├─ VS Code Server (remote IDE backend)               │ │
│  ├─ Your project files (mounted from host)            │ │
│  └─ VS Code extensions running remotely               │ │
│  └────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

### Step-by-Step Workflow

#### 1. **Initialization** (First Time Only)
```
developer@host $ git clone <repo>
developer@host $ cd reddog-code
developer@host $ # VS Code detects .devcontainer/devcontainer.json
# VS Code shows: "Reopen in Container?" prompt
developer@host $ # Click "Reopen in Container"
```

#### 2. **Image Building**
```
1. Read .devcontainer/devcontainer.json
2. Parse Dockerfile or image reference
3. Build/pull Docker image (with caching)
4. Create container from image
5. Mount project directory into container
6. Install VS Code Server inside container
7. Install configured extensions remotely
8. Run postCreateCommand (restore dependencies, etc.)
```

#### 3. **Development**
```
IDE UI (host)          ←→ VS Code Server (container)
  ├─ File editing      ←→ File system access in container
  ├─ Extensions        ←→ Language servers (C#, Python, etc.)
  ├─ Terminal          ←→ Shell in container
  ├─ Debugger          ←→ Runtime inside container
  └─ Ports             ←→ Port forwarding (5100→5100)
```

#### 4. **Seamless Switching**
- Stop container, delete it → start fresh with same exact setup
- Switch to different branch → container rebuilds if needed
- Switch machines/cloud → identical environment on new machine

### File Structure

```
.devcontainer/
├── devcontainer.json          # Main configuration (required)
├── Dockerfile                 # Optional: custom Dockerfile
├── docker-compose.yml         # Optional: multi-container setup
└── scripts/
    ├── postCreateCommand.sh   # Setup after container creation
    └── init-database.sh       # Database initialization
```

### Configuration Flow

```json
{
  // 1. Base image or Dockerfile
  "image": "mcr.microsoft.com/devcontainers/dotnet:1-8.0",
  // OR
  "build": {
    "dockerfile": "Dockerfile",
    "args": { "VARIANT": "8.0" }
  },

  // 2. Container setup
  "features": {
    "ghcr.io/devcontainers/features/python:1": { "version": "3.12" },
    "ghcr.io/devcontainers/features/node:1": { "version": "24" }
  },

  // 3. IDE configuration
  "customizations": {
    "vscode": {
      "extensions": [
        "ms-dotnettools.csharp",
        "ms-python.python",
        "golang.go"
      ]
    }
  },

  // 4. Execution setup
  "forwardPorts": [5000, 5001, 5100, 5200, 8080],
  "postCreateCommand": "dotnet restore && npm install --prefix RedDog.UI"
}
```

### Extension Installation

**Local Extensions** (UI/Theme):
- Installed on host machine
- Apply to all workspaces

**Remote Extensions** (Development Tools):
- Installed inside container
- Only apply to that container
- Include language servers, linters, formatters

VS Code automatically routes extensions to correct locations.

---

## Section 3: Pros and Cons

### Advantages

#### 1. **Reproducibility & Consistency**
- **Every developer has identical environment** regardless of host OS
- Eliminates "works on my machine" problems
- Onboarding new team members: clone → reopen in container → develop
- New hires productive in minutes instead of hours

#### 2. **Isolation**
- **No pollution of host machine** with dependencies
- Multiple projects with conflicting requirements can coexist
- Clean machine always available (restart container)
- OS-agnostic: Linux developers, Mac developers, Windows developers all use same container

#### 3. **Polyglot Support**
- Single dev container for projects with .NET, Go, Python, Node.js, Vue.js
- No complex environment variables or PATH manipulation
- Each language's latest LTS versions pre-configured

#### 4. **Cloud-Ready Development**
- **Works locally, moves to cloud seamlessly**
- Same container in dev, CI, and (nearly) production
- GitHub Codespaces integration: web-based dev from any browser
- Enables work from anywhere (Chromebook, iPad with SSH, public WiFi)

#### 5. **CI/CD Parity**
- **If tests pass locally, they'll pass in CI**
- Use identical container image in GitHub Actions, GitLab CI, etc.
- `devcontainers/ci` GitHub Action simplifies integration
- Reduces debugging cycles ("works locally but fails in CI")

#### 6. **Security Benefits**
- No secrets on local machine (credential injection at runtime)
- Container runs as non-root user (configurable)
- Audit trail: every tool version is documented in version control
- Supply chain transparency: image layers documented

#### 7. **Performance Improvements**
- Faster project startup vs. setup from scratch
- Caching mechanisms (named volumes, Docker layers)
- Pre-built images reduce build time
- Extension loading happens in parallel

#### 8. **Team Collaboration**
- Create role-based configurations (frontend-only, backend-only, full-stack)
- Easily share custom extensions and settings
- Reduce maintenance burden for open-source projects
- Standardize across organization

### Disadvantages & Limitations

#### 1. **Learning Curve**
- Developers must understand Docker basics
- Additional VS Code extension to learn
- Debugging Docker issues adds complexity
- Not beginner-friendly for developers new to containers

#### 2. **Performance Overhead**
- **Docker Desktop on macOS/Windows has I/O overhead**
  - Slower file operations than native (10-50x for some operations)
  - Named volumes help but create inconsistency between host/container
- **Docker for Linux**: minimal overhead but requires Linux expertise
- **Initial setup slower** than local development
- **Container startup time**: 30 seconds to 2 minutes depending on size

#### 3. **File Sync Complexity**
- **Bind mounts** (default): slow on macOS/Windows but always in sync
- **Named volumes**: fast but host can't see container changes easily
- **Volume mounts**: added complexity in Docker Compose scenarios
- Switching between strategies creates confusion

#### 4. **GPU/Hardware Limitations**
- GPU access requires special configuration
- Serial device access (Arduino, etc.) requires host mapping
- Some specialized hardware support is limited
- Not ideal for embedded systems development

#### 5. **Disk Space Requirements**
- Docker images can be large (1-3GB typical)
- Multiple base images multiply disk usage
- Named volumes accumulate quickly
- May be problematic on laptops with limited storage

#### 6. **Debugging Complexity**
- Debugging tools must be configured inside container
- Breakpoint setting more complex than local debugging
- Some IDE features work differently remotely
- Docker Desktop issues can block entire team

#### 7. **Network Complications**
- Port forwarding can conflict with host processes
- VPN access requires special configuration
- Private registry authentication adds setup steps
- Docker Desktop networking limitations on macOS

#### 8. **Windows-Specific Issues**
- WSL2 integration can be finicky
- Drive sharing requires permissions configuration
- Some Windows security policies block Docker
- Path handling (Windows vs. Linux paths) causes confusion

#### 9. **Maintenance Overhead**
- Images require regular updates (security patches)
- Base image upgrades can break builds
- Feature versions may become deprecated
- Requires monitoring of base image changelog

#### 10. **Resource Consumption**
- Docker Desktop runs hypervisor (uses 2-4GB RAM)
- Container overhead on top of IDE overhead
- Can make laptops slow if under-resourced
- Worse on shared infrastructure

### Use Cases: When Dev Containers Excel

✅ **Ideal For:**
- Teams with diverse operating systems (Linux, Mac, Windows)
- Polyglot projects (multiple programming languages)
- Cloud-native development (Kubernetes, Docker, containers already used)
- Open-source projects (reduce contributor setup friction)
- Microservices architectures (multiple services with different requirements)
- Projects requiring specific version combinations (Python 3.12 + Node 24 + .NET 10)
- Organizations standardizing on containers
- Teaching/mentoring (instant, consistent student environments)
- CI/CD environments (same container everywhere)

✅ **Red Dog Specific Benefits:**
- Polyglot stack: .NET, Go, Python, Node.js can coexist
- Microservices: each developer only needs specific services
- Dapr integration: consistent local development with production-like setup
- Onboarding: new developers productive immediately
- Testing: "does it work in the container?" validates production readiness

### Use Cases: When Dev Containers Struggle

❌ **Not Ideal For:**
- Embedded systems development (hardware-centric)
- Heavy machine learning workflows (GPU-intensive)
- Projects requiring very low latency file I/O
- Teams on very limited hardware (< 8GB RAM)
- Organizations with strict host machine policies
- Real-time system development
- Projects with complex serial/USB device requirements
- Developers uncomfortable with Docker/containers

---

## Section 4: Alternatives to Dev Containers

### Comparison Matrix

| Aspect | Dev Containers | Vagrant | Nix | Docker Compose | Local Setup | GitHub Codespaces |
|--------|---|---|---|---|---|---|
| **Setup Time** | 5-15 min | 15-30 min | 10-20 min | 5-10 min | 30 min - 2 hrs | 2-3 min |
| **Reproducibility** | Excellent | Good | Excellent | Good | Poor | Excellent |
| **Resource Usage** | Medium | High (VM) | Low | Medium | Low | N/A (cloud) |
| **Learning Curve** | Moderate | High | Very High | Moderate | Low | Low |
| **IDE Integration** | Excellent | None | None | Basic | Perfect | Excellent |
| **Multi-OS Support** | Excellent | Good | Good | Good | Poor | Excellent |
| **Cost** | Free | Free | Free | Free | Free | $0-40/month |
| **Cloud Ready** | Yes | No | No | Yes | No | Yes |

### Vagrant

**What It Is:** Tool for building complete development environments as lightweight VMs.

**Advantages:**
- True isolation (complete OS per environment)
- Works with VirtualBox, VMware, Hyper-V, AWS, etc.
- No Docker knowledge required
- Good for team consistency

**Disadvantages:**
- Resource-intensive (full OS per VM)
- Slow startup times (30-60 seconds minimum)
- Larger disk footprint (1-5GB per VM)
- Declining adoption (Docker won the container war)
- No IDE integration (must SSH or use XWindow forwarding)

**When to Use:** Teams that already have Vagrant infrastructure, projects requiring full OS isolation, organizations with mature VM management practices.

**Comparison to Dev Containers:** Dev containers are lighter, faster, have better IDE integration, but Vagrant provides stronger isolation.

### Nix & Nix Flakes

**What It Is:** Functional package manager enabling declarative, reproducible environments via `flake.nix` or `shell.nix`.

**Advantages:**
- Extremely reproducible (cryptographic hashes of inputs)
- Pure functional approach (no side effects)
- No VM overhead (native execution)
- Growing mindshare among systems engineers
- Works across Linux, macOS (native), WSL2

**Disadvantages:**
- Very steep learning curve (unfamiliar paradigm for most developers)
- Documentation can be dense
- Smaller ecosystem than containers
- Less mainstream industry adoption
- Requires Nix installation on host OS

**When to Use:** Teams with Nix expertise, systems engineers, projects requiring extreme reproducibility, macOS/Linux teams not using containers.

**Comparison to Dev Containers:** Nix is more reproducible and lightweight but harder to learn. Dev containers have better tooling support and IDE integration.

### Docker Compose (Standalone)

**What It Is:** Orchestration tool for multi-container applications without dev container metadata.

**Advantages:**
- Simpler than dev containers (just Docker Compose, no IDE metadata)
- Easy multi-service setup (app + database + cache)
- Fast once images are built
- Works on any OS with Docker

**Disadvantages:**
- Minimal IDE integration (manual config)
- No remote extension support
- Developers still need local tools installed
- Not designed for development workflow
- Manual port mapping and network setup

**When to Use:** Projects using Docker Compose for production, teams only needing multi-service setup, projects not requiring IDE integration.

**Comparison to Dev Containers:** Docker Compose is simpler but lacks IDE integration. Dev containers + Docker Compose together are more powerful than either alone.

### Local Setup (No Containers)

**What It Is:** Traditional approach: install everything locally on your machine.

**Advantages:**
- Maximum performance (no overhead)
- Familiar to all developers
- Works on offline systems
- No learning curve for containers

**Disadvantages:**
- Environment inconsistency across team
- Complex onboarding ("install Node, Python, .NET, 15 extensions...")
- Risk of version conflicts
- Cannot quickly switch to different project requirements
- No cloud development support
- Hard to achieve "clean state"

**When to Use:** Small teams on identical machines, simple projects, offline-required environments, organizations with strict security policies preventing containers.

**Comparison to Dev Containers:** No comparison for modern development—local setup is strictly inferior for team environments.

### GitHub Codespaces

**What It Is:** Cloud-hosted version of dev containers (runs container on GitHub's cloud, access via browser or VS Code).

**Advantages:**
- No Docker Desktop installation needed
- Instant environment (no local build)
- Accessible from any device (iPad, Chromebook)
- Always latest tools (cloud-managed)
- GitHub-integrated (native workflows)
- Consistent hardware (no local resource limits)

**Disadvantages:**
- Monthly cost ($0.36/hour compute, or free tier: 60 hours/month)
- Requires internet connection
- Network latency for file operations
- Data sovereignty concerns (code in GitHub cloud)
- Dependent on GitHub availability

**When to Use:** Teams using GitHub, distributed developers, contributors using shared hardware, organizations with limited device budgets.

**Comparison to Dev Containers:** Codespaces *uses* dev containers (same `devcontainer.json`). Dev Containers = local + cloud hybrid, Codespaces = cloud-only.

### JetBrains Fleet / IDE-Specific Solutions

**What It Is:** IDE-native remote development (IntelliJ, PyCharm, etc. without VS Code).

**Advantages:**
- Native IDE experience (not browser-based)
- Better performance than Codespaces
- Mature language support
- Rich debugging tools

**Disadvantages:**
- License required (many tools)
- Proprietary (not open standard)
- Only works with specific IDEs
- Not compatible with dev containers spec
- Higher cost

**When to Use:** Organizations with JetBrains licenses, teams standardized on IntelliJ ecosystem.

**Comparison to Dev Containers:** Different approach (IDE-specific vs. open standard). Dev Containers have broader tool support.

### Recommendation for Red Dog

**Use Dev Containers + Docker Compose together:**
1. **Dev Container** for IDE integration and base tools (.NET 10, shared tools)
2. **Docker Compose** for services (Redis, PostgreSQL) and multi-language support (Go, Python services)
3. **This combination provides:**
   - IDE integration (vs. pure Docker Compose)
   - Multi-service setup (vs. single container)
   - Polyglot support (each service in appropriate language)
   - CI/CD parity (same container images)
   - Cloud migration path (Kubernetes uses same images)

---

## Section 5: Implementation Steps (2025 Guide)

### Prerequisites

1. **Docker Desktop or Docker CE**
   - Windows: Docker Desktop 4.26+
   - macOS: Docker Desktop 4.26+
   - Linux: Docker CE, Docker Compose v2

2. **VS Code (1.94+) with Dev Containers Extension**
   ```bash
   # Install extension: "Dev Containers" by Microsoft
   # or via command line:
   code --install-extension ms-vscode-remote.remote-containers
   ```

3. **Git** (to clone and commit `.devcontainer/` directory)

4. **Adequate Resources**
   - Minimum: 8GB RAM, 50GB disk space
   - Recommended: 16GB RAM, 100GB+ disk space
   - macOS/Windows: allocate 4GB+ RAM to Docker Desktop

### Step-by-Step Walkthrough

#### Step 1: Create `.devcontainer/` Directory

```bash
cd /path/to/project
mkdir -p .devcontainer
```

#### Step 2: Create `devcontainer.json`

Start with official template:

```bash
# Option A: Use VS Code command
# - Open VS Code
# - Cmd+Shift+P (Ctrl+Shift+P on Linux/Windows)
# - Type "Add Dev Container Configuration Files"
# - Select your tech stack

# Option B: Manually create .devcontainer/devcontainer.json
```

#### Step 3: Choose Base Image or Dockerfile

**Option A: Use Official Microsoft Image** (fastest, recommended)

```json
{
  "name": "RedDog Development",
  "image": "mcr.microsoft.com/devcontainers/dotnet:1-8.0-ubuntu-24.04",

  "features": {
    "ghcr.io/devcontainers/features/go:1": { "version": "1.23" },
    "ghcr.io/devcontainers/features/python:1": { "version": "3.12" },
    "ghcr.io/devcontainers/features/node:1": { "version": "24" }
  }
}
```

**Option B: Use Custom Dockerfile** (more control)

Create `.devcontainer/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0-ubuntu-24.04

# Install additional tools
RUN apt-get update && apt-get install -y \
    golang-1.23 \
    python3.12 \
    nodejs \
    npm \
    && rm -rf /var/lib/apt/lists/*
```

Then in `devcontainer.json`:

```json
{
  "name": "RedDog Development",
  "build": {
    "dockerfile": "Dockerfile",
    "context": "."
  }
}
```

#### Step 4: Configure Development Tools

Add to `devcontainer.json`:

```json
{
  "customizations": {
    "vscode": {
      "extensions": [
        "ms-dotnettools.csharp",
        "ms-python.python",
        "golang.go",
        "ms-vscode.makefile-tools",
        "GitHub.copilot",
        "eamodio.gitlens"
      ],
      "settings": {
        "dotnet.defaultSolutionOrFolder": "/workspace/RedDog.sln",
        "[csharp]": {
          "editor.defaultFormatter": "ms-dotnettools.csharp"
        }
      }
    }
  }
}
```

#### Step 5: Configure Ports & Environment

```json
{
  "forwardPorts": [
    5000, 5001,      // .NET default
    5100, 5180,      // OrderService
    5200, 5280,      // MakeLineService
    5300, 5380,      // ReceiptGenerationService
    5400, 5480,      // LoyaltyService
    5700, 5780,      // AccountingService
    8080             // Vue.js UI
  ],

  "portsAttributes": {
    "5000-5001": { "label": ".NET Debug", "onAutoForward": "notify" },
    "5100-5180": { "label": "OrderService", "onAutoForward": "notify" },
    "8080": { "label": "UI", "onAutoForward": "openBrowser" }
  }
}
```

#### Step 6: Add Post-Create Command

```json
{
  "postCreateCommand": "bash .devcontainer/scripts/postCreateCommand.sh"
}
```

Create `.devcontainer/scripts/postCreateCommand.sh`:

```bash
#!/bin/bash
set -e

echo "Setting up development environment..."

# Restore .NET packages
dotnet restore RedDog.sln

# Install UI dependencies
cd RedDog.UI
npm install
cd ..

# Initialize Dapr (if not already initialized)
dapr init --kubernetes=false || true

echo "Development environment ready!"
```

#### Step 7: (Optional) Add Docker Compose for Multi-Container Setup

Create `.devcontainer/docker-compose.yml`:

```yaml
version: '3.8'

services:
  dev:
    build:
      context: ..
      dockerfile: .devcontainer/Dockerfile
    volumes:
      - ..:/workspace:cached
      - /workspace/.git
      - reddog-dotnet:/root/.cache/dotnet
      - reddog-npm:/workspace/RedDog.UI/node_modules
    ports:
      - "5000:5000"
      - "5001:5001"
      - "5100:5100"
      - "8080:8080"
    environment:
      DAPR_HOST_IP: "host.docker.internal"
    command: sleep infinity

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - reddog-redis:/data

  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: reddog
      POSTGRES_PASSWORD: reddog_dev_only
      POSTGRES_DB: reddog
    ports:
      - "5432:5432"
    volumes:
      - reddog-postgres:/var/lib/postgresql/data

volumes:
  reddog-dotnet:
  reddog-npm:
  reddog-redis:
  reddog-postgres:
```

Then update `devcontainer.json`:

```json
{
  "dockerComposeFile": "docker-compose.yml",
  "service": "dev",
  "workspaceFolder": "/workspace"
}
```

#### Step 8: Test the Setup

1. Open project in VS Code
2. Cmd+Shift+P → "Reopen in Container"
3. VS Code builds/pulls image and starts container (first time: 3-5 minutes)
4. Once connected, verify:

```bash
# In VS Code terminal (which is now inside container)
dotnet --version        # Should show .NET 8.0
go version              # Should show go1.23
python --version        # Should show Python 3.12
node --version          # Should show v24.x
dapr --version          # Should show 1.x
```

#### Step 9: Commit to Version Control

```bash
git add .devcontainer/
git commit -m "feat: add dev container configuration for polyglot development"
git push
```

---

## Section 6: Configuration File Structure & Examples

### Complete `devcontainer.json` Reference

```json
{
  // ============ IDENTIFICATION ============
  "name": "RedDog Development",
  "id": "reddog-polyglot",

  // ============ IMAGE SPECIFICATION ============
  // Choice A: Use pre-built image
  "image": "mcr.microsoft.com/devcontainers/dotnet:1-8.0",

  // Choice B: Build from Dockerfile
  // "build": {
  //   "dockerfile": "Dockerfile",
  //   "context": ".",
  //   "args": {
  //     "VARIANT": "8.0"
  //   },
  //   "cacheFrom": "mcr.microsoft.com/devcontainers/dotnet:1-8.0"
  // },

  // ============ FEATURES (Reusable Tools) ============
  "features": {
    "ghcr.io/devcontainers/features/go:1": {
      "version": "1.23"
    },
    "ghcr.io/devcontainers/features/python:1": {
      "version": "3.12",
      "installTools": true
    },
    "ghcr.io/devcontainers/features/node:1": {
      "version": "24"
    },
    "ghcr.io/devcontainers/features/github-cli:1": {
      "version": "2.54"
    }
  },

  // ============ USER & PERMISSIONS ============
  "remoteUser": "vscode",
  "remoteEnv": {
    "PATH": "/usr/local/go/bin:${PATH}"
  },

  // ============ PORT FORWARDING ============
  "forwardPorts": [5000, 5001, 5100, 5180, 5200, 5280, 8080],
  "portsAttributes": {
    "5000-5001": { "label": ".NET Runtime", "onAutoForward": "notify" },
    "5100-5180": { "label": "OrderService", "onAutoForward": "notify" },
    "5200-5280": { "label": "MakeLineService", "onAutoForward": "notify" },
    "8080": { "label": "UI", "onAutoForward": "openBrowser" }
  },

  // ============ IDE CUSTOMIZATION ============
  "customizations": {
    "vscode": {
      // Extensions to install in container
      "extensions": [
        "ms-dotnettools.csharp",           // C# support
        "ms-dotnettools.vscode-dotnet-runtime",
        "ms-python.python",                // Python
        "ms-python.debugpy",
        "ms-python.vscode-pylance",
        "golang.go",                       // Go
        "ms-vscode.makefile-tools",        // Makefile
        "GitHub.copilot",                  // AI assistance
        "eamodio.gitlens",                 // Git integration
        "ms-vscode.remote-explorer",       // Container explorer
        "redhat.vscode-yaml",              // YAML for Docker Compose
        "charliermarsh.ruff"               // Python linter
      ],

      // VS Code settings within container
      "settings": {
        "dotnet.defaultSolutionOrFolder": "/workspace/RedDog.sln",
        "[csharp]": {
          "editor.defaultFormatter": "ms-dotnettools.csharp",
          "editor.formatOnSave": true
        },
        "[python]": {
          "editor.defaultFormatter": "charliermarsh.ruff",
          "editor.formatOnSave": true
        },
        "[go]": {
          "editor.defaultFormatter": "golang.go",
          "editor.formatOnSave": true
        },
        "files.watcherExclude": {
          "**/node_modules/**": true,
          "**/.git/objects/**": true,
          "**/.git/subtree-cache/**": true
        }
      }
    }
  },

  // ============ LIFECYCLE COMMANDS ============
  "postCreateCommand": "bash .devcontainer/scripts/postCreateCommand.sh",
  "postStartCommand": "dapr init --kubernetes=false || true",
  // "initializeCommand": "git submodule update --init --recursive",

  // ============ CONTAINER OPTIONS ============
  "containerEnv": {
    "ASPNETCORE_ENVIRONMENT": "Development",
    "DOTNET_CLI_TELEMETRY_OPTOUT": "1"
  },

  // ============ MOUNTS ============
  "mounts": [
    "source=${localEnv:HOME}${localEnv:USERPROFILE}/.ssh,target=/home/vscode/.ssh,readonly",
    "source=${localEnv:HOME}${localEnv:USERPROFILE}/.gitconfig,target=/home/vscode/.gitconfig,readonly"
  ],

  // ============ SECURITY ============
  "privileged": false,
  "init": true,
  "capAdd": [],  // Leave empty for least privilege

  // ============ ADVANCED ============
  "hostNamePrefix": "reddog-dev",
  "wsl": true,  // WSL2 support on Windows

  // Uncomment for multi-container setup:
  // "dockerComposeFile": "docker-compose.yml",
  // "service": "dev",
  // "workspaceFolder": "/workspace"
}
```

### Minimal Configuration (Simplest Setup)

For teams wanting minimal config:

```json
{
  "name": "RedDog",
  "image": "mcr.microsoft.com/devcontainers/dotnet:1-8.0",

  "features": {
    "ghcr.io/devcontainers/features/go:1": {},
    "ghcr.io/devcontainers/features/python:1": {}
  },

  "forwardPorts": [5000, 5001, 5100, 5200, 8080],
  "postCreateCommand": "dotnet restore RedDog.sln"
}
```

### .NET Specific Configuration

```json
{
  "name": ".NET 10 Development",
  "image": "mcr.microsoft.com/devcontainers/dotnet:1-10.0-ubuntu-24.04",

  "customizations": {
    "vscode": {
      "extensions": [
        "ms-dotnettools.csharp",
        "ms-dotnettools.vscode-dotnet-runtime",
        "ms-dotnettools.csharp-dev-kit"
      ],
      "settings": {
        "dotnet.defaultSolutionOrFolder": "/workspace/RedDog.sln",
        "[csharp]": {
          "editor.formatOnSave": true,
          "editor.defaultFormatter": "ms-dotnettools.csharp"
        }
      }
    }
  },

  "forwardPorts": [5000, 5001],
  "postCreateCommand": "dotnet restore RedDog.sln && dotnet build RedDog.sln"
}
```

### Polyglot Microservices Configuration

```json
{
  "name": "RedDog Polyglot Microservices",

  "image": "mcr.microsoft.com/devcontainers/dotnet:1-8.0",

  "features": {
    "ghcr.io/devcontainers/features/go:1": { "version": "1.23" },
    "ghcr.io/devcontainers/features/python:1": { "version": "3.12" },
    "ghcr.io/devcontainers/features/node:1": { "version": "24" },
    "ghcr.io/devcontainers/features/github-cli:1": {},
    "ghcr.io/devcontainers/features/docker-in-docker:1": {}
  },

  "customizations": {
    "vscode": {
      "extensions": [
        "ms-dotnettools.csharp",
        "golang.go",
        "ms-python.python",
        "ms-vscode.makefile-tools",
        "eamodio.gitlens",
        "GitHub.copilot"
      ]
    }
  },

  "forwardPorts": [5000, 5001, 5100, 5180, 5200, 5280, 5300, 5380, 5400, 5480, 5700, 5780, 8080],

  "postCreateCommand": "bash .devcontainer/scripts/postCreateCommand.sh",

  "dockerComposeFile": "docker-compose.yml",
  "service": "dev",
  "workspaceFolder": "/workspace"
}
```

### `postCreateCommand.sh` Script Example

```bash
#!/bin/bash

# Exit on error
set -e

echo "🚀 Setting up RedDog development environment..."

# Section 1: .NET Setup
echo "📦 Restoring .NET packages..."
dotnet restore RedDog.sln 2>/dev/null

# Section 2: Node.js Setup
echo "📦 Installing UI dependencies..."
cd RedDog.UI
npm install --silent
cd ..

# Section 3: Dapr Setup
echo "🔗 Initializing Dapr (if needed)..."
dapr init --kubernetes=false --slim 2>/dev/null || true

# Section 4: Verify Setup
echo "✅ Verifying environment..."
echo "  .NET version: $(dotnet --version)"
echo "  Go version: $(go version)"
echo "  Python version: $(python3 --version)"
echo "  Node version: $(node --version)"

echo "✨ Development environment ready!"
echo "📝 Next steps:"
echo "   1. Run services: dapr run --app-id orderservice ..."
echo "   2. Start UI: npm run serve --prefix RedDog.UI"
echo "   3. View REST samples: open rest-samples/"
```

---

## Section 7: Docker Compose Integration for Multi-Container Setup

### When to Use Docker Compose with Dev Containers

Use Docker Compose when you need:
- **Multiple services** (database, cache, message queue)
- **Service isolation** (MongoDB in one container, Redis in another)
- **Volume management** (persistent database data)
- **Service networking** (containers communicate internally)

### Basic Multi-Service Setup

`.devcontainer/docker-compose.yml`:

```yaml
version: '3.8'

services:
  # Development container with all tools
  dev:
    build:
      context: ..
      dockerfile: .devcontainer/Dockerfile

    volumes:
      # Mount project directory (source code)
      - ..:/workspace:cached
      # Prevent host node_modules from interfering
      - /workspace/.git
      # Named volumes for performance
      - reddog-node-modules:/workspace/RedDog.UI/node_modules
      - reddog-dotnet-cache:/root/.cache/dotnet
      - reddog-nuget:/root/.nuget

    ports:
      # .NET services
      - "5000:5000"      # Default HTTP
      - "5001:5001"      # Default HTTPS
      - "5100:5100"      # OrderService
      - "5180:5180"      # OrderService Dapr
      - "5200:5200"      # MakeLineService
      - "5280:5280"      # MakeLineService Dapr
      - "8080:8080"      # Vue.js UI

    environment:
      ASPNETCORE_ENVIRONMENT: Development
      DOTNET_CLI_TELEMETRY_OPTOUT: "1"

    networks:
      - reddog

    # Keep container running when no command specified
    command: sleep infinity

    depends_on:
      - redis
      - postgres

  # Redis cache
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - reddog-redis:/data
    networks:
      - reddog
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 5s
      timeout: 3s

  # PostgreSQL database
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: reddog
      POSTGRES_PASSWORD: reddog_dev_only
      POSTGRES_DB: reddog
    ports:
      - "5432:5432"
    volumes:
      - reddog-postgres:/var/lib/postgresql/data
      - ./../scripts/init-db.sql:/docker-entrypoint-initdb.d/init.sql
    networks:
      - reddog
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U reddog"]
      interval: 10s
      timeout: 5s

volumes:
  reddog-node-modules:
  reddog-dotnet-cache:
  reddog-nuget:
  reddog-redis:
  reddog-postgres:

networks:
  reddog:
    driver: bridge
```

### Updated `devcontainer.json` for Docker Compose

```json
{
  "name": "RedDog Multi-Service Development",

  "dockerComposeFile": "docker-compose.yml",
  "service": "dev",
  "workspaceFolder": "/workspace",

  "features": {
    "ghcr.io/devcontainers/features/go:1": {},
    "ghcr.io/devcontainers/features/python:1": {},
    "ghcr.io/devcontainers/features/node:1": {}
  },

  "forwardPorts": [5000, 5001, 5100, 5200, 8080, 6379, 5432],

  "customizations": {
    "vscode": {
      "extensions": [
        "ms-dotnettools.csharp",
        "golang.go",
        "ms-python.python",
        "ckolkman.vscode-postgres"
      ]
    }
  },

  "postCreateCommand": "bash .devcontainer/scripts/postCreateCommand.sh"
}
```

### Service Connectivity

Inside the container:
- **Redis:** `redis://redis:6379`
- **PostgreSQL:** `postgresql://reddog:reddog_dev_only@postgres:5432/reddog`
- **From host:** `localhost:6379`, `localhost:5432`

---

## Section 8: Best Practices (2025)

### 1. **Use Official Base Images**

✅ **Good:**
```json
{
  "image": "mcr.microsoft.com/devcontainers/dotnet:1-8.0-ubuntu-24.04"
}
```

❌ **Avoid:**
```json
{
  "image": "ubuntu:latest"
  // Then installing .NET manually
}
```

**Why:** Official images are:
- Optimized for development
- Security patches applied
- Tested by Microsoft
- Include IDE integration built-in

### 2. **Leverage Features for Common Tools**

✅ **Good:**
```json
{
  "features": {
    "ghcr.io/devcontainers/features/go:1": { "version": "1.23" }
  }
}
```

❌ **Avoid:**
```json
{
  "build": {
    "dockerfile": "Dockerfile"
  }
  // Then manually installing Go in Dockerfile
}
```

**Why:** Features are:
- Maintained by DevContainers community
- Version-pinned for reproducibility
- Cached properly
- Tested across platforms
- Self-contained scripts

### 3. **Pin Versions Explicitly**

✅ **Good:**
```json
{
  "image": "mcr.microsoft.com/devcontainers/dotnet:1-8.0-ubuntu-24.04",
  "features": {
    "ghcr.io/devcontainers/features/go:1": { "version": "1.23.0" }
  }
}
```

❌ **Avoid:**
```json
{
  "image": "mcr.microsoft.com/devcontainers/dotnet:latest",
  "features": {
    "ghcr.io/devcontainers/features/go:1": {}  // Uses latest
  }
}
```

**Why:** Pinning prevents:
- Unexpected breaking changes
- CI/CD failures due to new versions
- "Works locally, fails in CI" scenarios
- Team divergence if teammates build at different times

### 4. **Use Non-Root User**

✅ **Good:**
```json
{
  "remoteUser": "vscode"
}
```

The default `mcr.microsoft.com/devcontainers/dotnet` image includes `vscode` user (UID 1000).

❌ **Avoid:**
```dockerfile
# In Dockerfile
USER root  # Or running as root throughout
```

**Why:**
- Prevents accidental system modifications
- Matches production least-privilege model
- Safer if container escapes
- File permissions more predictable

### 5. **Optimize for Performance**

#### Named Volumes Over Bind Mounts

```yaml
# docker-compose.yml
services:
  dev:
    volumes:
      # :cached improves macOS/Windows performance
      - ..:/workspace:cached

      # Named volumes even faster for specific folders
      - node-modules:/workspace/RedDog.UI/node_modules
      - dotnet-cache:/root/.cache/dotnet

volumes:
  node-modules:
  dotnet-cache:
```

#### Layer Caching

```json
{
  "build": {
    "dockerfile": "Dockerfile",
    "context": ".",
    "cacheFrom": [
      "type=registry,ref=ghcr.io/your-org/reddog:latest",
      "type=local,src=../docker-cache"
    ]
  }
}
```

#### Minimize Extensions

```json
{
  "customizations": {
    "vscode": {
      "extensions": [
        // Only essential extensions
        // Each extension adds 10-50MB and startup time
        "ms-dotnettools.csharp",
        "eamodio.gitlens"
        // NOT: every theme, formatter, linter you've ever used
      ]
    }
  }
}
```

### 6. **CI/CD Parity**

#### Same Image in Local and CI

`devcontainer.json`:
```json
{
  "build": {
    "dockerfile": "Dockerfile",
    "context": "."
  }
}
```

`.github/workflows/ci.yml`:
```yaml
uses: devcontainers/ci@v0
with:
  imageName: ghcr.io/your-org/reddog
  cacheEndpoint: ghcr.io
```

**Benefit:** Same exact container in development and CI guarantees consistent results.

### 7. **Environment-Specific Configurations**

#### Multiple Dev Containers for Different Roles

```
.devcontainer/
├── devcontainer.json              # Full stack (default)
├── docker-compose.yml
├── devcontainer.frontend.json     # Frontend-only (lighter)
├── devcontainer.backend.json      # Backend-only
└── scripts/
```

In VS Code: "Dev Containers: Open Folder in Container" → pick configuration

### 8. **Security Best Practices**

#### Don't Commit Secrets

✅ **Good:**
```json
{
  "postCreateCommand": "echo 'Export environment variables from CI/CD or local .env'"
}
```

❌ **Avoid:**
```json
{
  "containerEnv": {
    "DATABASE_PASSWORD": "super_secret_123"  // NEVER!
  }
}
```

#### Use Secret Management Tools

```bash
# Load from .env.local (gitignored)
export $(cat .env.local | grep -v '^#' | xargs)
```

Or GitHub Codespaces secrets (GitHub UI).

#### Minimal Capabilities

```json
{
  "privileged": false,              // Never privileged
  "capAdd": [],                     // No extra capabilities
  "securityOpt": ["no-new-privileges"]
}
```

### 9. **Documentation and Onboarding**

Create `.devcontainer/README.md`:

```markdown
# Development Container Setup

## Quick Start
1. Install Docker Desktop
2. Open in VS Code
3. Press "Reopen in Container"

## What's Included
- .NET 8.0
- Go 1.23
- Python 3.12
- Node.js 24
- Dapr CLI

## Running Services
- OrderService: `dapr run --app-id orderservice ...`
- UI: `npm run serve --prefix RedDog.UI`

## Troubleshooting
- Slow performance on macOS? Check Docker Desktop memory allocation
- Port already in use? Edit `devcontainer.json` forwardPorts
```

### 10. **Keep Images Lightweight**

✅ **Good Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0-ubuntu-24.04

RUN apt-get update && apt-get install -y \
    jq \
    && rm -rf /var/lib/apt/lists/*

# Result: ~500MB
```

❌ **Heavy Dockerfile:**
```dockerfile
FROM ubuntu:24.04

RUN apt-get update && apt-get install -y \
    dotnet-sdk-8.0 \
    nodejs \
    python3.12 \
    golang-1.23 \
    build-essential \
    # ... 50 more packages

# Result: ~2GB
```

### 11. **For Polyglot Projects Specifically**

#### Single Container vs. Multiple Containers

**Approach 1: Single Dev Container with All Languages** (Recommended for Red Dog)
```json
{
  "image": "mcr.microsoft.com/devcontainers/dotnet:1-8.0",
  "features": {
    "ghcr.io/devcontainers/features/go:1": {},
    "ghcr.io/devcontainers/features/python:1": {},
    "ghcr.io/devcontainers/features/node:1": {}
  }
}
```
- Pros: Simple, all tools available
- Cons: Larger image, longer startup

**Approach 2: Multiple Containers per Service** (For larger teams)
```yaml
services:
  dev-dotnet:
    image: mcr.microsoft.com/devcontainers/dotnet:1-8.0
  dev-go:
    image: mcr.microsoft.com/devcontainers/go:latest
  dev-python:
    image: mcr.microsoft.com/devcontainers/python:latest
```
- Pros: Minimal dependencies, faster startup
- Cons: Complex switching, redundant tools

#### Recommendation: Use Approach 1 (single container) with Docker Compose for services

### 12. **Monitoring and Maintenance**

#### Check for Outdated Base Images

```bash
# Schedule weekly checks
docker pull mcr.microsoft.com/devcontainers/dotnet:1-8.0

# If new version available, update devcontainer.json
# Test locally, commit, push
```

#### Track Image Size

```bash
# After build
docker images | grep devcontainers

# Keep under 1GB for good performance
```

---

## Section 9: Security Considerations

### Threat Model

| Threat | Mitigation |
|--------|-----------|
| **Compromised dependencies** | Pin versions, use official images, regular updates |
| **Secrets leaked in image** | Use environment variables, never commit secrets |
| **Container escape** | Run non-root, no privileged mode, minimal capabilities |
| **Host filesystem access** | Limit mounts, use readonly where possible |
| **Supply chain attack** | Use signed images, verify checksums, trusted registries |
| **Network exposure** | Don't expose ports unnecessarily, use private networks |

### Security Checklist

- [ ] Specify exact base image version (never `latest`)
- [ ] Run container as non-root user (`remoteUser: vscode`)
- [ ] Don't use `privileged: true`
- [ ] Don't mount `/var/run/docker.sock` unless absolutely necessary
- [ ] Review mounts: should they be readonly?
- [ ] Don't hardcode secrets in `devcontainer.json`
- [ ] Don't commit `.env` files with real secrets
- [ ] Use read-only mounts for configuration files
- [ ] Limit port forwarding to necessary ports
- [ ] Keep base images updated monthly
- [ ] Scan images for CVEs (`docker scan`)

### Handling Sensitive Data

**Pattern 1: Environment Variables (Recommended)**

```bash
# .env.local (gitignored)
DATABASE_PASSWORD=actual_password
API_KEY=secret_key

# In dev container
docker run --env-file .env.local ...
```

**Pattern 2: GitHub Secrets + Codespaces**

```yaml
# GitHub UI: Repo Settings → Secrets and Variables → Codespaces
PROD_DATABASE_PASSWORD=...
PROD_API_KEY=...
```

**Pattern 3: Secret Management Tools**

```bash
# In postCreateCommand
vault login -method=github
vault kv get secret/reddog/dev
```

### Least Privilege Principle

```json
{
  // ✅ Secure defaults
  "remoteUser": "vscode",      // Non-root
  "privileged": false,          // Never privileged
  "capAdd": [],                 // No extra capabilities
  "init": true,                 // PID 1 signal handling

  // Only mount what's needed
  "mounts": [
    "source=${localEnv:HOME}/.ssh,target=/home/vscode/.ssh,readonly"
  ]
}
```

---

## Section 10: Performance Optimization Deep Dive

### macOS/Windows Performance

**Challenge:** Docker Desktop for macOS/Windows runs Linux VM, file I/O crosses boundaries.

#### Solution 1: Use `:cached` Bind Mount (Recommended)

```yaml
volumes:
  - ..:/workspace:cached  # ~90% performance vs. local
```

macOS/Windows: Trade consistency for speed (file changes 200ms delay)
Linux: No effect (runs natively)

#### Solution 2: Named Volumes for `node_modules` and `.cache`

```yaml
volumes:
  # Bind mount for source code (with caching)
  - ..:/workspace:cached

  # Named volumes for heavy I/O folders (much faster)
  - node-modules:/workspace/RedDog.UI/node_modules
  - dotnet-cache:/root/.cache/dotnet
  - nuget-cache:/root/.nuget
```

**Why:** Docker can optimize named volume access more than mounted folders.

#### Solution 3: Allocate Sufficient Resources

Docker Desktop Settings:
- **Memory:** Minimum 4GB, recommended 8GB+
- **CPU:** Minimum 2 cores, recommended 4+ cores
- **Disk:** Minimum 50GB, recommended 100GB+

### Docker Layer Caching

```dockerfile
# Dockerfile
# ✅ Good: install dependencies first (changes less often)
FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0

RUN apt-get update && apt-get install -y jq
COPY RedDog.sln .
RUN dotnet restore RedDog.sln
COPY . .
RUN dotnet build

# ❌ Bad: copy code before dependencies (invalidates cache frequently)
FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0
COPY . .
RUN apt-get update && apt-get install -y jq
RUN dotnet restore RedDog.sln
RUN dotnet build
```

### Disk Cleanup

```bash
# Remove unused images/volumes (frees 10-50GB)
docker system prune -a

# Remove only dangling images (safer)
docker image prune

# List image sizes
docker images --format "table {{.Repository}}\t{{.Size}}"
```

### Extension Performance

Each VS Code extension:
- Adds ~10-50MB to image
- Takes ~1-2 seconds to initialize

**Recommended Extensions for Red Dog:**
- `ms-dotnettools.csharp` (100MB, essential)
- `golang.go` (150MB, essential)
- `ms-python.python` (300MB, essential)
- `ms-vscode.makefile-tools` (5MB)
- `eamodio.gitlens` (15MB, useful)

**Avoid:**
- Every theme ever created
- Multiple linters/formatters (use 1)
- Telemetry extensions

---

## Section 11: CI/CD Integration Patterns

### GitHub Actions with Dev Containers

`.github/workflows/ci.yml`:

```yaml
name: CI

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      # Build and run tests in dev container
      - uses: devcontainers/ci@v0
        with:
          imageName: ghcr.io/${{ github.repository }}/reddog
          cacheEndpoint: ghcr.io
          cacheFromImage: ${{ secrets.REGISTRY_USERNAME }}/reddog:latest
          cacheMethod: registry

          push: always
          imageTag: |
            ${{ github.sha }}
            ${{ github.ref }}
            latest

          # Run tests in container
          runCmd: |
            dotnet restore RedDog.sln
            dotnet build RedDog.sln -c Release
            dotnet test RedDog.sln

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: test-results
          path: '**/TestResults/'
```

### Benefits

1. **Dev/Test/Prod Parity:** Same image everywhere
2. **Caching:** Subsequent runs much faster
3. **Reproducibility:** If it works locally, works in CI
4. **Cost:** Pre-built images reduce CI time

### Alternative: GitHub Codespaces (Cloud Development)

`.devcontainer/devcontainer.json`:
```json
{
  "image": "mcr.microsoft.com/devcontainers/dotnet:1-8.0",
  // Same config works for local and Codespaces!
}
```

GitHub UI → Code → Codespaces → Create Codespace on Branch

Benefits:
- Instant cloud environment (no setup)
- No Docker Desktop needed
- Accessible from any device
- Pay-per-minute ($0.36/hour)
- Free tier: 60 hours/month

---

## Section 12: Polyglot Projects Best Practices

### Red Dog Specific Architecture

```
RedDog/
├── RedDog.OrderService/          (C# .NET 8)
├── RedDog.AccountingService/     (C# .NET 8)
├── makeline-service/             (Go)
├── receipt-service/              (Python)
├── loyalty-service/              (Node.js)
├── RedDog.UI/                    (Vue.js 3)
└── .devcontainer/
    ├── devcontainer.json         (Main config)
    ├── docker-compose.yml        (Services: Redis, Postgres)
    ├── Dockerfile                (Optional: custom base)
    └── scripts/
        └── postCreateCommand.sh  (Restore all languages)
```

### Unified Dev Container

Instead of separate containers per service, use single container with all tools:

```json
{
  "name": "RedDog Polyglot",

  "image": "mcr.microsoft.com/devcontainers/dotnet:1-8.0",

  "features": {
    "ghcr.io/devcontainers/features/go:1": { "version": "1.23" },
    "ghcr.io/devcontainers/features/python:1": { "version": "3.12" },
    "ghcr.io/devcontainers/features/node:1": { "version": "24" }
  },

  // All extensions for all languages
  "customizations": {
    "vscode": {
      "extensions": [
        "ms-dotnettools.csharp",
        "golang.go",
        "ms-python.python",
        "ms-vscode.makefile-tools"
      ]
    }
  },

  // All ports needed by any service
  "forwardPorts": [5000, 5001, 5100, 5200, 5300, 5400, 5700, 8080],

  // Restore all languages
  "postCreateCommand": "bash .devcontainer/scripts/postCreateCommand.sh"
}
```

### Language-Specific Setup

`.devcontainer/scripts/postCreateCommand.sh`:

```bash
#!/bin/bash
set -e

echo "Setting up polyglot development environment..."

# .NET
echo "📦 Restoring .NET packages..."
dotnet restore RedDog.sln

# Go
echo "📦 Go setup (no restore needed)"

# Python
echo "📦 Installing Python dependencies..."
[ -d "receipt-service" ] && pip install -r receipt-service/requirements.txt

# Node.js
echo "📦 Installing Node dependencies..."
cd RedDog.UI
npm install
cd ..

# Dapr
echo "🔗 Initializing Dapr..."
dapr init --kubernetes=false --slim 2>/dev/null || true

echo "✨ All languages ready!"
```

### IDE Configuration for Polyglot

VS Code settings in `devcontainer.json`:

```json
{
  "customizations": {
    "vscode": {
      "settings": {
        // .NET
        "[csharp]": {
          "editor.defaultFormatter": "ms-dotnettools.csharp",
          "editor.formatOnSave": true
        },

        // Python
        "[python]": {
          "editor.defaultFormatter": "charliermarsh.ruff",
          "editor.formatOnSave": true
        },

        // Go
        "[go]": {
          "editor.defaultFormatter": "golang.go",
          "editor.formatOnSave": true
        },

        // Node.js / Vue.js
        "[javascript]": {
          "editor.defaultFormatter": "esbenp.prettier-vscode",
          "editor.formatOnSave": true
        },
        "[json]": {
          "editor.defaultFormatter": "esbenp.prettier-vscode"
        },

        // File watching
        "files.watcherExclude": {
          "**/node_modules/**": true,
          "**/bin/**": true,
          "**/obj/**": true,
          "**/.git/objects/**": true
        }
      }
    }
  }
}
```

### Service Communication in Dev Container

With Docker Compose:

```yaml
services:
  dev:
    ports:
      - "5100:5100"  # OrderService
      - "5200:5200"  # MakeLineService (Go)
      - "5300:5300"  # ReceiptService (Python)
      - "5400:5400"  # LoyaltyService (Node.js)
    environment:
      # Service URLs for local development
      ORDERSERVICE_URL: "http://localhost:5100"
      MAKELINESERVICE_URL: "http://localhost:5200"
      RECEIPTSERVICE_URL: "http://localhost:5300"
      LOYALTYSERVICE_URL: "http://localhost:5400"

  dapr:
    image: dapr/daprd:latest
    command: [
      "./daprd",
      "-app-id", "reddog",
      "-app-port", "5000",
      "-dapr-http-port", "5500",
      "-config", "/config/dapr-config.yml"
    ]
    volumes:
      - ./dapr-config.yml:/config/dapr-config.yml
    ports:
      - "5500:5500"  # Dapr sidecar
    depends_on:
      - dev
```

---

## Section 13: Common Pitfalls to Avoid

### 1. **Using `latest` Tag**

❌ **Problem:**
```json
{
  "image": "mcr.microsoft.com/devcontainers/dotnet:latest"
}
```
- Breaking changes without notice
- Different developers get different versions
- CI builds fail unexpectedly

✅ **Solution:**
```json
{
  "image": "mcr.microsoft.com/devcontainers/dotnet:1-8.0-ubuntu-24.04"
}
```

### 2. **Hardcoding Too Many Extensions**

❌ **Problem:**
```json
{
  "extensions": [
    // 50+ extensions
    // Build time: 5+ minutes
    // Startup time: 30+ seconds
  ]
}
```

✅ **Solution:**
```json
{
  "extensions": [
    // Only essential for your project (5-8)
    "ms-dotnettools.csharp",
    "golang.go"
  ],
  "notes": "Other extensions can be installed locally if preferred"
}
```

### 3. **Mounting Sensitive Files**

❌ **Problem:**
```json
{
  "mounts": [
    "source=${localEnv:HOME}/.aws,target=/home/vscode/.aws"
  ]
}
```
- Credentials exposed in container
- If container breached, credentials compromised

✅ **Solution:**
```json
{
  // Don't mount .aws, .docker, etc.
  // Use environment variables or secret injection
  "containerEnv": {
    "AWS_ROLE_ARN": "${localEnv:AWS_ROLE_ARN}"
  }
}
```

### 4. **Not Testing Container Setup Locally**

❌ **Problem:**
```json
{
  "postCreateCommand": "npm install"
  // Not tested locally
  // Breaks for everyone when pushed
}
```

✅ **Solution:**
```bash
# Before committing
docker build -f .devcontainer/Dockerfile .
# OR
docker-compose -f .devcontainer/docker-compose.yml build
```

### 5. **Ignoring Permission Issues**

❌ **Problem:**
```dockerfile
RUN apt-get install -y myapp
# Runs as root
# File ownership: root:root
# Container user vscode can't write files
```

✅ **Solution:**
```dockerfile
# Use pre-built images with vscode user
FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0

RUN apt-get update && apt-get install -y myapp
RUN chown -R vscode:vscode /workspace

USER vscode
```

### 6. **Volume Bind Mount Inconsistency**

❌ **Problem:**
```yaml
volumes:
  - ..:/workspace       # Slow on macOS/Windows
```

✅ **Solution:**
```yaml
volumes:
  - ..:/workspace:cached                    # Better for code
  - node-modules:/workspace/node_modules    # Fast for deps
  - /workspace/.git                         # Exclude .git
```

### 7. **Not Updating Base Images**

❌ **Problem:**
```json
{
  "image": "mcr.microsoft.com/devcontainers/dotnet:1-8.0"
  // Not updated in 6 months
  // Security vulnerabilities accumulate
}
```

✅ **Solution:**
```bash
# Monthly check
docker pull mcr.microsoft.com/devcontainers/dotnet:1-8.0

# Update if new version available
git commit -m "chore: update base image to latest"
```

### 8. **No Timeouts on Long-Running Commands**

❌ **Problem:**
```bash
postCreateCommand: npm install  # Could hang forever
```

✅ **Solution:**
```bash
postCreateCommand: timeout 300 npm install || echo "npm install failed; continuing"
```

### 9. **Debugging Container Build Issues**

❌ **Problem:**
```bash
# Container fails to build
# No idea why
```

✅ **Solution:**
```bash
# Debug by building manually
docker build -f .devcontainer/Dockerfile --progress=plain .

# Or use VS Code: "Dev Containers: Open in Container (Show Log)"
# Check Output → Dev Containers
```

### 10. **Assuming Host == Container File Operations**

❌ **Problem:**
```bash
# Works in container
$ npm run build

# Host still sees old files
# Confusion results
```

✅ **Solution:**
```bash
# Understand your volume setup:
# - Bind mount (:cached): ~200ms delay on macOS/Windows
# - Named volume: instant but host can't see files
# - Check with: docker volume inspect volume-name
```

---

## Section 14: Recommendations for Red Dog Project

### Current State
- Phase 0 complete: `.devcontainer` removed during cleanup
- All services: .NET 6.0 (EOL) with Dapr 1.5.0
- No automated tests yet
- Planned: Phase 1A .NET 10 upgrade

### Recommendation: **Reintroduce Dev Containers with 2025 Best Practices**

#### Rationale

1. **Polyglot Architecture:** .NET, Go, Python, Node.js, Vue.js coexist → dev container essential
2. **Onboarding:** New developers → reopen in container → productive in 5 minutes
3. **CI/CD Parity:** GitHub Actions + same container = fewer surprises
4. **Cloud Ready:** Container-based development mirrors Kubernetes deployment
5. **Teaching Value:** Red Dog is demo/teaching project → containers show cloud-native practices

#### Phased Implementation

**Phase 0: Minimal Setup** (1-2 hours)
```
.devcontainer/
├── devcontainer.json       (Basic .NET 8 + features)
├── docker-compose.yml      (Redis, Postgres)
└── scripts/
    └── postCreateCommand.sh
```

**Phase 1: Testing Integration** (1-2 hours, after tests exist)
- Add test running to CI
- Use `devcontainers/ci` in GitHub Actions

**Phase 2: Optimization** (2-4 hours, optional)
- Pre-build images to ghcr.io for faster CI
- Multi-container approach (backend vs. frontend-only)
- Performance benchmarking

#### Configuration to Implement

**`.devcontainer/devcontainer.json`:**
```json
{
  "name": "RedDog Development",
  "image": "mcr.microsoft.com/devcontainers/dotnet:1-10.0-ubuntu-24.04",

  "features": {
    "ghcr.io/devcontainers/features/go:1": { "version": "1.23" },
    "ghcr.io/devcontainers/features/python:1": { "version": "3.12" },
    "ghcr.io/devcontainers/features/node:1": { "version": "24" }
  },

  "customizations": {
    "vscode": {
      "extensions": [
        "ms-dotnettools.csharp",
        "golang.go",
        "ms-python.python",
        "ms-vscode.makefile-tools",
        "eamodio.gitlens",
        "GitHub.copilot"
      ]
    }
  },

  "forwardPorts": [5000, 5001, 5100, 5180, 5200, 5280, 5300, 5380, 5400, 5480, 5700, 5780, 8080],

  "dockerComposeFile": "docker-compose.yml",
  "service": "dev",
  "workspaceFolder": "/workspace",

  "postCreateCommand": "bash .devcontainer/scripts/postCreateCommand.sh"
}
```

#### Expected Benefits

| Metric | Before | After |
|--------|--------|-------|
| **New dev setup time** | 2 hours | 5 minutes |
| **"Works for me" issues** | Frequent | Eliminated |
| **CI/dev environment match** | 70% | 100% |
| **Dependency conflicts** | Possible | Impossible |
| **Onboarding friction** | High | Low |

#### Risks to Mitigate

1. **Docker Desktop Overhead:** Allocate 4-8GB RAM in settings
2. **File I/O on macOS:** Use `:cached` bind mounts and named volumes
3. **Image Size:** Keep base image <1GB by using official images
4. **Maintenance:** Update base image monthly for security

#### Success Metrics

- [ ] New developers can work locally in <10 minutes
- [ ] All tests pass in container (once tests exist)
- [ ] GitHub Actions uses same container image
- [ ] Documentation updated with dev container setup
- [ ] At least 2 team members successfully using dev containers

---

## Section 15: Resources & References

### Official Documentation

- **Development Container Specification:** https://containers.dev/
- **VS Code Dev Containers:** https://code.visualstudio.com/docs/devcontainers/containers
- **GitHub Codespaces:** https://docs.github.com/en/codespaces
- **Microsoft Learn:** https://learn.microsoft.com/en-us/shows/beginners-series-to-dev-containers/

### Tools & Features

- **devcontainers/features:** https://github.com/devcontainers/features
- **devcontainers/ci:** https://github.com/devcontainers/ci (GitHub Actions)
- **devcontainers/cli:** https://github.com/devcontainers/cli (CLI tool)
- **Official Base Images:** https://github.com/microsoft/vscode-dev-containers

### Learning Resources

- **VS Code Advanced Containers:** https://code.visualstudio.com/remote/advancedcontainers/
- **Dev Containers Best Practices:** https://www.glukhov.org/post/2025/10/vs-code-dev-containers/
- **Real-World Examples:** https://github.com/microsoft/vscode-remote-try-dotnet

### Community & Help

- **GitHub Discussions:** https://github.com/devcontainers/spec/discussions
- **VS Code Remote Release:** https://github.com/microsoft/vscode-remote-release
- **Stack Overflow:** Tag: `devcontainers`

---

## Conclusion

Development Containers represent a significant shift in how teams manage development environments. For the Red Dog project—a polyglot microservices demonstration—they offer:

1. **Immediate Value:** Consistent environments across team/machines
2. **Teaching Value:** Show cloud-native best practices in action
3. **Scalability:** Works for 1 developer or 100+
4. **Future-Ready:** Same container approach for local, CI, and Kubernetes

The investment in setting up dev containers pays dividends through:
- Reduced onboarding time
- Fewer environment-related bugs
- Better CI/CD parity
- Team productivity gains

**Recommendation:** Implement a minimal dev container configuration immediately (1-2 hours of work), refine it as the project evolves, and leverage it as a teaching tool for cloud-native development practices.

---

**Document Version:** 1.0
**Last Updated:** November 2025
**Research Completed:** November 9, 2025
