# RADIUS: Critical Findings Summary
## Direct Answers to Evaluation Questions

**Date:** November 9, 2025
**RADIUS Version Evaluated:** v0.52.0 (October 14, 2025)

---

## CRITICAL QUESTION 1: Does RADIUS support Google Cloud Platform (GCP)?

### Answer: ❌ **NO - GCP is NOT supported in 2025**

**Evidence:**
- Official statement: "Radius is an open-source project that supports deploying applications across private cloud, Microsoft Azure, and Amazon Web Services, **with more cloud providers to come, such as Google and Alibaba**"
- Design document confirms only `azurerm`, `aws`, and `kubernetes` Terraform providers supported
- GCP provider explicitly listed as "not currently supported" in FAQ
- Reason: Credentials system doesn't support GCP service account JSON authentication

**Timeline:** GCP listed as "planned" with no committed delivery date

**Impact on Red Dog Coffee:**
- ❌ Cannot deploy to GKE using RADIUS recipes
- ❌ Cannot provision Cloud SQL, Cloud Memorystore, Firestore via RADIUS
- ⚠️ Forces choice: exclude GCP or use raw Kubernetes manifests for GCP

---

## CRITICAL QUESTION 2: Recipe Creation - Bicep Only or Multiple Languages?

### Answer: **Bicep (Full) + Terraform (Partial) + Future Languages**

**Current Support:**

| Language | Support Level | Cloud Coverage |
|----------|---------------|---|
| **Bicep** | ✅ FULL | Azure, AWS (via Terraform bridge) |
| **Terraform** | ⚠️ PARTIAL | Azure, AWS, Kubernetes ONLY (NOT GCP) |
| **Helm** | ❌ NOT YET | Planned for future |
| **Pulumi** | ❌ NOT YET | Planned (low priority) |
| **Crossplane** | ❌ NOT YET | Planned |
| **Ansible** | ❌ NOT PLANNED | Not mentioned in roadmap |

**Key Finding:** Terraform recipes CANNOT use GCP provider due to credential handling limitations

**Implication:** If using Terraform for AWS infrastructure, GCP must be provisioned outside RADIUS

---

## CRITICAL QUESTION 3: RADIUS vs Helm Charts - What's at Stake?

### Answer: **Helm charts CAN coexist; gradual adoption possible**

**Current Capability:**
- ✅ Helm charts deploy alongside RADIUS in same cluster
- ✅ Can add RADIUS annotations to existing Helm charts
- ❌ Cannot use Helm as a recipe language (yet)
- ❌ Cannot convert existing Helm charts to RADIUS resources (no auto-conversion)

**Migration Path:**
1. **Phase 1 (Now):** Keep infrastructure Helm charts (Dapr, KEDA, Redis); use RADIUS for applications
2. **Phase 2 (Months 1-3):** Add RADIUS annotations to Helm deployments
3. **Phase 3 (Future - timeline unknown):** Replace with Helm as recipe language once available

**Coexistence Strategy:**
```
Existing Helm Charts (Dapr, KEDA, Redis, etc.)
        ↓
    Kubernetes Cluster
        ↑
RADIUS Applications (with annotations)
```

**Decision:** Can gradually transition; no forced cutover

---

## CRITICAL QUESTION 4: Complete Learning Requirements

### Answer: **Developers need 4 core competencies**

#### Required Learning (Everyone)
1. **RADIUS Concepts** (2-4 hours)
   - Applications, Environments, Recipes, Connections, App Graph
   - Portable resource concept

2. **RADIUS CLI (rad)** (2-4 hours)
   - Installation, rad init, rad deploy, rad run
   - Key commands: env, group, recipe, resource

3. **Bicep Language** (8-16 hours)
   - Variables, parameters, resources, loops, modules
   - RADIUS Bicep extensions (daprSidecar, connections)
   - VSCode extension setup

4. **Dapr Integration** (4-8 hours)
   - State stores, secret stores, pub/sub in RADIUS
   - DaprSidecar extension
   - Connection management

