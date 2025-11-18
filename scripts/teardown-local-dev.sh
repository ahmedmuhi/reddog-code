#!/bin/bash
set -e

echo "========================================="
echo "Red Dog Local Development Teardown"
echo "========================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

print_status() {
    echo -e "${GREEN}✓${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

# Delete kind cluster
echo "Deleting kind cluster 'reddog-local'..."
if kind get clusters | grep -q "reddog-local"; then
    kind delete cluster --name reddog-local
    print_status "Cluster deleted"
else
    print_warning "Cluster 'reddog-local' not found"
fi

echo ""
echo "Teardown complete!"
