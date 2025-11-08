---
goal: "Upgrade cert-manager from 1.3.1 to 1.19 for Let's Encrypt ACME v2 and Security Patches"
version: 1.0
date_created: 2025-11-09
last_updated: 2025-11-09
owner: "Red Dog Modernization Team"
status: 'Planned'
tags: [infrastructure, upgrade, phase-0, certmanager, tls, security]
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

This plan upgrades cert-manager from version 1.3.1 (released 2021) to 1.19 (released 2024), ensuring TLS certificate management uses Let's Encrypt ACME v2 protocol and includes 13 minor versions of security patches.

**Critical Context:**
- cert-manager 1.3.1 is 3+ years outdated with known security vulnerabilities
- Manages TLS certificates for HTTPS access to Red Dog services
- Automates Let's Encrypt certificate issuance and renewal
- cert-manager 1.20+ requires Kubernetes 1.31+ (use 1.19 for K8s 1.30)

**Duration**: 2-3 days (within Phase 0)
**Risk Level**: LOW (TLS infrastructure only, doesn't affect service logic)

## 1. Requirements & Constraints

### Functional Requirements

- **REQ-001**: cert-manager 1.19 must be installed for Kubernetes 1.30 compatibility
- **REQ-002**: Let's Encrypt ACME v2 protocol support required
- **REQ-003**: Automatic certificate renewal before expiration (90-day cycle)
- **REQ-004**: Existing certificates must continue working during upgrade

### Technical Requirements

- **REQ-005**: Kubernetes 1.30+ (K8s 1.31+ required for cert-manager 1.20+)
- **REQ-006**: Helm 3.x or kubectl for installation
- **REQ-007**: Nginx Ingress Controller for HTTP-01 challenge solver

### Security Requirements

- **SEC-001**: TLS certificates issued by trusted CA (Let's Encrypt Production)
- **SEC-002**: Private keys securely stored in Kubernetes Secrets
- **SEC-003**: Certificate renewal automated (no manual intervention)

### Constraints

- **CON-001**: cert-manager 1.19 is latest for Kubernetes 1.30 (1.20+ needs K8s 1.31+)
- **CON-002**: CRD upgrades required (Certificate, Issuer, ClusterIssuer)
- **CON-003**: cert-manager upgrade may trigger certificate re-issuance

## 2. Implementation Steps

### Implementation Phase 1: Backup and Preparation (Day 1)

- **GOAL-001**: Backup current cert-manager configuration

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-101 | Backup existing Certificates: `kubectl get certificates -A -o yaml > certificates-backup.yaml` | | |
| TASK-102 | Backup Issuers: `kubectl get issuers,clusterissuers -A -o yaml > issuers-backup.yaml` | | |
| TASK-103 | Backup cert-manager deployment: `kubectl get deploy -n cert-manager -o yaml > certmanager-backup.yaml` | | |
| TASK-104 | Document current Let's Encrypt account email | | |
| TASK-105 | Verify Let's Encrypt rate limits won't be exceeded (50 certs/domain/week) | | |

### Implementation Phase 2: Helm Chart Upgrade (Day 1-2)

- **GOAL-002**: Upgrade cert-manager via Helm chart

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-201 | Add Jetstack Helm repository: `helm repo add jetstack https://charts.jetstack.io && helm repo update` | | |
| TASK-202 | Review cert-manager 1.19 Helm values for breaking changes | | |
| TASK-203 | Install CRDs separately: `kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.19.0/cert-manager.crds.yaml` | | |
| TASK-204 | Execute Helm upgrade: `helm upgrade cert-manager jetstack/cert-manager --namespace cert-manager --version v1.19.0 --set installCRDs=false --wait` | | |
| TASK-205 | Verify cert-manager pods are running: `kubectl get pods -n cert-manager` | | |
| TASK-206 | Check cert-manager webhook is healthy: `kubectl get validatingwebhookconfigurations cert-manager-webhook` | | |

### Implementation Phase 3: Validation (Day 2-3)

- **GOAL-003**: Validate certificate issuance and renewal

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-301 | Verify existing certificates are still valid: `kubectl get certificates -A` | | |
| TASK-302 | Test certificate issuance (create test Certificate resource) | | |
| TASK-303 | Verify Let's Encrypt ACME v2 endpoint connectivity | | |
| TASK-304 | Check cert-manager logs for errors: `kubectl logs -n cert-manager deployment/cert-manager` | | |
| TASK-305 | Test HTTP-01 challenge solver via Nginx ingress | | |
| TASK-306 | Monitor certificate renewal (check expiry dates < 30 days) | | |

## 3. Alternatives

- **ALT-001**: **Use cert-manager 1.20**
  - **Rejected**: Requires Kubernetes 1.31+, Red Dog uses K8s 1.30

- **ALT-002**: **Manual Certificate Management**
  - **Rejected**: Automation reduces operational burden, manual renewal error-prone

## 4. Dependencies

### Infrastructure Dependencies

- **DEP-001**: Kubernetes 1.30+ clusters
- **DEP-002**: Nginx Ingress Controller (HTTP-01 challenge solver)
- **DEP-003**: Let's Encrypt ACME endpoint accessibility

### Research Dependencies

- **DEP-004**: `docs/research/infrastructure-versions-verification.md`

## 5. Files

- **FILE-001**: `manifests/branch/dependencies/cert-manager/cert-manager.yaml` (Helm release - update version to 1.19)
- **FILE-002**: `manifests/branch/base/issuers/letsencrypt-prod.yaml` (ClusterIssuer for Let's Encrypt)
- **FILE-003**: Ingress resources with `tls` sections (UI, API gateways)

## 6. Testing

- **TEST-001**: Certificate Issuance
  - **Purpose**: Verify cert-manager can issue new certificates
  - **Steps**: Create test Certificate resource, verify Secret created with TLS cert
  - **Success Criteria**: Certificate status shows `Ready: True`

- **TEST-002**: Certificate Renewal
  - **Purpose**: Test automatic renewal before expiration
  - **Steps**: Check certificates expiring < 30 days, verify renewal triggered
  - **Success Criteria**: New certificate issued, Secret updated

- **TEST-003**: HTTPS Access
  - **Purpose**: Verify TLS termination works
  - **Steps**: Access Red Dog UI via HTTPS
  - **Success Criteria**: Valid Let's Encrypt certificate presented, no browser warnings

## 7. Risks & Assumptions

### Risks

- **RISK-001**: **Certificate Re-Issuance Triggers Rate Limit**
  - **Likelihood**: Low
  - **Impact**: Medium (temporary certificate issuance blocked)
  - **Mitigation**: Use Let's Encrypt staging environment for testing, production for final upgrade

### Assumptions

- **ASSUMPTION-001**: cert-manager 1.19 compatible with Kubernetes 1.30 (verified)
- **ASSUMPTION-002**: Let's Encrypt rate limits sufficient (50 certs/domain/week)
- **ASSUMPTION-003**: Existing certificates have >7 days before expiration

## 8. Related Specifications / Further Reading

### Research Documents

- [Infrastructure Versions Verification](../docs/research/infrastructure-versions-verification.md)

### External Documentation

- [cert-manager 1.19 Release Notes](https://github.com/cert-manager/cert-manager/releases/tag/v1.19.0)
- [cert-manager Documentation](https://cert-manager.io/docs/)
- [Let's Encrypt ACME v2](https://letsencrypt.org/docs/acme-protocol-updates/)

### Related Implementation Plans

- [Phase 0: Platform Foundation](./upgrade-phase0-platform-foundation-implementation-1.md)
