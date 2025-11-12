# .NET 10 Upgrade Procedure

**Purpose:** Prevent recurring upgrade failures (health probe drift, missing Dapr sidecars, configuration drift, stale images)

**Target Services:**
- ✅ OrderService (COMPLETE)
- ✅ ReceiptGenerationService (COMPLETE)
- ✅ AccountingService (COMPLETE)
- ⏳ MakeLineService (PENDING)
- ⏳ LoyaltyService (PENDING)
- ⏳ VirtualWorker (PENDING)
- ⏳ VirtualCustomers (PENDING)

---

## Quick Start

```bash
# Automated workflow (recommended)
./scripts/upgrade-dotnet10.sh MakeLineService

# Manual workflow (for fine-grained control)
./scripts/upgrade-preflight.sh MakeLineService
# ... make code and infrastructure changes ...
./scripts/upgrade-build-images.sh MakeLineService
helm upgrade reddog charts/reddog -f values/values-local.yaml
./scripts/upgrade-validate.sh MakeLineService
```

---

## The Four Recurring Failure Patterns

### 1. Health Probe Drift
**Problem:** Code exposes `/healthz`, `/livez`, `/readyz` but Helm chart still references `/probes/ready`

**Result:** HTTP 404 → CrashLoopBackOff

**Prevention:** Update code + Helm chart **together** in same commit

### 2. Missing Dapr Sidecar
**Problem:** Declaring success when pod shows `1/1` instead of verifying `2/2`

**Result:** Service runs but pub/sub doesn't work (sidecar missing)

**Prevention:** **ALWAYS** verify `2/2` container count before declaring success

### 3. Configuration Drift
**Problem:**
- Code expects `ConnectionStrings:RedDog`, Helm provides `ConnectionStrings__RedDog`
- Literal `${SA_PASSWORD}` not substituted
- Missing `ASPNETCORE_URLS` environment variable

**Result:** Database connection failures, timeouts, crashes

**Prevention:** Validate configuration keys match between code and Helm

### 4. Stale Image Tags
**Problem:** Built `:net10` tag but forgot to rebuild `:local` tag, kind cluster uses old image

**Result:** Deployment succeeds but runs old .NET 6 code lacking health endpoints

**Prevention:** Build **ALL** image tags in one operation, reload into kind

---

## Pre-Upgrade Checklist

Run `./scripts/upgrade-preflight.sh <ServiceName>` to verify:

- [ ] Dapr sidecar injector is healthy (1/1 Running, no recent restarts)
- [ ] No stuck rollouts in cluster
- [ ] Current image tags documented
- [ ] Helm chart probe paths identified
- [ ] Service currently deployed (if applicable)

**STOP IF PREFLIGHT FAILS** - Fix issues before proceeding

---

## Code Changes Checklist

### 1. Update `.csproj`

**File:** `RedDog.<ServiceName>/RedDog.<ServiceName>.csproj`

```xml
<TargetFramework>net10.0</TargetFramework>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<LangVersion>14.0</LangVersion>
```

**Package Updates:**
```xml
<PackageReference Include="Dapr.AspNetCore" Version="1.16.0" />
<PackageReference Include="OpenTelemetry" Version="1.12.0" />
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.12.0" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.12.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.12.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.12.0" />
```

**For REST APIs, add:**
```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0-rc.2.24574.11" />
<PackageReference Include="Scalar.AspNetCore" Version="2.1.30" />
```

**Remove:**
- All `Serilog.*` packages
- `Swashbuckle.*` packages

### 2. Copy Health Check Implementation

```bash
# Copy production-ready health check
cp RedDog.AccountingService/HealthChecks/DaprSidecarHealthCheck.cs \
   RedDog.<ServiceName>/HealthChecks/DaprSidecarHealthCheck.cs

# Update namespace
sed -i "s/AccountingService/<ServiceName>/g" \
   RedDog.<ServiceName>/HealthChecks/DaprSidecarHealthCheck.cs
```

### 3. Update `Program.cs`

**Add using:**
```csharp
using RedDog.<ServiceName>.HealthChecks;
```

**Replace health check registration:**
```csharp
// REMOVE old inline health checks

// ADD production-ready pattern
builder.Services.AddHealthChecks()
    .AddCheck("liveness", () =>
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Service is alive"),
        tags: ["live"])
    .AddCheck<DaprSidecarHealthCheck>("dapr-readiness", tags: ["ready"]);
```

