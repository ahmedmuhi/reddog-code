# Use official .NET dev container base image
FROM mcr.microsoft.com/devcontainers/dotnet:1-10.0-noble

# Version pinning for reproducible builds
ARG KIND_VERSION=0.30.0
ARG KUBECTL_VERSION=1.34.1
ARG HELM_VERSION=3.18.5
ARG DAPR_CLI_VERSION=1.16.3

# Install Kubernetes and Dapr tooling
# Combined into single RUN layer to reduce image size
RUN set -e && \
    # Install kind (Kubernetes-in-Docker)
    curl -fsSL -o /tmp/kind https://kind.sigs.k8s.io/dl/v${KIND_VERSION}/kind-linux-amd64 && \
    chmod +x /tmp/kind && \
    mv /tmp/kind /usr/local/bin/kind && \
    kind version && \
    \
    # Install kubectl (Kubernetes CLI)
    curl -fsSL -o /tmp/kubectl "https://dl.k8s.io/release/v${KUBECTL_VERSION}/bin/linux/amd64/kubectl" && \
    chmod +x /tmp/kubectl && \
    mv /tmp/kubectl /usr/local/bin/kubectl && \
    kubectl version --client && \
    \
    # Install Helm (package manager)
    curl -fsSL -o /tmp/get_helm.sh https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 && \
    chmod 700 /tmp/get_helm.sh && \
    DESIRED_VERSION=v${HELM_VERSION} /tmp/get_helm.sh && \
    rm /tmp/get_helm.sh && \
    helm version --short && \
    \
    # Install Dapr CLI
    curl -fsSL -o /tmp/install-dapr.sh https://raw.githubusercontent.com/dapr/cli/master/install/install.sh && \
    chmod +x /tmp/install-dapr.sh && \
    DAPR_INSTALL_DIR="/usr/local/bin" /tmp/install-dapr.sh ${DAPR_CLI_VERSION} && \
    rm /tmp/install-dapr.sh && \
    dapr version

# Set working directory to match VS Code mount point
WORKDIR /workspaces/reddog-code

# Configure Docker-in-Docker (kind needs Docker)
# This is handled by the docker-in-docker feature
