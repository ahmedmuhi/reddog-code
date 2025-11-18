# Workload Identity Setup Guide

This guide provides step-by-step instructions for configuring passwordless Workload Identity authentication for the Red Dog application across Azure, AWS, and GCP.

## Table of Contents

- [Overview](#overview)
- [Azure Workload Identity Setup](#azure-workload-identity-setup)
- [AWS IAM Roles for Service Accounts (IRSA) Setup](#aws-iam-roles-for-service-accounts-irsa-setup)
- [GCP Workload Identity Setup](#gcp-workload-identity-setup)
- [Validation](#validation)

## Overview

Workload Identity enables Kubernetes workloads to authenticate to cloud services using native cloud identity mechanisms without storing static credentials. This implementation:

- Eliminates secrets management overhead
- Improves security posture by removing long-lived credentials
- Provides automatic credential rotation
- Enables fine-grained access control

## Azure Workload Identity Setup

### Prerequisites

- Azure Kubernetes Service (AKS) cluster with Workload Identity enabled
- Azure CLI (`az`) installed and authenticated
- `kubectl` configured to access your AKS cluster

### Step 1: Enable Workload Identity on AKS

```bash
# Enable OIDC issuer and Workload Identity on existing cluster
az aks update \
  --resource-group <resource-group> \
  --name <cluster-name> \
  --enable-oidc-issuer \
  --enable-workload-identity
```

### Step 2: Create User-Assigned Managed Identities

Create a Managed Identity for each service that needs cloud resource access:

```bash
RESOURCE_GROUP="<your-resource-group>"
LOCATION="<azure-region>"

# Create Managed Identities
az identity create --name reddog-order-service-identity --resource-group $RESOURCE_GROUP --location $LOCATION
az identity create --name reddog-make-line-service-identity --resource-group $RESOURCE_GROUP --location $LOCATION
az identity create --name reddog-loyalty-service-identity --resource-group $RESOURCE_GROUP --location $LOCATION
az identity create --name reddog-accounting-service-identity --resource-group $RESOURCE_GROUP --location $LOCATION
az identity create --name reddog-receipt-service-identity --resource-group $RESOURCE_GROUP --location $LOCATION
```

### Step 3: Grant Permissions to Managed Identities

Grant necessary permissions for each identity to access Azure resources:

```bash
COSMOSDB_ACCOUNT="<your-cosmosdb-account>"
STORAGE_ACCOUNT="<your-storage-account>"

# Grant CosmosDB access to loyalty-service
LOYALTY_IDENTITY_ID=$(az identity show --name reddog-loyalty-service-identity --resource-group $RESOURCE_GROUP --query principalId -o tsv)
az cosmosdb sql role assignment create \
  --account-name $COSMOSDB_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --role-definition-name "Cosmos DB Built-in Data Contributor" \
  --principal-id $LOYALTY_IDENTITY_ID \
  --scope "/"

# Grant Blob Storage access to receipt-service
RECEIPT_IDENTITY_ID=$(az identity show --name reddog-receipt-service-identity --resource-group $RESOURCE_GROUP --query principalId -o tsv)
az role assignment create \
  --role "Storage Blob Data Contributor" \
  --assignee $RECEIPT_IDENTITY_ID \
  --scope "/subscriptions/<subscription-id>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Storage/storageAccounts/$STORAGE_ACCOUNT"
```

### Step 4: Federate Managed Identities with Kubernetes ServiceAccounts

```bash
AKS_OIDC_ISSUER=$(az aks show --name <cluster-name> --resource-group $RESOURCE_GROUP --query "oidcIssuerProfile.issuerUrl" -otsv)

# Federate each identity
az identity federated-credential create \
  --name reddog-loyalty-federated \
  --identity-name reddog-loyalty-service-identity \
  --resource-group $RESOURCE_GROUP \
  --issuer $AKS_OIDC_ISSUER \
  --subject system:serviceaccount:reddog-retail:loyalty-service
```

### Step 5: Update ServiceAccount Manifests

Update the ServiceAccount YAML files in `manifests/overlays/azure/service-accounts.yaml` with actual Managed Identity client IDs:

```bash
# Get client IDs
az identity show --name reddog-loyalty-service-identity --resource-group $RESOURCE_GROUP --query clientId -o tsv
```

Replace placeholders like `<loyalty-service-managed-identity-client-id>` with actual values.

### Step 6: Deploy Updated Manifests

```bash
kubectl apply -f manifests/overlays/azure/service-accounts.yaml
kubectl apply -f manifests/overlays/azure/reddog.state.loyalty.yaml
kubectl apply -f manifests/overlays/azure/reddog.state.makeline.yaml
kubectl apply -f manifests/overlays/azure/reddog.binding.blob.yaml
```

## AWS IAM Roles for Service Accounts (IRSA) Setup

### Prerequisites

- Amazon EKS cluster with OIDC provider configured
- AWS CLI (`aws`) installed and authenticated
- `kubectl` and `eksctl` installed

### Step 1: Create OIDC Provider (if not exists)

```bash
eksctl utils associate-iam-oidc-provider \
  --cluster <cluster-name> \
  --region <aws-region> \
  --approve
```

### Step 2: Create IAM Roles with Trust Policies

Create an IAM role for each service with appropriate trust policy:

```bash
CLUSTER_NAME="<your-cluster-name>"
AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
OIDC_PROVIDER=$(aws eks describe-cluster --name $CLUSTER_NAME --region <region> --query "cluster.identity.oidc.issuer" --output text | sed -e "s/^https:\/\///")

# Create trust policy
cat > trust-policy.json <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Federated": "arn:aws:iam::$AWS_ACCOUNT_ID:oidc-provider/$OIDC_PROVIDER"
      },
      "Action": "sts:AssumeRoleWithWebIdentity",
      "Condition": {
        "StringEquals": {
          "$OIDC_PROVIDER:sub": "system:serviceaccount:reddog-retail:loyalty-service",
          "$OIDC_PROVIDER:aud": "sts.amazonaws.com"
        }
      }
    }
  ]
}
EOF

aws iam create-role --role-name reddog-loyalty-service-role --assume-role-policy-document file://trust-policy.json
```

### Step 3: Attach Policies to IAM Roles

```bash
# Create DynamoDB policy
cat > dynamodb-policy.json <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "dynamodb:GetItem",
        "dynamodb:PutItem",
        "dynamodb:UpdateItem",
        "dynamodb:DeleteItem",
        "dynamodb:Query",
        "dynamodb:Scan"
      ],
      "Resource": "arn:aws:dynamodb:<region>:$AWS_ACCOUNT_ID:table/reddog-loyalty"
    }
  ]
}
EOF

aws iam put-role-policy --role-name reddog-loyalty-service-role --policy-name DynamoDBAccess --policy-document file://dynamodb-policy.json
```

### Step 4: Update ServiceAccount Manifests

Update `manifests/overlays/aws/service-accounts.yaml` with actual IAM Role ARNs:

```bash
# Get role ARN
aws iam get-role --role-name reddog-loyalty-service-role --query Role.Arn --output text
```

### Step 5: Deploy Updated Manifests

```bash
kubectl apply -f manifests/overlays/aws/service-accounts.yaml
kubectl apply -f manifests/overlays/aws/reddog.state.loyalty.yaml
kubectl apply -f manifests/overlays/aws/reddog.state.makeline.yaml
kubectl apply -f manifests/overlays/aws/reddog.binding.s3.yaml
```

## GCP Workload Identity Setup

### Prerequisites

- Google Kubernetes Engine (GKE) cluster with Workload Identity enabled
- `gcloud` CLI installed and authenticated
- `kubectl` configured to access your GKE cluster

### Step 1: Enable Workload Identity on GKE

```bash
# Enable on existing cluster
gcloud container clusters update <cluster-name> \
  --workload-pool=<project-id>.svc.id.goog \
  --region=<region>
```

### Step 2: Create GCP ServiceAccounts

```bash
PROJECT_ID="<your-project-id>"

gcloud iam service-accounts create reddog-loyalty-service \
  --display-name="Red Dog Loyalty Service" \
  --project=$PROJECT_ID
```

### Step 3: Grant Permissions to GCP ServiceAccounts

```bash
# Grant Firestore permissions
gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member="serviceAccount:reddog-loyalty-service@$PROJECT_ID.iam.gserviceaccount.com" \
  --role="roles/datastore.user"

# Grant Cloud Storage permissions (for receipt service)
gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member="serviceAccount:reddog-receipt-service@$PROJECT_ID.iam.gserviceaccount.com" \
  --role="roles/storage.objectAdmin"
```

### Step 4: Create IAM Bindings

Link Kubernetes ServiceAccounts with GCP ServiceAccounts:

```bash
gcloud iam service-accounts add-iam-policy-binding \
  reddog-loyalty-service@$PROJECT_ID.iam.gserviceaccount.com \
  --role roles/iam.workloadIdentityUser \
  --member "serviceAccount:$PROJECT_ID.svc.id.goog[reddog-retail/loyalty-service]"
```

### Step 5: Update ServiceAccount Manifests

Update `manifests/overlays/gcp/service-accounts.yaml` with actual GCP ServiceAccount emails.

### Step 6: Deploy Updated Manifests

```bash
kubectl apply -f manifests/overlays/gcp/service-accounts.yaml
kubectl apply -f manifests/overlays/gcp/reddog.state.loyalty.yaml
kubectl apply -f manifests/overlays/gcp/reddog.state.makeline.yaml
kubectl apply -f manifests/overlays/gcp/reddog.binding.gcs.yaml
```

## Validation

### Verify ServiceAccount Annotations

```bash
kubectl get serviceaccount loyalty-service -n reddog-retail -o yaml
```

### Test Workload Identity

Deploy a test pod to verify authentication:

```bash
# Azure
kubectl run -it --rm --image=mcr.microsoft.com/azure-cli --serviceaccount=loyalty-service test-identity -- bash
az login --identity

# AWS
kubectl run -it --rm --image=amazon/aws-cli --serviceaccount=loyalty-service test-identity -- sts get-caller-identity

# GCP
kubectl run -it --rm --image=google/cloud-sdk:slim --serviceaccount=loyalty-service test-identity -- gcloud auth list
```

### Check Dapr Component Status

```bash
kubectl logs <pod-name> -c daprd -n reddog-retail | grep -i "component loaded"
```

### Monitor Application Logs

```bash
kubectl logs -f deployment/loyalty-service -n reddog-retail
```

Look for successful connections to cloud resources without authentication errors.

## Troubleshooting

### Common Issues

1. **OIDC Provider Not Found (Azure)**
   - Ensure OIDC issuer is enabled on AKS cluster
   - Verify issuer URL matches in federated credentials

2. **Assume Role Failed (AWS)**
   - Check trust policy conditions match exactly
   - Verify ServiceAccount namespace and name are correct

3. **Permission Denied (GCP)**
   - Ensure Workload Identity User role is granted
   - Check that GCP ServiceAccount has necessary permissions

4. **Dapr Component Fails to Load**
   - Review Dapr sidecar logs for specific error messages
   - Verify metadata values (URLs, regions, etc.) are correct
   - Ensure ServiceAccount is properly annotated

## Additional Resources

- [Azure Workload Identity Documentation](https://learn.microsoft.com/en-us/azure/aks/workload-identity-overview)
- [AWS IRSA Documentation](https://docs.aws.amazon.com/eks/latest/userguide/iam-roles-for-service-accounts.html)
- [GCP Workload Identity Documentation](https://cloud.google.com/kubernetes-engine/docs/how-to/workload-identity)
- [Dapr Components Documentation](https://docs.dapr.io/reference/components-reference/)
