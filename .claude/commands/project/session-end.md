End the current development session by writing a final **Session End Summary** into the active session file and clearing the current-session pointer.

1. Determine the active session

   - Check if `.claude/sessions/.current-session` exists and is non-empty.
   - If it does not exist or is empty, inform the user that there is no active session to end and stop. Do not create or modify any session files.

2. Load session context

   - Read the filename from `.claude/sessions/.current-session`.
   - Open the corresponding markdown file in `.claude/sessions/`.
   - Parse the existing `Session Overview`, `Goals`, and `Progress` / `Update` blocks to understand what happened during the session. Use this as input for the final summary instead of duplicating all details.

3. Append a “Session End Summary” block

   Append a new section at the end of the session file with a clear heading, for example:

   ```markdown
   ## Session End Summary — 2025-11-02 07:03
```

This section should be thorough but still summarised. Another developer (or AI) should be able to understand the overall work of the session without reading every update, but should not have to read pages of repeated content.

The summary must include, in concise form:

**a) Session metadata**

* Session file name.
* Start time and end time.
* Calculated duration (for example, “~22.5 hours over two days, with breaks”).
* Repository URL and branch name, if known or already present.
* Final session status (for example, `Completed`).

**b) Git summary (best effort)**

Use `git` if available to construct an aggregate view of changes made during the session.

* Total number of files changed (added / modified / deleted) during the session (best effort using `git diff` or `git status`).
* A concise list of changed files with their change type (for example “Added: …”, “Modified: …”, “Deleted: …”). Group where possible instead of listing dozens of files individually.
* Number of commits created during the session, and a short list of their SHAs and messages if this is not too long.
* Final git status (for example “Clean working directory” or “Uncommitted changes remaining in X, Y, Z”).

If you cannot reliably compute some of these (for example, exact line counts), omit that detail rather than guessing.

**c) Todo / goals summary**

Based on the `Goals` section and any todo tracking in the session:

* Total number of tasks/goals completed vs remaining.
* A concise list of completed tasks (one line each).
* A concise list of incomplete or deferred tasks with their status and, where helpful, a short note about why they were left incomplete or where to pick up next.

**d) Key accomplishments and features implemented**

* Summarise the main accomplishments of the session in a handful of bullet points or short paragraphs.
* Call out any features or capabilities that were actually implemented (for example “Implemented session tracking system”, “Created 8-phase modernization roadmap”, “Organised planning docs under `plan/`”).
* Prefer grouping related accomplishments rather than listing every micro-step.

**e) Problems encountered and solutions**

* Summarise the main problems or risks identified during the session.
* For each, briefly note the solution or mitigation (or mark it as unresolved if it carries over to future work).
* Avoid repeating long narratives already present in the session; compress to the essence.

**f) Breaking changes and important findings**

* Note any breaking changes made in this session (or explicitly state “None in this session” if planning-only).
* Highlight any important design or architectural decisions, constraints, or findings that future developers must know (for example, “MVP philosophy: deploy in <10 minutes; local dev not a focus”).
* Where detailed rationale already exists in other docs (for example `plan/MODERNIZATION_PLAN.md`), reference those files instead of pasting their content.

**g) Dependencies and configuration**

* Summarise any dependencies added or removed (high-level: “Added `.claude/` session system”, “No runtime dependencies changed”, etc.).
* Note any significant configuration changes (for example `.gitignore` updates, environment variables, manifest changes), again in summarised form with file paths.

**h) Deployment and operations**

* Summarise any deployment steps actually taken during the session (for example “No deployments yet; planning only”, or “Deployed to AKS with script X; validated scaling with KEDA”).
* Mention any manual steps or one-off procedures that will matter to future runs.

**i) Lessons learned**

* Capture the most important lessons or guidelines (for example “Planning before coding pays off”, “MVP scope is critical to avoid drift”, “Docs belong under `plan/` for clarity”).
* Focus on 3–7 key points rather than an exhaustive essay.

**j) What wasn’t completed**

* List the main items that were intentionally deferred, not started, or left incomplete.
* Briefly state why they were not completed (for example “out of scope for planning session”, “dependent on Phase 0 cleanup”, etc.).
* These should line up with the todo/goals summary above.

**k) Tips for future developers**

* Provide practical next steps (“Next session should…”) and safety tips (“Start with Phase 0 cleanup”, “Work on a branch”, etc.).
* Reference key files (`CLAUDE.md`, `plan/MODERNIZATION_PLAN.md`, `plan/SAFE_CLEANUP.md`, etc.) instead of re-describing their full contents.
* Aim for a short, actionable checklist rather than another full planning document.

Overall, the end summary should usually fit within one to a few screens of text. It must be more comprehensive than an individual update, but still a summary: avoid copying entire plans or phase descriptions that already live in separate documents.

4. Clear the current-session pointer

   * After successfully appending the Session End Summary, open `.claude/sessions/.current-session` and clear its contents so it becomes empty.
   * Do not delete the `.current-session` file itself; it is reused for the next session.

5. Inform the user

   * Confirm to the user that the session has been ended and documented.
   * Optionally, show a very compact recap (for example session name, duration, number of tasks completed) and remind them they can review the full summary in the session file or list sessions with `/project:session-list`.