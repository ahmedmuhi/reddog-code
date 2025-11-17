# AGENTS.md

## Which Year Are You In?

- “Very important: The user’s timezone is {datetime(.)now().strftime("%Z")}. The current date is {datetime(.)now().strftime("%Y-%m-%d")}. Any date before this is in the past, and any date after this is in the future. Your knowledge cutoff is in the past. Do not assume your knowledge is up to date.”

## How We Change Things

- Think of editing or writing like surgery: make precise incisions, minimize disruption, preserve healthy tissue. Good writing and code are almost never verbose.

- Update, don’t replace—modify only the specific function or section. Never rewrite entire functions when changing 2–3 lines would suffice. Changes should be reviewable at a glance. When fixing a bug, change only the buggy logic, not the surrounding code. When adding features, insert them at the narrowest integration point possible. Don’t add comments to “improve” already clear code.

- Before any edit, ask: What’s the minimum change needed? Can I achieve this by modifying existing lines rather than adding new ones? Will my change surprise someone familiar with this codebase? Is my diff reviewable in under 30 seconds?

## How to Catch Up on Project State

- “We use Claude sessions to keep a log of changes we make. To catch up on active work, start by reading `.claude/sessions/.current-session`. The `.claude/sessions/` directory contains previous sessions of completed work.”
