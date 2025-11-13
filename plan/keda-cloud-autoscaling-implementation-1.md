---
goal: "Configure KEDA ScaledObjects for Cloud Autoscaling"
version: 1.0
date_created: 2025-11-14
owner: "Red Dog Modernization Team"
status: 'Planned'
tags: [cloud, keda, autoscaling, production]
---

# KEDA Cloud Autoscaling Implementation

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

**Context:** KEDA 2.18.1 installed and validated. Now configure production autoscaling.

**Scope:** Cloud environments only (Azure, AWS, GCP). Local stays fixed replicas.

## Prerequisites

- ✅ KEDA 2.18.1 installed (`plan/done/upgrade-keda-2.18-implementation-1.md`)
- ✅ RabbitMQ deployed (cloud pub/sub backend)
- ✅ Kubernetes Secrets configured per ADR-0013
- ⚠️ Redis 6.x for state stores (or cloud alternatives: Cosmos/DynamoDB)

## Implementation Tasks

### Phase 1: RabbitMQ Credentials

**Goal:** Wire RabbitMQ connection string into Kubernetes Secrets for TriggerAuthentication

| Task | Description | Done |
|------|-------------|------|
| Create `rabbitmq-keda-auth` Secret | Store AMQP URI with credentials | |
| Test connection from KEDA pod | `kubectl exec -n keda` curl test | |

### Phase 2: Create ScaledObjects

**Primary:**
- **MakeLineService** - RabbitMQ queue depth (`orders` queue)
  - Min: 1, Max: 10 replicas
  - Trigger: Queue length > 50 messages

**Secondary (optional):**
- **OrderService** - CPU utilization (70% threshold)
- **LoyaltyService** - RabbitMQ queue depth (if separate queue)

### Phase 3: Helm Integration

| Task | Description | Done |
|------|-------------|------|
| Add `scaledobjects/` template dir | New Helm templates | |
| Add `.Values.keda.enabled` flag | Enable/disable per environment | |
| Create `values-azure.yaml` KEDA config | RabbitMQ scaler settings | |
| Create `values-aws.yaml` KEDA config | Same pattern | |

### Phase 4: Validation

| Test | Success Criteria |
|------|------------------|
| Deploy to cloud with KEDA enabled | All ScaledObjects created |
| Scale VirtualCustomers to 5 replicas | RabbitMQ queue fills |
| Observe MakeLine autoscale | Replicas increase to 5+ |
| Stop VirtualCustomers | Queue drains, replicas scale down |

## Files to Create

```
charts/reddog/templates/scaledobjects/
  makeline-scaledobject.yaml
  orderservice-scaledobject.yaml (optional)

charts/reddog/templates/triggerauthentications/
  rabbitmq-triggerauth.yaml

values/values-azure.yaml (add keda section)
values/values-aws.yaml (add keda section)
```

## Example ScaledObject

```yaml
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: makeline-scaler
spec:
  scaleTargetRef:
    name: makelineservice
  minReplicaCount: 1
  maxReplicaCount: 10
  triggers:
  - type: rabbitmq
    metadata:
      protocol: amqp
      queueName: orders
      mode: QueueLength
      value: "50"
    authenticationRef:
      name: rabbitmq-trigger-auth
```

## Decision Points

1. **Redis scaler?** Defer - queue depth is clearer metric
2. **Prometheus scaler?** Optional - requires Prometheus deployed
3. **VirtualCustomers scaling?** Manual only (demo control)
4. **Local KEDA?** No - fixed replicas, resource savings

## Dependencies

- RabbitMQ deployed in cloud (`values-azure.yaml` pub/sub config)
- Secrets management per ADR-0013
- Cloud values files created (`plan/done/upgrade-keda-2.18-implementation-1.md` Phase 5 deferred to this plan)

## References

- [KEDA RabbitMQ Scaler](https://keda.sh/docs/scalers/rabbitmq-queue/)
- [ADR-0013: Secret Management](../docs/adr/adr-0013-secret-management-strategy.md)
- [values-azure.yaml.sample](../values/values-azure.yaml.sample)
