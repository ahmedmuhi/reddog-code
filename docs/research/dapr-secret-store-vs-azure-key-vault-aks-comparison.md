---
title: Secret Management for Red Dog Modernization - Dapr Secret Store vs Azure Key Vault Direct Integration
date: 2025-11-02
author: Red Dog Modernization Team
scope: AKS/Kubernetes Deployment
status: Research Complete
---

# Secret Management Comparison: Dapr Secret Store vs Azure Key Vault for AKS

## Executive Summary

**Key Finding:** Red Dog **already uses Azure Key Vault** as the secret backend, accessed through **Dapr's secret store abstraction layer**. The question isn't "Dapr OR Azure Key Vault" but rather "Dapr abstraction layer WITH Azure Key Vault backend vs direct Azure SDK integration."

**Recommendation for Modernization:** Continue using Dapr secret store with Azure Key Vault backend, but migrate authentication from **Service Principal + Certificate to Workload Identity** (modern best practice for AKS 2025).

---

## Current Red Dog Implementation

### What Was Found in the Codebase

**File:** `manifests/branch/base/components/reddog.secretstore.yaml`

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: reddog.secretstore
  namespace: reddog-retail
spec:
  type: secretstores.azure.keyvault  # ← Using Azure Key Vault!
  version: v1
  metadata:
    - name: vaultName
      value: reddog-kv-branch
    - name: spnClientId
      value:
    - name: spnTenantId
      value:
    - name: spnCertificate
      secretKeyRef:
        name: reddog.secretstore
        key: secretstore-cert
```

**Authentication Method:** Service Principal with Certificate (stored in Kubernetes secret)

**Services Using It:** `RedDog.AccountingService/Program.cs` line 68:
```csharp
config.AddDaprSecretStore(SecretStoreName, secretDescriptors, daprClient);
```

### Why This Was Chosen (Historical Context)

1. **2021 Architecture:** Original Red Dog was built when Service Principal authentication was standard practice
2. **Dapr Benefits:** Abstraction layer allows polyglot services (Go, Python, Node.js, .NET) to access secrets via simple HTTP API
3. **Azure Key Vault Benefits:** Enterprise-grade secret storage with audit logging, rotation, RBAC

---

## cert-manager: A Separate Concern

### What cert-manager Does

**File:** `manifests/branch/dependencies/yaml/cluster-issuer.yaml`

```yaml
apiVersion: cert-manager.io/v1alpha2
kind: Issuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    privateKeySecretRef:
      name: letsencrypt-prod  # ← Stores TLS private key in K8s secret
