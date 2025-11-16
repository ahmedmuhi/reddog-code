# CLAUDE.md

**Purpose:** This file is a lightweight operating manual for autonomous agents in this repo:  
how to think, where to look, and how to log work. It is **not** a status dashboard or full architecture doc.

---

## 1. Communication & Time (“When Are We?”)

### Voice Input

- The user usually speaks and uses **voice transcription**.
- If wording looks odd or ambiguous, **ask for clarification** instead of guessing.

### Time & “Latest” Guidance

- Treat the machine’s **current date/time** (e.g. from `date` in the shell) as “now”.
- When the user asks for *“latest”, “today”, “current”, “in 2025”* etc:
  1. Prefer **what the repo actually says** (code, docs, session logs).
  2. If you still need context, you may use external web search (if allowed in your environment).
- Do **not** trust hard-coded dates in this file as ground truth.  
  Always recompute “now” and cross-check with session logs.

---

## 2. Surgical Edit Principle ("How We Change Things")

### Core Philosophy
**Make the smallest possible change that achieves the goal.** Think of editing like surgery: precise incisions, minimal disruption, preserve healthy tissue.

### For Code
- **Never rewrite entire functions** when changing 2-3 lines would suffice
- **Preserve existing style** - match indentation, naming conventions, comment format
- **Keep git diffs clean** - changes should be reviewable at a glance
- When fixing a bug: change only the buggy logic, not the surrounding code
- When adding features: insert at the narrowest integration point possible

### For Documentation
- **Update, don't replace** - modify the specific outdated sentences/sections
- **Maintain document voice** - match the existing tone and style
- **Preserve structure** - don't reorganize unless explicitly asked
- Add new sections at logical points, don't restructure the whole doc
- Keep existing examples; add new ones only when needed

### For Comments
- **Fix factually wrong comments** but keep accurate ones untouched
- **Match comment style** - if they use `//`, don't switch to `/* */`
- Don't add comments to "improve" already clear code
- When adding comments for new code, follow the file's existing density
  (sparse comments = stay sparse, verbose = match that level)

### Decision Framework
Before any edit, ask:
1. What's the **minimum change** needed?
2. Can I achieve this by modifying **existing lines** rather than adding new ones?
3. Will my change **surprise** someone familiar with this codebase?
4. Is my diff **reviewable in under 30 seconds**?

If any answer is problematic, reconsider the approach.

### Examples

❌ **BAD:** Rewriting a 50-line function to fix one off-by-one error  
✅ **GOOD:** Changing `i <= length` to `i < length` on line 23

❌ **BAD:** Reformatting an entire JSON config to add one key  
✅ **GOOD:** Adding the single key-value pair where it belongs

❌ **BAD:** Converting all README headers from `##` to `===` style while fixing a typo  
✅ **GOOD:** Fixing just the typo, leaving formatting alone

---

## 3. How to Catch Up on Project State

Always start by understanding **what is currently being worked on**.

### Quick Catch-Up Procedure

1. **Find the active session**
   - Read `.claude/sessions/.current-session`.
   - If it contains a filename (e.g. `2025-11-15-1345-ui-vue3-upgrade.md`), open that file in `.claude/sessions/`.
   - If it is missing or empty, assume **no active session**.

2. **Read the current session file**
   - Skim from top to bottom:
     - Goals / Session name
     - Progress log entries
     - Current issues / decisions
     - “Next steps” / TODO items

3. **Optionally read the previous session**
   - List files in `.claude/sessions/` and open the most recent older one.
   - Use it to understand **continuity**: what changed between sessions.

4. **Only then consult high-level docs**
   - `README.md` – overall project context
   - `plan/modernization-strategy.md` – multi-phase modernization roadmap
   - `plan/upgrade-ui-vue3-implementation-1.md` – current UI upgrade plan (if you’re touching the UI)
   - `plan/testing-validation-strategy.md` – how tests/validation are supposed to work

> **Rule of thumb:**  
> For “where are we now?” → **trust session logs first**, then plans/docs.

---

## 4. Documentation Map (Navigation Only)

Use this section to find the right **entry point**. Do not duplicate content from these docs here.

- **Project overview & getting started**
  - `README.md`

- **Modernization & phases**
  - `plan/modernization-strategy.md`

- **Testing & validation**
  - `plan/testing-validation-strategy.md`

- **Architecture & decisions**
  - `docs/adr/README.md` (index + navigation hub for all ADRs)

