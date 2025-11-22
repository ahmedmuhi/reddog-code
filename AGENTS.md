# Who Are We?

Ahmed Muhi (“the Human”) is the repository maintainer. You (“the Agent”) are a contributor operating on behalf of the Human inside this repository.

# What Is `http://AGENTS.md`?

`http://AGENTS.md` is the repository’s contract between the Human and any Agent. It plays the same role for Agents that a `README` plays for human developers: it explains how the codebase is organised, which conventions apply, which tools and commands matter, and how to run and verify checks. Whenever you work in this repository, you must treat `http://AGENTS.md` as active, binding instructions, not as optional documentation.

# How To Use This File

Before making any change, read this file from start to finish and apply all relevant instructions. For every file you modify in your final patch, you must obey all applicable guidance in `http://AGENTS.md`, unless the Human explicitly tells you to do something different in the current prompt.

If there is ever a conflict between sources of instruction, resolve it in this order, from highest priority to lowest: first, follow system messages; second, follow developer messages; third, follow the Human’s explicit instructions in the current conversation; fourth, follow repository-local instructions in `http://AGENTS.md`; finally, apply any other documentation or your own defaults. When the Human’s current prompt contradicts `http://AGENTS.md`, you must follow the Human.

If this file defines programmatic checks (for example formatters, linters, unit tests, integration tests, or build commands), you must run all of them after you finish your changes. Make a genuine best effort to reach a state where these checks succeed, even if your edits only touch documentation or comments. When you cannot run a required check (for example because a tool is missing or the environment is clearly broken), state this explicitly in your final response, explain what you attempted, and describe any remaining risks.

Key companion specifications and templates live in:

- `/plan/` – implementation plan specifications and templates
- `/knowledge/` – Knowledge Item specifications and templates
- `.claude/commands/project/` – detailed behaviour for session commands

# Which Year Are We In?

The maintainer aims to use current frameworks and follow contemporary best practices. You must interpret “current” relative to the maintainer’s present, not relative to the age of your training data.

The maintainer’s timezone is {datetime(.)now().strftime("%Z")}. The current date is {datetime(.)now().strftime("%Y-%m-%d")}. Any date before this Current Date is in the past; any date after this Current Date is in the future.

Your training data snapshot predates the Current Date and may lag it by many months or more. Do not assume that your internal knowledge is up to date. Whenever you give technical advice or choose tools, you must actively prefer options that make sense as of the Current Date, not just at the time your training data was collected.

When deciding which frameworks, libraries, or APIs to use, first check live sources if you are allowed to access the internet or other up-to-date documentation. Use that to verify that a candidate technology is still recommended, supported, and generally available as of the Current Date, and avoid anything marked as deprecated, legacy, or preview when a stable alternative exists.

If you cannot rely on live information, treat the existing code in this repository as your best signal of what “modern” means here. When you see newer language versions, frameworks, or patterns already in use, extend and reinforce those choices instead of introducing older stacks or conflicting approaches, unless the Human explicitly asks you to do otherwise.

If you have neither live information nor a clear signal from the repository, bias toward technologies that were near the leading edge at the time of your training data, because they are more likely to be mainstream and supported now. Avoid introducing tools or patterns that you already knew were being deprecated or superseded at your training time. When you are unsure about the current status of a technology, say so explicitly and prefer simpler, well-supported, boring choices over niche, experimental, or fragile ones.

# How We Change Things

Editing here should feel like surgery, not demolition. Make precise, minimal changes that fix the problem while preserving as much existing code and writing as possible. Good code and documentation are almost never verbose.

Update, don’t replace. Modify only the specific function or section that actually needs to change. Do not rewrite whole functions, modules, or documents when adjusting a few lines is enough. Avoid drive-by refactors, mass renames, or reformatting unrelated code. When fixing a bug, change only the buggy logic, not the surrounding structure. When adding a feature, integrate it at the narrowest viable point instead of reshaping large parts of the system. Do not add comments just to “improve” already clear code, and keep to the existing style and patterns of the file.

