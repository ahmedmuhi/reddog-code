# Claude Code "Dev Container" Environment Setup

## Overview

This document explains how to create a specialized Claude Code environment called "dev container" that automatically installs all tools needed for Red Dog polyglot development.

## What Gets Installed

### Pre-installed in Universal Image
- ✅ Python 3.11+ (already available)
- ✅ Node.js 22+ LTS (already available)
- ✅ Go 1.24+ (already available)

### Installed via SessionStart Hook
- .NET 10 SDK
- Docker 27.4.1 (static binaries)
- kubectl (latest stable)
- kind v0.20.0
- Helm 3 (latest)
- Dapr CLI (latest)
- iptables (required by Docker)

## Environment Setup Instructions

### 1. Create a New Environment

1. In Claude Code on the web, select the current environment name
2. Click **"Add environment"**
3. Configure the environment:
   - **Name:** `dev-container` (or your preferred name)
   - **Network Access:** Full internet access
   - **Environment Variables:** (optional, add if needed)
     ```
     DOTNET_CLI_TELEMETRY_OPTOUT=1
     DAPR_CLI_TELEMETRY_ENABLED=false
     ```

### 2. How It Works

When you start a new session in the "dev-container" environment:

1. **Repository clones** to `/workspace` (or `$CLAUDE_PROJECT_DIR`)
2. **SessionStart hook runs** automatically (`.claude/settings.json`)
3. **Installation script executes** (`scripts/install-devcontainer-tools.sh`)
4. **Tools are installed** in ~2-3 minutes
5. **Environment is ready** for polyglot development

### 3. Verify Installation

After the session starts, you can verify all tools are installed:

```bash
# Check language runtimes
dotnet --version          # Should show 10.0.x
go version                # Should show go1.24.x
python3 --version         # Should show 3.11.x
node --version            # Should show v22.x

# Check Kubernetes tools
docker --version          # Should show 27.4.1
kubectl version --client  # Should show latest stable
kind version              # Should show v0.20.0
helm version              # Should show v3.x
dapr version              # Should show latest CLI version
```

### 4. What the SessionStart Hook Does

The hook is configured in `.claude/settings.json`:

```json
{
  "hooks": {
    "SessionStart": [
      {
        "matcher": "startup",
        "hooks": [
          {
            "type": "command",
            "command": "\"$CLAUDE_PROJECT_DIR\"/scripts/install-devcontainer-tools.sh"
          }
        ]
      }
    ]
  }
}
```

The installation script (`scripts/install-devcontainer-tools.sh`):
- Only runs in remote Claude Code environments (`CLAUDE_CODE_REMOTE=true`)
- Installs .NET 10 SDK to `/opt/dotnet`
- Downloads Docker static binaries to `/usr/local/bin`
- Installs kubectl, kind, Helm, and Dapr CLI
- Persists environment variables to `$CLAUDE_ENV_FILE`
- Takes ~2-3 minutes to complete

### 5. Benefits

✅ **Automatic setup** - No manual installation required
✅ **100% consistency** - Same tools every session
✅ **Polyglot ready** - All 5 languages available (.NET, Go, Python, Node.js, Vue.js)
✅ **Kubernetes ready** - kind, kubectl, Helm pre-installed
✅ **Dapr ready** - Dapr CLI available for microservices development

### 6. Comparison: Dev Container vs. SessionStart Hook

| Feature | Dev Container (.devcontainer/) | SessionStart Hook |
|---------|-------------------------------|-------------------|
| **Works in Claude Code?** | ❌ No (requires Docker) | ✅ Yes |
| **Works locally?** | ✅ Yes (VS Code + Docker) | ❌ No |
| **Setup time** | 3-5 minutes (first time) | 2-3 minutes (every session) |
| **Persistence** | Image cached | Installed each session |
| **Best for** | Local development | Claude Code on the web |

### 7. Recommended Usage

**For Local Development (your machine):**
- Use `.devcontainer/` with VS Code Dev Containers extension
- Requires Docker Desktop
- Fast startup after first build (image cached)

**For Claude Code on the Web:**
- Use "dev-container" environment with SessionStart hook
- No Docker required (tools installed directly)
- Automatic setup on each session start

### 8. Troubleshooting

**Installation takes too long:**
- Normal duration is 2-3 minutes
- Check network access is set to "Full internet"

**Tools not found after installation:**
- Run `source $CLAUDE_ENV_FILE` to reload environment variables
- Or start a new terminal session

**Docker daemon won't start:**
- This is expected - Docker daemon requires privileged mode
- Docker CLI is installed for building/inspecting images
- Use `docker` commands, not `dockerd` daemon

**Missing iptables errors:**
- The script installs iptables automatically
- If issues persist, manually run: `apt-get install -y iptables`

## Files Created

This setup creates/modifies the following files:

- `.claude/settings.json` - SessionStart hook configuration
- `scripts/install-devcontainer-tools.sh` - Tool installation script
- `docs/claude-code-devcontainer-environment.md` - This documentation

## Next Steps

1. Create the "dev-container" environment in Claude Code
2. Start a new session in that environment
3. Wait for SessionStart hook to complete (~2-3 minutes)
4. Verify tools with the commands shown in section 3
5. Start developing!

## References

- [Claude Code Environment Configuration](https://code.claude.com/docs/en/claude-code-on-the-web#environment-configuration)
- [SessionStart Hooks Documentation](https://code.claude.com/docs/en/claude-code-on-the-web#dependency-management)
- [Universal Image Pre-installed Tools](https://code.claude.com/docs/en/claude-code-on-the-web#available-pre-installed-tools)
