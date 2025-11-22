Show the current development session status to the user.

1. Determine whether there is an active session

   - Check if `.claude/sessions/.current-session` exists and is non-empty.
   - If it does not exist or is empty, inform the user that there is no active session and suggest starting one with `/project:session-start [name]`. Do not create or modify any session files in this command.

2. Load the active session

   - Read the filename from `.claude/sessions/.current-session`.
   - Open the corresponding markdown file in `.claude/sessions/`.
   - Parse the session header and the `Session Overview`, `Goals`, and `Progress` sections, assuming the standard structure created by `/project:session-start` and `/project:session-update`.

3. Show core session metadata

   - Show the human-readable session name and the underlying filename.
   - Extract the **Start Time** from the `Session Overview` section.
   - If an **End Time** exists and the status is `Completed`, compute the duration as `End Time − Start Time`.
   - If there is no **End Time**, compute the duration as `Now − Start Time` based on the current time.
   - Display the status (for example, `Active`, `Completed`) if present in `Session Overview`.

4. Show current goals/tasks

   - Read the `Goals` section and present the current goals as a concise list.
   - If the session uses explicit task statuses (for example checkboxes or emoji like ✅ / 🔄 / ⏳), preserve or summarise those to show which goals are completed, in progress, or pending.

5. Show the last few updates

   - In the `Progress` section, locate the most recent `### Update` blocks.
   - Show only the last few (for example, the last 3) updates, not the entire history.
   - For each shown update, include:
     - The update timestamp from the heading.
     - The **Summary** line (and at most one or two short additional details if needed).
   - Do not dump full `Git Changes`, `Todo Progress`, or long `Details` sections here unless they are very short; this command is a high-level status view.

6. Remind the user of available session commands

   - At the end of the output, briefly remind the user they can:
     - Update the session with `/project:session-update [notes]`
     - End the session with `/project:session-end`
     - List sessions with `/project:session-list`
     - Show help with `/project:session-help`

7. Keep the output concise and informative

   - The entire status response should be readable in under thirty seconds.
   - Focus on:
     - What session is active,
     - How long it has been running,
     - What the current goals are, and
     - What happened in the most recent updates.
   - Avoid repeating the full content of the session file; this command is a dashboard-style snapshot, not a full log dump.

Example shape of the response:

```text
Current session: Refactor auth: add Google + GitHub OAuth
File: 2025-11-01-0838-refactor-auth-google-github.md
Status: Active
Started: 2025-11-01 08:38 (running for ~2h 05m)

Goals:
- ✅ Remove legacy cookie-based auth
- 🔄 Implement Google + GitHub OAuth
- ⏳ Add basic audit logging

Recent updates:
- 2025-11-01 10:09 — Initial project audit and modernization planning phase
- 2025-11-01 10:41 — Completed comprehensive project structure audit and clarified teaching vision

You can update this session with `/project:session-update [notes]`, end it with `/project:session-end`, list all sessions with `/project:session-list`, or see help with `/project:session-help`.