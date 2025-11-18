#!/bin/bash

echo "========================================="
echo "Red Dog Local Development Status"
echo "========================================="
echo ""

# Check if cluster exists
if ! kind get clusters | grep -q "reddog-local"; then
    echo "Cluster 'reddog-local' not found!"
    echo "Run ./scripts/setup-local-dev.sh to create it"
    exit 1
fi

# Set context
kubectl config use-context kind-reddog-local >/dev/null 2>&1

echo "Cluster Nodes:"
kubectl get nodes
echo ""

echo "Dapr Control Plane:"
kubectl get pods -n dapr-system
echo ""

echo "Nginx Ingress:"
kubectl get pods -n ingress-nginx
echo ""

echo "Infrastructure Pods:"
kubectl get pods -l app=sqlserver -o wide
kubectl get pods -l app=redis -o wide
echo ""

echo "Application Pods:"
kubectl get pods | grep -E "order-service|make-line-service|loyalty-service|accounting-service|receipt-generation-service|virtual-customers|virtual-worker|ui"
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

echo "Useful Commands:"
echo "  kubectl logs <pod-name>              # View logs"
echo "  kubectl describe pod <pod-name>      # Describe pod"
echo "  kubectl port-forward svc/<svc> 8080:80  # Port forward"
echo ""
