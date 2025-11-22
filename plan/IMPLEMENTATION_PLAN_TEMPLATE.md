# Implementation Plan Specification

## 1. Primary Directive

You are an AI agent tasked with creating or updating an implementation plan file `${file}` based on new or updated requirements.

Your output must be:

- Machine-readable and deterministic
- Structured for autonomous execution by other AI systems or humans
- Strictly compliant with the template and rules in this document

## 2. Execution Context

This specification is designed for AI-to-AI communication and automated processing.

All instructions must be interpreted literally and executed systematically without human interpretation or clarification. Any ambiguity must be resolved by making the plan more explicit, not by assuming an unstated convention.

## 3. Core Requirements

Implementation plans MUST:

- Be executable by AI agents or humans without additional clarification
- Use precise, deterministic language that minimises interpretation
- Be self-contained: all necessary context to execute the plan must be present in the plan file itself (or via explicit references to other files)
- Be structured so that other agents can parse and transform them reliably

## 4. Plan Structure Requirements

Plans consist of discrete, atomic phases containing executable tasks.

- Each phase MUST have a clearly stated goal.
- Each phase MUST have measurable completion criteria.
- Tasks within a phase SHOULD be executable in parallel unless explicit dependencies are specified.
- Cross-phase dependencies MUST be explicitly declared (via task dependencies or text).
- Tasks MUST NOT depend on implicit human judgment. Any decision point MUST be encoded as explicit criteria or a finite set of labelled options.

## 5. Identifier & Naming Standards

### 5.1 Identifier prefixes

Use the following prefixes for identifiers:

- Requirements: `REQ-###`, `SEC-###`, `CON-###`, `GUD-###`, `PAT-###`
- Alternatives: `ALT-###`
- Dependencies: `DEP-###`
- Files: `FILE-###`
- Tests: `TEST-###`
- Risks: `RISK-###`
- Assumptions: `ASSUMPTION-###`
- Goals: `GOAL-###`
- Tasks: `TASK-###`

Rules:

- IDs MUST be unique within a single plan file.
- IDs MUST NOT be reused for different concepts within the same plan file.
- New items MUST get new IDs; do not renumber existing IDs when updating a plan.
- Numbering SHOULD be sequential (001, 002, …) where practical, but gaps are allowed if items are removed.

### 5.2 File naming

Implementation plan files MUST:

- Live in the `/plan/` directory at the repository root.
- Follow the naming convention:  
  `[purpose]-[component]-[version].md`

Where:

- `purpose` MUST be one of:  
  `upgrade | refactor | feature | data | infrastructure | process | architecture | design`
- `component` MUST be lowercase kebab-case:  
  `[a-z0-9-]+` (letters, digits, hyphens only, no spaces).
- `version` MUST be a positive integer starting from `1` (e.g. `1`, `2`, `3`).

Examples:

- `upgrade-system-command-4.md`
- `feature-auth-module-1.md`
- `architecture-red-dog-polyglot-2.md`

The `version` value in the filename and the `version` field in front matter MUST match. If they differ, the plan is invalid.

## 6. AI-Optimized Implementation Standards

Implementation plans MUST:

- Use explicit, concrete language and minimise the need for human interpretation.
- Encode decision points explicitly (for example: “IF `X` THEN do `A`, ELSE do `B`.”).
- Provide stable code anchors using file paths and symbols, not only line numbers.

Location rules:

- For code locations, prefer:  
  `relative/path/to/file.ext#SymbolName`  
  (e.g. `/src/orders/OrderService.cs#CreateOrder`).
- Line numbers MAY be added as hints (e.g. `#CreateOrder@L120`) but MUST NOT be the only locator and MUST NOT be treated as canonical.

Data structure rules:

- Use tables and lists as the primary machine-parseable structures.
- Use `YYYY-MM-DD` for all dates.
- Use explicit status values from controlled vocabularies (see below).
- Do not leave placeholder text in any final implementation plan.

