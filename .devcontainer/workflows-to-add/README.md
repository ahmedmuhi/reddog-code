# GitHub Actions Workflows for Dev Container

## Manual Installation Required

These workflow files enable CI/CD integration for the dev container but cannot be committed directly to `.github/workflows/` via automation due to GitHub App permissions.

## Installation Steps

1. **Copy the workflow files:**
   ```bash
   cp .devcontainer/workflows-to-add/devcontainer-ci.yml .github/workflows/
   cp .devcontainer/workflows-to-add/devcontainer-publish.yml .github/workflows/
   ```

2. **Commit and push:**
   ```bash
   git add .github/workflows/devcontainer-*.yml
   git commit -m "ci: add dev container GitHub Actions workflows"
   git push
   ```

## What These Workflows Do

### `devcontainer-ci.yml`
- **Triggers:** On push to main/develop branches, or when .devcontainer/ files change
- **Purpose:** Builds and tests in the dev container to ensure CI/dev parity
- **Actions:**
  - Verifies all language runtimes (.NET, Go, Python, Node.js)
  - Verifies Kubernetes tools (kind, kubectl, Helm, Dapr)
  - Builds .NET solution
  - Builds Vue.js UI (if exists)
  - Uploads build artifacts

### `devcontainer-publish.yml`
- **Triggers:** On push to main branch when .devcontainer/ files change, or manual trigger
- **Purpose:** Pre-builds and publishes dev container image to GitHub Container Registry
- **Actions:**
  - Builds dev container image
  - Publishes to `ghcr.io/<your-org>/reddog-code/devcontainer:latest`
  - Tags with commit SHA for versioning
  - Enables faster container startup for developers (uses pre-built image)

## Benefits

Once installed, these workflows provide:
- ✅ 100% CI/dev environment parity
- ✅ Automated container image publishing
- ✅ Faster dev container startup (pre-built images)
- ✅ Verification that all tools are correctly installed

## Optional: Delete These Files After Installation

Once you've manually copied the workflows to `.github/workflows/`, you can delete this directory:

```bash
git rm -r .devcontainer/workflows-to-add/
git commit -m "chore: remove staged workflow files after manual installation"
```