**Update health endpoint mapping:**
```csharp
// Startup check (no dependencies)
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

// Liveness check (no dependencies)
app.MapHealthChecks("/livez", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

// Readiness check (includes Dapr sidecar)
app.MapHealthChecks("/readyz", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

### 4. Verify Build

```bash
dotnet build RedDog.<ServiceName>/RedDog.<ServiceName>.csproj -c Release
```

**STOP IF BUILD FAILS** - Fix errors before proceeding

---

## Infrastructure Changes Checklist

### 1. Update Helm Chart Health Probes

**File:** `charts/reddog/templates/<service-name>-service.yaml`

**Replace entire probe section:**
```yaml
        startupProbe:
          httpGet:
            path: /healthz
            port: 80
          failureThreshold: 6
          periodSeconds: 10
        livenessProbe:
          httpGet:
            path: /livez
            port: 80
          periodSeconds: 10
          timeoutSeconds: 5
        readinessProbe:
          httpGet:
            path: /readyz
            port: 80
          periodSeconds: 5
          timeoutSeconds: 3
```

### 2. Add Environment Variables

**For all services:**
```yaml
        env:
        - name: ASPNETCORE_URLS
          value: "http://+:80"
```

**For services with database:**
```yaml
        - name: SA_PASSWORD
          valueFrom:
            secretKeyRef:
              name: {{ .Values.database.passwordSecret.name }}
              key: {{ .Values.database.passwordSecret.key }}
        - name: ConnectionStrings__RedDog
          value: {{ .Values.database.connectionString }}
```

### 3. Verify Configuration Alignment

**Check code expectations:**
```bash
grep -r "Configuration\[" RedDog.<ServiceName>/Program.cs
```

**Check Helm provides:**
```bash
grep "name:" charts/reddog/templates/<service-name>-service.yaml | grep -E "(ConnectionStrings|SA_PASSWORD|ASPNETCORE)"
```

**Verify keys match** (accounting for ASP.NET Core `__` → `:` translation)

---

## Image Build Checklist

Run `./scripts/upgrade-build-images.sh <ServiceName>` to:

- [ ] Build **all** image tags (`:net10`, `:net10-test`, `:local`, `:latest`)
- [ ] Load all images into kind cluster
- [ ] Verify images available locally and in cluster
- [ ] Generate image manifest (`.image-manifest-<ServiceName>.txt`)

**Why build all tags?**
- Helm uses `:local` tag in `values-local.yaml`
- If you only build `:net10`, deployment will use stale `:local` image
- This causes HTTP 404 on health endpoints (old code lacks endpoints)

---

## Deployment Checklist

### 1. Helm Dry-Run (Optional)

```bash
helm upgrade reddog charts/reddog -f values/values-local.yaml --dry-run --debug | grep -A 30 "<service-name>"
```

Verify:
- Probe paths are `/healthz`, `/livez`, `/readyz`
- Environment variables are correct
- Image tag references `:local`

### 2. Deploy

```bash
helm upgrade reddog charts/reddog -f values/values-local.yaml --wait --timeout 5m
```

### 3. Wait for Rollout

```bash
kubectl rollout status deployment/<service-name>-service --timeout=3m
```

### 4. Wait for Stabilization

```bash
# Give pods time to pass startup probes
sleep 5
```

---

## Validation Checklist

Run `./scripts/upgrade-validate.sh <ServiceName>` to verify:

### Critical Checks (MUST PASS)

- [ ] **Pod shows `2/2` Running** (NOT `1/1`, NOT `0/2`)
- [ ] `/healthz` returns HTTP 200
- [ ] `/livez` returns HTTP 200
- [ ] `/readyz` returns HTTP 200
- [ ] Zero probe failures in events
- [ ] Dapr sidecar container present and ready

### Additional Checks

- [ ] Restart count is 0 (or stable)
- [ ] Deployed image is recent (not stale cached image)
- [ ] No errors in application logs
- [ ] Dapr subscriptions registered (if pub/sub subscriber)

**STOP IF VALIDATION FAILS** - Debug before declaring success

---

## Common Issues and Fixes

### Issue 1: HTTP 404 on Health Endpoints

**Symptoms:**
```
Startup probe failed: HTTP probe failed with statuscode: 404
```

**Root Cause:** Stale image lacking health endpoints

**Fix:**
```bash
# Rebuild ALL image tags
./scripts/upgrade-build-images.sh <ServiceName>

