# Safe Cleanup Guide

This guide provides step-by-step instructions for safely removing outdated and unnecessary components from the Red Dog project.

## ⚠️ IMPORTANT: Read Before Starting

**Prerequisites:**
- All changes are committed to git (clean working directory)
- You have a backup or can easily revert via git
- You understand what each component does (see analysis below)

**Safety Strategy:**
- Work in phases (safest to riskiest)
- Test after each phase
- Use git to track all deletions
- Commit after each successful phase

---

## Pre-Flight Checklist

Before starting cleanup, verify:

```bash
# 1. Clean git status
git status
# Should show: "nothing to commit, working tree clean"
# OR only show .claude/, CLAUDE.md, MODERNIZATION_PLAN.md, SAFE_CLEANUP.md

# 2. Create a safety branch
git checkout -b cleanup/phase-0-foundation
git branch --show-current
# Should show: cleanup/phase-0-foundation

# 3. Verify you're on your fork (not Azure's repo)
git remote -v
# Should show YOUR GitHub username, not Azure/reddog-retail-demo
```

**If any checks fail, STOP and fix issues first.**

---

## Cleanup Phases

### Phase 1: Zero-Risk Removals (No Dependencies)

**What:** Remove devcontainer, personal notes, and isolated manifests
**Risk:** 🟢 None (no code dependencies)
**Time:** 2 minutes

#### Items to Remove:

1. **`.devcontainer/`** - Devcontainer configuration (not using devcontainers)
2. **`manifests/local/`** - Local development manifests (not doing local dev)
3. **`manifests/corporate/`** - Corporate/Arc scenario manifests (not using Arc)
4. **`docs/`** (if it exists and has outdated info) - May contain old diagrams

#### Commands:

```bash
# Remove devcontainer
rm -rf .devcontainer/
echo "✓ Removed .devcontainer/"

# Remove local dev manifests
rm -rf manifests/local/
echo "✓ Removed manifests/local/"

# Remove corporate/Arc manifests
rm -rf manifests/corporate/
echo "✓ Removed manifests/corporate/"

# Check if docs directory exists and inspect it
if [ -d "docs" ]; then
    echo "⚠️  docs/ directory exists. Review contents before removing:"
    ls -la docs/
    # If you determine it's safe to remove:
    # rm -rf docs/
fi
```

#### Verification:

```bash
# These directories should not exist
ls -d .devcontainer manifests/local manifests/corporate 2>/dev/null
# Should output: "No such file or directory"

# Git status should show deletions
git status
```

#### Commit:

```bash
git add -A
git commit -m "Phase 1: Remove devcontainer and unused manifest directories

- Removed .devcontainer/ (not using devcontainers)
- Removed manifests/local/ (no local development focus)
- Removed manifests/corporate/ (no Arc scenarios)"
```

---

### Phase 2: Service Cleanup (Isolated Services)

**What:** Remove services that won't be used in modernized architecture
**Risk:** 🟡 Low (isolated, but need to clean up references)
**Time:** 5 minutes

#### Services to Remove:

1. **`RedDog.CorporateTransferService/`** - Arc hub service (not in .sln file, completely isolated)
2. **`RedDog.Bootstrapper/`** - Database initialization service (will be replaced with init containers)

**Analysis:**
- `CorporateTransferService`: NOT in RedDog.sln, only in GitHub workflow and manifests
- `Bootstrapper`: IS in RedDog.sln (line 20), in manifests, needs careful removal

#### Commands:

```bash
# === Remove CorporateTransferService (safest first) ===

# 1. Remove service directory
rm -rf RedDog.CorporateTransferService/
echo "✓ Removed RedDog.CorporateTransferService/"

# 2. Remove GitHub workflow
rm .github/workflows/package-corp-transfer-service.yaml
echo "✓ Removed CorporateTransfer workflow"

# 3. Remove K8s manifest
rm manifests/branch/base/deployments/corp-transfer-fx.yaml
echo "✓ Removed CorporateTransfer deployment manifest"

# === Remove Bootstrapper (needs .sln update) ===

# 1. Remove service directory
rm -rf RedDog.Bootstrapper/
echo "✓ Removed RedDog.Bootstrapper/"

# 2. Remove GitHub workflow
rm .github/workflows/package-bootstrapper.yaml
echo "✓ Removed Bootstrapper workflow"

# 3. Remove K8s manifests (exists in multiple locations)
rm manifests/branch/base/deployments/bootstrapper.yaml
echo "✓ Removed Bootstrapper deployment manifest"

# 4. Remove from solution file (IMPORTANT)
# This requires manual editing or sed
sed -i '/RedDog.Bootstrapper/d' RedDog.sln
echo "✓ Removed Bootstrapper from .sln file"

# 5. Clean up .gitignore references
sed -i '/CorporateTransferService/d' .gitignore
echo "✓ Cleaned up .gitignore"
```