## 7. Status & Badge Rules

### 7.1 Plan status

The plan `status` field in front matter MUST be exactly one of:

- `Completed`
- `In progress`
- `Planned`
- `Deprecated`
- `On Hold`

### 7.2 Status badge

The plan MUST display a Shields static badge in the Introduction section.

- Badge URL format:  
  `https://img.shields.io/badge/status-<encoded_status>-<color>?style=flat`
- `<encoded_status>` is the `status` value with spaces encoded as `%20`.
- `<color>` is mapped from `status` as follows:

  - `Completed`   → `brightgreen`
  - `In progress` → `yellow`
  - `Planned`     → `blue`
  - `Deprecated`  → `red`
  - `On Hold`     → `orange`

Example:

```md
![Status: In progress](https://img.shields.io/badge/status-In%20progress-yellow)
```

The badge text (`Status: In progress`) MUST match the `status` field exactly.

## 8. Task & Phase Structure

### 8.1 Task table schema

Each Implementation Phase MUST contain a task table with the following columns:

```md
| Task ID  | Description                       | Location                                 | DependsOn              | Status      | Date       |
|----------|-----------------------------------|------------------------------------------|------------------------|-------------|------------|
| TASK-001 | Short, concrete task description  | /path/file.cs#SymbolName                 |                        | NotStarted  |            |
| TASK-002 | ...                               | /path/other.ts#ComponentName             | TASK-001               | InProgress  | 2025-04-26 |
| TASK-003 | ...                               | /plan/MODERNIZATION_PLAN.md#Phase-1      | TASK-001,TASK-002      | Done        | 2025-04-27 |
```

Rules:

* `Task ID` MUST be a `TASK-###` identifier as defined above.
* `Description` MUST be concise but specific enough that an agent can execute it without additional interpretation.
* `Location` MUST be either:

  * A code location: `relative/path#SymbolName` (optional `@L###` for line), or
  * A document location: `relative/path#Section-Anchor`, or
  * Empty, if the task is conceptual or spans multiple locations (in which case the description MUST clarify the scope).
* `DependsOn` MUST be a comma-separated list of `TASK-###` IDs, or empty for no dependencies.
* `Status` MUST be one of:

  * `NotStarted`
  * `InProgress`
  * `Blocked`
  * `Done`
* `Date` MUST be ISO `YYYY-MM-DD` or empty.

Tasks within a phase MAY be executed in parallel when `DependsOn` is empty or already satisfied. Phases themselves MAY also run in parallel if there are no cross-phase dependencies stated in the plan.

### 8.2 Phase goals and completion

Each phase MUST:

* Declare at least one `GOAL-###` item that describes the phase outcome.
* Have implicit or explicit completion criteria (e.g. “All tasks in this phase are `Done` and tests TEST-00x are passing.”).

## 9. Output File Specifications

* Implementation plan files MUST be saved in `/plan/` as described in section 5.2.
* The file MUST be valid Markdown with YAML front matter at the top.
* The file MUST strictly follow the Template Structure below.

## 10. Template Validation Rules

Plans MUST satisfy all of:

* All front matter fields listed in the template MUST be present.
* Section headers MUST match exactly (case-sensitive).
* All identifiers MUST follow the prefix and uniqueness rules in section 5.
* All task tables MUST use the exact column names and order specified in section 8.1.
* All dates MUST be `YYYY-MM-DD` or empty.
* No placeholder text (e.g. “Requirement 1”, “Description of task 1”) MAY remain in a finalized plan.
* Filename and front-matter `version` MUST match.

Any violation means the plan is invalid and MUST be corrected before execution.

---

## 11. Mandatory Implementation Plan Template

The following template is normative. Replace all bracketed text with concrete content and remove examples.

