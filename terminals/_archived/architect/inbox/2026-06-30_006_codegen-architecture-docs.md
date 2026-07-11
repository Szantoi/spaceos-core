---
id: MSG-ARCHITECT-006
from: root
to: architect
type: task
priority: high
status: READ
model: sonnet
created: 2026-06-30
processed: 2026-06-30
ref: ADR-050
content_hash: 30b81f1597694475c37558d7166a65676463a8970d6f172869bc617fcbe48d62
---

# Code Generator Toolchain — Architektúra Dokumentáció

## Kontextus

ADR-050 alapján 4 fázisú code generator toolchain-t vezetünk be, amely MCP integrációval zárul. Az Architect feladata a teljes architektúra dokumentálása.

## Feladat

### 1. Architektúra Dokumentum Készítése

**Fájl:** `docs/architecture/CODEGEN_ARCHITECTURE.md`

**Tartalom:**

#### A) Rendszer Áttekintés
- 4 fázis diagram (Mermaid)
- Komponensek és kapcsolataik
- Adatfolyam (OpenAPI → Generated Code → Runtime)

#### B) Phase 1-4 Részletes Architektúra

**Phase 1: Orval + NSwag**
```
┌─────────────┐     ┌──────────────┐     ┌─────────────┐
│   Kernel    │────▶│ OpenAPI Spec │────▶│   Orval     │
│  (.NET 8)   │     │   (JSON)     │     │  (Portal)   │
└─────────────┘     └──────────────┘     └─────────────┘
                           │
                           ▼
                    ┌──────────────┐
                    │    NSwag     │
                    │(Orchestrator)│
                    └──────────────┘
```

**Phase 4: MCP Integration**
```
┌─────────────┐     ┌──────────────┐     ┌─────────────┐
│  Terminal   │────▶│ MCP Server   │────▶│  Codegen    │
│  (Claude)   │     │ (Knowledge)  │     │  Engine     │
└─────────────┘     └──────────────┘     └─────────────┘
                           │
                           ▼
                    ┌──────────────┐
                    │  Generated   │
                    │    Files     │
                    └──────────────┘
```

#### C) MCP Tool Specifikáció

Definiáld a 3 új MCP tool interfészét:

```typescript
interface GenerateApiClientParams {
  source: 'kernel' | 'orchestrator';
  target: 'portal' | 'orchestrator';
  outputDir?: string;
}

interface GenerateComponentParams {
  name: string;
  category: 'feature' | 'ui' | 'layout';
  withTest: boolean;
  withStory: boolean;
  props?: PropertyDefinition[];
}

interface GenerateModuleParams {
  name: string;
  aggregate: string;
  states: string[];
  events?: string[];
  endpoints?: EndpointDefinition[];
}
```

#### D) SpaceOS Custom Patterns

Dokumentáld a testreszabásokat:
- Auth/Tenant header injection
- Error mapping (SpaceOS codes → messages)
- Audit logging pattern
- RLS context propagation
- Retry/Circuit breaker

#### E) Integrációs Pontok

- CI/CD workflow (GitHub Actions)
- Knowledge Service MCP server bővítés
- Terminal workflow integráció
- Validation és error handling

### 2. ADR Kiegészítések

Ha szükséges, javasolj kiegészítő ADR-eket:
- ADR-051: MCP Codegen Tool Specification (ha komplex)
- ADR-052: SpaceOS Component Template Standard (ha kell)

### 3. Knowledge Base Integráció

Frissítsd:
- `docs/knowledge/INDEX.md` — új dokumentum hozzáadása
- `docs/knowledge/patterns/` — ha új pattern keletkezik

## Elfogadási Kritériumok

- [ ] `docs/architecture/CODEGEN_ARCHITECTURE.md` elkészült
- [ ] Mermaid diagramok minden fázishoz
- [ ] MCP tool interfészek specifikálva
- [ ] SpaceOS custom patterns dokumentálva
- [ ] Integration points leírva
- [ ] Knowledge base frissítve
- [ ] DONE outbox az eredménnyel

## Kapcsolódó Dokumentáció

- `docs/architecture/decisions/ADR-050-code-generator-toolchain.md`
- `docs/knowledge/patterns/CODE_GENERATOR_CATALOGUE.md`
- `spaceos-nexus/knowledge-service/src/mcp.ts` — jelenlegi MCP toolok

## Együttműködés

- **Backend Terminal:** Phase 1 implementáció (Orval + NSwag)
- **Explorer:** CODE_GENERATOR_CATALOGUE.md (kutatás kész)
- **Librarian:** Knowledge base sync
