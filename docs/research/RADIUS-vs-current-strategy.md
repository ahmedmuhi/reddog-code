# RADIUS Integration Assessment for Red Dog Coffee Modernization

**Context:** Red Dog Coffee Phase 1 .NET Modernization + Dapr + Multi-Cloud Strategy
**Decision Point:** Adopt RADIUS as platform abstraction layer?

---

## Current Modernization Strategy

### ADR-0007: Cloud-Agnostic Deployment Strategy
**Current approach (from docs/adr/adr-0007-cloud-agnostic-deployment-strategy.md):**
- Container-based applications
- Kubernetes-agnostic via Dapr
- Deploy to: kind (local), AKS (Azure), EKS (AWS), GKE (Google Cloud)
- Infrastructure: Kubernetes manifests + Helm charts

### Dapr State (Current)
- ✅ OrderService, MakeLineService, LoyaltyService use Dapr pub/sub
- ✅ Redis state stores for MakeLineService + LoyaltyService
- ✅ Dapr components via manifests
- ✅ Multi-cloud portable code

### Infrastructure State (Current)
- ✅ Helm charts for: Dapr, KEDA, Redis, RabbitMQ, etc.
- ✅ Kubernetes manifests for applications
- ✅ Local development: manifests/local/branch/
- ❌ No unified infrastructure-as-code for cloud-specific services
- ❌ Cluster provisioning manual (Terraform or az CLI)
- ❌ No app graph or infrastructure visibility

---

## RADIUS: Does It Fit the Strategy?

### Alignment Assessment

| Goal | Current Approach | RADIUS | Assessment |
|------|---|---|---|
| **Polyglot Services** | .NET → Go, Python, Node.js | ✅ Supported (language-agnostic) | ✅ COMPATIBLE |
| **Dapr Integration** | Manual components + Helm | ✅ Native, automatic | ✅ IMPROVES |
| **Multi-Cloud Code** | Dapr handles portability | ✅ Dapr + recipes | ✅ COMPATIBLE |
| **Azure Support** | AKS cluster | ✅ Full Azure support | ✅ COMPATIBLE |
| **AWS Support** | EKS cluster | ✅ Full AWS support | ✅ COMPATIBLE |
| **GCP Support** | GKE cluster | ❌ NO GCP support | ❌ BLOCKS |
| **Kubernetes Flexibility** | Full control via manifests | ⚠️ Recipe constraints | ⚠️ TRADE-OFF |
| **Infrastructure-as-Code** | Helm + manifests | ✅ Bicep recipes | ✅ IMPROVES |
| **Production Maturity** | Battle-tested | 🟡 Pre-1.0 (Sandbox) | 🟡 RISK |

### Verdict: ⚠️ **PARTIAL FIT - GCP is a Blocker**

---

## The GCP Problem in Detail

### Current Red Dog Requirement
From your evaluation request:
> "cloud-agnostic Kubernetes application (Red Dog Coffee) that needs to deploy to:
> - Local (kind)
> - Azure (AKS)
> - AWS (EKS)
> - Google Cloud (GKE) ← **Need to verify if supported**"

**Finding:** GKE is NOT supported by RADIUS

### Impact Analysis

**If you adopt RADIUS:**

**Scenario 1: Azure/AWS focus (exclude GCP)**
```
RADIUS Coverage:
├─ kind (local) ✅
├─ AKS (Azure) ✅
├─ EKS (AWS) ✅
└─ GKE (Google Cloud) ❌ FALLBACK TO RAW K8S

Result: Split deployment strategy
- Azure/AWS use RADIUS (modern)
- GCP uses raw Kubernetes (traditional)
- Comparison point for teaching
- Extra complexity managing two approaches
```

**Scenario 2: Keep current raw Kubernetes approach**
```
No RADIUS - Full Kubernetes/Helm:
├─ kind (local) ✅
├─ AKS (Azure) ✅
├─ EKS (AWS) ✅
└─ GKE (Google Cloud) ✅

Result: Uniform deployment
- All clouds use same tools (kubectl, helm)
- No infrastructure-as-code (Bicep learning)
- Manual Dapr configuration (vs. RADIUS automatic)
- Less operational overhead
- More instructor prep work (multiple tools)
```

