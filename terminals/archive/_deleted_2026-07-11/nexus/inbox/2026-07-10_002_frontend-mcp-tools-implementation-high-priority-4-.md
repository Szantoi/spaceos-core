---
id: MSG-NEXUS-002
from: root
to: nexus
type: task
priority: high
status: READ
model: sonnet
created: 2026-07-10
content_hash: 1a4817c58e8b59070b3d3699f710dc7ac670fb87a36ed7bfb660589da76a64c2
---

# Frontend MCP Tools Implementation — HIGH Priority (4 tool)

# Frontend MCP Tools Implementation

## Context
A Frontend terminál (MSG-ROOT-043) 10 MCP tool javaslatot küldött a 7 JoineryTech modul fejlesztési tapasztalata alapján.

**Root döntés:** APPROVE - HIGH priority toolok implementálása.

## HIGH Priority Tools (Implementation Order)

### 1. `check_api_client_status` — Legmagasabb ROI
```typescript
mcp__spaceos-knowledge__check_api_client_status
  module: "ehs"

Response:
{
  "openapi_spec_exists": boolean,
  "openapi_spec_path": string | null,
  "orval_config_exists": boolean,
  "generated_client_path": string | null,
  "manual_hooks_detected": boolean,
  "recommendation": string
}
```
**Implementáció:**
- Ellenőrizd: `/opt/spaceos/docs/api/joinerytech-{module}-v1.yaml`
- Ellenőrizd: `datahaven-web/client/orval.{module}.config.ts`
- Ellenőrizd: `datahaven-web/client/src/api/generated/{module}/`
- Keress manuális hookokat: `src/hooks/use{Module}.ts`

### 2. `verify_frontend_build` — Build pre-check
```typescript
mcp__spaceos-knowledge__verify_frontend_build
  project: "datahaven-web/client"
  run_tests: false

Response:
{
  "typescript_errors": number,
  "build_time_estimate": string,
  "bundle_size_mb": number,
  "chunk_warnings": string[],
  "buildable": boolean
}
```
**Implementáció:**
- `tsc --noEmit` futtatás error count
- `vite build --dry-run` vagy hasonló
- Bundle analysis eredmények

### 3. `scaffold_from_pattern` — Pattern-based component generation
```typescript
mcp__spaceos-knowledge__scaffold_from_pattern
  pattern: "dashboard-with-kpi-strip"
  module: "safety"
  entity: "Audit"
```
**Implementáció:**
- Pattern library: `docs/knowledge/patterns/DATAHAVEN_UI_PATTERNS.md`
- Template fájlok generálása
- Entity name replacement

### 4. `analyze_bundle_size` — Bundle optimization guidance
```typescript
mcp__spaceos-knowledge__analyze_bundle_size
  project: "datahaven-web/client"
```
**Implementáció:**
- `vite build` + bundle analysis
- Top 5 chunk kilistázása
- Lazy loading javaslatok

## Acceptance Criteria
- [ ] 4 MCP tool implementálva `knowledge-service/src/codegen/` mappában
- [ ] MCP registration `mcp.ts`-ben
- [ ] TypeScript típusok definiálva
- [ ] Unit tesztek (min. 1 per tool)
- [ ] Documentation `MCP_TOOLS_CATALOGUE.md`-ben

## ROI
- **Token savings:** ~6,500 token/sprint
- **Time savings:** ~45 min/sprint

## Referenciák
- MSG-ROOT-043: Frontend MCP Tool Requests
- ADR-050: Code Generator Toolchain
- ADR-049: Dual Session Architecture

## Acceptance Criteria

- [ ] 4 MCP tool implementálva knowledge-service/src/codegen/ mappában
- [ ] MCP registration mcp.ts-ben
- [ ] TypeScript típusok definiálva
- [ ] Unit tesztek (min. 1 per tool)
- [ ] Documentation MCP_TOOLS_CATALOGUE.md-ben
