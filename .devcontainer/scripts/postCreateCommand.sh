#!/bin/bash
set -e

echo "🚀 Setting up Red Dog polyglot development environment..."

# Fix Claude Code credentials permissions (bind mount preserves host ownership)
if [ -f "$HOME/.claude/.credentials.json" ]; then
  echo "🔑 Fixing Claude Code credentials permissions..."
  chmod 600 "$HOME/.claude/.credentials.json" 2>/dev/null || echo "⚠️  Could not fix credentials permissions (mount is read-only)"
fi

# Restore .NET packages
echo "📦 Restoring .NET packages..."
dotnet restore RedDog.sln 2>/dev/null || echo "⚠️  No .NET solution found (expected during initial setup)"

# Install Node.js dependencies for UI (if package.json exists)
if [ -f "RedDog.UI/package.json" ]; then
  echo "📦 Installing Node.js dependencies for UI..."
  cd RedDog.UI && npm install && cd ..
fi

# Verify language runtimes
echo ""
echo "✅ Verifying language runtimes..."
echo "  .NET version: $(dotnet --version)"
echo "  Go version: $(go version | awk '{print $3}')"
echo "  Python version: $(python3 --version | awk '{print $2}')"
echo "  Node.js version: $(node --version)"
echo "  npm version: $(npm --version)"

# Verify Kubernetes/Dapr tooling
echo ""
echo "✅ Verifying Kubernetes & Dapr tools..."
echo "  kind version: $(kind version)"
echo "  kubectl version: $(kubectl version --client --short 2>/dev/null || echo 'not connected')"
echo "  Helm version: $(helm version --short)"
echo "  Dapr version: $(dapr version --client 2>/dev/null || echo 'CLI only')"

echo ""
echo "✨ Polyglot development environment ready!"
echo ""
echo "📝 Next steps:"
echo "   1. Create kind cluster: ./scripts/setup-local-dev.sh (when ADR-0008 implemented)"
echo "   2. Build .NET services: dotnet build RedDog.sln"
echo "   3. Build UI: cd RedDog.UI && npm run build"
echo "   4. Deploy to kind: helm install reddog ./charts/reddog -f values/values-local.yaml"
echo ""
