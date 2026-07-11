---
id: epic-mcp-maintenance-17
title: "Epic 17: Multi-Domain Configuration & Onboarding"
type: epic
milestone: M03
project: mcp-maintenance
project_id: mcp-context-server
status: done
fsm_workflow_id: "agile-epic-lifecycle-v1"
fsm_state: "DONE"
fsm_retry_count: 0
created: 2026-03-04
assignee: backend_developer
depends_on: EPIC-09, EPIC-10
---

# 🌐 EPIC-17: Multi-Domain Configuration & Onboarding

## Célkitűzés

Az MCP szerver jelenleg a `database/roles/` mappahoz kötött (engineering, discovery, management).
EPIC-17 feladata: **domain-agnostic szerver** megvalósítása, amely:

- Konfigurálható domain-eket támogat (e.g., "product", "data-science", "compliance")
- Runtime-on domain-context cserélhető (`switch_domain()` tool)
- Onboarding flow: új domain → seeder automatically betölti az `agent.db`-be
- Multi-tenant-safe: domains egymást nem zavarják

---

## Kontextus és Motiváció

M02 után az `agent.db` egyetlen igazság a roles + workflows-hez. De a seeder még a
`database/roles/` fájlrendszerre függ. EPIC-17 feladata:

1. **Domain Registry**: Adatbázisban tárolt domain-definíciók (nem hardcoded)
2. **Runtime Config**: Aktuális domain-context beállítható (agent-request header vagy env var)
3. **Onboarding**: Új domain hozzáadásakor `register_domain("domain-name", domain_config)` tool
4. **Isolation**: Domains elszigeteltek (query-k domain-filtered)

---

## Érintett Komponensek

| Komponens | Jelenlegi | Target M03-ben |
|:----------|:----------|:----------:|
| Domain definitions | Hardcoded (engineering, discovery, management) | `agent.db` domains table |
| Current domain context | Global (mcp env var) | Per-agent (request header + session) |
| Seeder | YAML files only | Plug-in architecture (YAML, JSON, API) |
| RBAC filtering | Hard-coded domain strings | Query-based (WHERE domain = ?) |
| Multi-domain support | ❌ nincs | ✅ Dynamic registration |

---

## Érintett MCP Tool-ok (New/Modified)

| Tool | Purpose |
|:-----|:--------|
| `register_domain(name, config)` | Admin tool: új domain regisztrálása |
| `switch_domain(domain_name)` | Agent tool: aktuális context domain-ja módosítása |
| `list_available_domains()` | Query tool: az ágent számára elérhető domains |
| `get_domain_config(domain_name)` | Admin tool: domain konfigurációjának lekérdezése |

---

## Database Schema Additions

```sql
-- Domain registry
CREATE TABLE domains (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL UNIQUE,
  description TEXT,
  config_json TEXT, -- domain-specific config
  created_at TIMESTAMP,
  updated_at TIMESTAMP
);

-- Domain + Role mapping (1:many)
ALTER TABLE roles ADD COLUMN domain_id TEXT REFERENCES domains(id);

-- Session domain context (per-agent)
ALTER TABLE sessions ADD COLUMN current_domain_id TEXT REFERENCES domains(id);
```

---

## Sikerkritérium (Definition of Done)

- [x] `domains` SQLite table létrehozva + seeded
- [x] `bootstrap_agent()` alapértelmezett domain-t betölt agent role-ja alapján
- [x] `switch_domain(domain_name)` MCP tool elérhető (admin-only)
- [x] `list_available_domains()` MCP tool elérhető
- [x] RBAC query-k automatikusan domain-filtered (WHERE domain_id = current_domain_id)
- [x] Multi-domain E2E teszt: 2 domain → roles betöltve → switch → correct tools visible
- [x] Onboarding dokumentáció: new domain hozzáadásának lépései
- [x] Zero breaking changes az existing agent API-hoz
- [x] Session isolation: agent1 switch A → agent2 B → no cross-contamination

---

## Nem Scope (EPIC-17-ben)

- Multi-organization support (separate databases) — M05 feladata
- Domain federation (cross-server domains) — future
- Real-time domain synchronization — mcp-pm-engine integration (M04)

---

## Task Breakdown

- [x] TASK-17-01: Domain schema design + SQLite migration (task file ready)
- [x] TASK-17-02: Domain seeder (YAML → agent.db) (task file ready)
- [x] TASK-17-03: `bootstrap_agent()` modification: domain context (task file ready)
- [x] TASK-17-04: `switch_domain()` + `list_available_domains()` tools (task file ready)
- [x] TASK-17-05: RBAC query filtering by domain (task file ready)
- [x] TASK-17-06: Multi-domain E2E test (task file ready)
- [x] TASK-17-07: Onboarding documentation (task file ready)

**Task file status / impl. status:**

- [x] TASK-17-01 ✅ IMPLEMENTED — `008_epic17_domain_schema.sql`, `AgentDb.upsertDomain/listRegisteredDomains/getRegisteredDomain`, unit tests (`domain-schema.test.ts`)
- [x] TASK-17-02 ✅ IMPLEMENTED — `AgentDbSeeder.ts`, `seedDomains()`, `upsertDomain()`, `index.ts` startup hook, unit tests (`domain-seeder.test.ts`)
- [x] TASK-17-03 ✅ IMPLEMENTED — `SessionManager.current_domain_id`, `McpContext.domain_id`, `bootstrap_agent` domain-id resolve + fallback, unit tests (`bootstrap-domain-context.test.ts`)
- [x] TASK-17-04 ✅ IMPLEMENTED — `switch_domain` + `list_available_domains` tools in `BootstrapPlugin`, admin-only guard + 404 handling, unit tests (`domain-switch-tools.test.ts`)
- [x] TASK-17-05 ✅ IMPLEMENTED — `AgentDb.getRole/getWorkflow/getTemplate` (+list variants) domain_id-aware WHERE filtering with backward-compatible null handling, unit tests (`domain-rbac-filtering.test.ts`)
- [x] TASK-17-06 ✅ IMPLEMENTED — MCP HTTP integration test: dual-session isolation + admin switch + forbidden non-admin (`multi-domain-e2e.test.ts`)
- [x] TASK-17-07 ✅ IMPLEMENTED — onboarding and operational usage guide (`usage-guide.md`)

---

## Függőségek

| Függőség | Állapot | Hatás |
|:---------|:--------|:------|
| EPIC-09: SQLite schema | ✅ M02 | Domains table szükséges |
| EPIC-10: bootstrap_agent() | ✅ M02 | Domain-aware bootstrap |
| EPIC-16: Legacy tool refactor | 🔶 M03 előfeltétel | RBAC queries erre épülnek |

---

## Kapcsolódó Dokumentáció

- `database/roles/` — Current domain structure
- Multi-tenant best practices: [docs/](Docs/mcp-context-server/)
- EPIC-16 db-first architecture implications
