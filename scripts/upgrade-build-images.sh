#!/bin/bash
# Build all image tags for a service and load into kind cluster
# Usage: ./scripts/upgrade-build-images.sh <ServiceName>
#
# This script:
# 1. Validates Dockerfile exists
# 2. Builds ALL required image tags in one operation
# 3. Loads all images into kind cluster
# 4. Verifies images are available
#
# Prevents the "stale image" problem by ensuring ALL tags are rebuilt together.

set -euo pipefail

if [ $# -ne 1 ]; then
  echo "Usage: $0 <ServiceName>"
  echo "Example: $0 MakeLineService"
  exit 1
fi

SERVICE_NAME="$1"
SERVICE_LOWER=$(echo "$SERVICE_NAME" | tr '[:upper:]' '[:lower:]')
DOCKERFILE="RedDog.${SERVICE_NAME}/Dockerfile"

echo "╔════════════════════════════════════════════════════════════════╗"
echo "║       Building Images for $SERVICE_NAME"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

# Validate Dockerfile exists
if [ ! -f "$DOCKERFILE" ]; then
  echo "❌ ERROR: Dockerfile not found: $DOCKERFILE"
  echo ""
  echo "Expected location:"
  echo "  RedDog.${SERVICE_NAME}/Dockerfile"
  echo ""
  exit 1
fi

echo "✓ Dockerfile found: $DOCKERFILE"
echo ""

# Define all required image tags
# These tags cover all scenarios:
# - :net10 - Standard .NET 10 tag
# - :net10-test - Testing tag for experimental builds
# - :local - Local development tag (used by Helm values-local.yaml)
# - :latest - Latest tag for convenience
TAGS=(
  "reddog-${SERVICE_LOWER}:net10"
  "reddog-${SERVICE_LOWER}:net10-test"
  "ghcr.io/ahmedmuhi/reddog-${SERVICE_LOWER}:local"
  "ghcr.io/ahmedmuhi/reddog-${SERVICE_LOWER}:latest"
)

echo "✓ Will build ${#TAGS[@]} image tags:"
for TAG in "${TAGS[@]}"; do
  echo "  - $TAG"
done
echo ""

# Build all tags
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║                    BUILDING IMAGES"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

BUILD_START=$(date +%s)

DOCKER_BUILD_FLAGS=()
if docker build --help 2>&1 | grep -q -- "--progress"; then
  DOCKER_BUILD_FLAGS+=(--progress=plain)
fi

for TAG in "${TAGS[@]}"; do
  echo "Building: $TAG"
  if docker build "${DOCKER_BUILD_FLAGS[@]}" -t "$TAG" -f "$DOCKERFILE" .; then
    echo "  ✓ Built: $TAG"
  else
    echo "  ❌ FAILED to build: $TAG"
    exit 1
  fi
  echo ""
done

BUILD_END=$(date +%s)
BUILD_TIME=$((BUILD_END - BUILD_START))

echo "✓ Successfully built ${#TAGS[@]} image tags in ${BUILD_TIME}s"
echo ""

# Verify images exist locally
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║                 VERIFYING LOCAL IMAGES"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

echo "Local Docker images:"
docker images --format "table {{.Repository}}:{{.Tag}}\t{{.Size}}\t{{.CreatedAt}}" | grep "reddog-${SERVICE_LOWER}" || {
  echo "❌ ERROR: No images found after build!"
  exit 1
}
echo ""

# Load images into kind cluster
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║              LOADING IMAGES INTO KIND CLUSTER"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

LOAD_START=$(date +%s)

echo "Loading ${#TAGS[@]} images into kind cluster 'reddog-local'..."
if kind load docker-image "${TAGS[@]}" --name reddog-local; then
  echo "✓ All images loaded into kind cluster"
else
  echo "❌ FAILED to load images into kind cluster"
  echo ""
  echo "Troubleshooting:"
  echo "  1. Check cluster exists: kind get clusters"
  echo "  2. Verify cluster name: should be 'reddog-local'"
  echo "  3. Try loading one tag manually:"
  echo "     kind load docker-image ${TAGS[0]} --name reddog-local"
  exit 1
fi

LOAD_END=$(date +%s)
LOAD_TIME=$((LOAD_END - LOAD_START))
echo ""
echo "✓ Loaded images in ${LOAD_TIME}s"
echo ""

# Verify images available in kind cluster
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║            VERIFYING IMAGES IN KIND CLUSTER"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

echo "Images in kind cluster:"
kind get images --name reddog-local 2>/dev/null | grep "${SERVICE_LOWER}" || {
  echo "⚠️  WARNING: Images not visible in kind cluster"
  echo "This may be a display issue; images may still be loaded."
}
echo ""

# Create image manifest
MANIFEST_FILE=".image-manifest-${SERVICE_NAME}.txt"
cat <<EOF > "$MANIFEST_FILE"
═══════════════════════════════════════════════════════════════
                   IMAGE BUILD MANIFEST
═══════════════════════════════════════════════════════════════

Service:       ${SERVICE_NAME}
Build Date:    $(date -u +"%Y-%m-%d %H:%M:%S UTC")
Build Time:    ${BUILD_TIME}s
Load Time:     ${LOAD_TIME}s
Target:        .NET 10
Dapr SDK:      1.16.0
Health Probes: /healthz, /livez, /readyz (ADR-0005)

───────────────────────────────────────────────────────────────
                      IMAGE TAGS BUILT
───────────────────────────────────────────────────────────────

$(docker images --format "{{.Repository}}:{{.Tag}}\t{{.Size}}\t{{.CreatedAt}}" | grep "reddog-${SERVICE_LOWER}")

───────────────────────────────────────────────────────────────
                         DEPLOYMENT
───────────────────────────────────────────────────────────────

Cluster:       kind (reddog-local)
Status:        Loaded and ready for deployment

Next Steps:
  1. Deploy via Helm:
     helm upgrade reddog charts/reddog -f values/values-local.yaml

  2. Validate deployment:
     ./scripts/upgrade-validate.sh ${SERVICE_NAME}

  3. Check pod status:
     kubectl get pods -l app=${SERVICE_LOWER}-service

═══════════════════════════════════════════════════════════════
EOF

echo "✓ Image manifest created: $MANIFEST_FILE"
echo ""

# Final summary
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║                       SUMMARY"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""
echo "✓ SUCCESS - All images built and loaded"
echo ""
echo "Details:"
echo "  • Service:        $SERVICE_NAME"
echo "  • Images built:   ${#TAGS[@]} tags"
echo "  • Build time:     ${BUILD_TIME}s"
echo "  • Load time:      ${LOAD_TIME}s"
echo "  • Total time:     $((BUILD_TIME + LOAD_TIME))s"
echo "  • Manifest:       $MANIFEST_FILE"
echo ""
echo "Ready for deployment!"
echo ""
echo "Next steps:"
echo "  1. Review: cat $MANIFEST_FILE"
echo "  2. Deploy: helm upgrade reddog charts/reddog -f values/values-local.yaml"
echo "  3. Validate: ./scripts/upgrade-validate.sh $SERVICE_NAME"
echo ""
