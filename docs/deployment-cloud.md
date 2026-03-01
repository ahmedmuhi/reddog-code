# Cloud Deployment Overview

Red Dog uses the same Helm charts for cloud as for local — the only difference is the values file.

```bash
# The pattern for any environment:
helm upgrade --install reddog ./charts/reddog -f values/values-<cloud>.yaml --namespace reddog
```

## Architecture: Same Chart, Different Values

| Concern | Local (kind) | Cloud (AKS/EKS/GKE) |
|---|---|---|
| State store | Redis (in-cluster) | Cosmos DB / DynamoDB / Firestore |
| Pub/Sub | Redis (in-cluster) | RabbitMQ / SNS+SQS / Cloud Pub/Sub |
| Secret store | Kubernetes Secrets | Key Vault / Secrets Manager / Secret Manager |
| Database | SQL Server (in-cluster) | Azure SQL / RDS / Cloud SQL |
| Infrastructure chart | Enabled (deploys Redis, SQL Server) | Disabled (managed PaaS) |
| Ingress | localhost, no TLS | Custom domain, TLS enabled |

Cloud environments **disable the infrastructure chart** because managed PaaS services replace in-cluster containers:

```yaml
infrastructure:
  redis:
    enabled: false
  sqlserver:
    enabled: false
```

## Azure (AKS) — Worked Example

The Azure sample (`values/values-azure.yaml.sample`) demonstrates the full pattern.

### Prerequisites

- AKS cluster with Dapr installed (`helm install dapr dapr/dapr ...`)
- nginx ingress controller
- Azure Cosmos DB account (state store)
- RabbitMQ instance (pub/sub) — e.g., Azure Service Bus with RabbitMQ protocol or CloudAMQP
- Azure Key Vault (secret store)
- Azure SQL Database (accounting)
- Workload Identity configured for Key Vault access

### Key Differences from Local

```yaml
# values/values-azure.yaml (based on sample)
global:
  environment: azure
  domain: reddog.contoso.cloud

dapr:
  pubsub:
    type: rabbitmq          # Instead of redis
    metadata:
      - name: host
        secretKeyRef:
          name: rabbitmq-shared-credentials
          key: amqp-uri

  stateStore:
    type: azure.cosmosdb    # Instead of redis
    metadata:
      - name: url
        value: https://reddog.documents.azure.com:443/
      - name: masterKey
        secretKeyRef:
          name: cosmos-account-key
          key: master-key

  secretStore:
    type: azure.keyvault    # Instead of kubernetes
    metadata:
      - name: vaultName
        value: reddog-kv
```

### Deploy

```bash
# Create namespace
kubectl create namespace reddog

# Deploy application (no infrastructure chart needed)
helm upgrade --install reddog ./charts/reddog \
  -f values/values-azure.yaml \
  --namespace reddog \
  --wait --timeout 10m
```

## AWS (EKS) — Placeholder

> No `values-aws.yaml.sample` exists yet. Create when an EKS cluster is available.

### What Would Change

| Concern | Azure | AWS Equivalent |
|---|---|---|
| State store | `azure.cosmosdb` | `aws.dynamodb` |
| Pub/Sub | `rabbitmq` | `snssqs` (Dapr SNS+SQS component) |
| Secret store | `azure.keyvault` | `aws.secretmanager` |
| Database | Azure SQL | Amazon RDS (SQL Server or PostgreSQL) |
| Identity | Azure Workload Identity | IRSA (IAM Roles for Service Accounts) |

### Dapr Component Mapping

```yaml
dapr:
  stateStore:
    type: aws.dynamodb
    metadata:
      - name: table
        value: reddog-state
      - name: region
        value: us-east-1

  pubsub:
    type: snssqs
    metadata:
      - name: region
        value: us-east-1

  secretStore:
    type: aws.secretmanager
    metadata:
      - name: region
        value: us-east-1
```

## GCP (GKE) — Placeholder

> No `values-gcp.yaml.sample` exists yet. Create when a GKE cluster is available.

### What Would Change

| Concern | Azure | GCP Equivalent |
|---|---|---|
| State store | `azure.cosmosdb` | `gcp.firestore` |
| Pub/Sub | `rabbitmq` | `gcp.pubsub` |
| Secret store | `azure.keyvault` | `gcp.secretmanager` |
| Database | Azure SQL | Cloud SQL (SQL Server or PostgreSQL) |
| Identity | Azure Workload Identity | GKE Workload Identity |

### Dapr Component Mapping

```yaml
dapr:
  stateStore:
    type: gcp.firestore
    metadata:
      - name: project_id
        value: my-gcp-project

  pubsub:
    type: gcp.pubsub
    metadata:
      - name: projectId
        value: my-gcp-project

  secretStore:
    type: gcp.secretmanager
    metadata:
      - name: project_id
        value: my-gcp-project
```

## Infrastructure Note

Cloud environments rely on managed PaaS services provisioned outside of Helm:

- **Azure:** Cosmos DB, Azure SQL, Service Bus / RabbitMQ, Key Vault — provisioned via Terraform/Bicep.
- **AWS:** DynamoDB, RDS, SNS/SQS, Secrets Manager — provisioned via Terraform/CDK.
- **GCP:** Firestore, Cloud SQL, Pub/Sub, Secret Manager — provisioned via Terraform.

The Helm chart does not create these resources. It only references them via endpoints and secret references in the values file. See [ADR-0007](adr/adr-0007-cloud-agnostic-deployment-strategy.md) for the full infrastructure strategy.