---

## Three Decision Paths

### Path A: Adopt RADIUS (Hybrid Approach)
**Use RADIUS for Azure/AWS + Raw K8s for GCP**

**Pros:**
- ✅ Modern platform engineering demonstration
- ✅ Excellent Dapr integration (automatic)
- ✅ Infrastructure-as-code with Bicep
- ✅ Shows real-world polyglot architecture
- ✅ Teaches cloud-agnostic design patterns
- ✅ Production case study available (Millennium bcp)
- ✅ App graph visualization
- ✅ Self-service infrastructure via recipes

**Cons:**
- ❌ GCP requires fallback to raw Kubernetes
- ❌ Instructors learn 2 deployment methods
- ❌ Students see split strategy (pedagogically confusing)
- ⚠️ Pre-1.0 stability risk (monthly breaking changes)
- ⚠️ Requires learning Bicep (new language)
- ⚠️ Small community (limited support)
- ⚠️ AWS has some non-idempotent resource quirks

**Effort:**
- Setup: 3-4 weeks (RADIUS learning + validation)
- Maintenance: Moderate (version updates, recipe management)
- Student learning: +2-3 hours (RADIUS + Bicep intro)
- Instructor training: 40-60 hours

**Timeline:**
- Phase 1: Azure/AWS with RADIUS
- Phase 2: GCP with raw Kubernetes
- Phase 3 (future): Migrate GCP if RADIUS adds support

**Teaching Value:** ⭐⭐⭐⭐ High (shows multiple approaches, realistic constraints)

---

### Path B: Stay with Raw Kubernetes + Helm (No RADIUS)
**Current approach - extend for all clouds**

**Pros:**
- ✅ Supports ALL clouds equally (including GCP)
- ✅ Battle-tested production pattern
- ✅ Large community (Stack Overflow, documentation)
- ✅ Standard industry tools (no learning curve for instructors)
- ✅ Zero breaking changes risk
- ✅ Maximum flexibility
- ✅ Students learn production-grade skills
- ✅ Can adopt RADIUS later if it matures

**Cons:**
- ❌ Manual Dapr component YAML (vs. RADIUS automatic)
- ❌ No unified infrastructure-as-code
- ❌ More deployment steps (lower demo speed)
- ❌ No app graph visualization
- ❌ Instructor must manage multiple tools (kubectl, helm, docker, etc.)
- ❌ Higher cognitive load for students

**Effort:**
- Setup: 1-2 weeks (mostly existing knowledge)
- Maintenance: Low (mature tools)
- Student learning: Same as current
- Instructor training: 0-10 hours (tools already familiar)

**Timeline:** Immediate (use existing Helm + manifests strategy)

**Teaching Value:** ⭐⭐⭐ Good (standard production practices, but less modern)

---

### Path C: RADIUS-First (Azure/AWS Only, Drop GCP)
**Commit to RADIUS, exclude GCP from demo**

**Pros:**
- ✅ Unified modern approach (no split strategy)
- ✅ Full RADIUS features (recipes, app graph, etc.)
- ✅ Excellent learning platform for cloud-agnostic design
- ✅ Matches real-world platform engineering trends
- ✅ Simplest to explain (not confusing split)

**Cons:**
- ❌ CANNOT demonstrate GCP deployment
- ❌ Ignores one-third of major cloud providers
- ❌ Limits demo scenarios (no GCP audience)
- ⚠️ Pre-1.0 stability risk
- ⚠️ GCP support promised but not delivered (student concerns valid)
- 🔴 Contradicts stated requirement: "deploy to... Google Cloud (GKE)"

**Effort:**
- Setup: 3-4 weeks
- Maintenance: Moderate
- Student learning: +2-3 hours (Bicep)
- Instructor training: 40-60 hours