```

### cert-manager vs Application Secrets

| Aspect | cert-manager | Dapr Secret Store / Azure Key Vault |
|--------|-------------|-------------------------------------|
| **Purpose** | TLS/SSL certificate management for HTTPS ingress | Application secrets (DB passwords, API keys, connection strings) |
| **Scope** | Infrastructure layer (Ingress controllers) | Application layer (Microservices) |
| **Storage** | Kubernetes Secrets (Base64, etcd) | Azure Key Vault (encrypted at rest, HSM-backed) |
| **Lifecycle** | Auto-renewal via ACME (Let's Encrypt) | Manual rotation or Azure Key Vault rotation policies |
| **Access** | Ingress controllers, service meshes | Application code via Dapr API or Azure SDK |

**Bottom Line:** cert-manager handles **TLS certificates for encrypted network traffic**. It has **NO relationship** to application secrets like database passwords. These are completely separate systems.

---

## Comparison: Dapr Secret Store vs Direct Azure Key Vault SDK

### Architecture Diagrams

#### Option 1: Dapr Secret Store (Current Red Dog)

```
┌─────────────────────────────────────────┐
│ Pod: order-service                      │
│                                         │
│  ┌──────────────────┐  HTTP GET        │
│  │ Application      │  /secrets/        │
│  │ (Any Language)   │─────────┐        │
│  └──────────────────┘         │        │
│                                ↓        │
│  ┌────────────────────────────────┐    │
│  │ Dapr Sidecar                   │    │
│  │ - Secret Store Component       │    │
│  │ - Azure Key Vault Provider     │────┼──→ Azure Key Vault
│  │ - Workload Identity Auth       │    │    (reddog-kv-branch)
│  └────────────────────────────────┘    │
└─────────────────────────────────────────┘
```

#### Option 2: Direct Azure Key Vault SDK

```
┌─────────────────────────────────────────┐
│ Pod: order-service                      │
│                                         │
│  ┌──────────────────┐                  │
│  │ Application      │                  │
│  │ - Azure.Identity │                  │
│  │ - Azure.KeyVault │────────────────────────→ Azure Key Vault
│  │ - Workload ID    │                  │      (reddog-kv-branch)
│  └──────────────────┘                  │
└─────────────────────────────────────────┘
```

### Detailed Comparison Matrix

| Criterion | Dapr Secret Store | Direct Azure Key Vault SDK | Winner |
|-----------|-------------------|---------------------------|--------|
| **Multi-Cloud Portability** | ✅ Change backend (AKV → Vault → AWS Secrets Manager) via YAML | ❌ Code rewrite required for each provider | 🏆 **Dapr** |
| **Polyglot Support** | ✅ HTTP API works for Go, Python, Node.js, .NET, Rust | ❌ Need language-specific SDKs (azure-sdk-for-go, boto3, etc.) | 🏆 **Dapr** |
| **Secret Caching** | ✅ Configurable TTL in Dapr component | ⚠️ Must implement manually | 🏆 **Dapr** |
| **Zero Code Changes** | ✅ Switch backends via component config | ❌ Change code for different secret stores | 🏆 **Dapr** |
| **Native Azure Features** | ⚠️ Limited to Dapr's abstraction (no HSM direct access, advanced policies) | ✅ Full access to Key Vault features (managed HSM, key rotation policies, RBAC) | 🏆 **Direct SDK** |
| **Performance** | ⚠️ HTTP hop to sidecar (2-5ms overhead) | ✅ Direct HTTPS call to Azure | 🏆 **Direct SDK** |
| **Troubleshooting** | ❌ Two components (app + sidecar) to debug | ✅ Single process, stack traces include SDK | 🏆 **Direct SDK** |
| **Observability** | ✅ Dapr metrics (Prometheus), distributed tracing (OpenTelemetry) | ⚠️ Must instrument manually | 🏆 **Dapr** |
| **Operational Complexity** | ❌ Dapr runtime version management, sidecar health checks | ✅ No additional runtime | 🏆 **Direct SDK** |
| **Secret Rotation** | ✅ New API calls get updated secrets (no app restart) | ⚠️ Depends on SDK caching strategy | 🏆 **Dapr** |
| **Security Model** | ✅ Workload Identity, Service Principal, Managed Identity | ✅ Workload Identity, Managed Identity | 🟰 **Tie** |
| **AKS Integration** | ✅ Dapr extension for AKS (fully managed) | ✅ Native Azure SDK | 🟰 **Tie** |

### Use Case Recommendations

| Scenario | Recommendation | Reasoning |
|----------|---------------|-----------|
| **Teaching Dapr Concepts** | 🏆 **Dapr Secret Store** | Demonstrates Dapr building blocks, component abstraction |
| **Production Polyglot App** | 🏆 **Dapr Secret Store** | Go/Python/Node.js services access secrets uniformly |
| **Azure-Only, .NET-Heavy** | ⚠️ **Direct SDK** | If never leaving Azure, direct SDK reduces complexity |
| **Multi-Cloud Strategy** | 🏆 **Dapr Secret Store** | Switch from AKV → Vault via config change |
| **Performance-Critical** | ⚠️ **Direct SDK** | Eliminate sidecar hop (2-5ms latency savings) |
| **Regulatory Compliance** | ⚠️ **Direct SDK** | May need HSM features, advanced audit policies |

---

## Authentication Evolution: 2021 → 2025

### Current Red Dog (2021): Service Principal + Certificate

```yaml
metadata:
  - name: spnClientId
    value: "<client-id>"
  - name: spnTenantId
    value: "<tenant-id>"
  - name: spnCertificate
    secretKeyRef:
      name: reddog.secretstore
      key: secretstore-cert
