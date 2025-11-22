Update the current development session by appending a new update block to the active session file.

1. Determine the active session

   - Check if `.claude/sessions/.current-session` exists and read its contents to get the active session filename.
   - If `.current-session` does not exist or is empty, inform the user that there is no active session and ask them to start one with `/project:session-start`. Do not create or guess a session.

2. Append a new update block

   Open the active session file in `.claude/sessions/` and append a new update section.

   The update section must always include:

   - A heading with the current timestamp, for example:  
     `### Update — 2025-11-01 10:41`
   - A **Summary** line (one or two sentences) describing what changed in this step.  
     Use the command arguments (`$ARGUMENTS`) as raw notes to help you write this summary, but do not paste them verbatim.
   - A **Git Changes** subsection (if this is a git repository and git is available), including:
     - Files added/modified/deleted since the last meaningful change, derived from `git status --porcelain`.
     - Current branch and last commit hash (for example, from `git rev-parse --abbrev-ref HEAD` and `git rev-parse --short HEAD`).
     Keep this list focused on files relevant to the current step, not a full audit of the repo.
   - A **Todo Progress** subsection (if a todo list exists in the session or repo, such as markdown checkboxes or a task file), including:
     - Number of completed / in-progress / pending tasks.
     - A short list of any tasks newly completed in this update.
   - An **Issues** subsection listing any problems encountered since the previous update (or “None” if there were no issues).
   - A **Solutions** subsection summarising how those issues were addressed (or “None yet” if unresolved).
   - An optional **Details** subsection that briefly captures key technical notes or decisions that are too specific for the summary but still useful later. This should be short: a few bullet points or a short paragraph, not a full design document.

3. Handling command arguments (`$ARGUMENTS`)

   - When `$ARGUMENTS` are provided, treat them as rough notes. Rewrite and compress them into the **Summary** and **Details** sections in clear, concise language.
   - When no `$ARGUMENTS` are provided, infer the **Summary** and **Details** from the recent conversation, recent code edits, git status, and the session’s goals.

4. Keep updates concise

   Each update should be readable in under thirty seconds. Aim for:

   - One heading line,
   - A one–two sentence summary,
   - Short, focused lists in **Git Changes**, **Todo Progress**, **Issues**, **Solutions**, and
   - Only a handful of bullets or a short paragraph in **Details** when truly necessary.

   If you need to record a long investigation, design, or plan, prefer creating or updating a separate document (for example an ADR or design note) and then reference it briefly in the session update instead of inlining pages of text.

Example format:

```markdown
### Update — 2025-06-16 12:15 PM

**Summary**: Implemented user authentication and wired login flow end-to-end.

**Git Changes**:
- Modified: app/middleware.ts, lib/auth.ts
- Added: app/login/page.tsx
- Current branch: main (commit: abc123)

**Todo Progress**: 3 completed, 1 in progress, 2 pending
- ✓ Completed: Set up auth middleware
- ✓ Completed: Create login page
- ✓ Completed: Add logout functionality

**Issues**:
- Initial NextAuth callback misconfigured, causing 500 errors on login.

**Solutions**:
- Fixed callback URL configuration.
- Updated session handling to store user roles.

**Details**:
- Switched to JWT-based sessions to simplify scaling.
- Left password reset flow for a later session (tracked in todos).