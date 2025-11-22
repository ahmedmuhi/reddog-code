Start a new development session by creating a session file in `.claude/sessions/` and recording it as the current active session.

Take the command arguments (`$ARGUMENTS`) as a short description of the unit of work (for example “refactor auth for Google + GitHub”, “fix checkout rounding bug”, “write Dapr identity ADR”), not as a generic label like “featureB” or “work”.

From `$ARGUMENTS` and the current context, derive:

- A human-readable session name (can be longer and descriptive).
- A filename-safe, kebab-case slug derived from that name (for example `refactor-auth-google-github`, `checkout-rounding-bug`, `dapr-identity-adr`).

Create a markdown file in `.claude/sessions/` using this format:

- `YYYY-MM-DD-HHMM-$SLUG.md` when you have a meaningful slug derived from `$ARGUMENTS` or context.
- `YYYY-MM-DD-HHMM.md` only as a last resort when no descriptive name can be inferred and the user has not provided one.

If no argument is provided, infer a descriptive name from the user’s most recent request (for example an issue title, implementation plan, or branch name). If you still cannot infer a clear name, ask the user for a short descriptive phrase and then normalize it as above.

The session file should begin with:

1. A title line that includes the human-readable session name and timestamp, for example:  
   `# 2025-11-22 14:30 — Refactor auth: add Google + GitHub OAuth`
2. A **Session overview** section with the start time and a brief description of what this session will work on.
3. A **Goals** section listing the concrete goals of this session. If the goals are not clear from context, ask the user to specify them.
4. An empty **Progress** section ready for updates.

After creating the file, create or update `.claude/sessions/.current-session` so that it contains exactly the active session filename.

Confirm that the session has started and remind the user that they can:

- Update it with `/project:session-update`
- End it with `/project:session-end`