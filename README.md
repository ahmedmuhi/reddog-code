## Red Dog Demo — Cloud-Native Microservices with Dapr

### Background

Microservices can be hard. But, while being exceedingly difficult to architect they have become an increasingly popular architecture pattern. As developers begin to migrate their existing monolithic codebases to a microservices system, they spend a lot of their time dealing with the inherent challenges presented by distributed applications, such as state management and service invocation.

Enter [Dapr](https://www.dapr.io) - The Distributed Application Runtime built with developers in mind. Dapr aims to solve some of these microservice-related challenges by providing consistent building blocks in the form of http/gRPC APIs that can be called natively or using one of the dapr language SDKs.

This repository contains the Red Dog application source code and deployment configuration. The same codebase deploys to local development clusters and cloud Kubernetes environments using Helm charts with environment-specific values files.

### Deployment

Red Dog uses a **same chart, different values** pattern — one set of Helm templates serves all environments:

```bash
# Local (kind)
helm upgrade --install reddog ./charts/reddog -f values/values-local.yaml --namespace reddog

# Cloud (AKS, EKS, GKE)
helm upgrade --install reddog ./charts/reddog -f values/values-<cloud>.yaml --namespace reddog
```

| Environment | Guide | Status |
|---|---|---|
| Local (kind) | [docs/deployment-local.md](docs/deployment-local.md) | Tested and working |
| Azure (AKS) | [docs/deployment-cloud.md](docs/deployment-cloud.md) | Sample values available |
| AWS (EKS) | [docs/deployment-cloud.md](docs/deployment-cloud.md) | Placeholder — no cluster yet |
| GCP (GKE) | [docs/deployment-cloud.md](docs/deployment-cloud.md) | Placeholder — no cluster yet |

### Architecture Diagram and Service Descriptions

The Red Dog application is developed with .NET 10 and Vue 3. It uses Dapr ([Distributed Application Runtime](https://dapr.io)) for state management, pub/sub messaging, secret access, service invocation, and runtime configuration.

![Logical Application Architecture Diagram](assets/reddog_code.png)

| Service | Description |
|---|---|
| AccountingService | Processes, stores and aggregates order data into sales metrics for the UI |
| Bootstrapper | Initializes database tables via Entity Framework Core Migrations (runs as a Job) |
| LoyaltyService | Manages the loyalty program by modifying customer reward points based on spend |
| MakeLineService | Simulates and coordinates a queue of current orders, monitoring processing and completion |
| OrderService | CRUD API for placing and managing orders |
| ReceiptGenerationService | Generates and stores order receipts for auditing and historical purposes |
| UI | Vue.js dashboard showing order/sales data for a hub location |
| VirtualCustomers | Customer simulator that generates orders (Dapr Config API pilot) |
| VirtualWorker | Worker simulator that completes orders on a schedule |

### Configuration Architecture

Red Dog uses a 4-layer configuration model:

1. **Chart defaults** — stable, environment-agnostic settings in `charts/*/values.yaml`
2. **Environment overrides** — per-environment values in `values/values-<env>.yaml` (gitignored)
3. **Runtime secrets** — Kubernetes Secrets (local) or managed secret stores (cloud)
4. **Dapr Configuration API** — business config that can change at runtime without redeployment

For details, see the [Configuration Architecture KI](knowledge/configuration-architecture-ki.md) and the [ADR index](docs/adr/README.md).

### Contributing

This project welcomes contributions and suggestions. Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