- **API standards**
  - `docs/standards/web-api-standards.md` (HTTP API conventions, health, errors, etc.)

> If you need detailed rationale or exact rules, **open the doc** instead of relying on summaries in AGENTS.md.

---

## 5. Development Sessions

This repo uses **session logs** to document development work. Agents are expected to work *inside* this system.

### Concept

- Session files live in: `.claude/sessions/`
- Naming: `YYYY-MM-DD-HHMM[-name].md`  
  Example: `2025-11-15-1345-ui-vue3-upgrade.md`
- Active session filename is stored in: `.claude/sessions/.current-session`
- Each session tracks:
  - Goals and scope
  - Timestamped updates & progress
  - Important decisions and issues
  - Next steps / TODOs

### How Agents Should Use Sessions

- **Starting serious work?**
  - If no session is active → **start one**.
  - If a session is active → **append updates** instead of creating a new one.

- **Each update should briefly cover:**
  - What changed (files, commands, or key actions)
  - Why it matters (impact, risk, context)
  - What’s next (TODO items, open questions)

- **Ending a session:**
  - Summarize:
    - What was done
    - What remains
    - Key decisions and any risks
  - Make it readable so a future developer (or agent) can jump in without rereading the entire log.

### Command Quick Reference

Session behavior is defined in `.claude/commands/project/*.md` and mirrored as Codex prompts in `~/.codex/prompts/`.

Typical commands (names may vary slightly):

- `session-start [name]` – start a new session file.
- `session-update [notes]` – append an update (with git status).
- `session-current` – show active session and last updates.
- `session-list` – list past sessions.
- `session-end` – finalize the current session with a summary.

> When in doubt: **check `.claude/commands/project/*.md`** to see exactly how each command expects to behave.

---

## 6. Minimal Dev Quickstart for Agents

These are **starting points**, not exhaustive recipes.  
For anything more complex, inspect the relevant scripts and workflows.

### .NET Services

Basic pattern:

```bash
# From repo root
dotnet restore RedDog.sln
dotnet build RedDog.sln -c Release
````

For per-service behavior, see:

* `.github/workflows/package-*.yaml`
* Individual `RedDog.*/*Service.csproj` files

### UI (Vue 3 + Vite)

Current expected workflow:

```bash
cd RedDog.UI
npm install
npm run dev     # Start Vite dev server
# npm run build # Production build (used by CI)
# npm run lint  # Linting, if configured
```

> **Important:** Scripts may change over time.
> Always check `RedDog.UI/package.json` to see the authoritative `scripts` block.

### Local Cluster / Infra (Pointer Only)

* Full local cluster & infrastructure setup is driven by scripts under `scripts/` and Helm charts under `charts/` + `values/`.
* Typical entry point: `./scripts/setup-local-dev.sh`
  (See comments in that script and `plan/modernization-strategy.md` for details.)

---

## 7. Repo-Specific Conventions

A few rules that matter a lot for correctness and security.

### Secrets & Config (ADR-0013)

* **Never** commit real secrets.
* Follow `docs/adr/adr-0013-secret-management-strategy.md`:

  * Applications read credentials from **Kubernetes Secrets** (or equivalent external providers).
  * `values/values-local.yaml` can contain **throwaway local values** only.
  * `.env/local` is for local scripts/processes and stays untracked.

### Container Registry & Images

* CI builds and pushes images to **GitHub Container Registry** under:
  `ghcr.io/ahmedmuhi/...`
* Kubernetes clusters pull from GHCR using an `imagePullSecret` (e.g. `ghcr-cred`):

  * Created via `kubectl create secret docker-registry` or `scripts/refresh-ghcr-secret.sh`.
  * Referenced in Helm values under `services.common.image.pullSecrets`.

### Dapr & Multi-Cloud Intent

* This is a **Dapr-based microservices** app:

  * Service invocation, pub/sub, and state go through Dapr components.
* When designing new behavior:

  * Prefer Dapr components & APIs over hard-coding cloud-specific endpoints.
  * Check ADRs in `docs/adr/` for decisions around Dapr, configuration, and multi-cloud deployment.

---

**TL;DR for Agents**

1. **Read the current session** (and maybe the previous one) before doing anything serious.
2. **Log your work** via the session commands.
3. **Use docs for details**, not AGENTS.md.
4. **Keep guidance modern**, secrets out of code, and behavior aligned with Dapr + GHCR + multi-cloud goals.
