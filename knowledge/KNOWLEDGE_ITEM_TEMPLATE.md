# Knowledge Item Specification

## 1. Purpose

Knowledge Items (KIs) capture stable, reusable knowledge that should survive across many sessions and implementation plans.

A KI answers:

> “What is the canonical, long-lived knowledge about topic X that future humans and agents should reuse instead of rediscovering it?”

KIs are not chronological logs (sessions) and not step-by-step change plans (implementation plans). They are curated summaries of facts, constraints, patterns, and lessons.

## 2. Storage & Naming

- All Knowledge Items MUST live in the `/knowledge/` directory at the repository root.
- Each KI MUST be a single Markdown file with YAML front matter.
- KI filenames MUST be lowercase kebab-case and descriptive of the topic.

Recommended naming convention:

- `knowledge/<topic-slug>-ki.md`

Examples:

- `knowledge/red-dog-architecture-ki.md`
- `knowledge/red-dog-dapr-patterns-ki.md`
- `knowledge/session-system-design-ki.md`
- `knowledge/verification-strategies-ki.md`

Filenames MUST be unique.

## 3. When to Create or Update a KI

Create or update a Knowledge Item when:

- A design, constraint, or pattern applies to **multiple** future tasks or components.
- A session or implementation plan produces insights that are likely to remain valid over time.
- There is recurring confusion or repeated research on the same topic.

Do NOT create KIs for:

- One-off decisions that only affect a single small change.
- Raw brainstorming or incomplete thoughts.
- Information already fully covered in another KI (update the existing KI instead).

## 4. Identifier & Tagging Standards

### 4.1 KI identity

Each KI MUST have an `id` field in front matter:

- `id` MUST start with `KI-`.
- `id` MUST be unique across all KIs in this repository.

Recommended pattern (not enforced but preferred):

- `KI-<UPPER_SNAKE_TOPIC>-NNN`

Examples:

- `KI-RED_DOG_ARCHITECTURE-001`
- `KI-SESSION_SYSTEM_DESIGN-001`

### 4.2 Internal identifiers

Inside a KI, you MAY use labelled identifiers to structure knowledge:

- Facts: `FACT-###`
- Constraints: `CON-###`
- Patterns / Recommendations: `PAT-###`
- Risks: `RISK-###`
- Open Questions: `OPEN-###`

Rules:

- IDs MUST be unique within a single KI file.
- IDs MUST NOT be reused for different concepts inside the same KI.
- Numbering SHOULD be sequential where practical (001, 002, …).

Example:

- `FACT-001`: “Red Dog modernization targets polyglot services (.NET, Go, Python, Node).”
- `CON-001`: “No local dev environment; cloud deployments only.”
- `PAT-001`: “Use Redis 6.x for Dapr state store unless overridden by explicit requirement.”

### 4.3 Tags

- Each KI MUST have a `tags` list in front matter.
- Tags MUST be lowercase kebab-case.
- Tags SHOULD include domain, component, and theme (e.g. `red-dog`, `architecture`, `dapr`, `sessions`, `verification`).

## 5. Structure & Sections

Each Knowledge Item MUST follow this structure:

1. YAML front matter
2. `# Summary`
3. `## Key Facts`
4. `## Constraints`
5. `## Patterns & Recommendations`
6. `## Risks & Open Questions`
7. `## Source & Provenance`

### 5.1 Front matter

Required fields:

- `id` — globally unique KI identifier starting with `KI-`.
- `title` — short human-readable title.
- `tags` — list of lowercase tags.
- `last_updated` — `YYYY-MM-DD`.
- `source_sessions` — list of session filenames this KI is derived from (may be empty).
- `source_plans` — list of implementation plan filenames related to this KI (may be empty).
- `confidence` — one of: `low`, `medium`, `high`.
- `status` — one of: `Active`, `Deprecated`.

Optional:

- `owner` — team/individual primarily responsible for maintaining this KI.
- `notes` — short free-form notes for maintainers.

### 5.2 Summary

- A short paragraph describing what this KI covers and when it applies.
- Should fit in 2–4 sentences.

### 5.3 Key Facts

- Bullet list of concrete, stable facts about the topic.
- Each important fact SHOULD be labelled with a `FACT-###` identifier.
- Facts MUST be phrased so they can be reused directly in planning or execution (no vague wording).

### 5.4 Constraints

- Bullet list of constraints that future work MUST respect (technical, business, process).
- Each constraint SHOULD be labelled with a `CON-###` identifier.
- Constraints SHOULD be as explicit and testable as possible.

