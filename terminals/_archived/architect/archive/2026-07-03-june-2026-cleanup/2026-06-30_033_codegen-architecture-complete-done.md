---
id: MSG-ARCHITECT-033
from: architect
to: root
type: done
priority: high
status: READ
ref: MSG-ARCHITECT-006
created: 2026-06-30
content_hash: 6e155ff2dcaf4333dd3f5a1b9ed97825afcbda400772bd4fcc57bc42493ae767
---

# DONE: Code Generator Toolchain — Architektúra Dokumentáció

## Összefoglaló

Elkészítettem a **CODEGEN_ARCHITECTURE.md** átfogó architektúra dokumentumot az ADR-050 alapján. A dokumentum tartalmazza a 4-fázisú code generator toolchain részletes architektúráját, MCP tool specifikációkat, SpaceOS custom patterns leírását és az integrációs pontokat.

## Elvégzett munka

### 1. Architektúra Dokumentum

**Fájl:** `docs/architecture/CODEGEN_ARCHITECTURE.md` (925+ sor)

**Tartalom:**

#### A) Rendszer Áttekintés
- 2 Mermaid diagram (High-level architecture + Data flow)
- OpenAPI → Generated Code → Runtime folyamat vizualizáció
- Komponensek és kapcsolataik részletes leírása

#### B) Phase 1-4 Részletes Architektúra

**Phase 1: Orval + NSwag** (IN PROGRESS)
- Teljes Orval config (orval.config.ts) Portal-hoz
- Teljes NSwag config (nswag.json) Orchestrator-hoz
- Generated code példák (React Query hooks)
- SpaceOS custom mutator (auth injection, error mapping)
- CI/CD workflow (GitHub Actions YAML)

**Phase 2: Wrapper Scripts** (PLANNED)
- 3 bash script specifikáció:
  - `generate-api-client.sh`
  - `generate-component.sh`
  - `generate-module.sh`
- Template directory struktúra

**Phase 3: SpaceOS CLI** (PLANNED)
- TypeScript CLI tool (commander.js)
- 3 parancs specifikáció: `spaceos generate api-client|component|module`
- Usage példák

**Phase 4: MCP Integration** (PLANNED)
- Mermaid workflow diagram (Terminal → MCP → Generators → Validation)
- `codegenEngine.ts` implementation logic
- Terminal workflow példák (Backend + Frontend)

#### C) MCP Tool Specifikáció

3 új MCP tool teljes TypeScript interface definíciója:

**1. generate_api_client**
```typescript
interface GenerateApiClientParams {
  source: 'kernel' | 'orchestrator';
  target: 'portal' | 'orchestrator';
  outputDir?: string;
}
```

**2. generate_component**
```typescript
interface GenerateComponentParams {
  name: string;
  category: 'feature' | 'ui' | 'layout';
  withTest: boolean;
  withStory: boolean;
  props?: PropertyDefinition[];
}
```

**3. generate_module**
```typescript
interface GenerateModuleParams {
  name: string;
  aggregate: string;
  states: string[];
  events?: string[];
  endpoints?: EndpointDefinition[];
}
```

**Plusz:** MCP tool registration pattern a `mcp.ts`-ben.

#### D) SpaceOS Custom Patterns

5 kritikus custom pattern dokumentálva:

1. **Auth/Tenant Header Injection**
   - Automatikus `X-Tenant-Id` és `Authorization` header
   - `X-User-Id` és `X-Session-Id` audit headers
   - Implementation kód Portal Axios interceptor-ral

2. **Error Mapping** (SpaceOS Codes → Magyar Hibaüzenetek)
   - 50+ SpaceOS error code → magyar üzenet mapping
   - `mapSpaceOsError()` függvény
   - Axios interceptor implementáció

3. **Audit Logging Pattern**
   - Minden API hívás naplózva (ki, mit, mikor, eredmény)
   - Backend C# middleware kód (`AuditMiddleware.cs`)
   - PostgreSQL audit log táblába írás

4. **RLS Context Propagation**
   - Row-Level Security PostgreSQL GUC változók
   - `DbConnectionInterceptor` implementáció
   - RLS policy példa

5. **Retry/Circuit Breaker**
   - Exponential backoff (3 retry max)
   - Circuit breaker (5 failure → open, 1 min timeout)
   - TypeScript `retryWithBackoff()` implementáció

#### E) Integrációs Pontok

4 integráció dokumentálva:

1. **CI/CD Workflow** (GitHub Actions)
   - Teljes `.github/workflows/api-codegen.yml`
   - 7 step pipeline: API startup → Generate → TypeCheck → Commit

