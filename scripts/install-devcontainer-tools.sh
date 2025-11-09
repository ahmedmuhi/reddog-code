#!/bin/bash
set -e

# Only run in remote Claude Code environment
if [ "$CLAUDE_CODE_REMOTE" != "true" ]; then
  echo "⏭️  Skipping dev container tools installation (not in remote environment)"
  exit 0
fi

echo "🚀 Installing dev container tools for Red Dog development..."
echo ""

# Check what's already available
echo "📋 Pre-installed tools:"
echo "  ✅ Python: $(python3 --version 2>/dev/null || echo 'Not found')"
echo "  ✅ Node.js: $(node --version 2>/dev/null || echo 'Not found')"
echo "  ✅ Go: $(go version 2>/dev/null | awk '{print $3}' || echo 'Not found')"
echo ""

# Install .NET 10 SDK
echo "📦 Installing .NET 10 SDK..."
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 10.0 --install-dir /opt/dotnet
export PATH="/opt/dotnet:$PATH"
export DOTNET_ROOT="/opt/dotnet"

# Persist .NET environment variables
echo "export PATH=\"/opt/dotnet:\$PATH\"" >> "$CLAUDE_ENV_FILE"
echo "export DOTNET_ROOT=\"/opt/dotnet\"" >> "$CLAUDE_ENV_FILE"

# Install Docker static binaries
echo "📦 Installing Docker..."
mkdir -p /opt/docker
cd /opt/docker
curl -fsSL https://download.docker.com/linux/static/stable/x86_64/docker-27.4.1.tgz -o docker.tgz
tar xzf docker.tgz
mv docker/* /usr/local/bin/
rm -rf docker.tgz docker
cd "$CLAUDE_PROJECT_DIR"

# Install iptables (required by Docker)
echo "📦 Installing iptables..."
DEBIAN_FRONTEND=noninteractive apt-get update -qq 2>/dev/null || true
DEBIAN_FRONTEND=noninteractive apt-get install -y --allow-unauthenticated iptables >/dev/null 2>&1 || true

# Install kubectl
echo "📦 Installing kubectl..."
curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
chmod +x kubectl
mv kubectl /usr/local/bin/

# Install kind
echo "📦 Installing kind..."
curl -Lo /usr/local/bin/kind https://kind.sigs.k8s.io/dl/v0.20.0/kind-linux-amd64
chmod +x /usr/local/bin/kind

# Install Helm
echo "📦 Installing Helm..."
curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash

# Install Dapr CLI
echo "📦 Installing Dapr CLI..."
wget -q https://raw.githubusercontent.com/dapr/cli/master/install/install.sh -O - | /bin/bash

echo ""
echo "✅ Dev container tools installation complete!"
echo ""
echo "📋 Installed versions:"
echo "  .NET: $(/opt/dotnet/dotnet --version 2>/dev/null || echo 'Failed to install')"
echo "  Docker: $(docker --version 2>/dev/null || echo 'Failed to install')"
echo "  kubectl: $(kubectl version --client --short 2>/dev/null || echo 'Failed to install')"
echo "  kind: $(kind version 2>/dev/null || echo 'Failed to install')"
echo "  Helm: $(helm version --short 2>/dev/null || echo 'Failed to install')"
echo "  Dapr: $(dapr version --client 2>/dev/null || echo 'Failed to install')"
echo ""
echo "🎉 Environment ready for Red Dog polyglot development!"
echo ""

exit 0
