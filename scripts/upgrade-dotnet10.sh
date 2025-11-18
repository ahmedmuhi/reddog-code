#!/bin/bash
# Complete .NET 10 upgrade orchestrator
# Usage: ./scripts/upgrade-dotnet10.sh <ServiceName>
#
# This script orchestrates the entire upgrade process:
# 1. Pre-flight checks
# 2. Code changes (manual with prompts)
# 3. Infrastructure changes (manual with prompts)
# 4. Image building
# 5. Deployment
# 6. Validation
#
# Prevents the four recurring failure patterns:
# - Health probe drift (sync code + Helm)
# - Missing Dapr sidecars (verify 2/2)
# - Configuration drift (checklist prompts)
# - Stale images (rebuild all tags)

set -euo pipefail

if [ $# -ne 1 ]; then
  echo "Usage: $0 <ServiceName>"
  echo "Example: $0 MakeLineService"
  echo ""
  echo "Remaining services to upgrade:"
  echo "  - MakeLineService"
  echo "  - LoyaltyService"
  echo "  - VirtualWorker"
  echo "  - VirtualCustomers"
  exit 1
fi

SERVICE_NAME="$1"
SERVICE_LOWER=$(echo "$SERVICE_NAME" | tr '[:upper:]' '[:lower:]')
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "╔════════════════════════════════════════════════════════════════╗"
echo "║    .NET 10 Upgrade Orchestrator for $SERVICE_NAME"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

# ============================================================================
# STEP 1: PRE-FLIGHT CHECKS
# ============================================================================
echo "═══════════════════════════════════════════════════════════════"
echo "                   STEP 1: PRE-FLIGHT CHECKS"
echo "═══════════════════════════════════════════════════════════════"
echo ""

if ! "$SCRIPT_DIR/upgrade-preflight.sh" "$SERVICE_NAME"; then
  echo "❌ Pre-flight checks failed. Fix issues before continuing."
  exit 1
fi

read -p "Pre-flight checks passed. Continue? (y/n) " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
  echo "Aborted."
  exit 0
fi
echo ""

# ============================================================================
# STEP 2: CODE CHANGES (MANUAL)
# ============================================================================
echo "═══════════════════════════════════════════════════════════════"
echo "                    STEP 2: CODE CHANGES"
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "Manual steps required (follow checklist):"
echo ""
echo "2.1 Update .csproj file:"
echo "    File: RedDog.${SERVICE_NAME}/RedDog.${SERVICE_NAME}.csproj"
echo ""
echo "    Changes:"
echo "    [ ] <TargetFramework>net10.0</TargetFramework>"
echo "    [ ] <Nullable>enable</Nullable>"
echo "    [ ] <ImplicitUsings>enable</ImplicitUsings>"
echo "    [ ] <LangVersion>14.0</LangVersion>"
echo "    [ ] Update Dapr.AspNetCore to 1.16.0"
echo "    [ ] Update OpenTelemetry packages to 1.12.0"
echo "    [ ] Remove Serilog packages (if present)"
echo "    [ ] Remove Swashbuckle packages (if present)"
echo "    [ ] Add Scalar.AspNetCore 2.1.30 (if REST API)"
echo "    [ ] Add Microsoft.AspNetCore.OpenApi 10.0.0-rc.2 (if REST API)"
echo ""
echo "2.2 Copy DaprSidecarHealthCheck.cs:"
echo "    From: RedDog.AccountingService/HealthChecks/DaprSidecarHealthCheck.cs"
echo "    To:   RedDog.${SERVICE_NAME}/HealthChecks/DaprSidecarHealthCheck.cs"
echo ""
echo "    Update namespace:"
echo "    namespace RedDog.${SERVICE_NAME}.HealthChecks;"
echo ""
echo "2.3 Update Program.cs:"
echo "    Add using: using RedDog.${SERVICE_NAME}.HealthChecks;"
echo ""
echo "    Update health checks:"
echo "    builder.Services.AddHealthChecks()"
echo "        .AddCheck(\"liveness\", () =>"
echo "            HealthCheckResult.Healthy(\"Service is alive\"),"
echo "            tags: [\"live\"])"
echo "        .AddCheck<DaprSidecarHealthCheck>(\"dapr-readiness\", tags: [\"ready\"]);"
echo ""
echo "    Update health endpoints:"
echo "    app.MapHealthChecks(\"/healthz\", new() { Predicate = check => check.Tags.Contains(\"live\") });"
echo "    app.MapHealthChecks(\"/livez\", new() { Predicate = check => check.Tags.Contains(\"live\") });"
echo "    app.MapHealthChecks(\"/readyz\", new() { Predicate = check => check.Tags.Contains(\"ready\") });"
echo ""
echo "2.4 Build and verify:"
echo "    dotnet build RedDog.${SERVICE_NAME}/RedDog.${SERVICE_NAME}.csproj -c Release"
echo ""

read -p "Have you completed ALL code changes? (y/n) " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
  echo "Complete code changes first, then run this script again."
  exit 0
fi

# Verify build
echo "Verifying build..."
if dotnet build "RedDog.${SERVICE_NAME}/RedDog.${SERVICE_NAME}.csproj" -c Release --no-restore; then
  echo "✓ Build succeeded"
else
  echo "❌ Build failed. Fix errors before continuing."
  exit 1
fi
echo ""

# ============================================================================
# STEP 3: INFRASTRUCTURE CHANGES (MANUAL)
# ============================================================================
echo "═══════════════════════════════════════════════════════════════"
echo "                STEP 3: INFRASTRUCTURE CHANGES"
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "Manual steps required (follow checklist):"
echo ""
echo "3.1 Update Helm chart health probes:"
echo "    File: charts/reddog/templates/${SERVICE_LOWER}-service.yaml"
echo ""
echo "    Replace probe section with:"
echo "    startupProbe:"
echo "      httpGet:"
echo "        path: /healthz"
echo "        port: 80"
echo "      failureThreshold: 6"
echo "      periodSeconds: 10"
echo "    livenessProbe:"
echo "      httpGet:"
echo "        path: /livez"
echo "        port: 80"
echo "      periodSeconds: 10"
echo "      timeoutSeconds: 5"
echo "    readinessProbe:"
echo "      httpGet:"
echo "        path: /readyz"
echo "        port: 80"
echo "      periodSeconds: 5"
echo "      timeoutSeconds: 3"
echo ""
echo "3.2 Add environment variables (if needed):"
echo "    env:"
echo "    - name: ASPNETCORE_URLS"
echo "      value: \"http://+:80\""
echo ""
echo "    For services with database:"
echo "    - name: SA_PASSWORD"
echo "      valueFrom:"
echo "        secretKeyRef:"
echo "          name: {{ .Values.database.passwordSecret.name }}"
echo "          key: {{ .Values.database.passwordSecret.key }}"
echo "    - name: ConnectionStrings__RedDog"
echo "      value: {{ .Values.database.connectionString }}"
echo ""

read -p "Have you completed ALL infrastructure changes? (y/n) " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
  echo "Complete infrastructure changes first, then run this script again."
  exit 0
fi
echo ""

# ============================================================================
# STEP 4: BUILD IMAGES
# ============================================================================
echo "═══════════════════════════════════════════════════════════════"
echo "                     STEP 4: BUILD IMAGES"
echo "═══════════════════════════════════════════════════════════════"
echo ""

if ! "$SCRIPT_DIR/upgrade-build-images.sh" "$SERVICE_NAME"; then
  echo "❌ Image build failed. Fix issues before continuing."
  exit 1
fi

read -p "Images built successfully. Continue to deployment? (y/n) " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
  echo "Aborted. Run deployment manually when ready:"
  echo "  helm upgrade reddog charts/reddog -f values/values-local.yaml"
  exit 0
fi
echo ""

# ============================================================================
# STEP 5: DEPLOY
# ============================================================================
echo "═══════════════════════════════════════════════════════════════"
echo "                      STEP 5: DEPLOY"
echo "═══════════════════════════════════════════════════════════════"
echo ""

echo "Deploying to cluster..."
if helm upgrade reddog charts/reddog -f values/values-local.yaml --wait --timeout 5m; then
  echo "✓ Helm upgrade completed"
else
  echo "❌ Helm upgrade failed"
  echo ""
  echo "Check Helm status:"
  echo "  helm list"
  echo "  helm history reddog"
  exit 1
fi
echo ""

# ============================================================================
# STEP 6: WAIT FOR ROLLOUT
# ============================================================================
echo "═══════════════════════════════════════════════════════════════"
echo "                  STEP 6: WAIT FOR ROLLOUT"
echo "═══════════════════════════════════════════════════════════════"
echo ""

echo "Waiting for deployment rollout..."
if kubectl rollout status deployment/${SERVICE_LOWER}-service --timeout=3m; then
  echo "✓ Rollout completed"
else
  echo "❌ Rollout failed or timed out"
  echo ""
  echo "Check pod status:"
  echo "  kubectl get pods -l app=${SERVICE_LOWER}-service"
  echo "  kubectl describe pod -l app=${SERVICE_LOWER}-service"
  exit 1
fi
echo ""

# Give pods a few seconds to stabilize
echo "Waiting for pods to stabilize..."
sleep 5
echo ""

# ============================================================================
# STEP 7: VALIDATE
# ============================================================================
echo "═══════════════════════════════════════════════════════════════"
echo "                     STEP 7: VALIDATE"
echo "═══════════════════════════════════════════════════════════════"
echo ""

if ! "$SCRIPT_DIR/upgrade-validate.sh" "$SERVICE_NAME"; then
  echo ""
  echo "❌ Validation failed"
  echo ""
  echo "Common issues and fixes:"
  echo "  1. Stale image - Rebuild: ./scripts/upgrade-build-images.sh $SERVICE_NAME"
  echo "  2. Missing sidecar - Check Dapr injector: kubectl get pods -n dapr-system"
  echo "  3. Config error - Check logs: kubectl logs -l app=${SERVICE_LOWER}-service"
  echo "  4. Health endpoint 404 - Verify code changes were built into image"
  echo ""
  exit 1
fi

# ============================================================================
# FINAL SUMMARY
# ============================================================================
echo ""
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║               ✓ UPGRADE COMPLETE: $SERVICE_NAME"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""
echo "Service successfully upgraded to .NET 10!"
echo ""
echo "Next steps:"
echo "  1. Run functional smoke tests"
echo "  2. Update session log:"
echo "     echo \"✓ $SERVICE_NAME upgraded\" >> .claude/sessions/.current-session"
echo "  3. Update modernization-strategy.md progress"
echo "  4. Commit changes:"
echo "     git add -A"
echo "     git commit -m \"feat: Upgrade $SERVICE_NAME to .NET 10\""
echo ""
echo "Remaining services:"
for svc in MakeLineService LoyaltyService VirtualWorker VirtualCustomers; do
  if [ "$svc" != "$SERVICE_NAME" ]; then
    echo "  - $svc"
  fi
done
echo ""