### 5.5 Patterns & Recommendations

- Bullet list of patterns, best practices, or recommended approaches for this topic.
- Each item SHOULD be labelled with a `PAT-###` identifier.
- Patterns SHOULD be worded to be directly actionable (e.g. “For new Red Dog services, use pattern X unless Y.”).

### 5.6 Risks & Open Questions

- `Risks`: items labelled `RISK-###` describing known pitfalls or possible failure modes.
- `Open Questions`: items labelled `OPEN-###` where knowledge is incomplete, or further validation is needed.

### 5.7 Source & Provenance

- Short section pointing to the origin of this knowledge:
  - session filenames,
  - implementation plan filenames,
  - ADRs / external docs.

This section connects the KI back to the chronological record and deeper context.

## 6. Content Constraints

Knowledge Items MUST:

- Focus on stable knowledge (facts, constraints, patterns) rather than step-by-step procedures.
- Avoid duplicating entire implementation plans or ADRs; instead, summarise and link out.
- Use explicit language; avoid ambiguous terms like “soon”, “later”, “maybe”.
- Use `YYYY-MM-DD` for all dates.
- Be kept up to date: when facts or constraints change, the KI MUST be updated, not silently ignored.

Knowledge Items MUST NOT:

- Contain large copies of code; refer to files and symbols instead.
- Include session-level detail such as “I tried X and it failed” unless it leads to a stable lesson.
- Depend on unstated external context (e.g. “as we discussed last week”) — everything needed should be in the KI or linked.

## 7. AI Usage Rules (High-Level)

The detailed wiring to Operational Modes lives in `CLAUDE.md`, but the following expectations apply:

- **PLANNING mode**  
  - Before writing or updating an implementation plan for a non-trivial change, agents SHOULD:
    - Search `/knowledge/` for relevant KIs (by tags/title/topic),
    - Read them fully,
    - Treat their facts, constraints, and patterns as binding unless overridden by explicit new requirements or maintainer instructions.

- **EXECUTION mode**  
  - Agents SHOULD follow applicable KIs when implementing changes.
  - When execution reveals that KI content is out of date or incorrect, this MUST be recorded in the current session and surfaced for KI update.

- **VERIFICATION mode**  
  - Agents SHOULD consult relevant KIs for verification and testing patterns if they exist (e.g. “how this repo usually verifies migrations”).

Authoring and updating KIs themselves (promoting session insights into KIs or editing KIs) MAY be delegated to a dedicated knowledge-maintenance process or subagent. Implementation agents SHOULD NOT silently contradict KIs; they MUST either conform or explicitly flag the conflict.

---

## 8. Mandatory Knowledge Item Template

The following template is normative. Replace all bracketed text with concrete content and remove placeholder examples.

```md
---
id: KI-[UNIQUE_ID_HERE]
title: [Short human-readable title for this knowledge item]
tags:
  - [tag-1]
  - [tag-2]
last_updated: [YYYY-MM-DD]
source_sessions:
  - [optional: .claude/sessions/2025-11-01-0838-modernize-red-dog-demo.md]
source_plans:
  - [optional: plan/upgrade-system-command-4.md]
confidence: low|medium|high
status: Active|Deprecated
owner: [Optional: team/individual]
notes: [Optional: short maintainer note]
---

# Summary

[2–4 sentences summarising what this KI covers, when it applies, and why it matters.]

## Key Facts

- **FACT-001**: [Concrete fact 1]
- **FACT-002**: [Concrete fact 2]
- **FACT-003**: [Concrete fact 3]

[Add more FACT entries as needed.]

## Constraints

- **CON-001**: [Constraint that must be respected]
- **CON-002**: [Constraint]
- **CON-003**: [Constraint]

[Add more CON entries as needed.]

## Patterns & Recommendations

- **PAT-001**: [Pattern or best practice 1]
- **PAT-002**: [Pattern or best practice 2]
- **PAT-003**: [Pattern or best practice 3]

[Add more PAT entries as needed.]

## Risks & Open Questions

### Risks

- **RISK-001**: [Known risk and its impact/likelihood]
- **RISK-002**: [Risk]

### Open Questions

- **OPEN-001**: [Question where knowledge is incomplete]
- **OPEN-002**: [Question]

## Source & Provenance

- Derived from sessions:
  - [e.g. `.claude/sessions/2025-11-01-0838-modernize-red-dog-demo.md`]
- Related implementation plans:
  - [e.g. `plan/upgrade-system-command-4.md`]
- Related ADRs / external docs:
  - [Optional links or references]
