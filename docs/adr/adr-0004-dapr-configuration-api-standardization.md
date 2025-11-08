---
title: "ADR-0004: Dapr Configuration API for Application Configuration Management"
status: "Accepted"
date: "2025-11-02"
authors: "Red Dog Modernization Team"
tags: ["architecture", "decision", "dapr", "configuration", "cloud-agnostic"]
supersedes: ""
superseded_by: ""
---

# ADR-0004: Dapr Configuration API for Application Configuration Management

## Status

**Accepted**

## Context

Red Dog's polyglot microservices architecture (8 services across 5 languages) requires a standardized approach to application configuration management. Services must read configuration values (store IDs, timeouts, feature flags, business rules) at runtime across multiple deployment platforms (AKS, Container Apps, EKS, GKE).

**Key Constraints:**
- Multi-cloud deployment targets require platform-agnostic configuration access
- Configuration must support runtime updates WITHOUT service redeployment (operational agility)
- Teaching/demo focus requires simple, consistent configuration patterns across all services
- REQ-004 (from `plan/orderservice-dotnet10-upgrade.md`) mandates cloud-agnostic implementation
- ADR-0002 establishes Dapr as abstraction layer for platform-specific integrations

**Current Practice (Anti-Pattern):**
- Services use environment variables for application configuration (`Environment.GetEnvironmentVariable("STORE_ID")`)
- Environment variables are **platform-specific**: Kubernetes ConfigMaps, Azure Container Apps environment variables, AWS Parameter Store references
- Configuration changes require container restart or redeployment (downtime or rolling updates)
- No centralized management UI - operators edit YAML files or container settings

**Problem:**
Environment variables create **platform lock-in**:
- **Kubernetes**: `ConfigMap` → mounted as env vars via `envFrom` in Deployment YAML
- **Azure Container Apps**: Environment variables set via `az containerapp update --set-env-vars`
- **AWS EKS**: Secrets Manager / Parameter Store → referenced via CSI driver or init containers
- **GCP GKE**: Secret Manager → referenced via Workload Identity + init containers

Each platform requires different operational workflows to update configuration. Application code is cloud-agnostic, but **configuration management is not**.

**Available Options:**
1. **Environment Variables**: Platform-specific management, requires container restart for updates
2. **Dapr Configuration API**: Cloud-agnostic building block (GA since Dapr 1.0), supports dynamic updates
3. **Direct Cloud SDKs**: Azure App Configuration SDK, AWS AppConfig SDK, GCP Runtime Configurator (violates ADR-0002)

## Decision

**Adopt Dapr Configuration API as the standardized mechanism for all application configuration across Red Dog microservices.**

**Scope:**
- **Use Dapr Configuration API for**: Application settings (storeId, requestTimeout), feature flags (enableLoyalty), business rules (maxOrderSize), operational parameters
- **Use Environment Variables for**: Dapr sidecar configuration (DAPR_HTTP_PORT, DAPR_GRPC_PORT), ASP.NET runtime settings (ASPNETCORE_URLS), container-level infrastructure settings

**Implementation:**
- **Local Development**: `configuration.redis` component (Redis in Docker)
- **Azure (AKS/Container Apps)**: `configuration.azureappconfig` component (Azure App Configuration)
- **AWS (EKS)**: `configuration.postgresql` component (AWS RDS PostgreSQL)
- **GCP (GKE)**: `configuration.postgresql` component (Cloud SQL PostgreSQL)
- **Generic/Self-Hosted**: `configuration.redis` component (Redis cluster)

**Rationale:**
- **CFG-001**: **Cloud-Agnostic Application Code**: All services use `DaprClient.GetConfiguration("reddog.config", keys)` - identical across all platforms. Component YAML differs per platform, application code does not.
- **CFG-002**: **Dynamic Configuration Updates**: Operators update configuration in Azure App Configuration UI, AWS Parameter Store UI, or Redis CLI. Applications subscribe to changes (`DaprClient.SubscribeConfiguration`) and receive updates WITHOUT redeployment.
- **CFG-003**: **Centralized Management**: Single source of truth per environment. Azure App Configuration, AWS Parameter Store, or Redis store provides UI, versioning, audit logs, and RBAC.
- **CFG-004**: **Consistent with ADR-0002**: Uses Dapr abstraction layer for platform integration (same pattern as secrets, state, pub/sub). No direct cloud SDK calls in application code.
- **CFG-005**: **Read-Only Access**: Applications consume configuration as read-only data via Dapr API. Cannot accidentally modify values (security/safety).
- **CFG-006**: **Operational Simplicity**: Operators use cloud-native tooling (Azure Portal, AWS Console, Redis CLI) to manage configuration. No need to edit Kubernetes YAML or redeploy containers.

