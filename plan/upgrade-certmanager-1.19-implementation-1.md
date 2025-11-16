---
goal: "Deploy cert-manager v1.19.1 for TLS Certificate Management"
version: 2.0
date_created: 2025-11-09
last_updated: 2025-11-16
owner: "Red Dog Modernization Team"
status: 'Planned'
tags: [infrastructure, certmanager, tls, phase-3]
---

# cert-manager v1.19.1 Deployment

![Status: Deferred](https://img.shields.io/badge/status-Deferred-yellow)

**Cloud-only scope (Phase 3 deployment)** – Production and staging clusters get cert-manager; **local/kind environments stay without cert-manager** (developers continue using HTTP locally).

## Scope & Assumptions

- Target clusters (AKS/EKS/GKE) are running **Kubernetes 1.31+** before rollout.
- DNS + ingress already route production domains publicly; Let’s Encrypt HTTP-01 challenges will succeed.
- cert-manager remains **absent from local dev/kind** to keep environments light and avoid HTTPS drift.
- ACME e-mail contacts + secrets are managed via Kubernetes Secrets per environment.
- Helm installations use OCI or jetstack repo with GitOps capture of values files in `plan/` + `manifests/`.

## Quick Reference

### Version
- **cert-manager**: v1.19.1 (October 2025)
- **Kubernetes**: 1.31+ required
- **Challenge Type**: HTTP-01 with Nginx Ingress

### Install Command

```bash
helm repo add jetstack https://charts.jetstack.io --force-update

helm install cert-manager jetstack/cert-manager \
  --namespace cert-manager \
  --create-namespace \
  --version v1.19.1 \
  --set installCRDs=true \
  --wait
```

### ClusterIssuer (Staging - Test First)

```yaml
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-staging
spec:
  acme:
    server: https://acme-staging-v02.api.letsencrypt.org/directory
    email: your-email@example.com  # UPDATE THIS
    privateKeySecretRef:
      name: letsencrypt-staging-account-key
    solvers:
      - http01:
          ingress:
            ingressClassName: nginx
```

### ClusterIssuer (Production)

```yaml
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-production
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: your-email@example.com  # UPDATE THIS
    privateKeySecretRef:
      name: letsencrypt-production-account-key
    solvers:
      - http01:
          ingress:
            ingressClassName: nginx
```

### Ingress Annotation

```yaml
metadata:
  annotations:
    cert-manager.io/cluster-issuer: "letsencrypt-staging"  # or letsencrypt-production
```

## Rate Limits

- **Staging**: 30,000 certs/3 hours (not browser-trusted)
- **Production**: 50 certs/domain/week

**Always test with staging first.**

## Cloud-Specific Notes

### AWS EKS (Custom CNI)
If webhook fails, add to Helm install:
```bash
--set webhook.hostNetwork=true \
--set webhook.securePort=10260
```

### GKE Private Cluster
May need firewall rule for webhook (port 10250).

## When to Implement

Implement when:
1. Deploying to cloud (AKS, EKS, GKE)
2. Have a real domain with DNS configured
3. Need HTTPS for production

Not needed for:
- Local development (kind cluster)
- localhost testing

## Implementation Timeline

### Phase 1 – Readiness & Backups (Days 0-2)
- Verify every target cluster is ≥1.31 and record evidence in session logs.
- Export existing cert-manager resources (CRDs, ClusterIssuers, Certificates, Secrets) from the `cert-manager` namespace.
- Create per-environment Helm values files (`values/cert-manager/<env>.yaml`) capturing webhook overrides, ACME contacts, and image registries.
- Reference `values/cert-manager/staging.yaml` and `values/cert-manager/production.yaml` for the canonical overrides.
- Draft operational note explaining that local/kind continues without cert-manager and HTTPS is only validated in cloud clusters.

### Phase 2 – Staging Upgrade (Days 3-4)
- Install/upgrade cert-manager via Helm with `installCRDs=true` in the staging cluster.
- Apply updated `ClusterIssuer` manifests (staging + prod templates with staging endpoints) committed under `manifests/branch/dependencies/cert-manager/`.
- Annotate staging ingress objects with `cert-manager.io/cluster-issuer` and request a test certificate for a non-critical hostname.
- Validate controller/webhook pod health, confirm Secrets issued, and document results (satisfies TASK-205/206 in the master plan for staging).

### Phase 3 – Production Rollout (Days 5-6)
- Schedule and communicate a 2-hour change window; confirm Let’s Encrypt rate limits for affected domains.
- Upgrade/install cert-manager in production using the vetted Helm values, ensuring CRDs are already present.
- Deploy production `ClusterIssuer`, rotate ACME account keys if required, and annotate production ingresses.
- Issue a pilot certificate, validate TLS on low-traffic endpoints, then enable certificates on primary domains.

### Phase 4 – Validation & Monitoring (Day 7+)
- Configure alerting for certificate expiration, `CertificateRequest` failures, and pod health (`TASK-806`).
- Run HTTPS smoke tests across services; capture evidence for modernization success criteria.
- Document rollback steps (Helm uninstall + secret restore) and keep backups for at least one release cycle.
- Update modernization strategy and session logs with outcomes; promote plan status to **Done** once production is stable for 7 days.

## Deliverables

- Updated Helm values + manifests for staging and production committed to the repo.
- Runbook entry covering cloud-only cert-manager usage vs. HTTP-only local dev.
- Validation report summarizing issuance tests, pilot certificate, and monitoring hooks.
- Monitoring/alerting configuration references (Grafana/Prometheus or cloud-native dashboards).

## Rollback Strategy

1. Keep previous cert-manager Helm release manifests + CRDs exported before upgrade.
2. If issues occur, uninstall cert-manager 1.19, reapply saved 1.3.1 manifests, and restore secrets from backup.
3. Remove TLS annotations to revert services to HTTP temporarily, then reattempt upgrade after remediation.