#### Optional (Platform Engineers)
- Terraform for recipes (4-8 hours)
- Creating custom recipes (4-8 hours)
- Multi-cloud environment setup (4-6 hours)

### Total Time Estimate

| Role | Time | Tools |
|------|------|-------|
| **Instructor** | 40-60 hours | rad CLI, Bicep, RADIUS docs |
| **Developer** | 20-30 hours | rad CLI, Bicep, VSCode |
| **Platform Engineer** | 60-80 hours | All + Terraform + recipe authoring |
| **Student** | 10-15 hours | rad CLI only |

### Required Tools
- **Essential:** RADIUS CLI (rad), Bicep CLI, Kubernetes, kubectl
- **Optional:** Terraform CLI (for Terraform recipes), Azure CLI (AKS), AWS CLI (EKS)
- **Development:** VSCode + extensions (Bicep, Docker, Kubernetes)

**Learning curve:** MODERATE - steeper than raw Kubernetes, shallower than full cloud platform

---

## CRITICAL QUESTION 5: RADIUS Maturity in 2025

### Answer: ⚠️ **CAUTIOUSLY PRODUCTION-READY** (with caveats)

**Metrics:**

| Metric | Status |
|--------|--------|
| **CNCF Status** | Sandbox (earliest maturity level) |
| **Version** | 0.52.0 (pre-1.0, released Oct 14, 2025) |
| **Release Cadence** | ~Monthly updates |
| **API Stability** | 🔴 NOT GUARANTEED (pre-1.0 project) |
| **Production Users** | Limited but exists (Millennium bcp since Dec 2024) |
| **Breaking Changes** | 🔴 LIKELY between minor versions |
| **Community Size** | Small (32k website visitors, 24k docs visitors) |