#### Verification:

```bash
# Directories should not exist
ls -d RedDog.CorporateTransferService RedDog.Bootstrapper 2>/dev/null
# Should output: "No such file or directory"

# Verify solution file still loads (optional, if you have dotnet installed)
# dotnet sln list

# Check git status
git status
```

#### Commit:

```bash
git add -A
git commit -m "Phase 2: Remove unused services (Bootstrapper, CorporateTransfer)

- Removed RedDog.CorporateTransferService/ (Arc scenarios not needed)
- Removed RedDog.Bootstrapper/ (will use init containers instead)
- Removed associated workflows and manifests
- Updated RedDog.sln
- Cleaned up .gitignore"
```

---

### Phase 3: VS Code Configuration (Optional)

**What:** Remove or minimize VS Code specific configuration
**Risk:** 🟡 Low (only affects local development experience)
**Time:** 2 minutes

**Decision Point:** You said you don't care about VS Code configs, but they don't hurt anything. Options:

**Option A: Complete Removal**
```bash
rm -rf .vscode/
git add -A
git commit -m "Phase 3: Remove VS Code configuration (cloud-first approach)"
```

**Option B: Minimal Preservation (Recommended)**
Keep just `.vscode/` for future reference but add a note that it's outdated:
```bash
# Create a README in .vscode explaining it's outdated
cat > .vscode/README.md << 'EOF'
# VS Code Configuration

⚠️ **Note:** These configurations are from the original .NET 6 local development setup.

They are **outdated** and not maintained. The project focuses on cloud deployments, not local debugging.

If you want to use VS Code debugging:
1. Update launch.json for your environment
2. Update to .NET 8/9 paths
3. Consider using Docker Compose instead
EOF

git add .vscode/README.md
git commit -m "Phase 3: Document VS Code configs as outdated (not maintained)"
```

**Choose your option and execute.**

---

### Phase 4: GitHub Workflows Cleanup

