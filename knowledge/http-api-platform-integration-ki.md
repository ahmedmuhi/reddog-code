---
id: KI-HTTP-API-PLATFORM-INTEGRATION-001
title: HTTP API Platform Integration (CORS, Edge Security, Rate Limiting)
tags:
  - http
  - api
  - platform
  - cors
  - security
  - rate-limiting
  - dapr
last_updated: 2025-11-09
source_sessions:
  - .claude/sessions/2025-11-01-0838.md
source_plans:
  - plan/modernization-strategy.md
confidence: high
status: active
owner: "Ahmed Muhi"
notes: >
  Captures cross-cutting platform concerns for HTTP APIs: browser access (CORS),
  internal service-to-service security, external API key usage, and rate limiting.
---

# Summary

This Knowledge Item defines how Red Dog HTTP APIs integrate with the platform
for browser access, internal security, and external access control. It focuses
on CORS configuration, Dapr-based service-to-service security, API keys for
external clients, and rate limiting.

It applies to all HTTP APIs that are called from the Vue.js UI or exposed
externally, across all services and languages.

## Key Facts

- **FACT-201**: All HTTP APIs that are called from the Vue.js UI must enable
  CORS via application-level middleware (not cloud-provider-specific CORS).
- **FACT-202**: Allowed origins are centrally configured via the Dapr
  Configuration API (`reddog.config` store, `allowedOrigins` key) in the
  target state; environment variables are currently used as a temporary
  workaround until ADR-0004 is implemented.
- **FACT-203**: Service-to-service HTTP calls inside the mesh use Dapr
  service invocation with built-in mTLS; services should not talk directly
  to each other over raw HTTP for internal calls.
- **FACT-204**: External clients authenticate using API keys stored in the
  Dapr secret store (per ADR-0002), not hard-coded in code or committed
  configuration files.
- **FACT-205**: Rate limiting is required for internet-exposed APIs and is
  provided primarily via platform scaling (e.g. KEDA) plus, where needed,
  application-level rate limiting.

## Constraints

- **CON-201**: CORS for browser-facing APIs must be implemented in the
  application (e.g. ASP.NET Core middleware, FastAPI middleware, Express
  middleware). Relying solely on cloud ingress CORS rules is not allowed.
- **CON-202**: Allowed CORS origins must be driven from configuration
  (`reddog.config` → `allowedOrigins` or the agreed environment variable)
  rather than being hard-coded. All environments (local, dev, prod) must
  use this mechanism.
- **CON-203**: Internal service-to-service traffic must use Dapr service
  invocation, which provides mTLS by default. Direct HTTP calls to another
  service’s pod IP, NodePort, or load balancer are not permitted for normal
  internal use.
- **CON-204**: API keys for external clients must be retrieved from the Dapr
  secret store (e.g. `reddog.secretstore`) at runtime. They must not be
  stored in source control, Docker images, or static config files.
- **CON-205**: External HTTP APIs must apply rate limiting (platform and/or
  application-level) so that abusive or misconfigured clients cannot
  overwhelm services.

## Patterns & Recommendations

- **PAT-201**: CORS configuration
  - Store allowed origins in Dapr configuration (`reddog.config`, key
    `allowedOrigins`) as a comma-separated list per environment.
  - At service startup, read `allowedOrigins` from Dapr config (or from a
    documented environment variable until ADR-0004 is fully implemented),
    split into a list, and feed it into the CORS middleware.
  - Apply a named policy (e.g. `AllowUI`) and ensure CORS middleware runs
    before routing in the HTTP pipeline.

- **PAT-202**: Internal service security with Dapr
  - For one service calling another, use Dapr’s service invocation:
    - .NET: `DaprClient.CreateInvokeHttpClient("target-service")`
    - Other languages: equivalent Dapr client abstractions.
  - Treat Dapr’s mTLS as the standard mechanism for internal auth; do not
    add ad-hoc API keys for purely internal calls unless explicitly required.

- **PAT-203**: External API key handling
  - Store external API keys in the Dapr secret store (`reddog.secretstore`).
  - On startup or per request, retrieve the API key from the secret store.
  - Validate incoming requests using a dedicated header (e.g. `X-API-Key`)
    or equivalent agreed mechanism.
  - Fail with `401 Unauthorized` or `403 Forbidden` using Problem Details
    responses when the key is missing or invalid.

- **PAT-204**: Rate limiting and autoscaling
  - Use KEDA or equivalent autoscaling based on request rate, queue depth, or
    other relevant metrics.
  - For fine-grained HTTP-level protection, apply application-level rate
    limiting (e.g. ASP.NET Core rate limiting middleware or equivalent).
  - Keep rate limiting policies simple and documented; avoid per-endpoint
    micro-policies unless strictly necessary.

- **PAT-205**: Environment-specific configuration
  - Use Dapr configuration and secret stores for values that differ between
    environments (origins, keys, scaling thresholds).
  - Use environment variables only as a transitional or fallback mechanism,
    and keep the mapping documented in ADR-0002/0004/0006.

## Risks & Open Questions

- **RISK-201**: Hard-coded CORS origins or ad-hoc CORS logic can diverge
  between services and environments, leading to intermittent browser issues
  and security drift.
- **RISK-202**: Bypassing Dapr for internal HTTP calls removes mTLS and
  trace propagation guarantees, making the system less secure and harder to
  observe.
- **RISK-203**: Incorrect API key handling (e.g. keys in code, logs, or
  client-side JavaScript) can lead to credential leaks.
- **RISK-204**: Lack of rate limiting for public endpoints can make services
  vulnerable to abuse, accidental DoS, or cost overruns.

- **OPEN-201**: The exact shape of per-environment rate limiting policies
  (limits per minute, burst values) may evolve; this KI does not mandate
  concrete numbers.
- **OPEN-202**: Whether some internal administrative endpoints can skip
  CORS (non-browser clients only) remains a product decision; any such
  exception must be explicitly documented and kept internal.

## Sources & Provenance

- **SRC-201**: `docs/standards/web-api-standards.md`
  - Sections: CORS Configuration, Authentication & Authorization.
- **SRC-202**: `adr/adr-0002-cloud-agnostic-configuration-via-dapr.md`
- **SRC-203**: `adr/adr-0004-dapr-configuration-api-standardization.md`
- **SRC-204**: `adr/adr-0006-infrastructure-configuration-via-environment-variables.md`