2. **Knowledge Service MCP Server Bővítés**
   - File struktúra: `codegen/codegenEngine.ts`, `codegen/mcpTools.ts`
   - Integration flow diagram

3. **Terminal Workflow Integráció**
   - Backend Terminal workflow példa (Pricing module generálás)
   - Frontend Terminal workflow példa (FlowEpicCard component generálás)

4. **Validation és Error Handling**
   - Validation pipeline Mermaid diagram
   - 5 error type kezelése (API unreachable, TypeScript error, .NET build error, test failures, git conflict)
   - Error response format specifikáció

### 2. Implementációs Roadmap

4-fázis roadmap task bontással:
- Phase 1: 4 task (Kernel OpenAPI → Orval → NSwag → CI/CD)
- Phase 2: 5 task (3 wrapper script + 2 template)
- Phase 3: 5 task (CLI setup + 3 command + tests)
- Phase 4: 6 task (MCP tools + validation + integration + docs)

**Status tracking:** Minden task-hoz assignee + status (✅ DONE / 🔄 IN PROGRESS / 📋 TODO / 📋 PLANNED)

### 3. Knowledge Base Frissítés

**Fájl:** `docs/knowledge/INDEX.md`

Hozzáadva a COLD Tier (3m+ stabil alapok) szekcióhoz:
```
- [CODEGEN_ARCHITECTURE.md](../architecture/CODEGEN_ARCHITECTURE.md) — ADR-050 Code Generator Toolchain (4 fázis: Orval/NSwag → Scripts → CLI → MCP)
```

## ADR Kiegészítések

**Nincs szükség kiegészítő ADR-ekre:**
- Az ADR-050 megfelelően átfogó (decision + rationale + roadmap)
- A CODEGEN_ARCHITECTURE.md tartalmazza a részletes implementációs architektúrát
- Az MCP tool specifikáció elég részletes a CODEGEN_ARCHITECTURE.md-ben (nincs szükség ADR-051-re)
- A SpaceOS custom patterns jól dokumentáltak (nincs szükség ADR-052-re)

## Acceptance Criteria ✅

- [x] `docs/architecture/CODEGEN_ARCHITECTURE.md` elkészült
- [x] Mermaid diagramok minden fázishoz (2 High-level + 1 Phase 1 + 1 Phase 4 + 1 Validation + 1 Sequence)
- [x] MCP tool interfészek specifikálva (3 tool, TypeScript interfaces)
- [x] SpaceOS custom patterns dokumentálva (5 pattern, code examples)
- [x] Integration points leírva (4 integráció)
- [x] Knowledge base frissítve (INDEX.md)
- [x] DONE outbox az eredménnyel

## Kulcs Metrikák

| Metrika | Érték |
|---------|-------|
| **Dokumentum hossz** | 925+ sor (29k karakterek) |
| **Mermaid diagramok** | 6 db |
| **Code példák** | 15+ (TypeScript, C#, Bash, YAML) |
| **MCP tool interfészek** | 3 db (teljes spec) |
| **Custom patterns** | 5 db (implementation kóddal) |
| **Integration pontok** | 4 db |
| **Roadmap tasks** | 20 db (4 fázisra bontva) |

## Következő lépések (Backend Terminal)

**Phase 1 folytatása:**
1. Kernel OpenAPI XML docs kiegészítése (már ✅ DONE)
2. Portal Orval setup (🔄 IN PROGRESS)
3. Orchestrator NSwag setup (📋 TODO)
4. CI/CD integráció (📋 TODO)

**Blocker:** Kernel OpenAPI completeness már megvan (Backend DONE), így Orval setup indítható.

## Együttműködés

- **Backend Terminal:** Phase 1 implementáció (Orval + NSwag)
- **Explorer:** CODE_GENERATOR_CATALOGUE.md (kutatás kész) ✅
- **Librarian:** Knowledge base sync (INDEX.md frissítve) ✅

## Források

**Használt dokumentumok:**
- `docs/architecture/decisions/ADR-050-code-generator-toolchain.md` — Decision
- `docs/knowledge/patterns/CODE_GENERATOR_CATALOGUE.md` — 20 tool research
- `spaceos-nexus/knowledge-service/src/mcp.ts` — Existing MCP patterns

**Linked ADR-ek:**
- ADR-005: Walking Skeleton First
- ADR-041: Graph-Based Workflow (dependency gráf pattern)

---

**Architect Terminal**
2026-06-30 — MSG-ARCHITECT-006 COMPLETED
