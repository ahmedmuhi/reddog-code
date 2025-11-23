# Nginx Ingress Controller Setup (Red Dog)

This document describes how the Nginx Ingress Controller is installed and wired for the Red Dog project across local (kind) and cloud (AKS/EKS/GKE) environments. Architectural rationale lives in [ADR-0010](../adr/adr-0010-nginx-ingress-controller-cloud-agnostic.md); this file only captures Red Dog–specific conventions.

## 1. Repository Locations

- Helm wrapper chart: `charts/external/nginx-ingress/`
- Shared values: `charts/external/nginx-ingress/values-base.yaml`
- Cloud values:
  - Azure: `charts/external/nginx-ingress/values-azure.yaml`
  - AWS: `charts/external/nginx-ingress/values-aws.yaml`
  - GCP: `charts/external/nginx-ingress/values-gcp.yaml`
- Application ingress template: `charts/reddog/templates/ingress.yaml`
- Local dev values example: `values/values-local.yaml` (`ingress` section)

## 2. Installation Commands

### Local (kind)

Red Dog uses the upstream kind manifest, not Helm, for local ingress:

```bash
kubectl apply -f https://raw.githubusercontent.com/kubernetes/engress-nginx/main/deploy/static/provider/kind/deploy.yaml

kubectl wait --namespace ingress-nginx \
  --for=condition=ready pod \
  --selector=app.kubernetes.io/component=controller \
  --timeout=90s
````

### Cloud (AKS/EKS/GKE)

Use the wrapper chart with shared + environment values:

```bash
# Azure
helm install nginx-ingress charts/external/nginx-ingress/ \
  -f charts/external/nginx-ingress/values-base.yaml \
  -f charts/external/nginx-ingress/values-azure.yaml \
  --namespace ingress-nginx --create-namespace

# AWS
helm install nginx-ingress charts/external/nginx-ingress/ \
  -f charts/external/nginx-ingress/values-base.yaml \
  -f charts/external/nginx-ingress/values-aws.yaml \
  --namespace ingress-nginx --create-namespace

# GCP
helm install nginx-ingress charts/external/nginx-ingress/ \
  -f charts/external/nginx-ingress/values-base.yaml \
  -f charts/external/nginx-ingress/values-gcp.yaml \
  --namespace ingress-nginx --create-namespace
```

## 3. Red Dog Ingress Contract

The main Ingress resource is generated from `charts/reddog/templates/ingress.yaml` and driven entirely by values.

Key values (example from `values/values-local.yaml`):

```yaml
ingress:
  enabled: true
  className: nginx
  tls:
    enabled: false
  annotations: {}
  hosts:
    - host: localhost
      paths:
        - path: /
          pathType: Prefix
          service: ui
          port: 80
        - path: /api/orders
          pathType: Prefix
          service: orderservice
          port: 80
        - path: /api/makeline
          pathType: Prefix
          service: makelineservice
          port: 80
```

To expose a new service externally, add a new `paths` entry under the appropriate host in the relevant values file (local or cloud), and redeploy the `reddog` Helm release.

## 4. Invariants and Common Pitfalls

* Always add routes via values files; do **not** edit `charts/reddog/templates/ingress.yaml` directly.
* `ingress.className` must match the ingress class used by the Nginx controller (default `nginx`).
* For local (kind), Nginx is installed via the upstream manifest; do not attempt to install the wrapper chart into kind unless you also adjust values accordingly.
* When `ingress.tls.enabled: true`:

  * `secretName` in the template is `reddog-tls`; either:

    * create that secret manually, or
    * configure cert-manager + issuer annotation in `ingress.annotations`.
* Cloud-specific LB behavior (DNS labels, NLB vs CLB, etc.) is configured via:

  * `charts/external/nginx-ingress/values-{azure,aws,gcp}.yaml`
    Do not duplicate these annotations in application manifests.

## 5. References

* [ADR-0010: Nginx Ingress Controller for Cloud-Agnostic Traffic Routing](../adr/adr-0010-nginx-ingress-controller-cloud-agnostic.md)
* [Helm Multi-Environment Deployment KI](../../knowledge/ki-deploy-helm-multi-environment-001.md)
* [Nginx Ingress Official Docs](https://kubernetes.github.io/ingress-nginx/)
* [Kubernetes Ingress Concepts](https://kubernetes.io/docs/concepts/services-networking/ingress/)
