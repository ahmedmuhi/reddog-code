# RADIUS Project Evaluation Report (2025)
## Multi-Cloud Support & Tooling Requirements for Red Dog Coffee

**Research Date:** November 2025
**Current RADIUS Version:** v0.52.0 (October 14, 2025)
**CNCF Status:** Sandbox Project (since April 2024)
**Last Updated:** 2025-11-09

---

## Executive Summary

RADIUS (Project RADIUS from Microsoft/CNCF) is a **cloud-native application platform** that abstracts over Kubernetes and cloud infrastructure. While it shows promise for multi-cloud deployment and integrates excellently with Dapr, **GCP support remains incomplete** and is a critical blocker for Red Dog Coffee's deployment strategy.

### Key Findings

| Factor | Status | Evidence |
|--------|--------|----------|
| **GCP Support** | ❌ NOT OFFICIALLY SUPPORTED | Only Azure, AWS, Kubernetes. GCP planned "to come" |
| **Terraform GCP Support** | ❌ NOT SUPPORTED | Terraform providers: Azure, AWS, Kubernetes only |
| **Bicep Support** | ✅ FULL SUPPORT | Complete for applications and recipes |
| **Helm Integration** | ✅ COEXISTENCE (Limited) | Annotations to existing Helm; Helm recipes planned |
| **Dapr Integration** | ✅ EXCELLENT | Native support for state stores, secrets, pub/sub |
| **Production Readiness** | ⚠️ EARLY STAGE | Millennium bcp production case study (Dec 2024), but still Sandbox |
| **Learning Curve** | 🟡 MODERATE | Requires: RADIUS CLI, Bicep, RADIUS concepts |

---

## SECTION 1: GCP Support Status

### Direct Answer: ❌ **NO - GCP is NOT officially supported in RADIUS 2025**

### Evidence

**Official Statement from RADIUS Documentation:**

> "Radius is an open-source project that supports deploying applications across private cloud, Microsoft Azure, and Amazon Web Services, **with more cloud providers to come, such as Google and Alibaba**."

**Source:** Microsoft Azure Blog, RADIUS Launch (2023); Confirmed in search results (2025)

### Current Supported Cloud Providers

1. **Azure** ✅ - Full support (Microsoft-backed)
2. **AWS** ✅ - Full support (Terraform AWS provider)
3. **Kubernetes** ✅ - Full support (on-premises, kind, etc.)
4. **GCP** ❌ - **NOT SUPPORTED** (Planned for future)

### Terraform Provider Limitation for GCP

**Design Document:** `design-notes/recipe/2024-02-terraform-providers.md`

**Current Terraform Support in RADIUS Recipes:**
- ✅ `azurerm` - Azure Resource Manager
- ✅ `aws` - Amazon Web Services
- ✅ `kubernetes` - Kubernetes resources
- ❌ `google` (GCP) - **NOT SUPPORTED**

**Reason for GCP Limitation:**

> "Terraform Recipes currently support the Azure, AWS, and Kubernetes providers, plus any provider that does not require any credentials or configuration to be passed in (e.g. Oracle, GCP, etc. are not currently supported)."

**Source:** RADIUS FAQ documentation (2025)

