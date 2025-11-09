# RADIUS Research Index
## Complete Evaluation of RADIUS Project for Red Dog Coffee Modernization

**Research Completed:** November 9, 2025
**RADIUS Version Evaluated:** v0.52.0 (October 14, 2025)
**Total Research Documents:** 3 comprehensive reports

---

## Quick Navigation

### For Quick Answers
→ **START HERE:** `/docs/research/RADIUS-critical-findings.md`
- Direct YES/NO answers to all critical questions
- Evidence links
- Trade-offs summary
- Decision tree
- Read time: 20 minutes

### For Strategic Alignment
→ **THEN READ:** `/docs/research/RADIUS-vs-current-strategy.md`
- Three implementation paths (A, B, C)
- Red Dog Coffee specific recommendations
- Timeline and effort estimates
- File structure for implementation
- Recommendation: Path A (Hybrid)
- Read time: 30 minutes

### For Deep Technical Details
→ **REFERENCE:** `/docs/research/RADIUS-evaluation-2025.md`
- 7 detailed research sections
- Evidence-backed findings
- Complete learning requirements
- Maturity assessment with risk factors
- Known limitations
- Production case studies
- Read time: 45 minutes

---

## Critical Questions Answered

### 1. Does RADIUS support Google Cloud Platform (GCP)?
**Answer:** ❌ **NO**
- Only Azure, AWS, Kubernetes supported
- GCP listed as "planned" with no timeline
- Terraform GCP provider not supported (credential limitation)
- **Location:** RADIUS-critical-findings.md → Section 1

### 2. Recipe Creation - Bicep Only or Multiple Languages?
**Answer:** **Bicep (Full) + Terraform (Partial)**
- Bicep: Full support for applications and recipes
- Terraform: Azure, AWS, Kubernetes only (NOT GCP)
- Helm: Planned but not available
- Pulumi/Crossplane: Planned (low priority)
- **Location:** RADIUS-critical-findings.md → Section 2

### 3. RADIUS vs Helm Charts - What's at Stake?
**Answer:** **Helm charts CAN coexist; gradual adoption possible**
- Helm charts can deploy in same cluster
- RADIUS annotations available for integration
- Helm as recipe language: Planned for future
- No forced migration required
- **Location:** RADIUS-critical-findings.md → Section 3

### 4. Complete Learning Requirements
**Answer:** **4 core competencies required**
- RADIUS CLI (rad) - 2-4 hours
- RADIUS Concepts - 2-4 hours
- Bicep Language - 8-16 hours
- Dapr Integration - 4-8 hours
- **Total:** 20-30 hours for developers, 40-60 hours for instructors
- **Location:** RADIUS-critical-findings.md → Section 4

### 5. RADIUS Maturity in 2025?
**Answer:** ⚠️ **CAUTIOUSLY PRODUCTION-READY**
- CNCF Sandbox status (earliest maturity)
- v0.52.0 (pre-1.0)
- Monthly releases with potential breaking changes
- Production case: Millennium bcp (Dec 2024)
- **Risk Level:** Moderate (acceptable for teaching demo)
- **Location:** RADIUS-critical-findings.md → Section 5

### 6. RADIUS + Dapr Integration?
**Answer:** ✅ **EXCELLENT & NATIVE**
- RADIUS natively supports 3 Dapr types:
  - State Stores ✅
  - Secret Stores ✅
  - Pub/Sub Brokers ✅
- Automatic Dapr sidecar injection
- Automatic backing service provisioning
- **Location:** RADIUS-critical-findings.md → Section 6

### 7. Infrastructure Provisioning Clarification?
**Answer:** **RADIUS handles app→infra binding, not cluster provisioning**
- Clusters provisioned outside RADIUS (Layer 1)
- RADIUS provisions app-level services (Layer 3)
- GCP limitation: Cannot provision Cloud SQL, Firestore, etc.
- **Location:** RADIUS-critical-findings.md → Section 7

---

## Trade-offs Summary

| Factor | RADIUS | Raw K8s + Helm |
|--------|--------|---|
| **GCP Support** | ❌ No | ✅ Yes |
| **Learning Curve** | 🟡 Moderate | 🔴 Steep |
| **Multi-Cloud** | 🟡 Azure/AWS only | ✅ Any cloud |
| **Deployment Speed** | ✅ Fast | ⏱️ Manual |
| **Production Maturity** | 🟡 Sandbox | ✅ Battle-tested |
| **Dapr Integration** | ✅ Native | ⏱️ Manual |
| **Instructor Overhead** | 🟡 Moderate | ✅ Low |

---

## Recommended Decision Path

### Path A: Hybrid (RADIUS + Raw Kubernetes) ✅ **RECOMMENDED**
- Use RADIUS for Azure/AWS scenarios
- Use raw Kubernetes for GCP scenarios
- Supports all clouds
- Teaches modern + traditional approaches
- **Effort:** 20+ weeks
- **Complexity:** High
- **Teaching Value:** ⭐⭐⭐⭐⭐

### Path B: Raw Kubernetes Only
- No RADIUS adoption
- All clouds use kubectl + helm
- Standard industry tools
- **Effort:** 4-6 weeks
- **Complexity:** Moderate
- **Teaching Value:** ⭐⭐⭐

