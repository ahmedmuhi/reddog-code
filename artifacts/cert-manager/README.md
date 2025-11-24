# cert-manager Deployment Artifacts

This directory stores deployment evidence and rollback artifacts for cert-manager v1.19.1 as specified in the issue Definition of Done: "Backups and rollback artifacts stored under artifacts/cert-manager/"

**Current Status:** Awaiting deployment - artifacts will be populated when cloud cluster access is available.

---

## Directory Structure

```
artifacts/cert-manager/
├── backups/           # Pre-upgrade backups (CRDs, ClusterIssuers, Certificates, Secrets)
├── verification/      # Post-deployment verification evidence (pod status, certificate status)
├── manifests/         # Helm values and deployment configs used during rollout
└── README.md          # This file
```

**Note:** Deployment manifests (ClusterIssuer YAML files) remain in `manifests/branch/dependencies/cert-manager/` as they are part of the infrastructure configuration. This directory stores operational artifacts from the deployment process.

---

## Backups Directory (`backups/`)

Pre-upgrade exports to enable rollback if needed.

### Expected Files:
- `cert-manager-crds-backup-<date>.yaml` - CustomResourceDefinitions export
- `cert-manager-clusterissuers-backup-<date>.yaml` - ClusterIssuer resources
- `cert-manager-certificates-backup-<date>.yaml` - Certificate resources
- `cert-manager-secrets-backup-<date>.yaml` - TLS secrets (store securely)
- `helm-release-backup-<date>.yaml` - Existing Helm release manifest

### Backup Commands:
```bash
# Export CRDs
kubectl get crds -l app.kubernetes.io/name=cert-manager -o yaml > backups/cert-manager-crds-backup-$(date +%Y%m%d).yaml

# Export ClusterIssuers
kubectl get clusterissuers -o yaml > backups/cert-manager-clusterissuers-backup-$(date +%Y%m%d).yaml

# Export Certificates
kubectl get certificates -A -o yaml > backups/cert-manager-certificates-backup-$(date +%Y%m%d).yaml

# Export Helm release
helm get values cert-manager -n cert-manager > backups/helm-release-backup-$(date +%Y%m%d).yaml
```

---

## Verification Directory (`verification/`)

Post-deployment validation evidence showing cert-manager is operational.

### Expected Files:
- `staging-deployment-<date>.txt` - Staging cluster deployment evidence
- `staging-certificate-<date>.yaml` - Test certificate issued in staging
- `staging-clusterissuer-status-<date>.txt` - ClusterIssuer ready status
- `production-deployment-<date>.txt` - Production cluster deployment evidence
- `production-certificate-<date>.yaml` - Production certificate issued
- `production-clusterissuer-status-<date>.txt` - ClusterIssuer ready status

### Verification Commands:
```bash
# Check cert-manager pods
kubectl get pods -n cert-manager -o wide > verification/cert-manager-pods-$(date +%Y%m%d).txt

# Check ClusterIssuer status
kubectl get clusterissuer -o wide > verification/clusterissuer-status-$(date +%Y%m%d).txt
kubectl describe clusterissuer letsencrypt-staging > verification/staging-issuer-details-$(date +%Y%m%d).txt

# Check Certificate status
kubectl get certificates -A > verification/certificates-$(date +%Y%m%d).txt

# Verify webhook
kubectl get validatingwebhookconfigurations -l app.kubernetes.io/name=cert-manager > verification/webhook-status-$(date +%Y%m%d).txt
```

---

## Manifests Directory (`manifests/`)

Helm values and deployment configurations used during rollout.

### Expected Files:
- `staging-values.yaml` - Helm values for staging deployment
- `production-values.yaml` - Helm values for production deployment
- `deployment-notes-<date>.md` - Deployment session notes

### Sample Helm Values:
```yaml
# Example: staging-values.yaml
installCRDs: true
replicaCount: 1

webhook:
  hostNetwork: false  # Set true for AWS EKS if needed
  securePort: 10250

resources:
  requests:
    cpu: 100m
    memory: 128Mi
  limits:
    cpu: 100m
    memory: 128Mi
```

---

## Prerequisites for Deployment

Before populating these artifacts:

1. **Cluster Access:** Staging and production Kubernetes clusters (AKS/EKS/GKE)
2. **Kubernetes Version:** Clusters running Kubernetes 1.31+
3. **Ingress Controller:** Nginx Ingress Controller operational (v1.14.0+)
4. **DNS Configuration:** Public DNS for HTTP-01 challenges
5. **Email Configuration:** Real email for ACME registration (update ClusterIssuer manifests in `manifests/branch/dependencies/cert-manager/`)

---

## Deployment Process

Follow `plan/upgrade-certmanager-1.19-implementation-1.md` for complete procedure:

### Phase 1: Readiness & Backups
- [ ] Verify Kubernetes ≥1.31 on target clusters
- [ ] Export existing resources to `backups/`
- [ ] Create Helm values in `manifests/`
- [ ] Update ClusterIssuer emails in `manifests/branch/dependencies/cert-manager/`

### Phase 2: Staging Deployment
- [ ] Install cert-manager v1.19.1 via Helm
- [ ] Apply ClusterIssuers from `manifests/branch/dependencies/cert-manager/`
- [ ] Request test certificate
- [ ] Capture evidence in `verification/`

### Phase 3: Production Deployment
- [ ] Schedule change window
- [ ] Deploy to production
- [ ] Issue production certificates
- [ ] Capture evidence in `verification/`

### Phase 4: Completion
- [ ] Update `plan/modernization-strategy.md` to ✅ Complete
- [ ] Update `plan/upgrade-certmanager-1.19-implementation-1.md` to "Done"
- [ ] Update session log

---

## Rollback Procedure

If issues occur:

1. Uninstall cert-manager: `helm uninstall cert-manager -n cert-manager`
2. Restore CRDs from `backups/cert-manager-crds-backup-<date>.yaml`
3. Restore ClusterIssuers from `backups/cert-manager-clusterissuers-backup-<date>.yaml`
4. Remove TLS annotations from Ingress to revert to HTTP temporarily
5. Document rollback in session and investigate before reattempt

---

## Related Documentation

- **Implementation Plan:** `plan/upgrade-certmanager-1.19-implementation-1.md`
- **Deployment Manifests:** `manifests/branch/dependencies/cert-manager/`
- **Session Log:** `.claude/sessions/2025-11-24-0013-cert-manager-upgrade-verification.md`

---

**Last Updated:** 2025-11-24  
**Status:** Awaiting cloud cluster access for deployment
