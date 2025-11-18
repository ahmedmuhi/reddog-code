#!/bin/bash
# Pre-flight checks before starting .NET 10 upgrade
# Usage: ./scripts/upgrade-preflight.sh <ServiceName>
#
# Validates:
# - Dapr sidecar injector health
# - No stuck rollouts
# - Current image tags
# - Helm chart health probe paths

set -euo pipefail

if [ $# -ne 1 ]; then
  echo "Usage: $0 <ServiceName>"
  echo "Example: $0 MakeLineService"
  exit 1
fi

SERVICE_NAME="$1"
SERVICE_LOWER=$(echo "$SERVICE_NAME" | tr '[:upper:]' '[:lower:]')

echo "╔════════════════════════════════════════════════════════════════╗"
echo "║         Pre-Flight Checks for $SERVICE_NAME"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

FAILED=0

# 1. Check Dapr sidecar injector health
echo -n "✓ Checking Dapr sidecar injector... "
INJECTOR_READY=$(kubectl get pods -n dapr-system -l app=dapr-sidecar-injector --no-headers 2>/dev/null | grep "1/1.*Running" | wc -l)
if [ "$INJECTOR_READY" -eq 0 ]; then
  echo "❌ FAILED"
  echo "  Dapr sidecar injector is not healthy. Deployment will fail without sidecar injection."
  kubectl get pods -n dapr-system -l app=dapr-sidecar-injector
  FAILED=1
else
  RESTARTS=$(kubectl get pods -n dapr-system -l app=dapr-sidecar-injector --no-headers | awk '{print $4}')
  echo "✓ (restarts: $RESTARTS)"
fi
echo ""

# 2. Check for stuck rollouts
echo "✓ Checking for stuck rollouts..."
STUCK=$(kubectl get deployments --no-headers 2>/dev/null | awk '$2 != $3 || $3 != $4' | grep -v "0/0" || true)
if [ -n "$STUCK" ]; then
  echo "  ⚠️  WARNING - Some deployments not fully ready:"
  echo "$STUCK" | while IFS= read -r line; do echo "    $line"; done
  echo "  This may indicate issues with existing services."
else
  echo "  All deployments healthy"
fi
echo ""

# 3. Check current image tags
echo "✓ Current Docker images for $SERVICE_NAME:"
IMAGES=$(docker images --format "table {{.Repository}}:{{.Tag}}\t{{.CreatedAt}}" 2>/dev/null | grep "reddog-${SERVICE_LOWER}" || echo "  (no images found)")
if [ "$IMAGES" = "  (no images found)" ]; then
  echo "$IMAGES"
else
  echo "$IMAGES" | while IFS= read -r line; do echo "  $line"; done
fi
echo ""

# 4. Check images in kind cluster
echo "✓ Images in kind cluster for $SERVICE_NAME:"
KIND_IMAGES=$(kind get images --name reddog-local 2>/dev/null | grep "${SERVICE_LOWER}" || echo "  (no images found in cluster)")
if [ "$KIND_IMAGES" = "  (no images found in cluster)" ]; then
  echo "$KIND_IMAGES"
else
  echo "$KIND_IMAGES" | while IFS= read -r line; do echo "  $line"; done
fi
echo ""

# 5. Check Helm chart health probe paths
echo "✓ Checking Helm chart health probe configuration..."
HELM_FILE="charts/reddog/templates/${SERVICE_LOWER}-service.yaml"
if [ -f "$HELM_FILE" ]; then
  echo "  File: $HELM_FILE"

  # Check for probe paths
  STARTUP_PATH=$(grep -A 3 "startupProbe:" "$HELM_FILE" | grep "path:" | awk '{print $2}' || echo "NOT FOUND")
  LIVENESS_PATH=$(grep -A 3 "livenessProbe:" "$HELM_FILE" | grep "path:" | awk '{print $2}' || echo "NOT FOUND")
  READINESS_PATH=$(grep -A 3 "readinessProbe:" "$HELM_FILE" | grep "path:" | awk '{print $2}' || echo "NOT FOUND")

  echo "  Current probe paths:"
  echo "    startupProbe:   $STARTUP_PATH"
  echo "    livenessProbe:  $LIVENESS_PATH"
  echo "    readinessProbe: $READINESS_PATH"

  # Warn if using legacy paths
  if [[ "$STARTUP_PATH" == *"/probes/"* ]] || [[ "$LIVENESS_PATH" == *"/probes/"* ]] || [[ "$READINESS_PATH" == *"/probes/"* ]]; then
    echo "  ⚠️  WARNING: Using legacy /probes/* paths - need to update to ADR-0005 (/healthz, /livez, /readyz)"
  fi
else
  echo "  ⚠️  WARNING - Helm chart not found: $HELM_FILE"
  echo "  Service may not be deployed via Helm yet."
fi
echo ""

# 6. Check if service currently deployed
echo "✓ Checking current deployment status..."
CURRENT_PODS=$(kubectl get pods -l app=${SERVICE_LOWER}-service --no-headers 2>/dev/null || echo "")
if [ -z "$CURRENT_PODS" ]; then
  echo "  Service not currently deployed (new service)"
else
  echo "  Current pods:"
  echo "$CURRENT_PODS" | while IFS= read -r line; do echo "    $line"; done

  # Check if any pods are 1/1 (missing Dapr sidecar)
  SINGLE_CONTAINER=$(echo "$CURRENT_PODS" | grep "1/1" || true)
  if [ -n "$SINGLE_CONTAINER" ]; then
    echo "  ⚠️  WARNING: Found pods with only 1 container (missing Dapr sidecar)"
  fi
fi
echo ""

# 7. Summary
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║                     PRE-FLIGHT SUMMARY"
echo "╚════════════════════════════════════════════════════════════════╝"
if [ $FAILED -eq 0 ]; then
  echo "✓ All critical checks passed"
  echo ""
  echo "Next steps:"
  echo "  1. Update .csproj to net10.0"
  echo "  2. Update Program.cs with ADR-0005 health endpoints"
  echo "  3. Copy DaprSidecarHealthCheck.cs from AccountingService"
  echo "  4. Update Helm chart with correct probe paths and env vars"
  echo "  5. Run: ./scripts/upgrade-build-images.sh $SERVICE_NAME"
  echo ""
  exit 0
else
  echo "❌ Pre-flight checks failed"
  echo ""
  echo "Fix the issues above before proceeding with upgrade."
  echo ""
  exit 1
fi