### Path C: RADIUS Only
- Azure/AWS with RADIUS
- No GCP support (unmet requirement)
- Unified modern approach
- **Effort:** 15-18 weeks
- **Complexity:** Moderate-High
- **Teaching Value:** ⭐⭐⭐⭐ (but missing GCP goal)

**→ DETAILED ANALYSIS:** RADIUS-vs-current-strategy.md

---

## Key Findings Summary

### What Makes RADIUS Attractive
1. ✅ Native Dapr integration (automatic provisioning)
2. ✅ Unified infrastructure-as-code (Bicep)
3. ✅ Infrastructure visibility (app graph)
4. ✅ Production case study (Millennium bcp)
5. ✅ Modern platform engineering approach

### What Makes RADIUS Problematic
1. ❌ **GCP NOT supported** (critical blocker)
2. ❌ Pre-1.0 instability (monthly breaking changes)
3. ❌ Terraform provider limitations (GCP, Oracle)
4. ❌ Small community (limited support)
5. ❌ New tool/language to learn (rad CLI, Bicep)

### GCP: The Critical Blocker
- Currently not supported by RADIUS
- Terraform GCP provider cannot be used in recipes
- Planned "to come" but no timeline
- Workaround: Use raw Kubernetes for GCP layer

### Production Readiness
- **For teaching demo:** ✅ Acceptable (pre-1.0 acceptable)
- **For mission-critical:** ❌ Not recommended (Sandbox status)
- **Stability:** Monthly updates, potential breaking changes
- **Case study:** Millennium bcp using successfully (December 2024)

---

## Evidence Sources

All findings backed by:
- ✅ Official RADIUS documentation (docs.radapp.io)
- ✅ RADIUS design documents (GitHub)
- ✅ RADIUS GitHub releases (v0.52.0, October 2025)
- ✅ CNCF project status (Sandbox, April 2024)
- ✅ Blog posts from RADIUS team
- ✅ Production case studies (Millennium bcp)
- ✅ Known limitations documentation

**→ See sources section in each document**

---

## Implementation Framework

If adopting Path A (Hybrid):

### Phase 0: Validation (Weeks 1-2)
- Test RADIUS stability
- Confirm GCP fallback approach works
- Instructor feasibility check

### Phase 1: Base Infrastructure (Weeks 3-6)
- Setup Helm-based infrastructure (all clouds)
- Establish baseline deployment process

### Phase 2: RADIUS Integration (Weeks 7-12)
- Migrate Azure/AWS to RADIUS recipes
- Write Bicep application definitions

### Phase 3: GCP Fallback (Weeks 13-16)
- Document GCP Kubernetes manifests
- Test multi-cloud deployment

### Phase 4: Documentation (Weeks 17-20)
- Create comparison guides
- Develop decision frameworks

### Phase 5: Training (Weeks 21-22)
- Instructor training program
- Troubleshooting guides

**→ DETAILED TIMELINE:** RADIUS-vs-current-strategy.md → Implementation Timeline

---

## Document Ownership & Next Steps

### Current State
- Research: COMPLETE (3 comprehensive reports)
- Status: Ready for stakeholder review
- Confidence: HIGH (based on official sources)

### Recommended Next Actions
1. Review RADIUS-critical-findings.md (executive summary)
2. Validate GCP limitation is acceptable
3. Confirm resource availability for Phase 0 validation
4. Schedule stakeholder alignment meeting
5. If approved, begin Phase 0 validation

### Timeline for Decision
- **Immediate:** Read RADIUS-critical-findings.md
- **This week:** Read RADIUS-vs-current-strategy.md
- **Next week:** Stakeholder alignment meeting
- **Week 3:** Begin Phase 0 validation (if approved)

---

## FAQ

**Q: Can we wait for RADIUS to support GCP?**
A: Possible but risky. No timeline provided. Design document lists GCP "to come" with no committed date. Recommend not blocking modernization on this.

**Q: Is RADIUS production-ready?**
A: Cautiously yes. CNCF Sandbox (pre-1.0), but Millennium bcp using in production. Acceptable for teaching demo, not for mission-critical workloads.

**Q: Must we adopt RADIUS?**
A: No. Path B (raw Kubernetes) fully viable and lower risk. RADIUS is optional optimization that brings value for Azure/AWS but not GCP.

**Q: Can we do hybrid approach gradually?**
A: Yes (recommended). Path A phases RADIUS in over 20 weeks while maintaining raw Kubernetes as fallback throughout.

**Q: What if RADIUS breaks between versions?**
A: Risk mitigated by version pinning (0.52.0) and fallback to raw Kubernetes. Base infrastructure (Helm) unchanged.

---

## Contact & Questions

For questions about this research:
- Review document sources (all linked)
- Check GitHub issues (radius-project/radius)
- Monitor RADIUS blog (blog.radapp.io) for updates
- Test v0.52.0+ in staging before committing

---

**Research Completion Date:** November 9, 2025
**Status:** COMPLETE - Ready for Decision
**Recommendation:** Adopt Path A (Hybrid Approach) with phased implementation
**Risk Level:** Moderate (manageable with fallback strategies)
**Confidence Level:** HIGH (all findings backed by official sources)

**Next Review Date:** February 2026 (check for v1.0, GCP support, Helm recipes)
