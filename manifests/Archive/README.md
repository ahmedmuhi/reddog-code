# Archived Raw Kubernetes Manifests

These manifests are **archived** and no longer used for deployment. All deployment is now handled exclusively through Helm charts (see [ADR-0009](../../docs/adr/adr-0009-helm-multi-environment-deployment.md)).

## What's here

| Directory | Contents | Superseded by |
|---|---|---|
| `branch/base/deployments/` | Raw K8s Deployment + Service manifests | `charts/reddog/templates/` |
| `branch/base/components.deprecated/` | Legacy Dapr component manifests | `charts/reddog/templates/dapr-components/` |
| `branch/dependencies/cert-manager/` | Cluster issuer manifests | `artifacts/cert-manager/` |
| `branch/redmond/` | Branch-specific patches | No longer needed |
| `overlays/{aws,azure,gcp}/` | Cloud-specific Dapr components + service accounts | `charts/reddog/values.yaml` + `values/values-*.yaml` |
| `cloud/secrets/` | RabbitMQ secrets template | Helm secret templates |

## Migration timeline

- **Phase 1c** (commit d76fda2): Dapr components deprecated in favour of Helm-managed components.
- **Phase 3** (this commit): CI workflows updated to stop writing to raw manifests; entire directory archived.

## Current deployment

```bash
# Local (kind)
helm upgrade --install reddog ./charts/reddog -f values/values-local.yaml

# Cloud environments
helm upgrade --install reddog ./charts/reddog -f values/values-<env>.yaml
```