```

**Problems:**
- ❌ Certificate stored in Kubernetes secret (chicken-egg problem)
- ❌ Manual certificate rotation required
- ❌ Service Principal credentials can be exfiltrated if pod compromised
- ❌ Deprecated: Azure is moving away from Service Principals for AKS workloads

### Modern Best Practice (2025): Workload Identity

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: reddog.secretstore
  namespace: reddog-retail
  labels:
    azure.workload.identity/use: "true"  # ← Enable Workload Identity
spec:
  type: secretstores.azure.keyvault
  version: v1
  metadata:
    - name: vaultName
      value: reddog-kv-branch
    # No credentials! Workload Identity handled by AKS
```

**Kubernetes Service Account:**
```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: order-service-sa
  namespace: reddog-retail
  annotations:
    azure.workload.identity/client-id: "<managed-identity-client-id>"
```

**Deployment:**
```yaml
spec:
  template:
    metadata:
      labels:
        azure.workload.identity/use: "true"
    spec:
      serviceAccountName: order-service-sa  # ← Links pod to identity
```

**Benefits:**
- ✅ No secrets stored in cluster (zero-trust security)
- ✅ Pod identity federated with Microsoft Entra ID (OIDC)
- ✅ Automatic token rotation (1-hour expiry, auto-renewed)
- ✅ Replaced deprecated Pod Managed Identity (October 2022)
- ✅ Works with both Dapr secret store AND CSI Secrets Store driver

---

## Alternative: Secrets Store CSI Driver

### What It Is

A **Kubernetes Container Storage Interface (CSI) driver** that mounts Azure Key Vault secrets as **files** in pod volumes.

### Architecture

```
┌─────────────────────────────────────────┐
│ Pod: order-service                      │
│                                         │
│  ┌──────────────────┐                  │
│  │ Application      │                  │
│  │ reads:           │                  │
│  │ /mnt/secrets/    │                  │
│  │   db-password    │◄─────────┐      │
│  └──────────────────┘          │      │
│                                  │      │
│  ┌───────────────────────────────┐    │
│  │ Volume: /mnt/secrets/         │    │
│  │ (CSI Driver mounted)          │────┼──→ Azure Key Vault
│  └───────────────────────────────┘    │
└─────────────────────────────────────────┘
```

### Dapr Secret Store vs CSI Driver Comparison

| Feature | Dapr Secret Store | CSI Secrets Store Driver |
|---------|-------------------|-------------------------|
| **Access Method** | HTTP API (`GET /v1.0/secrets/<store>/<key>`) | File system (`cat /mnt/secrets/db-password`) |
| **Secret Rotation** | New API calls get updated value | Polls Key Vault, updates mount (configurable interval) |
| **Multi-Cloud** | ✅ Swap backends (AKV, Vault, AWS) | ❌ Azure-specific |
| **Supported Backends** | 20+ (AWS, GCP, Azure, Vault, local) | Azure Key Vault only |
| **Kubernetes Native** | ❌ Requires Dapr runtime | ✅ Built-in Kubernetes CSI |
| **Observability** | Dapr metrics (Prometheus) | CSI driver + Key Vault provider metrics |
| **AKS Managed Add-on** | ✅ `az aks enable-addons dapr` | ✅ `az aks enable-addons azure-keyvault-secrets-provider` |

**From Microsoft Docs (AKS Dapr FAQ):**

> | - | Dapr secrets API | Secrets Store CSI driver |
> |---|---|---|
> | **Supported secrets stores** | 20+ (AWS, Azure, GCP, Vault, local) | Azure Key Vault only |
> | **Accessing secrets** | Call Dapr secrets API | Access mounted volume or sync to K8s secret |
> | **Secret rotation** | New API calls get updated secrets | Polls and updates mount at configurable interval |
> | **Logging & metrics** | Dapr sidecar logs, Prometheus metrics, health checks | CSI driver + Azure Key Vault provider metrics |

**Recommendation:** For Red Dog modernization, **Dapr secret store** is better because:
1. Polyglot services already using Dapr
2. Teaching Dapr building blocks is a project goal
3. Multi-cloud portability (can demo AWS/GCP variants)
4. Consistent with other Dapr components (pub/sub, state)

---

## Migration Recommendation for Red Dog Modernization

### Phase 1: Migrate to Workload Identity (High Priority)

**Current Risk:** Service Principal certificate-based auth is deprecated and less secure.