**Root Cause:** RADIUS credential management system currently lacks full support for providers requiring complex authentication (like GCP's service account JSON). The Terraform providers design document acknowledges this and lists GCP among providers under consideration for future support.

### GCP Services Cannot Be Provisioned

With RADIUS recipes, you **cannot**:
- Provision Cloud SQL instances
- Provision Firestore databases
- Use GCP load balancers
- Access other GCP-specific services

**Workaround:** You could manually provision GCP infrastructure outside RADIUS and then reference it, but this breaks the RADIUS value proposition of unified infrastructure-as-code.

### Timeline for GCP Support

- **Announced:** 2023 (at RADIUS launch)
- **Status:** Still planned "to come"
- **No committed timeline** in any official documentation

---

## SECTION 2: Recipe Language Support

### Direct Answer: **Bicep (Full) + Terraform (Partial) + Planned Future Languages**

### Current Recipe Languages

#### 1. **Bicep** ✅ FULL SUPPORT

**What can you do:**
- Define complete Radius applications in Bicep
- Create portable infrastructure recipes
- Use all Azure resources, AWS resources via Terraform provider bridge
- Access Dapr building blocks natively
- Use Bicep extensions for Dapr sidecars

**Example:**
```bicep
// Radius application with Dapr state store recipe
import radius as radius

param environment string
param location string = 'westus'

resource app 'Applications.Core/applications@2023-10-01-preview' = {
  name: 'reddog'
  properties: {
    environment: environment
  }
}

resource orderService 'Applications.Core/containers@2023-10-01-preview' = {
  name: 'order-service'
  parent: app
  properties: {
    container: {
      image: 'order-service:latest'
    }
    // Dapr sidecar with state store
    extensions: [
      {
        kind: 'daprSidecar'
        properties: {
          appId: 'order-service'
          appPort: 5100
        }
      }
    ]
  }
}
```

#### 2. **Terraform** ⚠️ PARTIAL SUPPORT

**What can you do:**
- Use Terraform modules for Azure resources
- Use Terraform modules for AWS resources
- Use Terraform modules for Kubernetes resources

**What you CANNOT do:**
- Use GCP Terraform provider
- Use Oracle Terraform provider
- Use any provider requiring credential/configuration pass-in

**Terraform Recipes Example:**
```bicep
// Radius recipe using Terraform
resource storageRecipe 'Applications.Datastores/redisStores@2023-10-01-preview' = {
  name: 'redis-cache'
  properties: {
    environment: environment
    recipe: {
      templateKind: 'terraform'
      templatePath: 'ghcr.io/my-org/redis-recipe:latest'
    }
  }
}
```

**Credential Challenge:** Terraform recipes require credentials stored in RADIUS UCP (User Control Plane). GCP credentials require special handling (JSON service account files) that the current credential system doesn't fully support.

#### 3. **Helm** ❌ NOT YET SUPPORTED (Planned)

**Current Status:**
- Helm charts can be **deployed alongside** RADIUS applications
- Helm annotations can be added to existing Helm charts to integrate with RADIUS
- Helm as a **recipe language** is not yet available

**What's planned:**
> "Based on community interest we will support additional integrations such as Helm, Pulumi or Crossplane for resource management."

**Source:** RADIUS FAQ (2025)

**Workaround:** You can add RADIUS annotations to existing Helm charts:

```yaml
# Example: Adding RADIUS to existing Helm chart
apiVersion: v1
kind: Chart
metadata:
  name: dapr-helm-chart
annotations:
  radapp.io/enabled: "true"
  radapp.io/recipe: "dapr-stack"
```

#### 4. **Pulumi & Crossplane** ⏳ PLANNED (No Timeline)

- Listed as "future support" in design documents
- No committed timeline
- Community feedback requesting this

#### 5. **Ansible** ❌ NOT PLANNED

- Not mentioned in any RADIUS roadmap or design documents
- RADIUS focuses on declarative IaC (Bicep, Terraform) rather than imperative configuration management

### Recipe Language Support Matrix

| Language | Applications | Recipes | Clouds | Status |
|----------|--------------|---------|--------|--------|
| **Bicep** | ✅ Full | ✅ Full | Azure, AWS (via TF bridge) | Production |
| **Terraform** | ❌ No | ⚠️ Partial | Azure, AWS, K8s (NOT GCP) | Production (limited) |
| **Helm** | ❌ No | ❌ No | N/A | Planned |
| **Pulumi** | ❌ No | ❌ No | N/A | Planned |
| **Crossplane** | ❌ No | ❌ No | N/A | Planned |
| **Ansible** | ❌ No | ❌ No | N/A | Not planned |

### Bicep Learning Curve

**RADIUS uses Bicep because:**
- Declarative syntax (easier than imperative languages)
- Cloud platform teams without programming backgrounds can use it
- Validation during compilation
- Better authoring experience than ARM templates

**Learning Resources:**
- Microsoft Learn: "Fundamentals of Bicep" (free training)
- RADIUS documentation: Bicep VSCode extension setup
- Multiple Udemy courses available

**Time to Proficiency:** 1-2 weeks for basic competency

---

## SECTION 3: Helm Charts - Migration & Coexistence Strategy

### Direct Answer: **Helm charts can coexist with RADIUS; gradual adoption possible**

### Helm & RADIUS Coexistence

**You CAN:**
- Deploy existing Helm charts in the same Kubernetes cluster running RADIUS
- Add RADIUS annotations to existing Helm charts to integrate with RADIUS app graph
- Use Helm charts for infrastructure (Dapr, KEDA, Redis, RabbitMQ) alongside RADIUS applications

**You CANNOT:**
- Define a Helm chart as a RADIUS recipe (Helm recipes are planned, not available)
- Automatically convert Helm charts to RADIUS resources
- Use Helm package manager commands within RADIUS recipes (currently)

### Migration Path: Helm → RADIUS

**Phase 1: Coexistence (Immediate)**
```
Existing Helm Charts (Dapr, KEDA, Redis)
        ↓
    Kubernetes Cluster
        ↑
RADIUS Applications + Annotations
```

**Phase 2: Annotation-Based Integration (Months 1-3)**
- Add RADIUS annotations to existing Helm deployments
- RADIUS gains visibility into Helm-deployed resources
- App graph shows connections between RADIUS apps and Helm resources

```yaml
# Example: Helm chart with RADIUS annotations
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: statestore
  annotations:
    radapp.io/recipe: "dapr-state-store"
spec:
  type: state.redis
```

**Phase 3: Future - Helm as Recipe Language (Timeline Unknown)**
- Once Helm recipe support is available, you could write recipes using Helm charts
- This would allow fully declarative infrastructure in RADIUS
- Current timeline: Unknown (listed as "planned")

### Current Helm Chart Deployments in Red Dog Coffee

**Must remain as Helm for now:**
1. **Dapr** - Installed via Helm, RADIUS integrates with Dapr components
2. **KEDA** - Kubernetes autoscaling, installed via Helm
3. **Redis** (if using) - Helm chart with RADIUS recipe wrapper
4. **RabbitMQ** (if using) - Helm chart with RADIUS recipe wrapper
5. **Contour Ingress** - Automatically installed by RADIUS (can be customized via Helm from v0.51.0+)

**Can transition to RADIUS recipes:**
- Custom databases (Cloud SQL, etc.) - Once provider support available
- Application-specific backing services - Write as Bicep recipes

### Helm + RADIUS Best Practices

1. **Keep infrastructure Helm charts separate** from RADIUS applications
2. **Use RADIUS recipes for application-level services** (databases, caches provisioned per-app)
3. **Annotate Helm deployments** to connect them to RADIUS app graph
4. **Use RADIUS recipes for Dapr components** - Use daprSidecar extension instead of separate Helm install

---

## SECTION 4: Complete Learning Requirements

### Everything Developers/Instructors Need to Learn

#### Level 1: RADIUS Fundamentals (Week 1)
1. **Concepts (1-2 hours)**
   - Applications vs Environments vs Recipes
   - Connections and dependencies
   - App graph visualization
   - Portable resources

2. **RADIUS CLI (2-4 hours)**
   - Installation: `rad install kubernetes`
   - Init app: `rad init`
   - Deploy: `rad deploy`
   - Run locally: `rad run`
   - Key commands: env, group, recipe, resource, workspace

3. **Simple Deployment (4-8 hours)**
   - Deploy basic container to local (kind) cluster
   - Deploy to Azure AKS
   - View app graph in dashboard

#### Level 2: Bicep for RADIUS (Week 2-3)
1. **Bicep Basics (8-16 hours)**
   - Variables, parameters, outputs
   - Resource declarations
   - Loops and conditions
   - Modules and imports
   - Using `radius` import for RADIUS types

2. **RADIUS Bicep Extensions (4-8 hours)**
   - Dapr sidecar extension
   - Connections syntax
   - Environment variables
   - Secret references

#### Level 3: Infrastructure Recipes (Week 3-4)
1. **Bicep Recipes (8-16 hours)**
   - Author recipe from template
   - Azure resources (Bicep only)
   - AWS resources (Terraform in recipe)
   - Dapr component recipes
   - Testing recipes locally

2. **Terraform Recipes (Optional, 8-12 hours)**
   - When to use Terraform over Bicep
   - Terraform module structure
   - Provider configuration
   - AWS-specific resource provisioning
   - **Note:** GCP not available

#### Level 4: Multi-Cloud Deployment (Week 4-5)
1. **Azure AKS Deployment (4-6 hours)**
   - Cluster setup
   - RADIUS environment configuration for AKS
   - Troubleshooting Bicep/ARM templates

2. **AWS EKS Deployment (4-6 hours)**
   - EKS cluster setup
   - RADIUS environment configuration for EKS
   - Terraform recipes for AWS services

3. **GCP/GKE Deployment (BLOCKED)**
   - ❌ Not officially supported
   - ❌ Cannot use Terraform GCP provider in recipes
   - ⚠️ Would require custom workarounds or raw Kubernetes manifests

#### Level 5: Integration with Dapr (Week 5-6)
1. **Dapr + RADIUS (8-12 hours)**
   - Dapr state store recipe
   - Dapr secret store recipe
   - Dapr pub/sub recipe
   - DaprSidecar extension
   - Testing Dapr connections

### Total Learning Time Estimate

| Role | Time | Priority |
|------|------|----------|
| **Instructor** | 40-60 hours | Essential |
| **Developer** | 20-30 hours | Essential |
| **Platform Engineer** | 60-80 hours | Essential (recipes) |
| **Student** | 10-15 hours | For deployment labs |

### Required Tools & Prerequisites

#### 1. **RADIUS CLI (rad)**
- Linux/macOS/Windows
- Version: 0.52.0+ recommended
- Installation: `curl -s https://docs.radapp.io/install.sh | bash`

#### 2. **Bicep CLI**
- Standalone or via Azure CLI: `az bicep install`
- VSCode extension: "Bicep" by Microsoft
- Version: 0.24.24+ recommended

#### 3. **Kubernetes CLI & Cluster**
- kubectl (v1.25+)
- Kubernetes cluster options:
  - Local: kind, minikube, Docker Desktop
  - Azure: AKS
  - AWS: EKS

#### 4. **Terraform CLI (optional, for Terraform recipes)**
- Version: 1.5.0+ recommended
- Required only if using Terraform recipes

#### 5. **Azure CLI (optional, for AKS)**
- For Azure deployments: `az login`, cluster management

#### 6. **AWS CLI (optional, for EKS)**
- For AWS deployments: `aws configure`, cluster management
- Note: EKS support confirmed; test thoroughly

#### 7. **VSCode Extensions**
- Bicep (Microsoft)
- RADIUS (if available)
- Docker (Microsoft)
- Kubernetes (Microsoft)

#### 8. **Dapr CLI & Runtime**
- Dapr CLI: `wget -q https://raw.githubusercontent.com/dapr/cli/master/install/install.sh -O - | /bin/bash`
- Version: 1.12.0+ recommended
- Essential for testing RADIUS + Dapr integration

### Knowledge Prerequisites

**For Instructors/Instructional Designers:**
- ✅ Kubernetes fundamentals required
- ✅ Basic infrastructure concepts
- ✅ IaC concepts (Bicep or Terraform familiarity helps)
- ⚠️ Dapr concepts helpful (can teach alongside RADIUS)

**For Developers:**
- ✅ Containerization (Docker)
- ✅ Basic Kubernetes knowledge
- ❌ Bicep knowledge not required (can learn via RADIUS)
- ❌ Cloud platform knowledge not required

---

## SECTION 5: Maturity & Risk Assessment

### Is RADIUS Production-Ready in 2025?

#### Answer: ⚠️ **CAUTIOUSLY - With Caveats**

### RADIUS Stability Metrics

| Metric | Status |
|--------|--------|
| CNCF Status | Sandbox (earliest maturity level) |
| Current Version | 0.52.0 (not 1.0, but recent) |
| Release Cadence | ~Monthly updates |
| Breaking Changes | Moderate (pre-1.0 project) |
| Production Use | Limited (but exists) |
| Community Size | Growing but small |

### Production Case Study: Millennium bcp

**Organization:** Portugal's largest private bank with 1.3M+ branches, 6M+ customers

**Timeline:**
- Production deployment: December 2024
- Scale: Internal Developer Platform (IDP) for bank-wide application deployments
- Duration: 1+ year in production (as of Nov 2025)

**Use Case:**
- Multi-cloud deployment (Azure + AWS)
- Reduce deployment time from days to minutes
- Self-service platform for developers
- Infrastructure orchestration across clouds

**Why significant:**
- Large enterprise production use
- Multi-cloud cloud-agnostic requirement met (Azure/AWS)
- GitHub Actions + Flux GitOps integration
- Presenter at KubeCon London 2025

**Limitations:**
- RADIUS repository states: "Radius is not yet ready for production workloads"
- However, Millennium bcp chose to use it despite this warning
- GitHub issue indicates early adopter status

### CNCF Sandbox Status

**What this means:**
- **Earliest maturity level** in CNCF hierarchy
- Sandbox → Incubating → Graduated
- Project accepted: April 2024 (1.5 years old as CNCF project)
- No timeline for incubation status

**Implications:**
- API stability not guaranteed
- Breaking changes between minor versions possible
- May not be suitable for long-term production with minimal maintenance
- Community vetting is ongoing

### Release Stability (v0.x.0)

**Major Releases in 2024-2025:**
- v0.47.0 (2024)
- v0.48.0 (2024)
- v0.49.0 (2024)
- v0.50.0 (2024)
- v0.51.0 (2025)
- v0.52.0 (Oct 14, 2025) - Latest

**Pattern:** Monthly to bi-weekly releases

**Risk Factors:**
- Semantic versioning: 0.x.0 = not 1.0 stable
- Pre-release status means breaking changes are possible
- Each release may include API changes
- Upgrade path requires testing

### Known Limitations (From Official Docs)

1. **Resource Naming**
   - Resources must have unique names per type in workspace
   - No underscores allowed in names
   - Gateway and container cannot share names

2. **Kubernetes Namespace Changes**
   - Changing namespace requires delete + redeploy of entire application
   - Workaround available but cumbersome

3. **AWS Idempotency Issues**
   - Some AWS resource types are non-idempotent
   - Bicep recipes may fail on re-application
   - Terraform recipes recommended for AWS

4. **Terraform Provider Gaps**
   - GCP not supported (critical for your use case)
   - Oracle not supported
   - Custom provider credentials challenging

5. **Kubernetes Bicep Resource Limits**
   - Kubernetes resources deployed via Bicep don't auto-generate UCP IDs
   - Manual workarounds required
   - Limits infrastructure linking capabilities

### Risk Assessment for Red Dog Coffee Teaching Demo

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|-----------|
| **GCP not supported** | CRITICAL | Certain | Switch to Azure/AWS for demo OR use raw K8s manifests |
| **Breaking changes** | HIGH | Likely (pre-1.0) | Pin RADIUS version, test upgrades thoroughly |
| **Helm recipe delays** | MEDIUM | Likely | Coexist Helm+RADIUS for now |
| **Limited community** | MEDIUM | Certain | Fewer Stack Overflow answers, slower issue resolution |
| **AWS quirks** | MEDIUM | Moderate | Use Terraform recipes for AWS, extensive testing |
| **Licensing** | LOW | Very unlikely | Apache 2.0, Microsoft backs it |

### Comparison: RADIUS vs Raw Kubernetes Approach

For teaching purposes, consider this trade-off:

| Factor | RADIUS | Raw K8s + Helm |
|--------|--------|----------------|
| **Learning Curve** | Moderate (RADIUS + Bicep) | Steep (kubectl + Helm + manifests) |
| **GCP Support** | ❌ No | ✅ Yes |
| **Time to Deploy** | Fast (rad deploy) | Manual (helm install, kubectl apply) |
| **Portability Demo** | Good (Bicep recipes) | Manual (rewrite Helm values) |
| **Multi-Cloud** | Azure/AWS only | Any cloud (including GCP) |
| **Production Ready** | 🟡 Early (Sandbox) | ✅ Battle-tested |
| **Instructor Overhead** | Moderate (new tool) | Low (standard tools) |
| **Flexibility** | Limited (recipe constraints) | Unlimited |
| **Infrastructure Clarity** | High (app graph) | Medium (manual relationships) |

---

## SECTION 6: RADIUS + Dapr Integration

### Maturity: ✅ **EXCELLENT & NATIVE**

### How RADIUS & Dapr Work Together

**Problem They Solve:**
- **Dapr:** Application-level cloud-agnosticism (state stores, secrets, pub/sub)
- **RADIUS:** Infrastructure-level cloud-agnosticism (cluster provisioning, service provisioning)

**Together:** Fully portable applications across Azure, AWS, and on-premises

### Native RADIUS Support for Dapr

RADIUS natively supports **three Dapr resource types:**

1. **State Stores**
   ```bicep
   resource stateStore 'Applications.Dapr/stateStores@2023-10-01-preview' = {
     name: 'reddog-state'
     properties: {
       recipe: {
         name: 'default'
       }
     }
   }
   ```

2. **Secret Stores**
   ```bicep
   resource secretStore 'Applications.Dapr/secretStores@2023-10-01-preview' = {
     name: 'reddog-secrets'
     properties: {
       recipe: {
         name: 'default'
       }
     }
   }
   ```

3. **Pub/Sub Brokers**
   ```bicep
   resource pubsub 'Applications.Dapr/pubSubBrokers@2023-10-01-preview' = {
     name: 'orders-pubsub'
     properties: {
       recipe: {
         name: 'default'
       }
     }
   }
   ```

### DaprSidecar Extension

**Attaching Dapr to containers:**
```bicep
resource orderService 'Applications.Core/containers@2023-10-01-preview' = {
  name: 'order-service'
  properties: {
    container: {
      image: 'reddog-order-service:latest'
      ports: [
        {
          containerPort: 5100
        }
      ]
    }
    extensions: [
      {
        kind: 'daprSidecar'
        properties: {
          appId: 'order-service'
          appPort: 5100
        }
      }
    ]
  }
}
```

### Deployment Workflow

**User specifies:**
1. Application with containers
2. DaprSidecar extension
3. Dapr state/secret/pubsub building blocks

**RADIUS automatically:**
1. Provisions backing infrastructure (Redis for state store, etc.)
2. Creates Dapr component definitions (YAML)
3. Injects sidecars into containers
4. Connects containers to Dapr components
5. Manages credentials and configuration

**Result:** No manual Helm charts, no YAML manipulation needed

### Red Dog Coffee + RADIUS + Dapr Benefits

**Current Setup (manual):**
```
Services → Write Dapr YAML files → Create secrets → Helm install Redis → Manual configuration
```

**With RADIUS:**
```
Services in Bicep → Specify daprSidecar + connections → rad deploy → Done
```

### Examples from Official Documentation

**Source:** https://docs.radapp.io/tutorials/dapr/

The official tutorial shows:
- Frontend container connected to backend via Dapr service invocation
- State store recipe automatically provisioning Redis
- Pub/Sub broker for messaging
- All defined in single Bicep file

### Dapr Component Types Supported

| Component Type | Supported | Recipe Available |
|---|---|---|
| State Stores | ✅ | ✅ (e.g., redis default) |
| Secret Stores | ✅ | ✅ |
| Pub/Sub | ✅ | ✅ |
| Input Bindings | ✅ | ⚠️ Limited |
| Output Bindings | ✅ | ⚠️ Limited |
| Configuration | ✅ | ⚠️ Limited |

### Testing RADIUS + Dapr Locally

```bash
# 1. Install RADIUS
rad install kubernetes

# 2. Create environment
rad env create --name local

# 3. Deploy app with Dapr
rad deploy --env local

# 4. Run service locally (Dapr sidecar auto-injected)
rad run --env local
```

### Production Considerations

**Millennium bcp's approach:**
- Used Dapr + RADIUS for multi-cloud workloads
- Reduced operational overhead
- Enabled self-service for developers
- GitOps (Flux) + Radius + Dapr = full CI/CD pipeline

---

## SECTION 7: Infrastructure Provisioning Clarity

### Separation of Concerns

RADIUS handles **application-to-infrastructure binding**, not cluster provisioning itself.

### Three Layers

```
Layer 1: Kubernetes Cluster (External)
  ↓ (Managed outside RADIUS)
  rad install kubernetes → Installs RADIUS control plane
  ↓
Layer 2: RADIUS Control Plane
  ↓ (Installed by RADIUS)
  Environment definition (Bicep) → Configures runtime
  ↓
Layer 3: Applications
  ↓ (Deployed by RADIUS)
  Application definition (Bicep) → Provisions services
```

### Cluster Provisioning (Layer 1)

**You must provision clusters separately:**

**Azure AKS:**
```bash
# Outside RADIUS
az aks create --resource-group mygroup --name myaks
```

**AWS EKS:**
```bash
# Outside RADIUS (e.g., using Terraform or eksctl)
eksctl create cluster --name my-cluster
```

**Local (kind):**
```bash
# Outside RADIUS
kind create cluster --name local
```

**Then install RADIUS:**
```bash
rad install kubernetes --kubeconfig /path/to/kubeconfig
```

### Application-Level Infrastructure (Layer 3)

**RADIUS recipes provision:**
- Databases (Azure Database, AWS RDS)
- Caches (Redis, ElastiCache)
- Message queues (RabbitMQ, SQS)
- Dapr components
- Other backing services

**Example: Redis recipe for MakeLineService**
```bicep
resource redisRecipe 'Applications.Datastores/redisCaches@2023-10-01-preview' = {
  name: 'makeline-redis'
  properties: {
    recipe: {
      name: 'default'
    }
  }
}
```

### GCP Infrastructure Limitation

**Because Terraform GCP provider is not supported:**
- ❌ Cannot provision Cloud SQL from recipes
- ❌ Cannot provision Firestore from recipes
- ❌ Cannot provision Cloud Memorystore (Redis) from recipes
- ❌ Cannot use GCP-specific services

**Workaround:**
1. Provision GCP infrastructure outside RADIUS (Terraform separately)
2. Store credentials in RADIUS secret store
3. Reference external services via environment variables
4. Application connects to pre-provisioned services

This breaks RADIUS's value of unified infrastructure-as-code.

### Environment Configuration (Layer 2)

**Define environment with infrastructure plugins:**
```bicep
param location string
param environment string

resource env 'Applications.Core/environments@2023-10-01-preview' = {
  name: environment
  properties: {
    compute: {
      kind: 'kubernetes'
      resourceId: '/subscriptions/.../resourceGroups/.../providers/Microsoft.ContainerService/managedClusters/myaks'
    }
    providers: [
      {
        portType: 'Dapr'
        config: {}
      }
    ]
  }
}
```

---

## SECTION 8: Trade-offs Summary Matrix

### RADIUS vs Raw Kubernetes + Helm Manifests

| Factor | RADIUS | Raw K8s + Helm |
|--------|--------|---|
| **GCP Support** | ❌ No | ✅ Yes (any cloud) |
| **Learning Curve** | 🟡 Moderate | 🔴 Steep |
| **Tooling Required** | 2 new (rad, Bicep) | 3 standard (kubectl, helm, docker) |
| **Multi-Cloud Portability** | 🟡 Limited (Azure/AWS) | ✅ Full (any K8s cluster) |
| **Time to Deploy** | ✅ Fast (1 command) | ⏱️ Manual (multiple steps) |
| **Flexibility** | ⚠️ Recipe constraints | ✅ Unlimited control |
| **Infrastructure Clarity** | ✅ App graph visualization | ⏱️ Manual relationship mapping |
| **Production Maturity** | 🟡 Sandbox (pre-1.0) | ✅ Battle-tested |
| **Breaking Changes Risk** | 🔴 High (0.x versioning) | ✅ Low (mature projects) |
| **Helm Chart Coexistence** | ✅ Yes (temporary) | ✅ Native |
| **Dapr Integration** | ✅ Native + excellent | ⏱️ Manual integration |
| **Community Size** | 🔴 Small | ✅ Huge |
| **Instructor Overhead** | 🟡 Moderate (new concepts) | ✅ Low (standard stack) |
| **Cost of Mistakes** | 🟡 Moderate (pre-1.0) | ✅ Low (standard) |
| **Long-term Support** | ⚠️ Uncertain (Sandbox) | ✅ Guaranteed (CNCF projects) |

### Decision Matrix for Red Dog Coffee

**Choose RADIUS if:**
- ✅ Teaching cloud-agnostic application design principles
- ✅ Focus on Azure + AWS multi-cloud scenarios
- ✅ Want to minimize operator/DevOps complexity
- ✅ Willing to accept pre-1.0 stability risks
- ✅ Can exclude GCP from demo scenarios
- ✅ Want tightly integrated Dapr + infrastructure example

**Choose Raw K8s + Helm if:**
- ✅ Need to support GCP/multi-cloud fully
- ✅ Require maximum flexibility and control
- ✅ Want proven, battle-tested production patterns
- ✅ Prefer using industry-standard tools
- ✅ Students/instructors already know Kubernetes deeply
- ✅ Cannot accommodate new tool learning (RADIUS CLI, Bicep)

**Hybrid Approach:**
- Use raw Kubernetes + Helm for base infrastructure
- Deploy RADIUS optionally for Azure/AWS scenarios
- Annotate Helm charts with RADIUS metadata
- Gradually transition layers to RADIUS as features stabilize

---

## SECTION 9: Dapr Integration Deep Dive

### RADIUS-Dapr Synergy

**Red Dog Coffee currently has:**
- ✅ Dapr for application-level portability (state, secrets, pub/sub)
- ❌ Manual infrastructure-as-code (Kubernetes YAML, Helm charts)
- ❌ No unified way to provision infrastructure across clouds

**RADIUS adds:**
- ✅ Infrastructure-as-code for application dependencies
- ✅ Automated backing service provisioning (Redis, databases)
- ✅ Multi-cloud deployment automation
- ✅ Application graph showing Dapr connections

### Concrete Red Dog Coffee Example

**Current deployment (without RADIUS):**
```
1. Provision AKS cluster manually
2. helm install dapr/dapr
3. helm install redis/redis
4. kubectl apply -f dapr-components.yaml (manual YAML)
5. Deploy services with kubectl
6. Manual secret management
```

**With RADIUS:**
```bicep
// Single app.bicep file
resource app 'Applications.Core/applications@2023-10-01-preview' = {
  name: 'reddog'
  properties: {
    environment: environment
  }
}

// Order Service with Dapr sidecar
resource orderService 'Applications.Core/containers@2023-10-01-preview' = {
  name: 'order-service'
  parent: app
  properties: {
    container: {
      image: 'order-service:latest'
      env: {
        ORDER_STORE_NAME: 'redis-orders'
      }
    }
    extensions: [
      {
        kind: 'daprSidecar'
        properties: {
          appId: 'order-service'
          appPort: 5100
        }
      }
    ]
    connections: [
      {
        source: 'order-service'
        target: stateStore.id
      }
    ]
  }
}

// Redis state store (automatically provisioned)
resource stateStore 'Applications.Dapr/stateStores@2023-10-01-preview' = {
  name: 'redis-orders'
  properties: {
    recipe: {
      name: 'default'  // Uses default Redis recipe
    }
  }
}

// MakeLine service
resource makelineService 'Applications.Core/containers@2023-10-01-preview' = {
  name: 'makeline-service'
  parent: app
  properties: {
    container: {
      image: 'makeline-service:latest'
    }
    extensions: [
      {
        kind: 'daprSidecar'
        properties: {
          appId: 'makeline-service'
          appPort: 5200
        }
      }
    ]
    connections: [
      {
        source: 'makeline-service'
        target: makelineState.id
      }
    ]
  }
}

resource makelineState 'Applications.Dapr/stateStores@2023-10-01-preview' = {
  name: 'redis-makeline'
  properties: {
    recipe: {
      name: 'default'
    }
  }
}

// Pub/Sub for order events
resource ordersPubSub 'Applications.Dapr/pubSubBrokers@2023-10-01-preview' = {
  name: 'orders'
  properties: {
    recipe: {
      name: 'default'  // Uses Redis pub/sub
    }
  }
}
```

**Deploy command:**
```bash
rad init --template tutorial
rad deploy  # Handles everything: AKS, Redis, Dapr, services
```

### What RADIUS Automatically Does

1. ✅ Connects to Kubernetes cluster
2. ✅ Installs Dapr runtime if needed
3. ✅ Provisions Redis instance (Azure Cache for Redis or ElastiCache)
4. ✅ Creates Dapr component definitions (component.yaml)
5. ✅ Injects sidecars into containers
6. ✅ Manages credentials and connection strings
7. ✅ Creates app graph showing all connections
8. ✅ Sets environment variables on containers

### Multi-Cloud Dapr Example

**Same app.bicep deploys to different clouds:**

**Deploy to Azure:**
```bash
rad env create --name azure --provider azure
rad deploy --env azure
```

**Deploy to AWS:**
```bash
rad env create --name aws --provider aws
rad deploy --env aws
```

**What changes in infrastructure:**
- Azure: Azure Cache for Redis + Azure Container Registry
- AWS: ElastiCache Redis + ECR
- Application code: Zero changes

**This is cloud-agnosticism in action.**

### Limitations for Red Dog Coffee

**Cannot do with RADIUS + Dapr (on GCP):**
- Cloud SQL + Dapr state store (GCP provider unsupported)
- Firestore + Dapr state store (GCP provider unsupported)
- Cloud Pub/Sub + Dapr pub/sub (GCP provider unsupported)

**Could do with raw Kubernetes + Dapr:**
- Manually provision GCP infrastructure
- Install Dapr components via YAML
- Connect services to GCP services via Dapr components
- Full parity with Azure and AWS

---

## SECTION 10: Recommended Path Forward

### Option A: RADIUS (Recommended for Azure/AWS Focus)

**Pros:**
- ✅ Unified infrastructure-as-code (Bicep)
- ✅ Excellent Dapr integration
- ✅ Modern platform engineering approach
- ✅ Less operational complexity
- ✅ Real production case study (Millennium bcp)
- ✅ Teaches cloud-agnostic architecture

**Cons:**
- ❌ No GCP support (critical blocker)
- ❌ Pre-1.0 stability (monthly breaking changes)
- ❌ Small community (limited Stack Overflow help)
- ❌ Requires learning Bicep
- ❌ New tool for instructors

**Implementation:**
1. Deploy only to Azure AKS + AWS EKS
2. Create GCP scenario using raw Kubernetes manifests as comparison
3. Use RADIUS + Dapr for Azure/AWS deployment labs
4. Document limitations and trade-offs for students

---

### Option B: Raw Kubernetes + Helm (Traditional, Safest)

**Pros:**
- ✅ Supports all clouds including GCP
- ✅ Battle-tested in production
- ✅ Standard industry tools (kubectl, helm)
- ✅ Large community
- ✅ Maximum flexibility
- ✅ No breaking changes concerns

**Cons:**
- ⏱️ More manual deployment steps
- ⏱️ Steeper learning curve (multiple tools)
- ⏱️ Requires manual Dapr component YAML
- ❌ No app graph visualization
- ❌ Infrastructure relationships manual

**Implementation:**
1. Use Helm charts for infrastructure (Dapr, Redis, KEDA)
2. Write Kubernetes manifests for applications
3. Support all clouds equally (kind, AKS, EKS, GKE)
4. Teach standard production practices

---

### Option C: Hybrid (Best of Both Worlds)

**Use RADIUS for Azure/AWS + Raw Kubernetes for GCP**

**Architecture:**
```
RADIUS Layer (Azure/AWS scenarios)
    ↓
Kubernetes Layer (Standard for all)
    ↓
Helm Charts (Infrastructure: Dapr, Redis, KEDA)
    ↓
Raw Manifests (Fallback for unsupported clouds)
```

**Implementation:**
1. **Base infrastructure** (same for all clouds):
   - Use Helm charts (Dapr, KEDA, Redis)
   - Deploy to any Kubernetes cluster

2. **Application deployment (Azure/AWS)**:
   - Use RADIUS for infrastructure-as-code
   - Teach Bicep + rad CLI
   - Demonstrate Dapr integration

3. **Application deployment (GCP)**:
   - Use raw Kubernetes manifests
   - Demonstrate YAML approach
   - Manual Dapr component configuration

4. **Comparison layer**:
   - Same application deployed three ways
   - Show trade-offs visually
   - Students learn multiple approaches

**Benefits:**
- ✅ Future-proof (GCP support if RADIUS adds it)
- ✅ Teach modern platform engineering (RADIUS)
- ✅ Teach production standards (Kubernetes)
- ✅ Support all clouds
- ✅ Demonstrates infrastructure flexibility

**Recommendation: Go with Option C for maximum teaching value**

---

## Conclusion

RADIUS is a **promising but incomplete solution** for multi-cloud Kubernetes deployments. While it excels at:
- Cloud-agnostic application platform design
- Dapr integration
- Infrastructure-as-code via Bicep
- Developer experience

It **critically fails** at:
- GCP support (planned but not available)
- Terraform GCP provider support
- Production stability (pre-1.0, Sandbox CNCF status)

### For Red Dog Coffee:

1. **If prioritizing Azure + AWS only:** Use RADIUS (Option A)
2. **If needing full multi-cloud (including GCP):** Use raw Kubernetes + Helm (Option B)
3. **If wanting to teach both approaches:** Use hybrid (Option C) ✅ **RECOMMENDED**

The hybrid approach allows you to demonstrate modern platform engineering practices with RADIUS while maintaining full multi-cloud flexibility and teaching production-grade Kubernetes practices.

---

## References & Sources

### Official Documentation
- RADIUS Project: https://radapp.io/
- RADIUS Docs: https://docs.radapp.io/
- CNCF Sandbox Project: https://www.cncf.io/projects/radius/
- GitHub: https://github.com/radius-project/radius

### Key Research Documents
- Design Notes (Terraform providers): https://github.com/radius-project/design-notes/blob/main/recipe/2024-02-terraform-providers.md
- Bicep Extensibility: https://blog.radapp.io/posts/2024/08/28/how-radius-leveraged-bicep-extensibility/
- RADIUS + Dapr Integration: https://blog.radapp.io/posts/2024/07/11/building-cloud-agnostic-applications-with-radius-and-dapr/
- Millennium bcp Case Study: https://blog.radapp.io/posts/2023/12/06/case-study-how-millennium-bcp-leverages-radius/

### Recent Releases
- RADIUS v0.52.0: https://blog.radapp.io/posts/2025/10/14/announcing-radius-v0.52.0/
- RADIUS GitHub Releases: https://github.com/radius-project/radius/releases

### Related Articles
- "Microsoft's Radius and the future of cloud-native development" (InfoWorld)
- "A Deep Dive into the Microsoft Radius Architecture" (The New Stack)
- "Is Radius Just Another Microsoft Attempt at Lock-in?" (The New Stack)

---

**Document Status:** Complete Research
**Last Verified:** November 9, 2025
**Next Review Date:** February 2026 (check for v1.0 release, GCP support, Helm recipe support)
