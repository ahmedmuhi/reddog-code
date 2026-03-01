# Environment Overlays

This directory contains per-cloud overrides that sit on top of the shared manifests in `manifests/branch/base`. Apply the base first, then layer on the overlay for the target platform:

- `aws/` – AWS-specific resources (IRSA, DynamoDB)
- `azure/` – Azure-specific resources (Workload Identity, CosmosDB)
- `gcp/` – GCP-specific resources (Workload Identity, Firestore)

## Cloud-Specific State Stores

Each cloud overlay includes passwordless, identity-based Dapr state store components:

### Azure (CosmosDB)
- Uses Azure AD authentication with Workload Identity
- No `masterKey` required
- ServiceAccounts annotated with `azure.workload.identity/client-id`

### AWS (DynamoDB)
- Uses IAM Roles for Service Accounts (IRSA)
- No `accessKey` or `secretKey` required
- ServiceAccounts annotated with `eks.amazonaws.com/role-arn`

### GCP (Firestore)
- Uses GKE Workload Identity
- No `private_key` or `private_key_id` required
- ServiceAccounts annotated with `iam.gke.io/gcp-service-account`

## Usage

### Azure (AKS)
```bash
# Apply base manifests
kubectl apply -k manifests/branch/base

# Apply Azure overlay with Workload Identity
kubectl apply -f manifests/overlays/azure/service-accounts.yaml
kubectl apply -f manifests/overlays/azure/reddog.state.loyalty.yaml
kubectl apply -f manifests/overlays/azure/reddog.state.makeline.yaml
kubectl apply -f manifests/overlays/azure/reddog.binding.blob.yaml
```

**Before applying:** Update the CosmosDB account URL in the state store YAMLs and the Managed Identity Client IDs in `service-accounts.yaml`.

### AWS (EKS)
```bash
# Apply base manifests
kubectl apply -k manifests/branch/base

# Apply AWS overlay with IRSA
kubectl apply -f manifests/overlays/aws/service-accounts.yaml
kubectl apply -f manifests/overlays/aws/reddog.state.loyalty.yaml
kubectl apply -f manifests/overlays/aws/reddog.state.makeline.yaml
kubectl apply -f manifests/overlays/aws/reddog.binding.s3.yaml
```

**Before applying:** Update the AWS region and IAM Role ARNs in `service-accounts.yaml`.

### GCP (GKE)
```bash
# Apply base manifests
kubectl apply -k manifests/branch/base

# Apply GCP overlay with Workload Identity
kubectl apply -f manifests/overlays/gcp/service-accounts.yaml
kubectl apply -f manifests/overlays/gcp/reddog.state.loyalty.yaml
kubectl apply -f manifests/overlays/gcp/reddog.state.makeline.yaml
kubectl apply -f manifests/overlays/gcp/reddog.binding.gcs.yaml
```

**Before applying:** Update the GCP Project ID in all YAMLs and the GCP Service Account emails in `service-accounts.yaml`.

## Prerequisites

### Azure
1. Enable Workload Identity on AKS cluster
2. Create User-Assigned Managed Identities for each service
3. Create Federated Credentials linking K8s ServiceAccounts to Managed Identities
4. Grant Managed Identities access to CosmosDB (Contributor role)

### AWS
1. Enable OIDC provider on EKS cluster
2. Create IAM Roles with Trust Policy referencing EKS OIDC provider
3. Grant IAM Roles permissions to DynamoDB tables
4. Update ServiceAccount annotations with IAM Role ARNs

### GCP
1. Enable Workload Identity on GKE cluster
2. Create GCP Service Accounts for each Kubernetes ServiceAccount
3. Bind GCP Service Accounts to K8s ServiceAccounts (`roles/iam.workloadIdentityUser`)
4. Grant GCP Service Accounts access to Firestore

## Security Notes

⚠️ **No secrets in manifests** - All authentication is federated via cloud identity providers.

🔒 **Principle of least privilege** - Each service has its own identity with minimal required permissions.

✅ **REQ-001 compliance** - No connection strings or access keys in any YAML files.
