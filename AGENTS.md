# Repository Guidelines

## Project Structure & Module Organization
RedDog.sln collects the .NET microservices under the `RedDog.*` directories (core services plus `RedDog.AccountingModel`, Bootstrapper, and the virtual customer/worker simulators). The Vue dashboard lives in `RedDog.UI`, decision records and standards in `docs/`, deployment assets in `manifests/branch`, and helper scripts in `tools/`. Keep build artifacts in `artifacts/` and never commit `bin/` or `obj/`.

## Build, Test, and Development Commands
- `dotnet restore RedDog.sln` — pulls packages and workloads with the .NET 10.0.100 SDK pinned by `global.json`.
- `dotnet build RedDog.sln -c Release && dotnet test RedDog.sln --no-build` — compiles every project with analyzers/ApiCompat then executes the suites.
- `dapr run --app-id orderservice --app-port 5100 -- dotnet run --project RedDog.OrderService` — canonical pattern for starting a service with its sidecar; swap ids/ports to match the target service.
- `npm install && npm run serve --prefix RedDog.UI` (plus `npm run lint --prefix RedDog.UI`) — manage Vue dependencies and run the dev server.

## Coding Style & Naming Conventions
Use 4-space indentation and newline braces for C#. Apply PascalCase to namespaces, types, DTOs, and Vue component names; keep locals/parameters camelCase and prefix interfaces with `I`. Whenever public APIs change, update `PublicAPI.Unshipped.txt` and promote entries to `PublicAPI.Shipped.txt` after release so the analyzer stays clean. Follow `docs/standards/web-api-standards.md`: define OpenAPI first, expose `/scalar`, and rely on Dapr pub/sub instead of cross-service database calls. Run `npm run lint --prefix RedDog.UI` to honor the repo’s ESLint rules.

## Testing Guidelines
Testing remains a known gap (`plan/testing-validation-strategy.md`), so every contribution must ship coverage. Create xUnit projects named `RedDog.<Service>.Tests` beside the service, mirror namespaces, and keep tests deterministic; execute `dotnet test RedDog.sln --no-build` before pushing. Use `dapr run` to provision dependencies for integration smoke tests and drop logs, traces, or k6 result files under `artifacts/` (performance evidence goes in `artifacts/performance/`). Vue work should add Vue Test Utils specs when possible or, at minimum, record manual steps plus `npm run lint --prefix RedDog.UI` and screenshots.

## Commit & Pull Request Guidelines
Commits follow the existing `area: summary` pattern (`docs: add adr for dapr config`, `orderservice: guard null hubId`) and stay scoped. PRs must link an issue, list verification commands (build/test/lint/dapr), call out new env vars or Dapr components, and include UI screenshots when relevant. CODEOWNERS routes reviews to `@Azure/cloud-native-gbb`, so open drafts early and expect the CLA check.
