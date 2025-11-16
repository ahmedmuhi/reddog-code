# cert-manager Helm Values (Cloud Only)

These values files capture environment-specific overrides for the cert-manager 1.19.1 deployment. Apply them only to cloud clusters (AKS/EKS/GKE). Local/kind environments do **not** run cert-manager; keep using HTTP during local development.

## Files
- `staging.yaml` – configuration for pre-production/staging clusters
- `production.yaml` – configuration for production clusters

## Usage
```bash
helm upgrade --install cert-manager jetstack/cert-manager \
  --namespace cert-manager \
  --create-namespace \
  --version v1.19.1 \
  --values values/cert-manager/<env>.yaml \
  --wait
```

Remember to keep ACME account e-mail addresses and API tokens in Kubernetes Secrets; do not hard-code sensitive data here.
