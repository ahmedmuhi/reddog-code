# Red Dog Coffee: Minimum Infrastructure Required for Local Development

## Research Summary

Based on investigation of git history, deleted manifests, application code, and research documents, here's what infrastructure is actually needed for local development.

---

## 1. What the OLD Local Setup Used (Deleted 2025-11-02)

### Docker Compose (2 containers)
```yaml
version: '3.7'
services:
  app:                           # VS Code development container
    build: ./
    volumes:
      - /var/run/docker.sock:/var/run/docker-host.sock
      - ..:/workspace:cached
    
  db:                            # SQL Server 2019
    image: "mcr.microsoft.com/mssql/server:2019-latest"
    environment:
      MSSQL_SA_PASSWORD: "pass@word1"
      ACCEPT_EULA: "Y"
    container_name: reddog-sql-server
```

### Dapr Components (manifests/local/branch/)
From git history, these components used:

**Pub/Sub (Redis-based):**
```yaml
spec:
  type: pubsub.redis
  metadata: 
    - name: redisHost
      value: dapr_redis_dapr-dev-container:6379
```

**State Stores (Redis-based):**
- MakeLine: `redisHost: dapr_redis_dapr-dev-container:6379`
- Loyalty: `redisHost: dapr_redis_dapr-dev-container:6379`

**Secrets (Local file):**
```yaml
spec:
  type: secretstores.local.file
  metadata:
    - name: secretsFile
      value: ./manifests/local/branch/secrets.json
```

**Bindings (Local):**
- Receipt: `bindings.localstorage` → `/tmp/receipts`
- VirtualWorker: `bindings.cron` → `@every 5s` polling

### Secrets File
```json
{
  "reddog-sql": "Data Source=reddog-sql-server;Initial Catalog=reddogdemo;User Id=sa;Password=pass@word1;TrustServerCertificate=true;"
}
```

---

## 2. What Does `dapr init` Actually Provide?

### Standard `dapr init` installs:
1. **Dapr control plane** (dapr runtime)
2. **Redis state store** (default for development)
3. **Zipkin tracing** (optional, for observability)
4. **Placement service** (for actors)

### Does it provide Redis?
**YES** - `dapr init` automatically installs and runs Redis locally in Docker. This Redis serves as:
- Default state store for Dapr components
- Default pub/sub broker for Dapr components
- In-memory cache

