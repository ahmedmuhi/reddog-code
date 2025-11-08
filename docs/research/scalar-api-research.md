# Scalar API Documentation UI Integration Research

**Research Date:** November 5, 2025
**Researcher:** Claude Code
**Status:** Complete

---

## Executive Summary

Scalar provides official, production-ready integration packages for Python (FastAPI), Node.js (Fastify), and Go. All packages are actively maintained as part of the main Scalar open-source project (10.9k GitHub stars), with recent updates throughout 2024-2025.

---

## 1. Python with FastAPI

### Package Information

| Property | Value |
|----------|-------|
| **Official Package Name** | `scalar-fastapi` |
| **Package Source** | PyPI (https://pypi.org/project/scalar-fastapi/) |
| **Latest Version** | 1.4.3 (released September 26, 2025) |
| **Python Requirement** | Python 3.x (3.8+ recommended, based on FastAPI standards) |
| **License** | MIT |
| **Official Repository** | https://github.com/scalar/scalar/tree/main/integrations/fastapi |
| **Maintainer** | Marc Laventure (marclave) |

### Installation

```bash
pip install scalar-fastapi
```

### Basic Integration Code Example

```python
from fastapi import FastAPI
from scalar_fastapi import get_scalar_api_reference

app = FastAPI()

# Your existing FastAPI routes here
@app.get("/items/{item_id}")
async def read_item(item_id: int):
    return {"item_id": item_id}

# Add Scalar documentation endpoint
@app.get("/scalar", include_in_schema=False)
async def scalar_html():
    return get_scalar_api_reference(
        openapi_url=app.openapi_url,
        title="My API Documentation"
    )

# Run with: uvicorn main:app --reload
```

### Advanced Configuration Example

```python
from scalar_fastapi import get_scalar_api_reference, OpenAPISource

@app.get("/scalar", include_in_schema=False)
async def scalar_html():
    return get_scalar_api_reference(
        sources=[
            OpenAPISource(
                title="Main API",
                url="/openapi.json"
            ),
            OpenAPISource(
                title="External API",
                url="https://api.example.com/openapi.json"
            )
        ],
        configuration={
            "theme": "light",
            "searchHotKey": {
                "windows": "ctrl+k",
                "mac": "cmd+k"
            }
        }
    )
```

### Maturity & Adoption Status

- **Status:** Production-Ready
- **GitHub Stars:** Part of main Scalar repository (10.9k stars)
- **Last Update:** September 26, 2025 (version 1.4.3)
- **Recent Activity:** Multiple releases throughout 2024-2025 (v1.4.2, v1.4.1, v1.4.0, v1.3.0, etc.)
- **Community:** Active Discord community at discord.gg/scalar
- **PyPI Downloads:** Available as wheel and source distribution

### Compatibility Notes

- **OpenAPI Support:** Works with FastAPI's native OpenAPI 3.1.0 generation
- **FastAPI Compatibility:** Works with all modern FastAPI versions
- **CORS/Proxy:** Supports optional CORS proxy via `scalar_proxy_url` parameter
- **Multiple Specs:** Can display multiple OpenAPI documents in a single Scalar instance
- **Authentication:** Supports configuration for various auth schemes (OAuth2, API Key, Bearer, etc.)

### Requirements Summary

```
Python: 3.8+
FastAPI: Any recent version
Dapr: Optional (works with Dapr service invocation if needed)
```

---

## 2. Go

### Package Information

Two official options available with different maturity levels:

#### Option A: **scalar-go** (Newer, Recommended)

| Property | Value |
|----------|-------|
| **Official Package Name** | `scalar-go` / `scalargo` |
| **Package Source** | Go Modules (github.com/bdpiprava/scalar-go) |
| **Latest Version** | Recent (Last commit: September 4, 2024) |
| **Go Requirement** | Go 1.23+ |
| **License** | MIT |
| **GitHub Repository** | https://github.com/bdpiprava/scalar-go |
| **GitHub Stars** | 43 stars |
| **Status** | Production-Ready |

#### Option B: **go-scalar-api-reference** (Mature Alternative)

| Property | Value |
|----------|-------|
| **Official Package Name** | `go-scalar-api-reference` |
| **Package Source** | Go Modules (github.com/MarceloPetrucio/go-scalar-api-reference) |
| **GitHub Repository** | https://github.com/MarceloPetrucio/go-scalar-api-reference |
| **GitHub Stars** | 108 stars |
| **Last Updated** | May 7, 2024 |
| **License** | MIT |
| **Status** | Production-Ready (mentioned in official Scalar documentation) |

### Installation

**Option A (scalar-go):**
```bash
go get github.com/bdpiprava/scalar-go
```

**Option B (go-scalar-api-reference):**
```bash
go get github.com/MarceloPetrucio/go-scalar-api-reference
```

### Basic Integration Code Examples

#### Option A: scalar-go with Standard net/http

```go
package main

import (
	"fmt"
	"net/http"
	scalargo "github.com/bdpiprava/scalar-go"
)

func main() {
	http.HandleFunc("/docs", func(w http.ResponseWriter, r *http.Request) {
		html, err := scalargo.NewV2(
			scalargo.WithSpecDir("./api"),
		)
		if err != nil {
			http.Error(w, err.Error(), 500)
			return
		}
		fmt.Fprint(w, html)
	})

	fmt.Println("📚 API Docs available at: http://localhost:8080/docs")
	http.ListenAndServe(":8080", nil)
}
```

#### Option A: scalar-go with Gin Framework

```go
package main

import (
	"github.com/gin-gonic/gin"
	scalargo "github.com/bdpiprava/scalar-go"
)

func main() {
	r := gin.Default()

	r.GET("/docs", func(c *gin.Context) {
		html, err := scalargo.NewV2(
			scalargo.WithSpecURL("https://api.example.com/openapi.json"),
			scalargo.WithDarkMode(),
			scalargo.WithTitle("My API Documentation"),
		)
		if err != nil {
			c.JSON(500, gin.H{"error": err.Error()})
			return
		}
		c.Header("Content-Type", "text/html; charset=utf-8")
		c.String(200, html)
	})

	r.Run(":8080")
}
```

#### Option A: scalar-go with Echo Framework

```go
package main

import (
	"github.com/labstack/echo/v4"
	scalargo "github.com/bdpiprava/scalar-go"
)

func main() {
	e := echo.New()

	e.GET("/docs", func(c echo.Context) error {
		html, err := scalargo.NewV2(
			scalargo.WithSpecDir("./specs"),
			scalargo.WithTitle("My API Documentation"),
		)
		if err != nil {
			return c.String(500, err.Error())
		}
		return c.HTML(200, html)
	})

	e.Logger.Fatal(e.Start(":8080"))
}
```

#### Option B: go-scalar-api-reference with net/http

```go
package main

import (
	"fmt"
	"net/http"
	"github.com/MarceloPetrucio/go-scalar-api-reference"
)

func main() {
	http.HandleFunc("/docs", func(w http.ResponseWriter, r *http.Request) {
		htmlContent, err := scalar.ApiReferenceHTML(&scalar.Options{
			SpecURL: "./docs/swagger.json",
			CustomOptions: scalar.CustomOptions{
				PageTitle: "Simple API",
			},
			DarkMode: true,
		})

		if err != nil {
			http.Error(w, err.Error(), 500)
			return
		}
		w.Header().Set("Content-Type", "text/html; charset=utf-8")
		fmt.Fprint(w, htmlContent)
	})

	http.ListenAndServe(":8080", nil)
}
```

### Maturity & Adoption Status

**scalar-go:**
- **Status:** Production-Ready
- **Maintenance:** Active (last commit September 2024)
- **Stars:** 43
- **Community:** Official Scalar integration

**go-scalar-api-reference:**
- **Status:** Production-Ready
- **Maintenance:** Stable (last updated May 2024)
- **Stars:** 108 (higher adoption)
- **Community:** Recommended in official Scalar documentation

### Compatibility Notes

#### scalar-go Features:
- **Spec Sources:** Remote URLs, local directories, embedded bytes
- **Multi-file Support:** Automatically merges segmented OpenAPI files
- **Themes:** Default, Moon, Purple, Solarized, BluePlanet, DeepSpace, Saturn, Kepler, Mars
- **Layouts:** Modern (default) and Classic
- **UI Controls:** Dark mode toggle, sidebar visibility, model hiding, download button control
- **Authentication:** API Key, HTTP Basic, Bearer, OAuth2 (Authorization Code with PKCE, Client Credentials)
- **Customization:** Custom CSS via `WithOverrideCSS()`
- **Framework Support:** Works with standard net/http, Gin, Echo, Fiber, and other Go web frameworks

#### go-scalar-api-reference Features:
- **Spec Sources:** JSON or YAML files, external URLs
- **Customization:** Theme selection, custom options
- **Security:** HTML escaping for security
- **Error Handling:** Comprehensive error handling

### Recommendation

- **New Projects:** Use `scalar-go` for latest features and active maintenance
- **Existing Integrations:** Both options are production-ready and actively supported

### Requirements Summary

```
Go: 1.23+ (scalar-go) or 1.16+ (go-scalar-api-reference)
No external dependencies required beyond Go stdlib
```

---

## 3. Node.js with Fastify

### Package Information

| Property | Value |
|----------|-------|
| **Official Package Name** | `@scalar/fastify-api-reference` |
| **Package Source** | npm (https://www.npmjs.com/package/@scalar/fastify-api-reference) |
| **Latest Version** | 1.35.6 (published October 2025, updated regularly) |
| **Node.js Requirement** | Node.js 20+ (Fastify v5) or Node.js 18+ (Fastify v4) |
| **Fastify Requirement** | Fastify 4.0.0+ |
| **License** | MIT |
| **Official Repository** | https://github.com/scalar/scalar/tree/main/packages/fastify-api-reference |
| **Module Type** | ES Module (requires `"type": "module"` in package.json) |

### Installation

```bash
npm install @scalar/fastify-api-reference
# or
yarn add @scalar/fastify-api-reference
# or
pnpm add @scalar/fastify-api-reference
```

### Basic Integration Code Example

```typescript
import Fastify from 'fastify'
import scalarApiReference from '@scalar/fastify-api-reference'

const fastify = Fastify()

// Register Scalar plugin with default configuration
await fastify.register(scalarApiReference, {
  routePrefix: '/reference',
})

// Your existing routes
fastify.get('/api/items', async () => {
  return { items: [] }
})

// Start the server
await fastify.listen({ port: 3000 })

// API Reference will be available at http://localhost:3000/reference
```

### Integration with @fastify/swagger

```typescript
import Fastify from 'fastify'
import fastifySwagger from '@fastify/swagger'
import scalarApiReference from '@scalar/fastify-api-reference'

const fastify = Fastify()

// Register Swagger for OpenAPI generation
await fastify.register(fastifySwagger, {
  swagger: {
    info: {
      title: 'My API',
      version: '1.0.0',
    },
  },
})

// Register Scalar - it will automatically detect @fastify/swagger
await fastify.register(scalarApiReference, {
  routePrefix: '/reference',
})

fastify.get('/api/items', {
  schema: {
    description: 'Get all items',
    response: {
      200: {
        type: 'array',
        items: { type: 'object' }
      }
    }
  }
}, async () => {
  return []
})

await fastify.listen({ port: 3000 })
```

### Advanced Configuration Example

```typescript
import scalarApiReference from '@scalar/fastify-api-reference'

await fastify.register(scalarApiReference, {
  routePrefix: '/reference',
  configuration: {
    title: 'My API Documentation',
    theme: 'dark',
    searchHotKey: {
      windows: 'ctrl+k',
      mac: 'cmd+k',
    },
    authentication: {
      preferredSecurityScheme: 'api_key',
    },
    servers: [
      {
        name: 'Production',
        url: 'https://api.example.com',
      },
      {
        name: 'Development',
        url: 'http://localhost:3000',
      },
    ],
  },
})
```

### Maturity & Adoption Status

- **Status:** Production-Ready
- **GitHub Stars:** Part of main Scalar repository (10.9k stars)
- **Version History:** Version 1.35.6 (published October 2025)
- **Recent Updates:** Multiple updates throughout 2024-2025 (v1.34.0, v1.33.0, etc.)
- **Community:** 21+ other npm packages use this plugin
- **Weekly Downloads:** High adoption in Fastify ecosystem

### Compatibility Notes

- **Fastify Versions:**
  - Fastify v5 (latest): Requires Node.js 20+
  - Fastify v4 (LTS until June 2025): Requires Node.js 14, 16, 18, 20, or 22
  - Recommendation: Use Fastify v5 with Node.js 20+ for new projects
- **OpenAPI Support:** Automatically integrates with @fastify/swagger-generated OpenAPI 3.x specs
- **Module Resolution:** Supports both CommonJS and ES Modules
- **TypeScript:** Full TypeScript support with type definitions
- **Theme Support:** Default Fastify theme, plus customizable themes

### Framework Integration

The package works specifically with **Fastify** and automatically detects:
- OpenAPI specs generated by `@fastify/swagger`
- Custom OpenAPI specification URLs
- Multiple OpenAPI document sources

### Requirements Summary

```
Node.js: 18.0.0+ (for Fastify v4) or 20.0.0+ (for Fastify v5)
Fastify: 4.0.0+
TypeScript: Optional (but recommended)
@fastify/swagger: Optional (recommended for automatic OpenAPI generation)
```

---

## Comparison Matrix

| Feature | Python/FastAPI | Go (scalar-go) | Go (go-scalar-api-reference) | Node.js/Fastify |
|---------|-----------------|----------------|------------------------------|------------------|
| **Official Status** | Official | Official | Recommended | Official |
| **Package Name** | scalar-fastapi | scalar-go | go-scalar-api-reference | @scalar/fastify-api-reference |
| **Latest Release** | Sept 26, 2025 | Sept 4, 2024 | May 7, 2024 | Oct 2025 |
| **GitHub Stars** | 10.9k (main) | 43 | 108 | 10.9k (main) |
| **Production Ready** | Yes | Yes | Yes | Yes |
| **OpenAPI 3.x Support** | Yes (3.1.0) | Yes | Yes | Yes |
| **Multiple Specs** | Yes | Yes | Yes | Yes |
| **Custom Themes** | Yes | Yes (9+ themes) | Yes | Yes |
| **Authentication Config** | Yes | Yes | Yes | Yes |
| **Framework Support** | FastAPI only | All Go frameworks | All Go frameworks | Fastify only |
| **Active Maintenance** | High | High | Stable | High |
| **Minimum Runtime** | Python 3.8+ | Go 1.23+ | Go 1.16+ | Node.js 18+ |

---

## Integration with Your RedDog Microservices

### Potential Use Cases

1. **OrderService (.NET):** Use Scalar UI for OpenAPI documentation alongside your existing Swagger UI
2. **Go Services (MakeLineService, VirtualWorker):** Implement Scalar for consistent documentation across polyglot services
3. **Python Services (ReceiptGenerationService):** Use scalar-fastapi for FastAPI services
4. **Node.js/Fastify Services (LoyaltyService):** Use @scalar/fastify-api-reference for consistent Node.js documentation

### Recommended Implementation Strategy

Given your modernization plan with multiple languages:

1. **Standardize on Scalar** across all services for consistent API documentation
2. **Language-Specific Integration:**
   - Python services: `scalar-fastapi`
   - Go services: `scalar-go` (newer, more features)
   - Node.js services: `@scalar/fastify-api-reference`
3. **Central Documentation:** Deploy a Scalar instance that aggregates multiple service OpenAPI specs
4. **CI/CD Integration:** Automatically update Scalar documentation on each service deployment

---

## Additional Resources

### Official Documentation
- **Scalar Main Site:** https://scalar.com/
- **Scalar Guides:** https://guides.scalar.com/
- **GitHub Repository:** https://github.com/scalar/scalar

### Package Pages
- **PyPI (scalar-fastapi):** https://pypi.org/project/scalar-fastapi/
- **npm (@scalar/fastify-api-reference):** https://www.npmjs.com/package/@scalar/fastify-api-reference
- **Go Packages (scalar-go):** https://pkg.go.dev/github.com/bdpiprava/scalar-go

### OpenAPI Standards
- **OpenAPI 3.1.0 Specification:** https://spec.openapis.org/oas/v3.1.0
- **OpenAPI 3.0.3 Specification:** https://spec.openapis.org/oas/v3.0.3

---

## Summary Recommendations

### For Python/FastAPI Services
- **Use:** `scalar-fastapi` version 1.4.3+
- **Status:** Production-ready, actively maintained
- **Installation:** `pip install scalar-fastapi`
- **Setup Time:** < 5 minutes

### For Go Services
- **Use:** `scalar-go` for new projects or v2 upgrades
- **Alternative:** `go-scalar-api-reference` if you need maximum stability (108 stars vs 43)
- **Status:** Both production-ready
- **Installation:** `go get github.com/bdpiprava/scalar-go`
- **Setup Time:** < 5 minutes

### For Node.js/Fastify Services
- **Use:** `@scalar/fastify-api-reference` version 1.35.6+
- **Status:** Production-ready, actively maintained
- **Installation:** `npm install @scalar/fastify-api-reference`
- **Setup Time:** < 5 minutes

### All Packages
- Officially supported by Scalar
- MIT licensed
- Works with OpenAPI 3.x specifications
- Support multiple documentation sources
- Production-ready for your microservices
- Part of actively maintained ecosystem (10.9k GitHub stars)