## Consequences

### Positive

- **POS-001**: **Runtime Configuration Updates**: Change storeId, timeouts, feature flags without redeployment. Subscribe to updates and react dynamically (e.g., enable/disable features based on config).
- **POS-002**: **Zero Application Code Changes Across Platforms**: `DaprClient.GetConfiguration()` call identical on AKS, Container Apps, EKS, GKE. Platform differences isolated to component YAML.
- **POS-003**: **Centralized Operational Control**: Operators use Azure App Configuration UI (Azure), AWS Parameter Store UI (AWS), or Redis Commander (local/self-hosted). Single pane of glass per environment.
- **POS-004**: **Version Control and Audit Logs**: Azure App Configuration and AWS Parameter Store provide built-in versioning, change history, and audit trails. Track who changed what configuration when.
- **POS-005**: **Feature Flag Support**: Built-in feature flag capabilities in Azure App Configuration. Enable/disable features per environment without code deployment.
- **POS-006**: **Configuration Validation**: Cloud services (Azure App Configuration, AWS Parameter Store) support schema validation, key/value constraints, and approval workflows.
- **POS-007**: **Teaching Clarity**: "All Red Dog services use Dapr Configuration API" - simple, consistent message. Demonstrates modern cloud-native configuration patterns.
- **POS-008**: **Gradual Migration**: Can coexist with environment variables during transition. Read critical settings from Dapr Config API, fallback to env vars for backward compatibility.

### Negative

- **NEG-001**: **Dapr Dependency for Configuration**: Services cannot start without Dapr sidecar providing Configuration API. Increases coupling to Dapr runtime (mitigated by ADR-0002 - already committed to Dapr).
- **NEG-002**: **Additional Infrastructure**: Requires configuration store backend (Azure App Configuration, AWS Parameter Store, Redis). Local development needs Redis container.
- **NEG-003**: **Learning Curve**: Teams must learn Dapr Configuration API, component configuration, and subscription patterns. More complex than `Environment.GetEnvironmentVariable()`.
- **NEG-004**: **Configuration Store Costs**: Azure App Configuration and AWS Parameter Store incur costs (typically $1-10/month for small deployments). Redis (self-hosted) requires infrastructure management.
- **NEG-005**: **Initial Setup Overhead**: Each environment requires configuration store provisioning, Dapr component YAML creation, and initial key/value population.
- **NEG-006**: **Subscription Complexity**: Real-time configuration updates require subscription handling, error recovery, and application state management. More code than static env vars.
- **NEG-007**: **Cold Start Latency**: Services must call `GetConfiguration()` at startup. Adds ~10-50ms latency vs reading environment variables from memory.

## Alternatives Considered

### Environment Variables (Status Quo)

- **ALT-001**: **Description**: Continue using environment variables for all application configuration. Manage via Kubernetes ConfigMaps (AKS/EKS/GKE), Azure Container Apps settings, AWS Parameter Store references.
- **ALT-002**: **Rejection Reason**: Platform lock-in. Each cloud requires different operational workflows (kubectl edit configmap vs az containerapp update vs AWS Console). Configuration updates require container restart (downtime or rolling updates). No dynamic updates, no centralized management UI, no audit logs. Contradicts ADR-0002 cloud-agnostic strategy.

### Direct Cloud Configuration SDKs

