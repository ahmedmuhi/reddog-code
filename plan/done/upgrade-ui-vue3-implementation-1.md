---
goal: Upgrade RedDog UI from Vue 2.x (Vue CLI) to Vue 3.x (Vite + Node.js 24) with modern tooling and CI compliance
version: 1.0
date_created: 2025-11-06
last_updated: 2025-11-06
owner: Red Dog Modernization Team
status: Planned
tags: [upgrade, vue3, frontend, nodejs, ui]
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

This implementation plan executes Phase 2 of `plan/modernization-strategy.md`, upgrading the Vue.js UI from Vue 2.6/Vue CLI to Vue 3.5/Vite on Node.js 24. The plan aligns with `plan/cicd-modernization-strategy.md` (GitHub Actions), `plan/testing-validation-strategy.md` (tooling + validation artifacts), and `docs/research/dotnet-upgrade-analysis.md` (overall modernization context).

## Research References

- `plan/modernization-strategy.md` (Phase 2 Vue modernization goals)
- `plan/testing-validation-strategy.md` (Tool installation requirements, validation artifacts, CI prerequisites)
- `plan/cicd-modernization-strategy.md` (GitHub Actions workflow expectations for Node/Vite)
- External: Vue 3 Migration Guide, Vite documentation

## 1. Requirements & Constraints

- **REQ-001**: UI must target Vue 3.5+, Vue Router 4, Pinia (if state store used), and build with Vite.
- **REQ-002**: Node.js runtime set to 24 LTS; `package.json`/`package-lock.json` must enforce `"engines": { "node": ">=24.0.0" }`.
- **REQ-003**: TypeScript support enabled (per modernization plan) with strict mode.
- **REQ-004**: ESlint + Prettier configuration updated to Vue 3 recommended presets.
- **SEC-001**: `npm audit --production` must report zero HIGH/CRITICAL vulnerabilities (artifact stored).
- **CON-001**: Build pipeline must run in GitHub Actions using Node 24 and Vite build commands; old Vue CLI scripts removed.
- **GUD-001**: Composition API and script setup syntax preferred over Options API for new components.
- **PAT-001**: `.env` handling via Vite conventions (prefix `VITE_`).

## 2. Implementation Steps

### Implementation Phase 1 – Tooling & Dependency Baseline

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-000** | Update `.nvmrc`/`.node-version` (if present) and `package.json` `engines` to Node 24; install Node 24 locally via `nvm install 24`. | | |
| **TASK-001** | Capture dependency baseline: run `npm outdated --long > artifacts/ui/npm-outdated.txt` and `npm audit --json > artifacts/ui/npm-audit.json`. | | |
| **TASK-002** | Remove Vue CLI scaffolding dependencies (`@vue/cli-service`, `vue-cli-plugin-*`, webpack configs). | | |
| **TASK-003** | Install Vite + Vue 3 stack: `npm install vue@^3.5.0 vue-router@^4 pinia@latest @vitejs/plugin-vue vite@latest typescript@latest @types/node@latest`. | | |
| **TASK-004** | Initialize Vite config: run `npm create vite@latest` in temp directory, copy `vite.config.ts`, `tsconfig.json`, and scripts into project; update `package.json` scripts to `vite`, `vite build`, `vite preview`. | | |
| **TASK-005** | Configure eslint/prettier for Vue 3: `npm install -D eslint@latest @vue/eslint-config-typescript @vue/eslint-config-prettier prettier` and update `.eslintrc.cjs`. | | |
| **TASK-006** | Run `npm install` to refresh `package-lock.json`, then re-run `npm audit --production` and save to `artifacts/ui/npm-audit-postinstall.json`. | | |
| **TASK-007** | Update GitHub Actions workflow to use `actions/setup-node@v4` with `node-version: 24.x` and run `npm ci`, `npm run build`, `npm run test`. | | |

**Completion Criteria (Phase 1):** Node 24 enforced; Vite tooling installed; dependency/audit artifacts stored under `artifacts/ui/`; GitHub Actions workflow updated to Node 24 + Vite build.

### Implementation Phase 2 – Code Migration & Validation

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-008** | Convert `src/main.js` to `src/main.ts` using Vue 3 `createApp`, register Router 4, Pinia. Remove deprecated Vue 2 APIs (e.g., `new Vue`). | | |
| **TASK-009** | Update component syntax to Composition API or `<script setup>`; replace `this` usage with `const state = reactive(...)` as needed. | | |
| **TASK-010** | Migrate Vue Router configuration to Router 4 with history mode (`createWebHistory`). | | |
| **TASK-011** | Refactor global state from Vuex (if used) to Pinia stores. | | |
| **TASK-012** | Replace Webpack-specific asset imports/env variables with Vite equivalents (`import.meta.env.VITE_*`). | | |
| **TASK-013** | Update unit/e2e tests (Jest/Vitest/Cypress) to Vue 3 compatible tooling; install `vitest` + `@vue/test-utils` for Vue 3 if not already present. | | |
| **TASK-014** | Run local validation: `npm run lint`, `npm run test`, `npm run build`; capture outputs to `artifacts/ui/npm-run-report.md`. | | |
| **TASK-015** | Execute GitHub Actions workflow in feature branch to ensure Node 24 + Vite builds succeed; attach run logs to `artifacts/ui/github-actions-run.md`. | | |

**Completion Criteria (Phase 2):** UI builds via Vite; lint/test/build succeed locally and in CI; artifacts recorded; Vue 2 specific code removed.

## 3. Alternatives

- **ALT-001**: Keep Vue CLI/Webpack — rejected due to modernization strategy and Vite performance requirements.
- **ALT-002**: Delay Pinia migration — rejected; state store must align with Vue 3 best practices.

## 4. Dependencies

- `plan/modernization-strategy.md` (Phase 2 Vue modernization)
- `plan/cicd-modernization-strategy.md` (CI workflow updates)
- `plan/testing-validation-strategy.md` (tooling artifacts + validation)

## 5. Files

- `package.json`, `package-lock.json`
- `src/main.ts`, component files in `src/`
- `vite.config.ts`, `tsconfig.json`, `.eslintrc.*`, `.prettierrc.*`
- GitHub workflow(s) under `.github/workflows/`
- Artifact outputs under `artifacts/ui/`

## 6. Testing

- `npm run lint`
- `npm run test` (unit tests via Vitest/Jest)
- `npm run build` (Vite production build)
- Optional end-to-end tests (Cypress/Playwright) if already present

## 7. Risks & Assumptions

- **RISK-001**: Legacy Vue 2 libraries may lack Vue 3 support — mitigation: replace with Vue 3 compatible packages during migration.
- **RISK-002**: Lack of existing TypeScript definitions — mitigation: add typing gradually, use `any` only as stopgap.
- **ASSUMPTION-001**: UI tests (unit/e2e) will be updated to run under Node 24 + Vite toolchain.

## 8. Related Specifications / Further Reading

- `plan/modernization-strategy.md` (Phase 2)
- `plan/cicd-modernization-strategy.md`
- `plan/testing-validation-strategy.md`
- Vue official migration guide: https://v3-migration.vuejs.org/
- Vite documentation: https://vitejs.dev/guide/