# Force pod recreation
kubectl delete pod -l app=<service-name>-service
```

### Issue 2: Pod Shows 1/1 Instead of 2/2

**Symptoms:**
```
<service-name>-xxxx   1/1   Running   0   2m
```

**Root Cause:** Dapr sidecar not injected

**Fix:**
```bash
# Check Dapr injector health
kubectl get pods -n dapr-system -l app=dapr-sidecar-injector

# If injector was restarting, wait until stable, then:
kubectl delete pod -l app=<service-name>-service
```

### Issue 3: Database Connection Failures

**Symptoms:**
```
Login failed for user 'sa'
Connection timeout
Literal ${SA_PASSWORD} in connection string
```

**Root Cause:** Configuration key mismatch or password not substituted

**Fix:**
```bash
# Verify configuration in Helm chart
kubectl get pod <pod-name> -o jsonpath='{.spec.containers[0].env}' | jq

# Check for:
# - ASPNETCORE_URLS present
# - ConnectionStrings__RedDog present
# - SA_PASSWORD present (not literal ${SA_PASSWORD})
```

Update Helm chart if needed, then redeploy.

### Issue 4: Service Crashes Immediately

**Symptoms:**
```
<service-name>-xxxx   0/2   CrashLoopBackOff
```

**Root Cause:** Missing environment variables or configuration

**Fix:**
```bash
# Check logs
kubectl logs <pod-name> -c <service-name>-service

# Common missing: ASPNETCORE_URLS
# Add to Helm chart env section
```

### Automation Guardrails (Must Follow)

- **Build/output parsing:** Never rely on log text to decide whether a `docker build` or `dotnet` command succeeded. Use the command’s exit code. When additional detail is needed, opt into `docker build --progress=plain` (supported in recent Docker versions) but still key decisions from the exit status, not stdout.
- **Environment validation order:** Run helpers such as `EnsureDaprEnvironmentVariables` *after* `WebApplication.CreateBuilder` has loaded configuration and options. Always provide safe defaults for Development (e.g., default `DAPR_HTTP_PORT` to 3500) so local `dapr run` or port-forwarding scenarios don’t crash prior to binding.
- **Identifier alignment:** Keep the Helm release, Kubernetes labels, deployments, and Dapr `app-id` in sync. Whenever you rename a service or tweak casing (`make-line-service` vs `makelineservice`), update component scopes and validation scripts in the same change to avoid “component not configured” errors.

---

## Post-Upgrade Tasks

### 1. Functional Smoke Test

**OrderService Example:**
```bash
kubectl port-forward svc/orderservice 5100:80
curl http://localhost:5100/Product
```

**MakeLineService Example:**
```bash
PORT=$(./scripts/find-open-port.sh 5200 15200 25200)
kubectl port-forward svc/makelineservice ${PORT}:80 &
PF_PID=$!
sleep 3
curl http://localhost:${PORT}/orders
kill $PF_PID
```

### 2. Update Session Log

```bash
echo "✓ <ServiceName> upgraded to .NET 10" >> .claude/sessions/.current-session
```

### 3. Update Modernization Strategy

Edit `plan/modernization-strategy.md`:

```markdown
## Phase 1A Progress

- ✅ OrderService - .NET 10 ✓
- ✅ ReceiptGenerationService - .NET 10 ✓
- ✅ AccountingService - .NET 10 ✓
- ✅ <ServiceName> - .NET 10 ✓
- ⏳ RemainingService - .NET 6.0
```

### 4. Commit Changes

```bash
git add -A
git commit -m "feat: Upgrade <ServiceName> to .NET 10

- Update .csproj to net10.0
- Add ADR-0005 health endpoints (/healthz, /livez, /readyz)
- Implement DaprSidecarHealthCheck
- Update Helm chart with correct probe paths
- Add required environment variables

