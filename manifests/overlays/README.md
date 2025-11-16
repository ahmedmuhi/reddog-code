# Environment Overlays

This directory contains per-cloud overrides that sit on top of the shared manifests in `manifests/branch/base`. Apply the base first, then layer on the overlay for the target platform:

- `aws/` – resources that only exist (or differ) in AWS clusters
- `azure/` – Azure-specific overrides
- `gcp/` – GCP-specific overrides

Example (AWS):
```bash
kubectl apply -k manifests/branch/base
kubectl apply -k manifests/overlays/aws
```

For Helm/GitOps pipelines, include the overlay path so the object storage bindings, cert-manager issuers, etc., are swapped out automatically per environment.
