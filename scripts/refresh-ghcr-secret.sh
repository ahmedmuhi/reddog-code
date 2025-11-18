#!/usr/bin/env bash
# Refreshes the Kubernetes pull secret used to authenticate to GHCR.
# Usage: ./scripts/refresh-ghcr-secret.sh [namespace]
# Requires kubectl on PATH and access to the target cluster.

set -euo pipefail

NAMESPACE=${1:-default}
SECRET_NAME=${GHCR_SECRET_NAME:-ghcr-cred}
USERNAME=${GHCR_USERNAME:-ahmedmuhi}
TOKEN=${GHCR_PAT:-}

if [[ -z "$TOKEN" ]]; then
  read -rsp "Enter GHCR_PAT (token with read:packages scope): " TOKEN
  echo
fi

if [[ -z "$TOKEN" ]]; then
  echo "❌ GHCR_PAT is required" >&2
  exit 1
fi

echo "Refreshing secret '$SECRET_NAME' in namespace '$NAMESPACE'..."
if kubectl get secret "$SECRET_NAME" -n "$NAMESPACE" >/dev/null 2>&1; then
  kubectl delete secret "$SECRET_NAME" -n "$NAMESPACE" >/dev/null
fi

kubectl create secret docker-registry "$SECRET_NAME" \
  --docker-server=ghcr.io \
  --docker-username="$USERNAME" \
  --docker-password="$TOKEN" \
  --namespace="$NAMESPACE"

echo "✅ Secret '$SECRET_NAME' updated in namespace '$NAMESPACE'."
echo "You can now unset GHCR_PAT if desired (e.g., 'unset GHCR_PAT')."