### What about `dapr init --slim`?
**Slim mode** = minimal installation (smaller footprint, faster startup)
- Still provides Dapr control plane
- Still provides Redis (can't be removed)
- Skips optional components like Zipkin

---

## 3. Minimum Infrastructure Needed for Local Development

### Answer to Each Question:

**[✅] Dapr (via `dapr init` or `dapr init --slim`)**
- `dapr init` already installs Redis
- No additional setup needed
- Handles: service invocation, pub/sub, state management, bindings
- Connection: Services talk to Dapr on localhost:3500 (HTTP) or localhost:3501 (gRPC)

**[✅] Redis (provided by `dapr init`)**
- `dapr init` installs Redis in Docker automatically
- No separate docker-compose entry needed
- Serves: pub/sub + state stores (MakeLine, Loyalty)
- Default port: 6379
- Note: Must use `dapr_redis_dapr-default` hostname (Docker network)

**[✅] SQL Server (required via docker-compose)**
- Application data for AccountingService (EF Core)
- Migrations run via Bootstrapper
- Image: `mcr.microsoft.com/mssql/server:2022-latest` (upgrade from 2019)

**[❌] RabbitMQ (NOT needed locally)**
- Kubernetes production uses RabbitMQ for pub/sub
- Locally, Redis pub/sub is sufficient
- No performance/feature gap for development
- Rationale: Dapr's pub/sub abstraction allows swapping brokers

**[❌] Nginx (NOT needed locally)**
- Ingress controller for Kubernetes only
- Locally: Services are accessed directly via port forwards (5100, 5200, etc.)
- Not needed for microservices to communicate

**[❌] Cert-Manager (NOT needed locally)**
- TLS certificate management for Kubernetes
- Locally: Development uses HTTP (no TLS)
- No HTTPS required for local development

**[❌] KEDA (NOT needed locally)**
- Kubernetes event-driven autoscaling
- Locally: Services run statically (manual scaling not needed)
- Used in production for queue-based scaling

---

## 4. Service Dependencies Matrix

| Service | Needs | Connection Method |
|---------|-------|-------------------|
| **OrderService** | Dapr (pub/sub) | `localhost:3500` |
| **MakeLineService** | Dapr (pub/sub + state) | `localhost:3500` + Redis via Dapr |
| **LoyaltyService** | Dapr (pub/sub + state) | `localhost:3500` + Redis via Dapr |
| **ReceiptGenerationService** | Dapr (pub/sub) | `localhost:3500` + local file storage |
| **AccountingService** | Dapr (pub/sub) + SQL Server | `localhost:3500` + SQL Server 1433 |
| **VirtualCustomers** | Dapr (service invocation) | `localhost:3500` |
| **VirtualWorker** | Dapr (state + bindings) | `localhost:3500` + cron trigger |
| **UI (Vue.js)** | HTTP APIs | Direct ports 5100, 5200, 5700 |

---

## 5. So What Should the Setup Be?

```bash
# Step 1: Initialize Dapr runtime (installs Redis automatically)
dapr init --runtime-version 1.16.0

# Step 2: Install EF Core tools for migrations
dotnet tool install --global dotnet-ef

# Step 3: Run SQL Server in docker-compose
docker-compose -f manifests/local/docker-compose.yml up

# Step 4: Run Bootstrapper to initialize database
dotnet run --project RedDog.Bootstrapper/RedDog.Bootstrapper.csproj
```

### Minimal docker-compose.yml
```yaml
version: '3.8'
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    ports:
      - "1433:1433"
    environment:
      MSSQL_PID: "Developer"
      SA_PASSWORD: "pass@word1"
      ACCEPT_EULA: "Y"
    container_name: reddog-sql-server
    restart: on-failure
    healthcheck:
      test: /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'pass@word1' -Q 'SELECT 1' || exit 1
      interval: 10s
      timeout: 5s
      retries: 5
```

---

## 6. Restore Local Dapr Components

The deleted manifests should be restored at `manifests/local/branch/`:

**reddog.pubsub.yaml:**
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: reddog.pubsub
spec:
  type: pubsub.redis
  version: v1
  metadata: 
    - name: redisHost
      value: localhost:6379
scopes:
  - order-service
  - make-line-service
  - loyalty-service
  - receipt-generation-service
  - accounting-service
auth:
  secretStore: reddog.secretstore
```

**reddog.state.makeline.yaml:**
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: reddog.state.makeline
spec:
  type: state.redis
  version: v1
  metadata:
    - name: redisHost
      value: localhost:6379
scopes:
  - make-line-service
auth:
  secretStore: reddog.secretstore
```

**reddog.secretstore.yaml:**
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: reddog.secretstore
spec:
  type: secretstores.local.file
  version: v1
  metadata:
  - name: secretsFile
    value: ./manifests/local/branch/secrets.json
```

**secrets.json:**
```json
{
  "reddog-sql": "Data Source=localhost;Initial Catalog=reddogdemo;User Id=sa;Password=pass@word1;TrustServerCertificate=true;"
}
```

---

## 7. Evidence Summary

### Git History Shows:
- **2022-01-12:** Initial local Dapr configs added with Redis pub/sub
- **2025-11-02:** All local infrastructure deleted (removal decision not well-documented)

### Current Kubernetes Uses:
- **Redis:** Helm Release 15.0.0 (needs upgrade to 7.x)
- **RabbitMQ:** Helm Release 8.20.2 (production only, not needed locally)
- **SQL Server:** 2019-latest (should upgrade to 2022)
- **Dapr:** 1.3.0 (outdated, needs 1.16+)

### Research Documents Recommend:
- Docker Compose for local infrastructure setup
- Tilt.dev for orchestrating services
- Keep `dapr init` for Dapr runtime + Redis
- Separate concerns: Infrastructure (compose) vs Services (direct run or Tilt)

---

## 8. Key Insights

### What Changed in 2025
The project transitioned from:
1. **VS Code dev containers** → No local development path
2. **Local docker-compose** → Only Kubernetes manifests
3. **Local Dapr components** → Deleted without replacement

This created a **gap**: No documented way to run services locally for development.

### Discrepancy: Pub/Sub Broker
- **Local (deleted):** Redis pub/sub
- **Kubernetes:** RabbitMQ pub/sub
- **Solution:** Dapr abstraction handles both; local Redis is fine

### Redis Hostname Importance
- Old local: `dapr_redis_dapr-dev-container:6379` (Docker network name)
- New local: `localhost:6379` (direct connection when dapr init runs)
- Both work when using Dapr components (Dapr handles resolution)

---

## 9. Recommended Next Steps

### Immediate (Phase 0)
1. Restore `/manifests/local/branch/` directory with Dapr components
2. Create `/manifests/local/docker-compose.yml` with SQL Server + Redis (or rely on `dapr init`)
3. Document in README: Setup instructions for local development
4. Test with one service to validate (e.g., OrderService)

### Short-term (Phase 1A)
1. Upgrade Dapr from 1.3.0 → 1.16.2
2. Upgrade Redis from 15.0.0 Helm → 7.x (or cloud-native DynamoDB/Cosmos)
3. Upgrade SQL Server from 2019 → 2022
4. Update Dapr component configs to point to upgraded versions

### Long-term
1. Evaluate Tilt.dev for better service orchestration
2. Migrate non-.NET services (Go, Python) once Dapr components are stable
3. Consider cloud-agnostic deployment (Dapr + Kubernetes on any cloud)

---

## Final Answer

**Minimum infrastructure for local development:**

```bash
# 1. Dapr runtime + Redis (automatic)
dapr init --runtime-version 1.16.0

# 2. Database
docker-compose -f manifests/local/docker-compose.yml up

# 3. Run application services (manually or via IDE)
# Services connect to:
# - Dapr: localhost:3500
# - SQL Server: localhost:1433
# - Redis: localhost:6379 (via dapr init)
```

**Infrastructure checklist:**
- [x] Dapr (via `dapr init`)
- [x] Redis (via `dapr init` automatically)
- [x] SQL Server (via docker-compose)
- [ ] RabbitMQ (Kubernetes only)
- [ ] Nginx (Kubernetes only)
- [ ] Cert-Manager (Kubernetes only)
- [ ] KEDA (Kubernetes only)

