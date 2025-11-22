List recent development sessions for the user, not the entire history. This command is a quick “what have we been working on lately” view, not a full archive browser.

1. Check for the sessions directory

   - Check if `.claude/sessions/` exists.
   - If it does not exist or contains no `.md` session files, inform the user that there are no recorded sessions yet.
   - Do not create any files or directories as part of this command.

2. Collect and sort session files

   - List all `.md` files directly under `.claude/sessions/`.
   - Exclude hidden files and non-session control files such as `.current-session`.
   - Assume session filenames follow `YYYY-MM-DD-HHMM[-slug].md`. Sort them by date/time in descending order (most recent first). Lexicographic sort on the `YYYY-MM-DD-HHMM` prefix is sufficient.
   - Select only the most recent sessions to display, up to a maximum of 5. If there are fewer than 5, show all of them.

3. Identify the active session (if any)

   - If `.claude/sessions/.current-session` exists and is non-empty, read its contents to get the active session filename.
   - Compare this filename against the sessions you are listing and mark the matching one as the active session (for example with an “(active)” label).

4. Extract summary information for each listed session

   For each of the selected session files:

   - Read the first heading line as the session title. This might look like:
     - `# Development Session - 2025-11-01 08:38`, or
     - `# 2025-11-01 08:38 — Refactor auth: add Google + GitHub OAuth`
   - Extract the date/time from the title or from the filename prefix (`YYYY-MM-DD-HHMM`).
   - Locate the `Session Overview` section, if present, and take the first one to three short lines that describe the session (for example, start time, status, or one-sentence description).
   - Do not load or show the full `Progress` history for each session here; this command is a high-level index.

5. Present a concise, readable list

   - Present the selected sessions in a clean, readable format, ordered from most recent to older.
   - For each session, show:
     - The session title (and mark it as active if it matches `.current-session`).
     - The date/time.
     - One short overview snippet (a line or two) summarising what the session was about.
   - The entire list should be skimmable in a few seconds. Avoid dumping long paragraphs or full overviews.

Example shape of the response:

```text
Recent sessions:

1) 2025-11-01 08:38 — Modernize Red Dog demo (active)
   File: 2025-11-01-0838-modernize-red-dog-demo.md
   Overview: Modernizing Red Dog for Dapr/KEDA teaching; auditing outdated dependencies; focusing on AKS/K8s deployments.

2) 2025-10-28 14:10 — Dapr Redis → Cosmos migration spike
   File: 2025-10-28-1410-dapr-redis-to-cosmos-spike.md
   Overview: Investigated moving state store from Redis to Cosmos DB; captured trade-offs and preliminary config.

3) 2025-10-25 09:02 — CI flakiness investigation
   File: 2025-10-25-0902-ci-flakiness-investigation.md
   Overview: Traced intermittent test failures to race condition in order service and unstable Docker build cache.
````

If there are no sessions, respond with a short message such as:

```text
There are no recorded development sessions yet. You can start one with:
/project:session-start [short-description]
```