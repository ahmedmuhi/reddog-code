---
id: KI-SHELL-SCRIPTING-STANDARDS-001
title: Shell Scripting Best Practices & Standards
tags:
  - shell
  - bash
  - scripting
  - automation
  - standards
last_updated: 2025-11-24
source_sessions: []
source_plans: []
confidence: high
status: Active
owner: Platform Engineering
notes: Applies to all shell scripts (*.sh) in the repository
---

# Summary

This Knowledge Item defines the standard patterns, safety practices, and conventions for writing shell scripts in the Red Dog repository. It covers bash, sh, zsh, and other shells, with emphasis on error handling, structured data parsing, and maintainability for automation and testing workflows.

## Key Facts

- **FACT-001**: All shell scripts must use `set -euo pipefail` to fail fast on errors, catch unset variables, and surface pipeline failures.
- **FACT-002**: Shell scripts are primarily for automation and testing rather than production systems unless explicitly specified.
- **FACT-003**: Modern Bash features (`[[ ]]`, `local`, arrays) are preferred when portability requirements allow; POSIX constructs are used only when needed.
- **FACT-004**: Structured data (JSON, YAML) must be parsed using dedicated tools (`jq`, `yq`) rather than ad-hoc text processing with `grep`, `awk`, or shell string splitting.
- **FACT-005**: Temporary files and directories must be created using `mktemp` and cleaned up via `trap` handlers on script exit.
- **FACT-006**: All scripts must start with a clear shebang (`#!/bin/bash` unless specified otherwise) and include a header comment explaining the script's purpose.
- **FACT-007**: Variable references must be double-quoted (`"$var"`) and use `${var}` syntax for clarity; `eval` must be avoided.

## Constraints

- **CON-001**: All required parameters must be validated before script execution begins.
- **CON-002**: Scripts must provide clear error messages with context when failures occur.
- **CON-003**: Immutable values must be declared with `readonly` (or `declare -r`) to prevent accidental reassignment.
- **CON-004**: Parser dependencies (`jq`, `yq`, or alternatives) must be documented at the top of the script and fail fast with a helpful message if required tools are missing.
- **CON-005**: Parser errors from `jq`/`yq` must be treated as fatal; scripts must test command success before using results.
- **CON-006**: Echo output must be concise and provide execution status; avoid excessive logging or unnecessary output.
- **CON-007**: Cleanup handlers registered via `trap` must handle temporary resources, ensuring they are removed even on unexpected exits.

## Patterns & Recommendations

- **PAT-001**: Structure scripts with default values defined at the top, functions for reusable code blocks, and a clean main execution flow at the bottom.
- **PAT-002**: Use `trap cleanup EXIT` pattern to register cleanup handlers that remove temporary resources or perform teardown steps.
- **PAT-003**: Implement a `usage()` function that displays help text with script options and exits cleanly when `-h` or `--help` is provided.
- **PAT-004**: Validate requirements in a dedicated `validate_requirements()` function before executing main logic.
- **PAT-005**: Parse command-line arguments using a `while [[ $# -gt 0 ]]` loop with `case` statements for clarity.
- **PAT-006**: When parsing JSON with `jq`, validate that required fields exist, handle missing data paths explicitly (e.g., `// empty`), quote filters to prevent shell expansion, and prefer `--raw-output` for plain strings.
- **PAT-007**: When parsing YAML with `yq`, convert to JSON first if possible, or use `yq`'s native output modes with the same validation and quoting practices as `jq`.
- **PAT-008**: Create reusable functions instead of repeating similar blocks of code; keep functions focused and single-purpose.
- **PAT-009**: Use modern Bash test syntax `[[ ]]` instead of `[ ]` for conditional expressions when targeting bash/zsh environments.
- **PAT-010**: Add comments where helpful for understanding how the script works, but avoid over-commenting obvious operations.
- **PAT-011**: Use `shellcheck` for static analysis when available to catch common pitfalls and antipatterns.

## Risks & Open Questions

### Risks

- **RISK-001**: Scripts without `set -euo pipefail` may silently fail or continue executing with invalid state, causing hard-to-diagnose issues.
- **RISK-002**: Ad-hoc text processing of JSON/YAML with `grep`/`awk`/`sed` is fragile and breaks when data structure changes or contains unexpected whitespace/special characters.
- **RISK-003**: Unquoted variable expansions can cause word splitting and globbing, leading to incorrect behavior or security issues.
- **RISK-004**: Scripts without cleanup handlers may leave temporary files or directories behind, consuming disk space or leaking sensitive data.
- **RISK-005**: Using `eval` introduces code injection risks and makes scripts difficult to reason about.

### Open Questions

- **OPEN-001**: Should the repository standardize on a specific minimum version of bash/zsh, or continue supporting POSIX sh for maximum portability?
- **OPEN-002**: Should scripts include a version check or banner to indicate they require `jq`/`yq` before attempting any operations?

## Source & Provenance

- Derived from: Shell scripting guidelines instruction set
- Related ADRs: None (cross-cutting standard)
- External references:
  - [ShellCheck](https://www.shellcheck.net/) for static analysis
  - [Google Shell Style Guide](https://google.github.io/styleguide/shellguide.html)
  - Bash manual for `set` options and modern features
