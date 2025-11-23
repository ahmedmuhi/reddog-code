# KI-HTTP-JSON-NAMING-001: JSON Naming and Contract Policy

## Summary

This Knowledge Item defines the **JSON naming rules** for Red Dog HTTP APIs and related JSON contracts.
It separates internal language conventions (e.g. C# PascalCase) from on-the-wire JSON conventions (camelCase) and ensures consistent, language-neutral contracts.

It applies to all JSON returned or consumed by Red Dog HTTP APIs and UI clients.

---

## Applies To

* All HTTP APIs exposed by Red Dog services
* JSON request and response bodies (including error payloads)
* API clients:

  * Vue.js UI
  * External client applications
* DTOs, models, and serializers used at HTTP boundaries in:

  * .NET
  * Go
  * Python
  * Node.js

---

## Key Facts

1. **FACT-001:** **All JSON field names on the wire must use camelCase.**

   * This applies to success responses, error responses, and any JSON-based health/metadata endpoints exposed to clients.

2. **FACT-002:** Internal language conventions (e.g. **PascalCase** properties in C#) are **decoupled** from JSON naming.

   * Mapping is required at the HTTP boundary.

3. **FACT-003:** RFC 7807 **Problem Details** responses use camelCase for JSON fields in Red Dog APIs, even though the RFC’s examples are language-neutral.

4. **FACT-004:** For .NET services, `System.Text.Json` is the standard JSON serializer and must be configured to emit camelCase by default for HTTP APIs.

5. **FACT-005:** For non-.NET services (Go, Python, Node.js), API DTOs and serialization settings must be chosen such that the emitted JSON fields follow the same **camelCase** convention.

6. **FACT-006:** JSON naming is part of the **public contract** of the API; once published and used by clients, field names must be treated as stable and require deprecation procedures to change.

---

## Constraints

1. **CON-001:** New HTTP endpoints **must not** expose JSON with PascalCase, snake_case, or kebab-case field names to clients.

2. **CON-002:** Service implementations **must not rely** on serializer defaults that conflict with the agreed naming policy. The naming strategy must be explicitly configured where necessary.

3. **CON-003:** For .NET HTTP APIs:

   * `System.Text.Json` must be configured with `JsonNamingPolicy.CamelCase` (or equivalent) for property names (and dictionary keys where appropriate).
   * Alternative serializers are not allowed unless a future ADR explicitly approves them.

4. **CON-004:** Error responses, including Problem Details, **must use** the same camelCase field naming as normal responses.

5. **CON-005:** When JSON is used as part of inter-service contracts (e.g. message payloads in pub/sub that are also consumed by external clients), the same camelCase rule applies unless explicitly documented otherwise.

6. **CON-006:** JSON field name changes in existing contracts **must not** be made without:

   * A documented deprecation path,
   * Versioning strategy (e.g. new API version),
   * Or a compensating client-side compatibility layer.

---

## Patterns and Recommendations

1. **PAT-001 – .NET global JSON naming configuration**

   Configure JSON naming globally in HTTP pipeline startup:

   ```csharp
   builder.Services.AddControllers()
       .AddJsonOptions(options =>
       {
           options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
           options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
       });
   ```

   * Apply this in a shared startup or base API registration to avoid divergent configurations.

2. **PAT-002 – DTO design in C#**

   * Use PascalCase for C# class and property names, following .NET conventions:

     ```csharp
     public sealed record OrderSummary(
         string OrderId,
         string CustomerId,
         decimal TotalPrice);
     ```
   * Rely on the global camelCase policy (or `[JsonPropertyName("fieldName")]` attributes) to map to:

     ```json
     {
       "orderId": "...",
       "customerId": "...",
       "totalPrice": 123.45
     }
     ```

3. **PAT-003 – JSON naming in other languages**

   * **Node.js / TypeScript**:

     * Use camelCase property names directly in DTOs and objects.
     * Ensure any frameworks (e.g. NestJS, Express) do not transform names away from camelCase.
   * **Go**:

     * Use struct tags to map to camelCase:

       ```go
       type OrderSummary struct {
           OrderID     string  `json:"orderId"`
           CustomerID  string  `json:"customerId"`
           TotalPrice  float64 `json:"totalPrice"`
       }
       ```
   * **Python**:

     * Use Pydantic or dataclasses + custom encoders to emit camelCase where defaults differ.

4. **PAT-004 – Problem Details and error responses**

   * Define a shared Problem Details type in each language that maps to camelCase fields:

     * `type`, `title`, `status`, `detail`, `instance`, `traceId`, etc.
   * Enforce `application/problem+json` content type and camelCase naming for error payloads.

5. **PAT-005 – Contract-first mindset**

   * Treat OpenAPI definitions as the **source of truth** for JSON naming.
   * When adding fields:

     * First define the field name in OpenAPI (camelCase),
     * Then map the implementation to match.

---

## Risks and Open Questions

1. **RISK-001 – Inconsistent naming across services**

   * If some services emit PascalCase or snake_case, clients will see inconsistent contracts and may need special handling.
   * Mitigation: automated contract checks (schema tests, OpenAPI diffs) and shared templates for startup configuration.

2. **RISK-002 – Hidden serializer defaults**

   * If individual services rely on framework defaults instead of explicit configuration, future library upgrades might change naming behavior.
   * Mitigation: explicit configuration in startup; avoid multiple serializers with different defaults.

3. **RISK-003 – Breaking changes to existing contracts**

   * Renaming fields in existing APIs without versioning can silently break clients.
   * Mitigation: use URL versioning + deprecation headers, or compatible evolution (add new fields, keep old ones until retired).

4. **OPEN-001 – Non-HTTP internal payloads**

   * Some internal-only payloads (e.g. pure internal pub/sub events not consumed by external clients) may consider alternative naming if strongly justified.
   * Any deviation must be documented and coordinated with observability and tooling.

5. **OPEN-002 – Code generation and tooling**

   * As code generators and tools evolve, they might introduce new defaults.
   * New tools or generators must be validated to ensure they adhere to camelCase on-wire naming.

---

## Sources and Provenance

* Red Dog HTTP Web API Standard (`web-api-standard.md`)
* RFC 7807 – Problem Details for HTTP APIs
* .NET `System.Text.Json` documentation and naming policy guidelines
* Red Dog OpenAPI + Scalar documentation practices