**Timeline:**
- Phase 1: RADIUS on Azure/AKS
- Phase 2: RADIUS on AWS/EKS
- Phase 3: Explain GCP limitation to students

**Teaching Value:** ⭐⭐⭐⭐ High (unified approach), but ❌ Missing goal (GCP)

---

## Recommendation Matrix

| Path | Supports GCP | Modern | Unified | Complexity | Risk | Teaching Value |
|------|---|---|---|---|---|---|
| **A: Hybrid (RADIUS + Raw K8s)** | ✅ | ✅ | ⚠️ | High | Moderate | ⭐⭐⭐⭐ |
| **B: Raw K8s Only** | ✅ | ❌ | ✅ | Moderate | Low | ⭐⭐⭐ |
| **C: RADIUS Only** | ❌ | ✅ | ✅ | Moderate | Moderate | ⭐⭐⭐⭐ |

---

## RECOMMENDED CHOICE: Path A (Hybrid)

### Rationale

1. **Meets all requirements:**
   - ✅ Deploy to kind, AKS, EKS, GKE
   - ✅ Multi-cloud strategy
   - ✅ Dapr integration
   - ✅ Polyglot architecture

2. **Teaches valuable lessons:**
   - Shows real-world constraints (GCP support gap)
   - Demonstrates fallback strategies
   - Students learn 2 deployment models
   - Honest about technology maturity levels

3. **Future-proof:**
   - If RADIUS adds GCP, trivial to migrate
   - Base infrastructure (Helm) unchanged
   - Smooth transition path

4. **Manages risk:**
   - RADIUS instability isolated to Azure/AWS layers
   - GCP uses proven Kubernetes approach
   - Fallback strategy available
   - Can revert to all-Kubernetes if needed

5. **Pedagogical value:**
   - Shows infrastructure flexibility
   - Demonstrates trade-off decisions
   - Realistic platform engineering scenario
   - Students understand when to use which tools

### Implementation Structure

```
Red Dog Coffee Modernization (Hybrid)

Layer 1: Base Infrastructure (Helm - all clouds)
├─ install-dapr.sh (helm install dapr/dapr)
├─ install-keda.sh (helm install keda/keda)
├─ redis-values.yaml (helm install redis/redis)
├─ rabbitmq-values.yaml (helm install rabbitmq/rabbitmq)
└─ Works identically on: kind, AKS, EKS, GKE

Layer 2a: Azure/AWS Deployment (RADIUS)
├─ app.bicep (RADIUS application definition)
├─ recipes/ (Bicep + Terraform recipes)
├─ rad init, rad deploy, rad run
├─ Automatic Dapr component provisioning
├─ Infrastructure-as-code
└─ Supported clouds: Azure (AKS), AWS (EKS)

Layer 2b: GCP Deployment (Raw Kubernetes)
├─ manifests/gcp/ (Kubernetes YAML)
├─ dapr-components.yaml (manual Dapr setup)
├─ kubectl apply -f manifests/
└─ Supported clouds: Google Cloud (GKE)

Comparison Tools
├─ deployment-comparison.md (show all 3 approaches)
├─ trade-offs-analysis.md (RADIUS vs K8s)
└─ when-to-use-what.md (decision framework)
```

### Learning Path (Students)

**Phase 1: Base Kubernetes (1-2 weeks)**
- Deploy to local (kind)
- Learn kubectl basics
- Understand Helm charts
- Deploy Dapr + Redis via Helm

**Phase 2: Multi-Cloud Comparison (1-2 weeks)**
- Deploy same app to AKS using RADIUS (automatic Dapr)
- Deploy same app to EKS using RADIUS (automatic infrastructure)
- Deploy same app to GKE using raw Kubernetes (manual Dapr)
- Compare experiences, see trade-offs

**Phase 3: Infrastructure-as-Code (1-2 weeks)**
- Write Bicep recipes
- Understand RADIUS app graph
- Learn when RADIUS helps vs. when it doesn't