```md
---
goal: [Concise title describing the implementation plan's goal]
version: [Integer, e.g., 1]
date_created: [YYYY-MM-DD]
last_updated: [YYYY-MM-DD]
owner: [Team/Individual responsible for this plan]
status: 'Completed'|'In progress'|'Planned'|'Deprecated'|'On Hold'
tags: [Optional: list of tags, e.g. feature, upgrade, architecture, migration]
---

# Introduction

![Status: <status>](https://img.shields.io/badge/status-<encoded_status>-<color>)

[Short, concise introduction to the plan and the goal it is intended to achieve. Reference relevant knowledge items or specs if needed.]

## 1. Requirements & Constraints

[Explicitly list all requirements & constraints affecting this plan. Derive these from specs, knowledge items, ADRs, etc.]

- **REQ-001**: [Functional requirement]
- **REQ-002**: [...]
- **SEC-001**: [Security requirement]
- **CON-001**: [Constraint]
- **GUD-001**: [Guideline]
- **PAT-001**: [Pattern to follow]

(You MAY also use a table if that is clearer, but IDs MUST be preserved.)

## 2. Implementation Steps

### Implementation Phase 1

- GOAL-001: [Describe the goal of this phase, e.g., "Modernise OrderService to .NET 9"]

| Task ID  | Description                              | Location                                   | DependsOn     | Status      | Date       |
|----------|------------------------------------------|--------------------------------------------|--------------|-------------|------------|
| TASK-001 | [Concrete step 1]                        | /src/orders/OrderService.cs#CreateOrder    |              | NotStarted  |            |
| TASK-002 | [Concrete step 2]                        | /src/orders/OrderService.cs#UpdateOrder    | TASK-001     | NotStarted  |            |
| TASK-003 | [Concrete step 3]                        | /tests/orders/OrderServiceTests.cs#...     | TASK-001     | NotStarted  |            |

[Add more tasks/rows as needed.]

### Implementation Phase 2

- GOAL-002: [Describe the goal of this phase, e.g., "Update GitHub Actions workflows for new build pipeline"]

| Task ID  | Description                              | Location                                      | DependsOn     | Status      | Date       |
|----------|------------------------------------------|-----------------------------------------------|--------------|-------------|------------|
| TASK-004 | [Concrete step 4]                        | .github/workflows/build-orderservice.yml      | TASK-001     | NotStarted  |            |
| TASK-005 | [Concrete step 5]                        | .github/workflows/build-ui.yml                | TASK-004     | NotStarted  |            |
| TASK-006 | [Concrete step 6]                        | /plan/upgrade-system-command-4.md#Testing     |              | NotStarted  |            |

[Add additional phases as required.]

## 3. Alternatives

[List alternative approaches considered and why they were not chosen.]

- **ALT-001**: [Alternative approach and reason rejected]
- **ALT-002**: [...]

## 4. Dependencies

[List dependencies such as libraries, frameworks, services, or external systems.]

- **DEP-001**: [Dependency 1 and version]
- **DEP-002**: [...]

## 5. Files

[List the files significantly affected by this plan.]

- **FILE-001**: `/src/orders/OrderService.cs` — [Short description]
- **FILE-002**: `/src/ui/App.vue` — [Short description]

## 6. Testing

[List tests that MUST exist or be created, and how they map to tasks.]

- **TEST-001**: [Description, e.g. "Unit tests for TASK-001 and TASK-002"]
- **TEST-002**: [Description, e.g. "Integration tests for updated workflow"]

## 7. Risks & Assumptions

[List risks and assumptions related to the implementation.]

- **RISK-001**: [Risk description and impact]
- **RISK-002**: [...]
- **ASSUMPTION-001**: [Assumption that must hold true]
- **ASSUMPTION-002**: [...]

## 8. Related Specifications / Further Reading

[Links to related specifications, ADRs, knowledge items, or external documentation.]

- [Link to related spec 1]
- [Link to relevant external documentation]
- [Link to knowledge item(s) if applicable]