Show help for the session management system:

## Session Management Commands

The session system helps you document development work so future you (and other agents) can understand what happened, why, and how to continue.

### Available Commands:

- `/project:session-start [name]`  
  Start a new session with an optional name.

- `/project:session-update [notes]`  
  Append notes to the current session (progress, issues, decisions).

- `/project:session-end`  
  End the active session and write a comprehensive summary.

- `/project:session-list`  
  List all session files in `.claude/sessions/`.

- `/project:session-current`  
  Show the current session and its status.

- `/project:session-help`  
  Show this help.

### How It Works:

1. Sessions are markdown files stored in `.claude/sessions/`.
2. Filenames use the format `YYYY-MM-DD-HHMM-name.md` (or `YYYY-MM-DD-HHMM.md` if no name is provided).
3. Only one session can be active at a time; the active filename is tracked in `.claude/sessions/.current-session`.
4. Each session tracks progress, issues, decisions, solutions, and learnings.

### Best Practices:

- Start a session when you begin any significant piece of work.
- Use `/project:session-update` regularly to capture important changes, problems, and insights.
- End sessions with a thorough summary that future work can build on.
- Review relevant past sessions before starting similar or related work.

### Example Workflow:

```
/project:session-start refactor-auth
/project:session-update Added Google OAuth restriction
/project:session-update Fixed Next.js 15 params Promise issue  
/project:session-end
```