**Phase 4: Production Patterns (1-2 weeks)**
- Multi-cloud deployment strategies
- When to abstract (RADIUS) vs. when to control (Kubernetes)
- Technology selection decisions

### File Structure for Red Dog Coffee

```
reddog-code/
├─ docs/research/
│  ├─ RADIUS-evaluation-2025.md (detailed research)
│  ├─ RADIUS-critical-findings.md (executive summary)
│  ├─ RADIUS-vs-current-strategy.md (this file)
│  └─ RADIUS-implementation-plan.md (phase-by-phase)
│
├─ infrastructure/
│  ├─ base/ (Helm for all clouds)
│  │  ├─ install-dapr.sh
│  │  ├─ install-keda.sh
│  │  ├─ redis-values.yaml
│  │  └─ rabbitmq-values.yaml
│  │
│  ├─ radius/ (Azure/AWS)
│  │  ├─ app.bicep (main application)
│  │  ├─ recipes/
│  │  │  ├─ redis-store.bicep
│  │  │  ├─ database.bicep
│  │  │  └─ queue.bicep
│  │  ├─ azure-values.json (AKS config)
│  │  └─ aws-values.json (EKS config)
│  │
│  └─ kubernetes/ (GCP fallback)
│     ├─ manifests/
│     │  ├─ order-service.yaml
│     │  ├─ makeline-service.yaml
│     │  ├─ dapr-components.yaml
│     │  └─ ...
│     └─ gke-setup.sh
│
├─ tutorials/
│  ├─ 01-local-deployment.md (kind + Helm)
│  ├─ 02-azure-radius.md (AKS + RADIUS)
│  ├─ 03-aws-radius.md (EKS + RADIUS)
│  ├─ 04-gcp-kubernetes.md (GKE + raw K8s)
│  └─ 05-comparison-analysis.md
│
└─ docs/adr/
   ├─ adr-0008-radius-adoption.md (decision record)
   └─ adr-0009-hybrid-deployment-strategy.md
```

---

## Implementation Timeline

### Phase 0: Research & Validation (Weeks 1-2)
- Validate RADIUS v0.52.0 stability with sample app
- Test Azure/AWS deployment
- Confirm GCP deployment requires fallback
- Gather instructor feedback

### Phase 1: Base Infrastructure Setup (Weeks 3-6)
- Document Helm-based base infrastructure
- Create scripts for kind, AKS, EKS, GKE
- Test deployment consistency
- Write tutorials for Phase 1

### Phase 2: RADIUS Integration (Weeks 7-12)
- Migrate Azure/AWS to RADIUS recipes
- Write app.bicep for Red Dog services
- Create Azure/AWS-specific recipes
- Test multi-cloud RADIUS deployment
- Write tutorials for Phase 2

### Phase 3: GCP Fallback (Weeks 13-16)
- Document GCP Kubernetes manifests
- Write Dapr components YAML manually
- Create GCP-specific tutorials
- Document trade-offs

### Phase 4: Comparison & Documentation (Weeks 17-20)
- Create comparison guide (all 3 approaches)
- Develop decision framework documentation
- Gather instructor feedback
- Refine learning paths

### Phase 5: Instructor Training (Weeks 21-22)
- Train instructors on RADIUS
- Train instructors on Bicep
- Prepare troubleshooting guides
- Mock student labs

---

## Success Criteria

### Technical Criteria
- ✅ Red Dog Coffee deploys to kind, AKS, EKS, GKE
- ✅ Dapr components auto-configured on Azure/AWS
- ✅ Manual Dapr setup on GCP works equivalently
- ✅ Same container images deploy everywhere
- ✅ No GCP-specific code in application

### Pedagogical Criteria
- ✅ Students understand cloud-agnostic design
- ✅ Students can explain RADIUS strengths/limitations
- ✅ Students learn modern platform engineering (RADIUS)
- ✅ Students learn production Kubernetes (fallback)
- ✅ Clear decision framework for tool selection

