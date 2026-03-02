#!/bin/bash
# Local Development Workflow: kind + Helm (sole supported method)
# The legacy `dapr run` workflow with `.dapr/components/` is no longer supported.
# All Dapr components are deployed via Helm charts (charts/reddog/templates/dapr-components/).
set -e

echo "========================================="
echo "Red Dog Local Development Setup"
echo "========================================="
echo ""

source "$(dirname "${BASH_SOURCE[0]}")/_helpers.sh"

# Check prerequisites
echo "Checking prerequisites..."
command -v kind >/dev/null 2>&1 || { print_error "kind is not installed"; exit 1; }
command -v kubectl >/dev/null 2>&1 || { print_error "kubectl is not installed"; exit 1; }
command -v helm >/dev/null 2>&1 || { print_error "helm is not installed"; exit 1; }
command -v docker >/dev/null 2>&1 || { print_error "docker is not installed"; exit 1; }
print_status "All prerequisites installed"
echo ""

# Check WSL2 memory configuration (Windows users only)
echo "Checking WSL2 configuration..."
if grep -qi microsoft /proc/version 2>/dev/null; then
    print_status "Running on WSL2"

    # Check if .wslconfig exists
    WSLCONFIG_PATH="/mnt/c/Users/$(whoami)/.wslconfig"
    if [ ! -f "$WSLCONFIG_PATH" ]; then
        print_warning "No .wslconfig file found"
        echo ""
        echo "  WSL2 can consume significant memory (up to 50% of RAM or 8GB by default)."
        echo "  Without limits, VMMEMWSL may consume 14GB+ RAM, leaving only 2GB for Windows."
        echo ""
        echo "  Recommended: Create C:\\Users\\$(whoami)\\.wslconfig with:"
        echo ""
        echo "  [wsl2]"
        echo "  memory=4GB           # Hard limit (adjust based on your RAM)"
        echo "  processors=4         # CPU core limit"
        echo "  swap=2GB"
        echo "  pageReporting=true   # Return memory to Windows"
        echo ""
        echo "  [experimental]"
        echo "  autoMemoryReclaim=dropcache  # Docker-compatible mode"
        echo "  sparseVhd=true"
        echo ""
        echo "  After creating .wslconfig, run 'wsl --shutdown' and restart Docker Desktop."
        echo ""
        echo "  For more details, see: https://learn.microsoft.com/en-us/windows/wsl/wsl-config#wslconfig"
        echo ""
        read -p "Press Enter to continue without .wslconfig (or Ctrl+C to exit and configure)..."
        echo ""
    else
        print_status ".wslconfig exists at $WSLCONFIG_PATH"

        # Check if memory limit is set
        if grep -q "memory=" "$WSLCONFIG_PATH" 2>/dev/null; then
            MEMORY_LIMIT=$(grep "memory=" "$WSLCONFIG_PATH" | cut -d= -f2 | tr -d ' ')
            print_status "WSL2 memory limit: $MEMORY_LIMIT"
        else
            print_warning ".wslconfig exists but no memory limit set"
            echo "  Consider adding 'memory=4GB' to limit WSL2 memory usage."
        fi
    fi
else
    print_status "Not running on WSL2, skipping .wslconfig check"
fi
echo ""

# Ensure local values file exists (auto-copy from sample if missing)
if [ ! -f values/values-local.yaml ]; then
    print_warning "values/values-local.yaml not found. Copying from sample..."
    cp values/values-local.yaml.sample values/values-local.yaml
    print_warning "IMPORTANT: Edit values/values-local.yaml and set your SQL Server password!"
    echo "  Default password is 'CHANGE_ME' — update infrastructure.sqlserver.saPassword"
    echo ""
    read -p "Press Enter after editing values/values-local.yaml (or Ctrl+C to exit)..."
fi
print_status "values/values-local.yaml exists"
echo ""

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
NGINX_VERSION="controller-v1.14.0"
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/${NGINX_VERSION}/deploy/static/provider/kind/deploy.yaml
kubectl wait --namespace ingress-nginx \
    --for=condition=ready pod \
    --selector=app.kubernetes.io/component=controller \
    --timeout=90s
print_status "Nginx Ingress Controller ${NGINX_VERSION} installed"
echo ""

# 4. Create application namespace and deploy infrastructure
echo "Step 4: Creating namespace and deploying infrastructure (SQL Server, Redis)..."
kubectl create namespace reddog --dry-run=client -o yaml | kubectl apply -f -
print_status "Namespace 'reddog' ready"

helm install reddog-infra ./charts/infrastructure \
    -f values/values-local.yaml \
    --namespace reddog \
    --wait \
    --timeout 10m
print_status "Infrastructure deployed"
echo ""

# Wait for SQL Server to be ready
echo "Waiting for SQL Server to be ready..."
kubectl wait --for=condition=ready pod \
    -n reddog \
    --selector=app=sqlserver \
    --timeout=300s
print_status "SQL Server is ready"
echo ""

# Wait for Redis to be ready
echo "Waiting for Redis to be ready..."
kubectl wait --for=condition=ready pod \
    -n reddog \
    --selector=app=redis \
    --timeout=120s
print_status "Redis is ready"
echo ""

# 5. Deploy Red Dog application
echo "Step 5: Deploying Red Dog application..."
helm install reddog ./charts/reddog \
    -f values/values-local.yaml \
    --namespace reddog \
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
kubectl get pods -n reddog -l app=sqlserver -o wide
kubectl get pods -n reddog -l app=redis -o wide
echo ""
echo "Application Status:"
kubectl get pods -n reddog
echo ""
echo "Services:"
kubectl get svc -n reddog
echo ""
echo "Ingress:"
kubectl get ingress -n reddog
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
