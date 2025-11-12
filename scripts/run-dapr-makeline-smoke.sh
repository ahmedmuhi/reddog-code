#!/usr/bin/env bash
# Runs MakeLineService smoke tests (direct HTTP + Dapr service invocation).
# Usage: ./scripts/run-dapr-makeline-smoke.sh [storeId]

set -euo pipefail

STORE_ID=${1:-Redmond}
SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
FIND_PORT_SCRIPT="${SCRIPT_DIR}/find-open-port.sh"

if ! command -v kubectl >/dev/null 2>&1; then
  echo "kubectl is required to run this script." >&2
  exit 1
fi

if ! command -v curl >/dev/null 2>&1; then
  echo "curl is required to run this script." >&2
  exit 1
fi

echo "🔍 Locating MakeLineService pod..."
POD_NAME=$(kubectl get pods -l app=make-line-service --no-headers 2>/dev/null | head -1 | awk '{print $1}' || true)
if [ -z "$POD_NAME" ]; then
  echo "Unable to find pod with label app=make-line-service" >&2
  exit 1
fi
echo "  Pod: $POD_NAME"

echo ""
echo "☕ HTTP smoke test (kubectl port-forward svc/makelineservice)..."
SERVICE_PORT=$("$FIND_PORT_SCRIPT" 5200 15200 25200)
kubectl port-forward svc/makelineservice ${SERVICE_PORT}:80 >/tmp/makeline-http-portforward.log 2>&1 &
HTTP_PF_PID=$!
sleep 3

set +e
HTTP_STATUS=$(curl -s -o /tmp/makeline-http-response.json -w "%{http_code}" http://localhost:${SERVICE_PORT}/orders/${STORE_ID})
set -e
kill $HTTP_PF_PID 2>/dev/null || true
wait $HTTP_PF_PID 2>/dev/null || true

if [ "$HTTP_STATUS" = "200" ]; then
  echo "  ✅ /orders/${STORE_ID} returned 200 via service ingress (port ${SERVICE_PORT})"
else
  echo "  ❌ HTTP smoke test failed (status $HTTP_STATUS). See /tmp/makeline-http-response.json for details."
  exit 1
fi

echo ""
echo "🔄 Dapr service invocation smoke test..."
DAPR_PORT=$("$FIND_PORT_SCRIPT" 3510 23510 33510)
kubectl port-forward $POD_NAME ${DAPR_PORT}:3500 >/tmp/makeline-dapr-portforward.log 2>&1 &
DAPR_PF_PID=$!
sleep 3

set +e
DAPR_STATUS=$(curl -s -o /tmp/makeline-dapr-response.json -w "%{http_code}" \
  http://localhost:${DAPR_PORT}/v1.0/invoke/makelineservice/method/orders/${STORE_ID})
set -e
kill $DAPR_PF_PID 2>/dev/null || true
wait $DAPR_PF_PID 2>/dev/null || true

if [ "$DAPR_STATUS" = "200" ]; then
  echo "  ✅ Dapr invocation succeeded (status 200)."
else
  echo "  ❌ Dapr invocation failed (status $DAPR_STATUS). See /tmp/makeline-dapr-response.json for details."
  exit 1
fi

echo ""
echo "🎉 MakeLineService smoke tests passed for store '${STORE_ID}'."
