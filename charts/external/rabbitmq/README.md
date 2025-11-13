# RabbitMQ 4.2.0 Cloud Deployment

This directory contains the Helm values needed to deploy RabbitMQ 4.2.0 (Bitnami chart) into cloud clusters. Local development remains on Redis; RabbitMQ is only provisioned for AKS/EKS/GKE per ADR-0007.

## Prerequisites

- Kubernetes 1.30+ cluster with a `premium-ssd` (or equivalent) StorageClass
- Helm 3.14+
- Secrets created from `manifests/cloud/secrets/rabbitmq-secrets.template.yaml`
- `helm repo add bitnami https://charts.bitnami.com/bitnami` (OCI pulls are also supported)

## Deploy / Upgrade

```bash
helm upgrade --install rabbitmq oci://registry-1.docker.io/bitnamicharts/rabbitmq \
  --namespace rabbitmq --create-namespace \
  -f charts/external/rabbitmq/values-cloud.yaml
```

## Key Settings

- `image.tag`: `4.2.0-debian-12-r0` (RabbitMQ 4.2.0 GA)
- `extraConfiguration`: enables the `khepri_db` feature flag for the new metadata store
- `definitionsSecret`: points to exported exchanges/queues (`rabbitmq-load-definitions`)
- `metrics.enabled`: true with ServiceMonitor for Prometheus scraping

## Next Steps

- Update `values/values-<cloud>.yaml` to point Dapr’s `pubsub.rabbitmq` metadata at this cluster
- Configure KEDA ScaledObjects in `plan/keda-cloud-autoscaling-implementation-1.md`