- **ALT-003**: **Description**: Use Azure App Configuration SDK (.NET), AWS AppConfig SDK (boto3), GCP Runtime Configurator SDK directly in application code. Conditional compilation or runtime platform detection.
- **ALT-004**: **Rejection Reason**: Violates ADR-0002. Requires platform-specific code (`#if AZURE`, `#if AWS`, `#if GCP`) or runtime branching. Testing complexity (must mock 3+ SDKs). Human error risk (forgot to update one platform's code path). Does not simplify operations.

### Kubernetes ConfigMaps with Volume Mounts

- **ALT-005**: **Description**: Use Kubernetes ConfigMaps mounted as volumes. Applications read config files from `/etc/config/` directory. Watch files for updates using FileSystemWatcher.
- **ALT-006**: **Rejection Reason**: Kubernetes-only solution. Does not support Azure Container Apps (serverless, not full Kubernetes). File watching adds complexity (inotify, polling). No structured configuration (key/value parsing required). ConfigMaps limited to 1MB size. No versioning, no audit logs beyond Kubernetes events.

### Spring Cloud Config Server / Consul

- **ALT-007**: **Description**: Deploy Spring Cloud Config Server or HashiCorp Consul as centralized configuration service. Services call REST API to fetch configuration.
- **ALT-008**: **Rejection Reason**: Additional infrastructure component to deploy, scale, and maintain across 4 platforms. Spring Cloud Config is Java-centric (not polyglot-friendly). Consul requires separate cluster, service mesh integration. Dapr Configuration API provides same functionality with lower operational overhead (backed by cloud-native services).

### Hardcoded Configuration Files (ConfigMap-Like)

- **ALT-009**: **Description**: Bundle configuration files (JSON, YAML) in Docker images. Read from `/app/config/appsettings.json` at startup.
- **ALT-010**: **Rejection Reason**: Requires Docker image rebuild for every configuration change. No runtime updates. Cannot vary configuration per environment without multiple Docker tags. Security risk (configuration baked into image, visible in container registry).

## Implementation Notes

- **IMP-001**: **Configuration Store Selection**:

| Environment | Component Type | Backend Service | Rationale |
|-------------|---------------|-----------------|-----------|
| **Local Development** | `configuration.redis` | Redis (Docker: `docker run -d -p 6379:6379 redis:6`) | Lightweight, no cloud dependencies, fast startup |
| **Azure (AKS/Container Apps)** | `configuration.azureappconfig` | Azure App Configuration | Native Azure service, UI, feature flags, versioning, Workload Identity auth |
| **AWS (EKS)** | `configuration.postgresql` | AWS RDS PostgreSQL | No AWS-native Dapr component yet, PostgreSQL widely supported, managed service |
| **GCP (GKE)** | `configuration.postgresql` | Cloud SQL PostgreSQL | No GCP-native Dapr component yet, PostgreSQL widely supported, managed service |

- **IMP-002**: **Component YAML Examples**:

**Local Development (Redis)**:
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: reddog.config
spec:
  type: configuration.redis
  version: v1
  metadata:
  - name: redisHost
    value: localhost:6379
  - name: redisPassword
    value: ""
  - name: enableTLS
    value: "false"
```

**Azure (App Configuration)**:
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: reddog.config
spec:
  type: configuration.azureappconfig
  version: v1
  metadata:
  - name: connectionString
    secretKeyRef:
      name: azureAppConfigConnectionString
      key: connectionString
  - name: subscribeAllChanges
    value: "true"
  - name: maxRetries
    value: "3"
```

**AWS/GCP (PostgreSQL)**:
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: reddog.config
spec:
  type: configuration.postgresql
  version: v1
  metadata:
  - name: connectionString
    secretKeyRef:
      name: postgresqlConnectionString
      key: connectionString
  - name: table
    value: "reddog_configuration"
  - name: maxIdleTimeoutOld
    value: "60s"
```

- **IMP-003**: **Application Code Pattern (.NET Example)**:

**Startup Configuration Retrieval**:
```csharp
// Program.cs
using Dapr.Client;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDaprClient();

var app = builder.Build();

// Get configuration at startup
using var daprClient = app.Services.GetRequiredService<DaprClient>();

var configKeys = new[] { "storeId", "requestTimeout", "maxOrderSize", "enableLoyalty" };
var configItems = await daprClient.GetConfiguration("reddog.config", configKeys);

var storeId = configItems["storeId"].Value;
var timeout = int.Parse(configItems["requestTimeout"].Value);
var maxOrderSize = int.Parse(configItems["maxOrderSize"].Value);
var enableLoyalty = bool.Parse(configItems["enableLoyalty"].Value);

// Store in IOptions pattern or static class for access
app.Services.Configure<OrderServiceConfig>(config =>
{
    config.StoreId = storeId;
    config.RequestTimeout = timeout;
    config.MaxOrderSize = maxOrderSize;
    config.EnableLoyalty = enableLoyalty;
});
```

**Dynamic Configuration Subscription**:
```csharp
// Background service for runtime updates
public class ConfigurationUpdateService : BackgroundService
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<ConfigurationUpdateService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configKeys = new[] { "enableLoyalty", "maxOrderSize" };

        var subscription = await _daprClient.SubscribeConfiguration(
            "reddog.config",
            configKeys
        );

        await foreach (var items in subscription.Source.WithCancellation(stoppingToken))
        {
            if (items.Keys.Count == 0)
            {
                _logger.LogInformation("Subscribed to config changes: {SubId}", subscription.Id);
                continue;
            }

            foreach (var (key, configItem) in items)
            {
                _logger.LogInformation(
                    "Config updated: {Key} = {Value}",
                    key,
                    configItem.Value
                );

                // Update application state dynamically
                switch (key)
                {
                    case "enableLoyalty":
                        FeatureFlags.EnableLoyalty = bool.Parse(configItem.Value);
                        break;
                    case "maxOrderSize":
                        BusinessRules.MaxOrderSize = int.Parse(configItem.Value);
                        break;
                }
            }
        }
    }
}
```

- **IMP-004**: **Migration Strategy**:
  1. **Phase 1**: Add Dapr Configuration API support alongside existing env vars (dual-read)
  2. **Phase 2**: Migrate critical settings to Dapr Config API (storeId, feature flags)
  3. **Phase 3**: Deprecate env vars for application config (keep only Dapr sidecar settings)
  4. **Phase 4**: Remove env var fallback code after 2+ sprints of validation

- **IMP-005**: **Configuration Key Naming Convention**:
  - Use camelCase: `storeId`, `requestTimeout`, `maxOrderSize`
  - Prefix with service name for shared stores: `orderService.maxOrderSize`, `loyaltyService.pointsMultiplier`
  - Use feature flag prefix: `feature.enableLoyalty`, `feature.enableReceipts`

- **IMP-006**: **Environment Variable Exceptions** (Still Use Env Vars):
  - Dapr sidecar ports: `DAPR_HTTP_PORT`, `DAPR_GRPC_PORT`
  - ASP.NET runtime: `ASPNETCORE_URLS`, `ASPNETCORE_ENVIRONMENT`
  - Container orchestration: `POD_NAME`, `POD_NAMESPACE` (Kubernetes downward API)
  - Infrastructure settings: Node selectors, resource limits (not application config)

- **IMP-007**: **Success Criteria**:
  - All 8 services read application configuration from Dapr Configuration API
  - Configuration updates propagate within 10 seconds (Azure App Configuration polling interval)
  - Zero application code differences between AKS, Container Apps, EKS, GKE deployments
  - Operators can update configuration via cloud-native UI (Azure Portal, AWS Console, Redis Commander)
  - Feature flags toggle without redeployment (validated via end-to-end test)

- **IMP-008**: **Testing Strategy**:
  - Unit tests: Mock `DaprClient.GetConfiguration()` responses
  - Integration tests: Use Redis configuration store in Docker Compose
  - E2E tests: Update configuration in Azure App Configuration, verify service behavior changes
  - Chaos tests: Simulate configuration store unavailability, verify fallback/retry behavior

## References

- **REF-001**: Related ADR: `docs/adr/adr-0002-cloud-agnostic-configuration-via-dapr.md` (establishes Dapr as abstraction layer)
- **REF-002**: Related Plan: `plan/orderservice-dotnet10-upgrade.md` REQ-004 (multi-platform deployment, configuration requirements)
- **REF-003**: Related Plan: `plan/modernization-strategy.md` (Phase 3: Dapr 1.16 upgrade includes Configuration API support)
- **REF-004**: Dapr Docs: [Configuration API Overview](https://docs.dapr.io/developing-applications/building-blocks/configuration/configuration-api-overview/)
- **REF-005**: Dapr Docs: [How-To: Manage Configuration](https://docs.dapr.io/developing-applications/building-blocks/configuration/howto-manage-configuration/)
- **REF-006**: Dapr Component: [Azure App Configuration](https://docs.dapr.io/reference/components-reference/supported-configuration-stores/azure-appconfig-configuration-store/)
- **REF-007**: Dapr Component: [Redis Configuration Store](https://docs.dapr.io/reference/components-reference/supported-configuration-stores/redis-configuration-store/)
- **REF-008**: Dapr Component: [PostgreSQL Configuration Store](https://docs.dapr.io/reference/components-reference/supported-configuration-stores/postgresql-configuration-store/)
- **REF-009**: Session Log: `.claude/sessions/2025-11-02-1105-orderservice-dotnet10-refinement.md` (Dapr Configuration API research and decision)
