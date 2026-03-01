#!/bin/bash
set -e

echo "========================================="
echo "Red Dog Local Development Teardown"
echo "========================================="
echo ""

source "$(dirname "$0")/_helpers.sh"

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