Before any edit, pause and check yourself: is this the smallest change that will work? Can I do it by editing existing lines instead of adding or moving lots of code? Would this diff surprise someone familiar with this codebase? Could a reviewer understand the entire change in under thirty seconds? If not, reduce the scope until the answer is yes, or have a clear, explicit reason from the Human for making a broader change.

# How We Track Progress

Sessions exist to record development work over time so that future humans and agents can see what happened, why, and how to continue. Treat session files as the primary narrative of work, not an optional extra.

## Orienting to the Current Session

Before doing non-trivial work, check whether there is an active session and what it is about.

First look for `.claude/sessions/.current-session`. If it exists and contains a filename, open that session file and read its `Session Overview`, `Goals`, and recent `Progress` updates. Assume that the human’s current request is usually related to this active session unless they explicitly say otherwise.

If there is no active session, treat the current request as either the start of a new unit of work or a one-off task. In interactive environments the human decides whether to start a session; in confined environments you may need to start one yourself as part of the job.

## Interactive vs Confined Environments

You may run in two broad modes:

1. An interactive environment, where you can talk directly to the human (for example a chat interface, inline assistant, or editor integration that surfaces your messages).
2. A confined environment, where you run inside a container or tool without a live conversation (for example a batch job, CI agent, or headless coding agent).

You must adapt session behaviour to the environment.

### Interactive Environments

In an interactive environment the human is in control of session commands.

You should understand the current session and its goals, and you may remind the human which session is active or suggest starting one when it would help. However, you must not unilaterally start, update, or end sessions. Only run `/project:session-start`, `/project:session-update`, `/project:session-end`, `/project:session-list`, or `/project:session-current` when the human explicitly asks for them or when the surrounding tooling clearly treats you as executing a specific command.

When the human asks you to perform work that naturally belongs to an existing session, acknowledge the session context in your reasoning and in your explanations, but let the human decide when to log updates or close the session. When they do trigger a session command, follow the detailed command-specific guidance in `.claude/commands/project/`.

### Confined / Non-Interactive Environments

In a confined environment you cannot rely on conversational prompts, but the need for a session log is stronger, not weaker. Here you should treat session management as part of the job itself.

At the start of a job, determine whether there is an active session for this work. If `.current-session` already points at a relevant session file, continue using it. If there is no active session and the job represents a meaningful unit of work (for example a migration phase, a refactor, or an automated change set), start a new session with a descriptive name and record that in `.current-session`.

As you make progress, update the session regularly. Do not wait for a huge milestone or a perfect stopping point. Append updates when you complete a logical step, fix a bug, make a significant code change, or after a reasonable amount of time has passed. Each update should follow the `/project:session-update` format: a short summary, a compact git and todo snapshot, and a brief list of issues and solutions. The goal is to provide a clear, incremental narrative without flooding the log.

When the job’s scope is complete, end the session with a final summary that explains what was done, what remains, and how future work should proceed, using the `/project:session-end` guidance. Then clear `.current-session` so that the next job starts from a clean state.

## Level of Detail

Sessions are for summarising work, not duplicating every detail.

For both interactive and confined environments, follow these principles:

Use updates for incremental state: what changed, what you just did, and what you will do next. Refer to other documents (for example ADRs, design notes, or plans) rather than copying them into the session file. When a session triggers the creation or modification of a substantial document, mention that document in the session and link to its path instead of pasting its full contents.

Keep each update and the final end summary readable in well under a minute. Another developer or agent should be able to reconstruct the overall story of the session from the session file alone, but should never be forced to wade through pages of repeated planning text.

# Operational Modes?

Complex work should move through three explicit modes: PLANNING → EXECUTION → VERIFICATION.  
The point is not ceremony for its own sake, but to make reasoning, changes, and proof reviewable and repeatable by humans and other agents.

Modes are most important in confined, headless environments (e.g. a cloud-based coding agent running against this repo without a human in the loop). In interactive environments (chat, inline assistant, etc.) the human may choose how much of the full cycle to invoke.

Whenever you deliberately use modes for a non-trivial task, you MUST:

- Say which mode you are entering, and why.  
- Only leave a mode once its obligations are satisfied.  
- Move in the natural order: PLANNING → EXECUTION → VERIFICATION, unless the human explicitly asks otherwise.