Validated:
- Pod running 2/2 (Dapr sidecar injected)
- All health endpoints returning 200
- No probe failures
- Dapr subscriptions registered
"
```

---

## Success Criteria

Before moving to next service, verify:

- ✅ Build succeeded with zero errors
- ✅ All image tags built and loaded into kind
- ✅ Helm deployment succeeded
- ✅ Pod shows `2/2 Running` with 0 restarts
- ✅ All health endpoints return HTTP 200
- ✅ Zero probe failures in events
- ✅ Dapr sidecar healthy and ready
- ✅ No errors in application logs
- ✅ Functional smoke test passed
- ✅ Session log updated
- ✅ Modernization strategy updated
- ✅ Changes committed to git

---

## Automation Scripts Reference

### `upgrade-preflight.sh`

**Purpose:** Pre-flight checks before starting upgrade

**Checks:**
- Dapr injector health
- Stuck rollouts
- Current image tags
- Helm chart probe paths
- Current deployment status

**Usage:**
```bash
./scripts/upgrade-preflight.sh MakeLineService
```

### `upgrade-build-images.sh`

**Purpose:** Build ALL image tags and load into kind

**Actions:**
- Validates Dockerfile exists
- Builds 4 image tags (`:net10`, `:net10-test`, `:local`, `:latest`)
- Loads all images into kind cluster
- Verifies images available
- Generates image manifest

**Usage:**
```bash
./scripts/upgrade-build-images.sh MakeLineService
```

### `upgrade-validate.sh`

**Purpose:** Post-deployment validation

**Checks:**
- Pod exists and is running
- **Container count is 2/2** (CRITICAL)
- All health endpoints return 200
- No probe failures in events
- Dapr sidecar healthy
- Dapr subscriptions registered
- No errors in logs

**Usage:**
```bash
./scripts/upgrade-validate.sh MakeLineService
```

### `upgrade-dotnet10.sh`

**Purpose:** Complete orchestrated upgrade workflow

**Steps:**
1. Pre-flight checks
2. Prompt for code changes (manual)
3. Verify build
4. Prompt for infrastructure changes (manual)
5. Build all image tags
6. Deploy via Helm
7. Wait for rollout
8. Validate deployment

**Usage:**
```bash
./scripts/upgrade-dotnet10.sh MakeLineService
```

---

## Prevention Principles

### 1. Code and Infrastructure Move Together

**DON'T:**
- Commit code changes
- Update Helm chart days later
- Deploy and wonder why health checks fail

**DO:**
- Update code + Helm chart in same session
- Review both in same PR
- Commit together

### 2. Verify Before Declaring Success

**DON'T:**
- See "green" status, move on
- Trust `kubectl get pods` shows "Running"
- Skip validation steps

**DO:**
- **Always** verify `2/2` container count
- Test all three health endpoints directly
- Check events log for probe failures
- Run validation script

### 3. Build All Image Tags

**DON'T:**
- Build `:net10` tag only
- Assume kind will use latest build
- Skip kind image load step

**DO:**
- Build ALL tags in one operation (`:net10`, `:net10-test`, `:local`, `:latest`)
- Load all tags into kind cluster
- Verify images with `docker images` and `kind get images`
- Track build times in manifest

### 4. Configuration as Code

**DON'T:**
- Guess configuration key names
- Assume password substitution works
- Copy old Helm charts without reviewing

**DO:**
- Copy working patterns from AccountingService/OrderService
- Validate config keys match between code and Helm
- Test password substitution explicitly
- Document env var requirements per service

### 5. Automation Over Memory

**DON'T:**
- Rely on memory for checklist steps
- Skip "obvious" validation checks
- Rush through upgrades

**DO:**
- Use automation scripts to enforce checklist
- Run validation even if "everything looks fine"
- Follow procedure for every service

---

## Remaining Services

### MakeLineService
- **Complexity:** Medium
- **Database:** No (uses Redis state store)
- **Pub/Sub:** Subscriber (orders topic)
- **Notes:** Service invocation API for VirtualWorker

### LoyaltyService
- **Complexity:** Low
- **Database:** No (uses Redis state store)
- **Pub/Sub:** Subscriber (orders topic)
- **Notes:** Straightforward pub/sub subscriber

### VirtualWorker
- **Complexity:** Low
- **Database:** No
- **Pub/Sub:** Publisher (order completions)
- **Notes:** Background worker, no REST API

### VirtualCustomers
- **Complexity:** Low
- **Database:** No
- **Pub/Sub:** None (directly invokes OrderService)
- **Notes:** Load generator, simple HTTP client

---

## Contact

For issues with this procedure:
1. Check session logs in `.claude/sessions/`
2. Review ADR-0005 (health probes)
3. Compare with working services (AccountingService, OrderService)

---

**Last Updated:** 2025-11-12 (after OrderService, ReceiptGenerationService, AccountingService upgrades)

**Next Review:** After completing remaining 4 services