### Operational Criteria
- ✅ Deployment process documented and reproducible
- ✅ Troubleshooting guides written
- ✅ Version pinned (RADIUS, Dapr, Kubernetes)
- ✅ Scripts automated where possible
- ✅ Instructor training completed

---

## Risk Mitigation

### RADIUS Stability Risk
**Risk:** Pre-1.0 breaking changes
**Mitigation:**
- Pin RADIUS version (e.g., 0.52.0)
- Test major version upgrades in staging
- Maintain fallback to all-Kubernetes approach
- Monitor GitHub releases for breaking changes

### GCP Limitation Disappointment
**Risk:** Students/stakeholders expect GCP support
**Mitigation:**
- Clearly document GCP limitation
- Explain RADIUS roadmap (planned but no timeline)
- Show GCP works with fallback strategy
- Frame as "realistic technology landscape" lesson

### Instructor Learning Curve
**Risk:** Instructors unfamiliar with RADIUS/Bicep
**Mitigation:**
- 40-60 hour instructor training program
- Microsoft Learn resources (free)
- Hands-on lab exercises
- Gradual rollout (start with Azure/AWS)

### Maintenance Burden
**Risk:** Complex infrastructure to maintain
**Mitigation:**
- Automate everything possible (scripts)
- Document troubleshooting for each approach
- Version-pin all dependencies
- Regular compatibility testing

---

## Alternative: Deferral Option

### If RADIUS maturity concerns too high:

**Defer RADIUS adoption to Phase 2 (2026-2027)**

```
2025 (NOW):
└─ Red Dog modernization continues with raw Kubernetes
   ├─ Faster implementation (no RADIUS learning curve)
   ├─ Lower risk (proven approach)
   ├─ Full GCP support
   └─ All clouds supported equally

2026:
└─ RADIUS reassesses
   ├─ Check if v1.0 released
   ├─ Evaluate if GCP support added
   ├─ Decide on Phase 2 migration if beneficial
   └─ By then, Helm recipes may be available

2027:
└─ RADIUS Phase 2 modernization (if warranted)
```

**Advantage:** Lower near-term risk, can adopt RADIUS later once mature

**Disadvantage:** Misses opportunity to demonstrate modern platform engineering now

---

## Final Recommendation

### **ADOPT PATH A (HYBRID) IN PHASES**

**Phase 1 (Weeks 1-6): Foundation with Raw Kubernetes**
- Deploy Red Dog to kind, AKS, EKS, GKE using Helm + manifests
- Establish baseline for comparison
- Full multi-cloud support working

**Phase 2 (Weeks 7-16): Add RADIUS for Azure/AWS**
- Gradually migrate AKS deployment to RADIUS
- Gradually migrate EKS deployment to RADIUS
- Keep GKE as raw Kubernetes reference

**Phase 3 (Weeks 17-22): Comparative Analysis & Documentation**
- Create side-by-side deployment guides
- Document trade-offs clearly
- Train instructors on both approaches

**Outcome:**
- ✅ Meets all technical requirements (including GCP)
- ✅ Teaches modern platform engineering (RADIUS)
- ✅ Demonstrates realistic technology constraints
- ✅ Provides fallback strategy
- ✅ Future-proof (can migrate GCP if RADIUS supports it)

---

## Decision Checklist

**Before committing to this path, verify:**

- [ ] RADIUS v0.52.0 stability acceptable for teaching demo
- [ ] Team capacity for 20+ week implementation
- [ ] Instructor availability for training
- [ ] GCP limitation acceptable for stakeholders
- [ ] Willing to manage pre-1.0 project
- [ ] Can test thoroughly before student labs

**If ANY of these unchecked, consider Path B (Raw Kubernetes only) instead.**

---

**Prepared for:** Red Dog Coffee Modernization Planning
**Date:** November 9, 2025
**Recommendation:** Adopt Hybrid Approach (Path A) with phased implementation
**Next Step:** Schedule stakeholder alignment meeting to confirm