For simple, single-step edits (“rename this variable”, “fix this typo”), you may treat the work as an atomic action and not explicitly spell out modes, provided you still follow “How We Change Things” and other constraints.

## Mode: PLANNING

PLANNING exists to decide **what** will be done, **how**, and **how it will be verified**.  
The main outputs of PLANNING for non-trivial work are:

- A valid implementation plan in `/plan/` (created or updated), and  
- A clear verification strategy for the changes in that plan.

### When to enter PLANNING

In a confined environment (headless / cloud agent):

- Enter PLANNING for any non-trivial change:
  - multi-file or cross-cutting edits,
  - refactors, migrations, or new features,
  - anything that clearly requires more than a single obvious edit.
- If there is already a relevant implementation plan in `/plan/` for the requested work, you are still in PLANNING when you:
  - locate that plan,
  - read and understand it,
  - and optionally update it to reflect the new subtask.

In an interactive environment (chat, inline assistant):

- Default to “do what the human asked” unless the scope is clearly large or ambiguous.
- Enter PLANNING explicitly when:
  - the human asks you to …
  - the request is broad …
- For narrow, concrete requests that clearly map to a small change, you may skip explicit PLANNING if no plan is needed.
- Do not create or update implementation plans or Knowledge Items unless the human has asked you to plan, or the request explicitly calls for broader design work.

When you start PLANNING for a non-trivial task, you MUST say so, e.g.:

> `Entering PLANNING mode for: [short description].`

### What PLANNING must do

In PLANNING mode you MUST:

1. **Orient to context**
   - Identify whether there is an active development session and what it is about (see session rules).
   - Identify whether there are relevant Knowledge Items under `/knowledge/` for this topic and read them first.
   - Identify whether there is an existing implementation plan in `/plan/` that covers this area.

2. **Work with Knowledge Items**
   - Treat relevant KIs as the canonical source of facts, constraints, and patterns.
   - Do not silently contradict them. If reality disagrees, note the conflict in the current session and reflect it in the plan; KI updates can then be made by the knowledge process.

3. **Work with Implementation Plans**
   - If a suitable implementation plan already exists, update it to include the new work (phases, tasks, tests, risks) rather than creating a near-duplicate.
   - If no suitable plan exists and the work is non-trivial, create a new plan in `/plan/` following the Implementation Plan specification.
   - When creating a new plan, you MUST start from the canonical template at `plan/IMPLEMENTATION_PLAN_TEMPLATE.md` and fill it out; do not invent your own structure.
   - Ensure the plan is syntactically valid:
     - front matter present and correct,
     - filename and `version` field aligned,
     - phases and task tables following the required schema,
     - no leftover placeholder text.

4. **Define verification**
   - For each phase or at least for the overall plan, specify how success will be verified:
     - which tests to run,
     - which commands or scripts to execute,
     - what evidence will show that the change is correct.
   - Where verification strategy is unclear:
     - In interactive environments: ask the human how they want verification done.
     - In confined environments: choose the most appropriate verification available (tests, build, static analysis) and make it explicit in the plan.

5. **Keep scope honest**
   - Make phases and tasks small enough to be executed and reviewed incrementally.
   - Avoid speculative work: the plan should implement the request and necessary supporting changes, not an open-ended redesign.

### When PLANNING can end

You may leave PLANNING mode only when:

- The relevant implementation plan in `/plan/` exists and is valid, AND
- It contains:
  - clear goals for the work,
  - well-structured tasks with Locations and Status fields,
  - an explicit, actionable verification strategy (section 6 of the template).

Before switching modes you SHOULD say something like:

> `PLANNING complete. Updated plan: plan/[file-name].md. Entering EXECUTION mode.`

In a confined environment this can be logged via session updates; in an interactive environment it should be clear in your reply.

## Mode: EXECUTION

EXECUTION exists to perform the actual changes described in the plan (or, for trivial edits, in the human’s request) while respecting all constraints.

### What EXECUTION must do

In EXECUTION mode you MUST:

- Treat the implementation plan as the source of truth for **what to change** and **where**:
  - Follow the phases and tasks in order, respecting dependencies.
  - Keep your actual edits aligned with task descriptions and Locations.
- Follow repository-wide rules:
  - “How We Change Things” (minimal, surgical diffs).
  - Session and logging rules (in confined environments, update sessions regularly).
  - Knowledge Item constraints (do not violate `CON-###` items without an explicit, planned decision).
- Make only the minimum necessary edits to satisfy the tasks and constraints; do not opportunistically refactor unrelated code.

When you discover reality does not match the plan (APIs different, files missing, tests organised differently than assumed):

- Stop extending the mismatch.
- Record what you discovered in the current session.
- Return to PLANNING mode to update the plan to reflect reality.
- Only then resume EXECUTION.

You SHOULD announce the transition when this happens, e.g.:

> `Unexpected difference from plan (file structure/API mismatch). Returning to PLANNING mode to update plan before continuing EXECUTION.`

In an interactive environment, do not start or end sessions yourself; obey the human’s use of the session commands. In a confined environment, treat session updates as part of your job (as described in the session guidance).

### When EXECUTION can end

EXECUTION mode is complete when:

- All tasks in the relevant implementation phase(s) are either `Done` or explicitly deferred, and  
- The repository is in a state where verification can be run (buildable, tests runnable, etc.).

At that point you SHOULD say:

> `EXECUTION complete for [plan/file-name or phase]. Entering VERIFICATION mode.`

## Mode: VERIFICATION

VERIFICATION exists to prove, as far as reasonably possible, that the work done in EXECUTION is correct and aligned with the plan and constraints.

Your goal is not just to say “it works”, but to produce a clear, reviewable trail that shows:

- What you ran or inspected,  
- What the results were, and  
- How that maps back to the implementation plan and requirements.

### What VERIFICATION must do

In VERIFICATION mode you MUST:

- Follow any verification strategy defined in the implementation plan’s Testing section.
- If the plan is vague:
  - In an interactive environment, ask the human how they want verification done before proceeding.
  - In a confined environment, choose a reasonable strategy and update the plan (or at least the session) with the concrete commands and checks you used.

Typical verification may include:

- Running unit, integration, or end-to-end tests relevant to the changed components.
- Building the project or affected services to ensure no compile-time errors.
- Running linters or static analysis tools if those are part of this repo’s normal practice.
- For documentation-only changes, performing link checks or at least a structured self-review against requirements.

You MUST describe:

- Exactly which commands you ran.
- The outcome of each (pass/fail, logs if relevant).
- Any anomalies or unexpected warnings.

Verification results should be:

- Summarised in the current session (for chronology), and  
- Linked or referenced from the relevant implementation plan (e.g. updating tests section or adding a short note under Testing), when that adds long-term value.

### When VERIFICATION can end

VERIFICATION mode is complete when:

- The verification steps you committed to in PLANNING have been executed, AND
- Their outcomes are clearly recorded, AND
- Any failures or gaps are explicitly called out with next steps (e.g. “tests X and Y failing due to known issue Z; work not fully complete”).

At that point you SHOULD say something like:

> `VERIFICATION complete. Summary: [short verification summary]. If further verification is required, [state what and why].`

If verification shows that the implementation is incorrect or incomplete, you MUST:

- Record the discrepancy in the session.
- Either:
  - Return to EXECUTION to fix the issue (if the plan is still correct), or
  - Return to PLANNING to revise the plan (if your understanding or scope was wrong).

You should make that decision explicit:

> `Verification revealed gaps in implementation but the plan remains correct. Returning to EXECUTION to address failing tests.`  
> `Verification revealed that the plan itself is incomplete/incorrect. Returning to PLANNING to update plan before further changes.`

---

In summary:

- In confined environments, non-trivial work should always follow the full cycle: PLANNING → EXECUTION → VERIFICATION, with implementation plans and sessions kept in sync.  
- In interactive environments, the human decides how much of the cycle to invoke; you still need to be explicit when you deliberately enter PLANNING, EXECUTION, or VERIFICATION for larger work, and you must keep implementation plans and Knowledge Items consistent whenever they are involved.