**Production Case Study:**
- **Organization:** Millennium bcp (Portugal's largest bank, 6M+ customers)
- **Timeline:** Production since December 2024
- **Use Case:** Internal Developer Platform (IDP) for multi-cloud deployments
- **Result:** Deployment time reduced from days → minutes; enabling self-service

**Risk Assessment:**

| Risk | Impact | Likelihood | For Teaching Demo |
|------|--------|------------|---|
| API breaking changes | HIGH | HIGH (0.x) | Test thoroughly each release |
| GCP not supported | CRITICAL | CERTAIN | Exclude GCP or use fallback |
| Small community | MEDIUM | CERTAIN | Limited Stack Overflow help |
| Pre-1.0 instability | MEDIUM | LIKELY | Version-pin, test upgrades |
| AWS resource quirks | MEDIUM | MODERATE | Use Terraform recipes |

**Verdict for Teaching:** ✅ Acceptable risk IF you accept pre-1.0 instability and focus on Azure/AWS only

---

## CRITICAL QUESTION 6: RADIUS + Dapr Integration

### Answer: ✅ **EXCELLENT & NATIVE**

**RADIUS natively supports all three Dapr types:**
1. ✅ State Stores
2. ✅ Secret Stores
3. ✅ Pub/Sub Brokers

**Integration approach:**
- Declare Dapr building blocks in Bicep
- RADIUS automatically provisions backing infrastructure
- Injects Dapr sidecars into containers
- Manages credentials and connection strings

**Example Red Dog Coffee workflow:**

```bicep
// Define MakeLineService with Dapr state store
resource makelineService 'Applications.Core/containers@...' = {
  name: 'makeline-service'
  extensions: [{
    kind: 'daprSidecar'
    properties: {
      appId: 'makeline-service'
      appPort: 5200
    }
  }]
}

// Dapr state store (Redis auto-provisioned)
resource makelineState 'Applications.Dapr/stateStores@...' = {
  name: 'redis-makeline'
  properties: {
    recipe: { name: 'default' }
  }
}

// Single deployment command handles everything
// rad deploy
```

**What RADIUS does automatically:**
- Provisions Redis (Azure Cache or ElastiCache)
- Creates Dapr component YAML
- Injects sidecars
- Manages secrets and connection strings
- Creates app graph showing connections

**Current manual approach:** Helm install dapr → Write YAML components → Helm install redis → kubectl apply → Manual configuration

**RADIUS approach:** Just `rad deploy`

**Dapr Components Supported:**
- State stores: ✅ Full
- Secret stores: ✅ Full
- Pub/Sub: ✅ Full
- Input/Output bindings: ⚠️ Partial
- Configuration: ⚠️ Partial

---

## CRITICAL QUESTION 7: Infrastructure Provisioning Clarification

### Answer: **RADIUS handles app→infra binding, not cluster provisioning**

**Three-layer model:**

```
Layer 1: Kubernetes Clusters
  ├─ Azure AKS - provisioned externally (e.g., az aks create)
  ├─ AWS EKS - provisioned externally (e.g., eksctl)
  └─ Local (kind) - provisioned externally (e.g., kind create)

Layer 2: RADIUS Control Plane
  └─ Installed via: rad install kubernetes

Layer 3: Applications & Services
  └─ Deployed via: rad deploy (from Bicep files)
```

**What RADIUS provisions (Layer 3):**
- ✅ Databases (Azure Database, AWS RDS)
- ✅ Caches (Redis, ElastiCache)
- ✅ Message queues (RabbitMQ, SQS)
- ✅ Dapr components
- ✅ Container services

**What RADIUS DOES NOT provision (Layer 1):**
- ❌ Kubernetes clusters
- ❌ Network infrastructure
- ❌ Storage accounts (though can reference them)

**GCP Implication:**
- ❌ Cannot provision Cloud SQL via RADIUS recipe
- ❌ Cannot provision Cloud Memorystore via RADIUS recipe
- ⚠️ Workaround: Provision GCP services outside RADIUS, reference via environment variables

**Separation of concerns (recommended):**
- Use Terraform/IaC for cluster provisioning (Layer 1)
- Use RADIUS for app deployment (Layer 3)
- Use Helm for base infrastructure (Layer 2)

---

## Trade-offs Summary: RADIUS vs Raw Kubernetes

### Quick Comparison

| Factor | RADIUS | Raw K8s + Helm |
|--------|--------|---|
| **GCP Support** | ❌ No | ✅ Yes |
| **Learning Curve** | 🟡 Moderate | 🔴 Steep |
| **Tools Required** | 2 new | 3 standard |
| **Multi-Cloud** | 🟡 Azure/AWS only | ✅ Any cloud |
| **Deployment Speed** | ✅ Fast | ⏱️ Manual |
| **Flexibility** | ⚠️ Constrained | ✅ Unlimited |
| **Infrastructure Visibility** | ✅ App graph | ⏱️ Manual |
| **Production Maturity** | 🟡 Sandbox | ✅ Battle-tested |
| **Breaking Changes** | 🔴 High | ✅ Low |
| **Community Size** | 🔴 Small | ✅ Huge |
| **Dapr Integration** | ✅ Native | ⏱️ Manual |
| **Instructor Overhead** | 🟡 Moderate | ✅ Low |

### Decision Tree

```
Q1: Must support GCP?
├─ YES → Use Raw Kubernetes + Helm (Option B)
└─ NO → Continue to Q2

Q2: Want modern platform engineering demo?
├─ YES → Use Hybrid Approach (Option C) ✅ RECOMMENDED
└─ NO → Use Raw Kubernetes + Helm (Option B)
```

---

## RECOMMENDED APPROACH: Hybrid (Option C)

### Architecture

**Use RADIUS for Azure/AWS scenarios + Raw Kubernetes for GCP**

```
Infrastructure Layer (Helm - same for all clouds)
├─ Dapr
├─ KEDA
├─ Redis
└─ Ingress (Contour)

Azure/AWS Layer (RADIUS)
├─ Applications (Bicep)
├─ Infrastructure recipes (Bicep + Terraform)
└─ Dapr components (auto-configured)

GCP Layer (Raw Kubernetes)
├─ Applications (YAML manifests)
├─ Dapr components (YAML)
└─ Cloud SQL references (env vars)
```

### Why This Works

1. ✅ **Full cloud coverage** - Azure, AWS, GCP all supported
2. ✅ **Modern teaching** - Show RADIUS + Dapr integration
3. ✅ **Production standards** - Raw Kubernetes for flexibility
4. ✅ **Comparison layer** - Same app deployed 3 ways shows trade-offs
5. ✅ **Future-proof** - If RADIUS adds GCP support, trivial to switch
6. ✅ **Dapr integration** - Excellent on Azure/AWS, manual on GCP

### Implementation Steps

1. **Base infrastructure** (Helm - all clouds)
   - Deploy Dapr, KEDA, Redis via Helm
   - Same commands for kind, AKS, EKS, GKE

2. **Azure/AWS deployment** (RADIUS)
   - Write app.bicep with RADIUS resources
   - Demonstrate Dapr integration
   - Show infrastructure-as-code
   - `rad deploy` handles everything

3. **GCP deployment** (Raw Kubernetes)
   - Write Kubernetes manifests
   - Manually configure Dapr components
   - Show YAML-based approach
   - Demonstrate flexibility limitations

4. **Educational value**
   - Students see 3 deployment models
   - Understand trade-offs pragmatically
   - Learn both modern (RADIUS) and traditional (K8s) approaches

---

## Critical Timeline Notes

**For Production Adoption (Red Dog Coffee as demo):**
- ✅ v0.52.0 (Oct 2025) - stable enough for demo
- ⚠️ Expect breaking changes if updating major features
- 🟡 NOT recommended for mission-critical infrastructure
- ✅ Acceptable for teaching/demonstration purposes

**GCP support:**
- ❌ NO timeline provided
- ❌ Still listed as "to come" in 2025
- ⚠️ Do not plan around GCP support arriving soon

**Helm recipes:**
- ⏳ Planned but no timeline
- 🟡 Expected 2026 at earliest
- ✅ Can coexist with Helm charts meanwhile

---

## Recommended Decision

### For Red Dog Coffee Modernization:

**GO WITH HYBRID APPROACH (Option C)**

**Rationale:**
1. Addresses all critical questions with trade-offs visible
2. Teaches modern platform engineering (RADIUS + Dapr)
3. Maintains full multi-cloud compatibility (including GCP)
4. Future-proof (can migrate GCP layer if RADIUS adds support)
5. Educational value (students learn multiple approaches)
6. Risk-managed (no single point of failure)

**Implementation phases:**
- Phase 1: Base infrastructure with Helm (all clouds)
- Phase 2: Azure/AWS with RADIUS + Dapr
- Phase 3: GCP with raw Kubernetes (fallback)
- Phase 4 (Future): Migrate GCP to RADIUS if support arrives

---

## Evidence Sources

**GCP Support:**
- Source: Microsoft Azure Blog (RADIUS launch, 2023)
- Source: RADIUS FAQ documentation (2025)
- Source: Design notes: `recipe/2024-02-terraform-providers.md`

**Recipe Languages:**
- Source: RADIUS documentation - Recipes overview
- Source: GitHub design notes on Terraform providers
- Source: RADIUS Bicep repository (fork for extensibility)

**Helm Integration:**
- Source: RADIUS documentation - "Tutorial: Use Helm to run your first app"
- Source: CNCF sandbox issue comments

**Dapr Integration:**
- Source: Official RADIUS tutorial - Dapr Microservices
- Source: Blog post - "Building Cloud Agnostic Applications with Radius and Dapr"
- Source: Microsoft Learn - video content

**Maturity:**
- Source: CNCF project page (Sandbox status, April 2024)
- Source: GitHub releases (v0.52.0, October 2025)
- Source: RADIUS blog - Millennium bcp case study (December 2024)
- Source: Known limitations documentation

---

**Document Status:** Complete - Critical Questions Answered
**Confidence Level:** HIGH (based on official documentation, design documents, and recent releases)
**Next Review:** February 2026 (check GCP support, v1.0 release, Helm recipes)
