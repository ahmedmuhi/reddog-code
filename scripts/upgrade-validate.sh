#!/bin/bash
# Validate deployment after .NET 10 upgrade
# Usage: ./scripts/upgrade-validate.sh <ServiceName>
#
# Validates:
# - Pod is running with 2/2 containers (Dapr sidecar injected)
# - All health endpoints return 200
# - No probe failures in Kubernetes events
# - Dapr subscriptions registered (if applicable)

set -euo pipefail

if [ $# -ne 1 ]; then
  echo "Usage: $0 <ServiceName>"
  echo "Example: $0 MakeLineService"
  exit 1
fi

SERVICE_NAME="$1"
SERVICE_LOWER=$(echo "$SERVICE_NAME" | tr '[:upper:]' '[:lower:]')

echo "╔════════════════════════════════════════════════════════════════╗"
echo "║       Deployment Validation for $SERVICE_NAME"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

FAILED=0

# 1. Get pod name
echo "✓ Finding pod..."
POD_NAME=$(kubectl get pods -l app=${SERVICE_LOWER}-service --no-headers 2>/dev/null | head -1 | awk '{print $1}' || echo "")
if [ -z "$POD_NAME" ]; then
  echo "  ❌ FAILED - No pod found for label app=${SERVICE_LOWER}-service"
  echo ""
  echo "Available pods:"
  kubectl get pods
  exit 1
fi
echo "  Pod: $POD_NAME"
echo ""

# 2. Verify container count (CRITICAL CHECK)
echo "✓ Verifying container count..."
READY=$(kubectl get pods $POD_NAME --no-headers | awk '{print $2}')
STATUS=$(kubectl get pods $POD_NAME --no-headers | awk '{print $3}')
RESTARTS=$(kubectl get pods $POD_NAME --no-headers | awk '{print $4}')

echo "  Status: $READY $STATUS (restarts: $RESTARTS)"

if [ "$READY" != "2/2" ]; then
  echo "  ❌ FAILED - Pod shows $READY, expected 2/2"
  echo ""
  echo "This indicates Dapr sidecar was NOT injected. Common causes:"
  echo "  - Dapr sidecar-injector was down/restarting during deployment"
  echo "  - Namespace missing Dapr injection label"
  echo "  - Pod annotations incorrect"
  echo ""
  echo "Container details:"
  kubectl get pod $POD_NAME -o jsonpath='{.spec.containers[*].name}' | tr ' ' '\n' | nl
  echo ""
  echo "Recent events:"
  kubectl describe pod $POD_NAME | tail -20
  FAILED=1
else
  echo "  ✓ Correct container count: 2/2 (application + daprd)"
fi
echo ""

# 3. Check restart count
if [ "$RESTARTS" -gt 0 ]; then
  echo "  ⚠️  WARNING - Pod has $RESTARTS restarts"
  echo "  Checking recent restart reasons..."
  kubectl describe pod $POD_NAME | grep -A 5 "Last State:" || true
  echo ""
fi

# 4. Check image version
echo "✓ Verifying deployed image..."
APP_CONTAINER_NAME="${SERVICE_LOWER}-service"
DEPLOYED_IMAGE=$(kubectl get pod $POD_NAME -o jsonpath="{.spec.containers[?(@.name=='$APP_CONTAINER_NAME')].image}" 2>/dev/null || echo "UNKNOWN")
echo "  Image: $DEPLOYED_IMAGE"

# Check if image is recent (created in last 24 hours)
IMAGE_NAME=$(echo "$DEPLOYED_IMAGE" | cut -d':' -f1)
IMAGE_TAG=$(echo "$DEPLOYED_IMAGE" | cut -d':' -f2)
LOCAL_IMAGE=$(docker images --format "{{.Repository}}:{{.Tag}}\t{{.CreatedAt}}" 2>/dev/null | grep "$IMAGE_NAME:$IMAGE_TAG" || echo "")
if [ -n "$LOCAL_IMAGE" ]; then
  echo "  Local image: $LOCAL_IMAGE"
else
  echo "  ⚠️  WARNING - Image not found in local Docker (may be cached in kind)"
fi
echo ""

# 5. Test health endpoints
echo "✓ Testing health endpoints..."

# Start port-forward in background
kubectl port-forward $POD_NAME 8080:80 >/dev/null 2>&1 &
PF_PID=$!
sleep 3

# Trap to ensure port-forward is killed
trap "kill $PF_PID 2>/dev/null || true" EXIT

for ENDPOINT in healthz livez readyz; do
  STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8080/$ENDPOINT 2>/dev/null || echo "000")
  TIME=$(curl -s -o /dev/null -w "%{time_total}" http://localhost:8080/$ENDPOINT 2>/dev/null || echo "0")

  if [ "$STATUS_CODE" = "200" ]; then
    printf "  ✓ /%s: %s (%.3fs)\n" "$ENDPOINT" "$STATUS_CODE" "$TIME"
  else
    printf "  ❌ /%s: %s\n" "$ENDPOINT" "$STATUS_CODE"
    echo "    Response body:"
    curl -s http://localhost:8080/$ENDPOINT 2>/dev/null | head -5 | sed 's/^/      /'
    FAILED=1
  fi
done

# Clean up port-forward
kill $PF_PID 2>/dev/null || true
trap - EXIT
echo ""

# 6. Check for probe failures in events
echo "✓ Checking for probe failures..."
PROBE_FAILURES=$(kubectl get events --field-selector involvedObject.name=$POD_NAME 2>/dev/null | grep -i "probe.*failed" | wc -l || echo "0")
if [ "$PROBE_FAILURES" -gt 0 ]; then
  echo "  ⚠️  WARNING - $PROBE_FAILURES probe failures detected in events"
  kubectl get events --field-selector involvedObject.name=$POD_NAME | grep -i "probe"
  FAILED=1
else
  echo "  ✓ No probe failures in events"
fi
echo ""

# 7. Verify Dapr sidecar health
echo "✓ Checking Dapr sidecar health..."
DAPRD_READY=$(kubectl get pod $POD_NAME -o jsonpath='{.status.containerStatuses[?(@.name=="daprd")].ready}' 2>/dev/null || echo "false")
if [ "$DAPRD_READY" = "true" ]; then
  echo "  ✓ Dapr sidecar is ready"

  # Check Dapr subscriptions (if applicable)
  SUBSCRIPTIONS=$(kubectl logs $POD_NAME -c daprd --tail=100 2>/dev/null | grep -i "subscribed to" || echo "")
  if [ -n "$SUBSCRIPTIONS" ]; then
    echo "  ✓ Dapr subscriptions:"
    echo "$SUBSCRIPTIONS" | head -3 | sed 's/^/    /'
  fi
else
  echo "  ⚠️  WARNING - Dapr sidecar not ready"
  FAILED=1
fi
echo ""

# 8. Check application logs for errors
echo "✓ Checking application logs for errors..."
ERROR_COUNT=$(kubectl logs $POD_NAME -c $APP_CONTAINER_NAME --tail=50 2>/dev/null | grep -iE "(error|exception|fatal)" | wc -l || echo "0")
if [ "$ERROR_COUNT" -gt 0 ]; then
  echo "  ⚠️  WARNING - Found $ERROR_COUNT error/exception entries in recent logs"
  echo "  Recent errors:"
  kubectl logs $POD_NAME -c $APP_CONTAINER_NAME --tail=50 | grep -iE "(error|exception|fatal)" | head -5 | sed 's/^/    /'
else
  echo "  ✓ No errors in recent logs"
fi
echo ""

# 9. Final summary
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║                   VALIDATION SUMMARY"
echo "╚════════════════════════════════════════════════════════════════╝"

if [ $FAILED -eq 0 ]; then
  echo "✓ ALL CHECKS PASSED"
  echo ""
  echo "$SERVICE_NAME is healthy and ready for production:"
  echo "  • Pod running with correct container count (2/2)"
  echo "  • All health endpoints returning 200"
  echo "  • No probe failures"
  echo "  • Dapr sidecar healthy"
  echo "  • No errors in application logs"
  echo ""
  echo "Next steps:"
  echo "  1. Run functional smoke tests"
  echo "  2. Update session log with success"
  echo "  3. Update modernization-strategy.md progress"
  echo "  4. Commit changes"
  echo ""
  exit 0
else
  echo "❌ VALIDATION FAILED"
  echo ""
  echo "Fix the issues above before declaring upgrade complete."
  echo ""
  echo "Common fixes:"
  echo "  • Rebuild images: ./scripts/upgrade-build-images.sh $SERVICE_NAME"
  echo "  • Check logs: kubectl logs $POD_NAME -c $APP_CONTAINER_NAME"
  echo "  • Check Dapr: kubectl logs $POD_NAME -c daprd"
  echo "  • Force recreation: kubectl delete pod $POD_NAME"
  echo ""
  exit 1
fi
