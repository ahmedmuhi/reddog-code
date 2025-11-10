#!/bin/bash
set -e

echo "========================================="
echo "Red Dog Local Development Setup"
echo "========================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}✓${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

print_error() {
    echo -e "${RED}✗${NC} $1"
}

# Check prerequisites
echo "Checking prerequisites..."
command -v kind >/dev/null 2>&1 || { print_error "kind is not installed"; exit 1; }
command -v kubectl >/dev/null 2>&1 || { print_error "kubectl is not installed"; exit 1; }
command -v helm >/dev/null 2>&1 || { print_error "helm is not installed"; exit 1; }
command -v docker >/dev/null 2>&1 || { print_error "docker is not installed"; exit 1; }
print_status "All prerequisites installed"
echo ""

# Ensure local values file exists
if [ ! -f values/values-local.yaml ]; then
    print_error "values/values-local.yaml not found."
    echo "Create it by copying values/values-local.yaml.sample and setting local secrets (e.g., SQL password)."
    exit 1
fi

# 1. Create kind cluster
echo "Step 1: Creating kind cluster..."
if kind get clusters | grep -q "reddog-local"; then
    print_warning "Cluster 'reddog-local' already exists. Deleting..."
    kind delete cluster --name reddog-local
fi

kind create cluster --config kind-config.yaml
print_status "kind cluster created"
echo ""

# 2. Install Dapr
echo "Step 2: Installing Dapr..."
helm repo add dapr https://dapr.github.io/helm-charts/ 2>/dev/null || true
helm repo update
helm install dapr dapr/dapr \
    --namespace dapr-system \
    --create-namespace \
    --version 1.16.2 \
    --wait \
    --timeout 5m
print_status "Dapr installed"
print_warning "Known Issue: Dapr 1.16.x sidecars will show 1/2 Ready due to probe port bug (port 3501 vs 3500)"
print_warning "This is cosmetic only - services are functional. See docs/known-issues.md for details."
echo ""

# Verify Dapr installation
echo "Verifying Dapr installation..."
kubectl wait --namespace dapr-system \
    --for=condition=ready pod \
    --selector=app.kubernetes.io/part-of=dapr \
    --timeout=300s
print_status "Dapr is ready"
echo ""

# 3. Install Nginx Ingress Controller
echo "Step 3: Installing Nginx Ingress Controller..."
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/kind/deploy.yaml
kubectl wait --namespace ingress-nginx \
    --for=condition=ready pod \
    --selector=app.kubernetes.io/component=controller \
    --timeout=90s
print_status "Nginx Ingress Controller installed"
echo ""

# 4. Deploy infrastructure
echo "Step 4: Deploying infrastructure (SQL Server, Redis)..."
helm install reddog-infra ./charts/infrastructure \
    -f values/values-local.yaml \
    --wait \
    --timeout 10m
print_status "Infrastructure deployed"
echo ""

# Wait for SQL Server to be ready
echo "Waiting for SQL Server to be ready..."
kubectl wait --for=condition=ready pod \
    --selector=app=sqlserver \
    --timeout=300s
print_status "SQL Server is ready"
echo ""

# Wait for Redis to be ready
echo "Waiting for Redis to be ready..."
kubectl wait --for=condition=ready pod \
    --selector=app=redis \
    --timeout=120s
print_status "Redis is ready"
echo ""

# 5. Deploy Red Dog application
echo "Step 5: Deploying Red Dog application..."
helm install reddog ./charts/reddog \
    -f values/values-local.yaml \
    --wait \
    --timeout 10m
print_status "Red Dog application deployed"
echo ""

# Display status
echo ""
echo "========================================="
echo "Setup Complete!"
echo "========================================="
echo ""
echo "Cluster Status:"
kubectl get nodes
echo ""
echo "Dapr Status:"
kubectl get pods -n dapr-system
echo ""
echo "Infrastructure Status:"
kubectl get pods -l app=sqlserver -o wide
kubectl get pods -l app=redis -o wide
echo ""
echo "Application Status:"
kubectl get pods -l app.kubernetes.io/managed-by=Helm
echo ""
echo "Services:"
kubectl get svc
echo ""
echo "Ingress:"
kubectl get ingress
echo ""
echo "========================================="
echo "Access Points:"
echo "========================================="
echo "UI: http://localhost"
echo "OrderService API: http://localhost/api/orders"
echo ""
echo "Next steps:"
echo "1. Verify services are running: kubectl get pods"
echo "2. Access the UI: open http://localhost"
echo "3. Test the API: curl http://localhost/api/orders"
echo ""