**Steps:**
1. Enable Workload Identity on AKS cluster:
   ```bash
   az aks update \
     --resource-group reddog-rg \
     --name reddog-aks \
     --enable-oidc-issuer \
     --enable-workload-identity
   ```

2. Create Managed Identity for each service:
   ```bash
   az identity create \
     --name order-service-identity \
     --resource-group reddog-rg
   ```

3. Grant Key Vault access:
   ```bash
   az role assignment create \
     --role "Key Vault Secrets User" \
     --assignee <managed-identity-client-id> \
     --scope /subscriptions/.../resourceGroups/.../providers/Microsoft.KeyVault/vaults/reddog-kv-branch
   ```

4. Update Dapr component (remove certificate):
   ```yaml
   spec:
     type: secretstores.azure.keyvault
     version: v1
     metadata:
       - name: vaultName
         value: reddog-kv-branch
       # ← No credentials! Workload Identity handles authentication
   ```

5. Annotate Service Accounts:
   ```yaml
   apiVersion: v1
   kind: ServiceAccount
   metadata:
     name: order-service-sa
     annotations:
       azure.workload.identity/client-id: "<managed-identity-client-id>"
   ```

6. Update Deployments:
   ```yaml
   spec:
     template:
       metadata:
         labels:
           azure.workload.identity/use: "true"
       spec:
         serviceAccountName: order-service-sa
   ```

**Timeline:** 1-2 days per service (8 services total = 2 weeks)

### Phase 2: Update Dapr to 1.16 (Covered in Modernization Plan)

Dapr 1.16 (released Sept 2025) includes:
- Workflow improvements
- Better OpenTelemetry integration
- Performance optimizations
- Security patches

### Phase 3: Consider CSI Driver for Bootstrapper (Optional)

**Scenario:** Bootstrapper service needs SQL connection string before Dapr sidecar is ready.

**Solution:** CSI Secrets Store driver mounts secrets **before container starts**, solving bootstrap timing issue.

```yaml
volumes:
  - name: secrets-store
    csi:
      driver: secrets-store.csi.k8s.io
      readOnly: true
      volumeAttributes:
        secretProviderClass: "reddog-sql-secrets"
volumeMounts:
  - name: secrets-store
    mountPath: "/mnt/secrets"
    readOnly: true
```

**Decision:** Only if bootstrap timing becomes an issue. Most services can use Dapr secret store.

---

## Cost Analysis

### Azure Key Vault Pricing (2025)

| Operation | Cost (per 10,000 operations) |
|-----------|------------------------------|
| Secret operations (read) | $0.03 |
| Certificate operations | $3.00 |
| Standard Key Vault | $0.00/month (no vault fee) |
| Premium Key Vault (HSM) | $1.00/month |

**Dapr Secret Store Impact:**
- Dapr caches secrets (configurable TTL), reducing Key Vault API calls
- Example: 10,000 pods × 10 restarts/day × 5 secrets = 500,000 reads/month = **$1.50/month**

**CSI Driver Impact:**
- Polling interval (default: 2 minutes) = 720 polls/day per pod
- 10 pods × 720 polls/day × 30 days × 5 secrets = 1,080,000 reads/month = **$3.24/month**

**Verdict:** Dapr secret store is more cost-effective due to caching.

---

## Pros & Cons Summary

### ✅ Dapr Secret Store (RECOMMENDED for Red Dog)

**Pros:**
1. **Polyglot-friendly:** Go, Python, Node.js, .NET services use same HTTP API
2. **Multi-cloud portability:** Switch Azure Key Vault → HashiCorp Vault via config
3. **Teaching value:** Demonstrates Dapr secret management building block
4. **Built-in caching:** Reduces Azure Key Vault API costs
5. **Observability:** Dapr metrics, distributed tracing out-of-the-box
6. **Zero code changes:** Swap secret backends via component YAML
7. **AKS managed add-on:** `az aks enable-addons dapr` (Microsoft-supported)

**Cons:**
1. **Operational complexity:** Dapr runtime version management, sidecar health
2. **Performance overhead:** 2-5ms HTTP hop to sidecar
3. **Limited native features:** Can't access Azure Key Vault HSM directly
4. **Troubleshooting:** Two processes to debug (app + sidecar)

### ⚠️ Direct Azure Key Vault SDK

