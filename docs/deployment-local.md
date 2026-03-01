# Local Development Deployment (kind)

This guide walks through deploying Red Dog to a local [kind](https://kind.sigs.k8s.io/) cluster using Helm.

## Prerequisites

| Tool | Version | Install |
|---|---|---|
| Docker | 20.10+ | [docs.docker.com](https://docs.docker.com/get-docker/) |
| kind | 0.20+ | [kind.sigs.k8s.io](https://kind.sigs.k8s.io/docs/user/quick-start/#installation) |
| kubectl | 1.28+ | [kubernetes.io](https://kubernetes.io/docs/tasks/tools/) |
| Helm | 3.x | [helm.sh](https://helm.sh/docs/intro/install/) |
| Dapr CLI | 1.16+ | [docs.dapr.io](https://docs.dapr.io/getting-started/install-dapr-cli/) |

WSL2 users: ensure Docker Desktop is running and WSL integration is enabled.

## Quick Start

The setup script handles everything — cluster creation, Dapr install, infrastructure, and application deployment:

```bash
./scripts/setup-local-dev.sh
```

The script will:
1. Check all prerequisites are installed.
2. Warn about WSL2 memory configuration if applicable.
3. Auto-copy `values/values-local.yaml.sample` → `values/values-local.yaml` if the values file is missing (you'll be prompted to set the SQL Server password).
4. Create a kind cluster (`reddog-local`) using `kind-config.yaml`.
5. Install Dapr (Helm chart, `dapr-system` namespace).
6. Install nginx ingress controller.
7. Deploy infrastructure chart (Redis, SQL Server) to `reddog` namespace.
8. Deploy application chart (8 services + bootstrapper + config seeder) to `reddog` namespace.

## Values File

The local values file follows the sample-copy pattern:

```
values/values-local.yaml.sample  ← committed (placeholder secrets)
values/values-local.yaml         ← gitignored (real secrets)
```

Key settings to review after copying:

```yaml
infrastructure:
  sqlserver:
    saPassword: "YourPasswordHere"  # Must meet SQL Server complexity requirements
```

The values file is 57 lines of local-only overrides. Chart defaults live in `charts/reddog/values.yaml` and `charts/infrastructure/values.yaml`.

## What Gets Deployed

### Infrastructure chart (`charts/infrastructure/`)

| Component | Description |
|---|---|
| Redis | `redis:7.2-alpine` — state store, pub/sub, and config store backend |
| SQL Server | `mcr.microsoft.com/mssql/server:2022-latest` — accounting database |
| Kubernetes Secrets | `sqlserver-secret`, `reddog-sql` |
| RBAC | `secret-reader` Role/RoleBinding |

### Application chart (`charts/reddog/`)

| Component | Description |
|---|---|
| OrderService | CRUD API for placing orders |
| MakeLineService | Order queue simulation |
| LoyaltyService | Customer reward points |
| AccountingService | Order data aggregation and SQL storage |
| ReceiptGenerationService | Receipt archival |
| VirtualCustomers | Customer order simulator |
| VirtualWorker | Order completion simulator |
| UI | Vue.js dashboard |
| Bootstrapper | EF Core migrations (Job, runs once) |
| Config Seeder | Seeds business config keys into Redis (Job, runs once) |
| 8 Dapr Components | pubsub, 2 state stores, secret store, 2 bindings, configuration, config store |
| Ingress | nginx with path-based routing |

## Access Points

| Endpoint | URL |
|---|---|
| UI Dashboard | `http://localhost` |
| Order API | `http://localhost/api/orders` |
| MakeLine API | `http://localhost/api/makeline/orders/Redmond` |
| Accounting API | `http://localhost/api/accounting` |

## Common Operations

### Upgrade / Redeploy

After changing values or updating images:

```bash
# Upgrade application
helm upgrade reddog ./charts/reddog \
  -f values/values-local.yaml \
  --namespace reddog

# Upgrade infrastructure
helm upgrade reddog-infra ./charts/infrastructure \
  -f values/values-local.yaml \
  --namespace reddog
```

### Check Status

```bash
kubectl get pods -n reddog          # All services should be 2/2 Ready
kubectl get ingress -n reddog       # Ingress routes
kubectl logs -n reddog deploy/orderservice -c orderservice  # App logs
kubectl logs -n reddog deploy/orderservice -c daprd         # Dapr sidecar logs
```

### Teardown

```bash
kind delete cluster --name reddog-local
```

## Troubleshooting

### Docker not running

```
ERROR: Cannot connect to the Docker daemon
```

Start Docker Desktop (Windows/Mac) or the Docker service (Linux).

### Port 80 already in use

kind uses port 80 for ingress. If another service is using it, either stop that service or modify `kind-config.yaml` to use a different host port.

### WSL2 memory exhaustion

Without limits, WSL2 can consume 50% of host RAM. Create `C:\Users\<you>\.wslconfig`:

```ini
[wsl2]
memory=4GB
processors=4
swap=2GB
pageReporting=true

[experimental]
autoMemoryReclaim=dropcache
sparseVhd=true
```

Then: `wsl --shutdown` and restart Docker Desktop.

### Pods stuck in CrashLoopBackOff

```bash
kubectl describe pod -n reddog <pod-name>   # Check events
kubectl logs -n reddog <pod-name> -c daprd   # Dapr sidecar issues
```

Common causes: Dapr not installed, Redis not ready, SQL Server password too weak.

### Bootstrapper keeps restarting

The bootstrapper Job runs EF Core migrations. It needs SQL Server to be ready first. Check:

```bash
kubectl logs -n reddog -l app=bootstrapper
kubectl get pods -n reddog -l app=sqlserver  # Should be 1/1 Ready
```