**What:** Remove all old workflows (they'll be replaced with modern ones later)
**Risk:** 🟢 None (just CI/CD, doesn't affect runtime)
**Time:** 1 minute

**Note:** We're removing ALL workflows because they:
- Use deprecated GitHub Actions syntax
- Push to Azure's registry (not yours)
- Have hardcoded Microsoft emails
- Will be completely replaced in Phase 7 of modernization

#### Commands:

```bash
# Remove all workflow files
rm -rf .github/workflows/

# Recreate empty directory (keep .github/ structure)
mkdir -p .github/workflows/

# Add a placeholder README
cat > .github/workflows/README.md << 'EOF'
# GitHub Workflows

Workflows removed during modernization.

New workflows will be added in Phase 7 of the modernization plan.

See MODERNIZATION_PLAN.md for details.
EOF
```

#### Verification:

```bash
# Only README should exist
ls .github/workflows/
# Should show: README.md

git status
```

#### Commit:

```bash
git add -A
git commit -m "Phase 4: Remove outdated GitHub workflows

- Removed all 11 workflow files (deprecated syntax, wrong registry)
- Workflows will be recreated in Phase 7 with modern patterns
- See MODERNIZATION_PLAN.md"
```

---

### Phase 5: Manifest Simplification

**What:** Clean up Flux and unused components from manifests
**Risk:** 🟡 Low (only affects deployment, not code)
**Time:** 3 minutes

#### Items to Remove:

1. **Flux v1 configurations** - Deprecated GitOps tool
2. **RabbitMQ manifests** - Will use Redis for simplicity
3. **Cert-manager** (optional) - May not need for learning scenarios

#### Commands:

```bash
# Remove Flux configuration files
find manifests/ -name ".flux.yaml" -delete
echo "✓ Removed Flux configuration files"

# Remove Flux dependency
rm -rf manifests/branch/dependencies/.flux.yaml
echo "✓ Removed Flux dependency"

# Optional: Remove RabbitMQ (if switching to Redis only)
# Uncomment if you want to simplify to Redis-only:
# rm -rf manifests/branch/dependencies/rabbitmq/
# echo "✓ Removed RabbitMQ manifests"

# Optional: Remove cert-manager (if not doing TLS in demos)
# Uncomment if you don't need TLS:
# rm -rf manifests/branch/dependencies/cert-manager/
# echo "✓ Removed cert-manager manifests"
```

#### Verification:

```bash
# Flux files should not exist
find manifests/ -name ".flux.yaml"
# Should output nothing

git status
```

#### Commit:

```bash
git add -A
git commit -m "Phase 5: Remove Flux v1 configurations

- Removed all .flux.yaml files (Flux v1 is deprecated)
- Simplified manifest structure"
```

---

### Phase 6: REST Samples (Optional)

**What:** Decide whether to keep REST samples
**Risk:** 🟢 None (just testing files)
**Time:** 1 minute

**Decision:** You said to keep these for content creation. But here's how to remove if needed:

```bash
# Only run if you decide you don't need them
# rm -rf rest-samples/
# git add -A
# git commit -m "Phase 6: Remove REST samples (not needed)"
```

**Recommended:** Keep them for now.

---

## Post-Cleanup Verification

After completing all phases, run these checks:

```bash
# 1. Verify remaining directory structure
ls -la

# Should have:
# - RedDog.AccountingModel/
# - RedDog.AccountingService/
# - RedDog.LoyaltyService/
# - RedDog.MakeLineService/
# - RedDog.OrderService/
# - RedDog.ReceiptGenerationService/
# - RedDog.UI/
# - RedDog.VirtualCustomers/
# - RedDog.VirtualWorker/
# - manifests/branch/
# - assets/
# - rest-samples/ (optional)
# - .claude/
# - CLAUDE.md
# - MODERNIZATION_PLAN.md
# - SAFE_CLEANUP.md
# - RedDog.sln

# Should NOT have:
# - .devcontainer/
# - RedDog.Bootstrapper/
# - RedDog.CorporateTransferService/
# - manifests/local/
# - manifests/corporate/

# 2. Verify git history
git log --oneline -6
# Should show your cleanup commits

# 3. Verify solution file (if dotnet installed)
# dotnet sln list
# Should list 9 projects (not 10)
```

---

## What NOT to Remove Yet

**Do NOT remove these (needed for current functionality):**

- ❌ `RedDog.AccountingModel/` - Still used by AccountingService
- ❌ `manifests/branch/` - Primary K8s deployment manifests
- ❌ `RedDog.sln` - Solution file
- ❌ `assets/` - May contain images/diagrams
- ❌ Any `.csproj` files in remaining services
- ❌ `rest-samples/` - Useful for your content creation

These will be updated/replaced during later modernization phases, not deleted.

---

## Rollback Instructions

If something goes wrong, you can rollback:

```bash
# Rollback specific phase (while still on branch)
git log --oneline  # Find the commit before your cleanup
git reset --hard <commit-sha>

# OR: Discard entire cleanup branch
git checkout master
git branch -D cleanup/phase-0-foundation
```

---

## Merge to Master

Once all phases complete successfully:

```bash
# Review all changes
git log --oneline master..cleanup/phase-0-foundation

# Merge to master
git checkout master
git merge cleanup/phase-0-foundation

# Push to your remote
git push origin master

# Clean up branch (optional)
git branch -d cleanup/phase-0-foundation
```

---

## Summary

### What Was Removed:
- ✅ `.devcontainer/` - Devcontainer config
- ✅ `manifests/local/` - Local dev manifests
- ✅ `manifests/corporate/` - Arc scenario manifests
- ✅ `RedDog.Bootstrapper/` - Database init service
- ✅ `RedDog.CorporateTransferService/` - Arc hub service
- ✅ `.github/workflows/` - Old workflows (11 files)
- ✅ Flux v1 configs - Deprecated GitOps
- ✅ (Optional) `.vscode/` - VS Code configs

### What Remains (8 Services + UI):
1. RedDog.OrderService (.NET) - Will stay .NET
2. RedDog.MakeLineService (.NET) - Will migrate to Go
3. RedDog.LoyaltyService (.NET) - Will migrate to Node.js
4. RedDog.ReceiptGenerationService (.NET) - Will migrate to Python
5. RedDog.AccountingService (.NET) - Will stay .NET
6. RedDog.VirtualCustomers (.NET) - Will migrate to Python
7. RedDog.VirtualWorker (.NET) - Will migrate to Go
8. RedDog.AccountingModel (.NET) - Shared library
9. RedDog.UI (Vue 2) - Will upgrade to Vue 3

### Disk Space Saved:
Approximately 50-100 MB of unused code and configuration.

### Next Steps:
See `MODERNIZATION_PLAN.md` (in this same directory) for Phase 1 (.NET Modernization).

---

## Troubleshooting

### "Directory not empty" errors
```bash
# Add -f flag to force removal
rm -rf <directory>/
```

### "Permission denied" errors
```bash
# Check file permissions
ls -la <file>
# Add write permission if needed
chmod +w <file>
```

### Git shows unexpected changes
```bash
# Review what changed
git diff
# If changes look wrong, reset
git checkout -- <file>
```

### Solution file won't load
```bash
# Restore from git
git checkout HEAD -- RedDog.sln
# Then manually remove only the Bootstrapper lines
```

---

## Notes

- All cleanup is reversible via git
- Commit after each phase for safety
- Test can skip any phase if uncertain
- Keep `plan/` directory for reference (contains all planning docs)
- Session tracking in `.claude/sessions/` documents all decisions

**Ready to start? Begin with Pre-Flight Checklist above.**