**Pros:**
1. **Native features:** Full access to HSM, rotation policies, advanced RBAC
2. **Performance:** Direct HTTPS to Azure (no sidecar hop)
3. **Simpler troubleshooting:** Single process, clear stack traces
4. **No runtime dependency:** No Dapr version to manage

**Cons:**
1. **Vendor lock-in:** Rewrite code to switch to Vault/AWS Secrets Manager
2. **Polyglot burden:** Implement secret logic in each language (.NET SDK, Go SDK, Python SDK)
3. **No built-in caching:** Must implement TTL/refresh logic manually
4. **No observability:** Manual Prometheus metrics, tracing instrumentation

### 🔧 CSI Secrets Store Driver

**Pros:**
1. **Kubernetes-native:** No external runtime (Dapr) required
2. **Bootstrap-friendly:** Secrets available before container starts
3. **File-based:** Works with legacy apps expecting config files
4. **AKS managed add-on:** Microsoft-supported

**Cons:**
1. **Azure-only:** No multi-cloud portability
2. **Polling overhead:** Increased Key Vault API costs
3. **No API access:** Must read files (less elegant than HTTP API)

---

## Recommendation for Red Dog Modernization

### Primary Approach: **Dapr Secret Store + Workload Identity**

**Rationale:**
1. **Alignment with Goals:**
   - ✅ Teaching Dapr (project objective)
   - ✅ Polyglot architecture (Go, Python, Node.js, .NET)
   - ✅ Multi-cloud portability (can demo AWS/GCP)

2. **Security:**
   - ✅ Workload Identity (2025 best practice)
   - ✅ Zero secrets in cluster
   - ✅ Automatic token rotation

3. **Operational:**
   - ✅ AKS Dapr managed extension (Microsoft-supported)
   - ✅ Consistent with other Dapr components
   - ✅ Built-in observability

### Configuration Updates Needed

**Current (2021):**
```yaml
spec:
  type: secretstores.azure.keyvault
  metadata:
    - name: vaultName
      value: reddog-kv-branch
    - name: spnClientId
      value: "<client-id>"
    - name: spnCertificate
      secretKeyRef:
        name: reddog.secretstore
        key: secretstore-cert
```

**Modernized (2025):**
```yaml
spec:
  type: secretstores.azure.keyvault
  version: v1
  metadata:
    - name: vaultName
      value: reddog-kv-branch
    # No credentials! Workload Identity auto-configured via service account
```

### Deployment Priority

| Priority | Task | Impact | Effort |
|----------|------|--------|--------|
| **P0** | Migrate to Workload Identity | 🔴 Security | Medium (2 weeks) |
| **P1** | Update Dapr to 1.16 | 🟡 Features | Low (1 day) |
| **P2** | Add secret caching config | 🟢 Cost | Low (1 day) |
| **P3** | Document multi-cloud swap | 🔵 Teaching | Low (1 day) |

---

## References

### Microsoft Documentation
- [AKS Workload Identity Overview](https://learn.microsoft.com/en-us/azure/aks/workload-identity-overview)
- [Azure Key Vault Secrets Store CSI Driver](https://learn.microsoft.com/en-us/azure/aks/csi-secrets-store-driver)
- [Dapr Extension for AKS](https://learn.microsoft.com/en-us/azure/aks/dapr-overview)
- [Best Practices for Pod Security in AKS](https://learn.microsoft.com/en-us/azure/aks/developer-best-practices-pod-security)

### Dapr Documentation
- [Dapr Secrets Management Overview](https://docs.dapr.io/developing-applications/building-blocks/secrets/secrets-overview/)
- [Azure Key Vault Secret Store Component](https://docs.dapr.io/reference/components-reference/supported-secret-stores/azure-keyvault/)
- [Dapr Azure Authentication with Workload Identity](https://docs.dapr.io/developing-applications/integrations/Azure/azure-authentication/howto-wif/)

### Red Dog Codebase
- Current implementation: `/manifests/branch/base/components/reddog.secretstore.yaml`
- AccountingService usage: `/RedDog.AccountingService/Program.cs:68`
- cert-manager config: `/manifests/branch/dependencies/yaml/cluster-issuer.yaml`

---

**Document Version:** 1.0
**Last Updated:** 2025-11-02
**Next Review:** After Workload Identity migration
