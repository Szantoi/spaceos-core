---
id: epic-mcp-maintenance-16
title: "Epic 16: Legacy Tool Refactor — fájl-alapú eszközök DB-wrapper-ré"
type: epic
milestone: M03
project: mcp-maintenance
project_id: mcp-context-server
status: COMPLETE
fsm_workflow_id: "agile-epic-lifecycle-v1"
fsm_state: "DONE"
fsm_retry_count: 0
completed: 2026-03-12
created: 2026-03-04
assignee: backend_developer
depends_on: EPIC-09
---

# 🔄 EPIC-16: Legacy Tool Refactor — fájl-alapú eszközök DB-wrapper-ré

## Célkitűzés

Az MCP szerver M01-ben épített fájlrendszer-alapú eszközeit (`get_role`, `get_workflow`,
`get_template`, `get_core`, `list_roles`) **backward-compatible DB-wrapper-ré alakítani**,
amelyek belülről az `agent.db`-t hívják — a külső API nem változik.

**Végállapot:** A szerver `database/` mappa jelenléte nélkül is teljes értékűen kiszolgál.

---

## Kontextus és Motiváció

M02 után két párhuzamos kiszolgálási útvonal él:

```
[régi]  get_role(domain, role) → fájlrendszer olvasás → database/roles/.../role.md
[új]    bootstrap_agent(domain, role) → agent.db lekérdezés → roles tábla
```

Ez inkonzisztenciát okoz: ha a seeder frissül, de a fájl nem, a két útvonal eltérő
tartalmat ad vissza. Az `agent.db` az egyetlen igazság — a régi útvonalat be kell zárni.

---

## Érintett Eszközök

| Tool | Jelenlegi forrás | Célállapot |
|:-----|:-----------------|:-----------|
| `get_role` | `database/roles/<d>/<r>/<r>.role.md` olvasás | `AgentDb.getRole(domain, role)` |
| `get_workflow` | `database/roles/<d>/<r>/workflows/*.workflow.md` olvasás | `AgentDb.getWorkflow(domain, role, type)` |
| `get_template` | `database/roles/<d>/<r>/templates/*.template.md` olvasás | `AgentDb.getTemplate(domain, role, name)` |
| `get_core` | `database/standards/**/*.md` olvasás | `AgentDb.getStandard(std_id)` |
| `list_roles` | `database/roles/` mappa szkennelés | `AgentDb.listRoles(domain?)` |
| `search_knowledge` | ChromaDB query (változatlan API) | ChromaDB szinkronizálva `agent.db`-vel |

---

## Megközelítés — Backward-Compatible Wrapper

A tool signature-ok **nem változnak** — csak a belső implementáció vált DB-re:

```typescript
// Előtte
async function get_role({ domain, role }: GetRoleArgs) {
  const filePath = path.join(DATABASE_ROOT, 'roles', domain, role, `${role}.role.md`);
  return fs.readFile(filePath, 'utf-8');
}

// Utána
async function get_role({ domain, role }: GetRoleArgs) {
  const record = agentDb.getRole(domain, role);
  if (!record) throw new Error(`Role not found: ${domain}/${role}`);
  return record.content;
}
```

---

## ChromaDB Szinkronizáció

A `search_knowledge` tool ChromaDB-t használ, amely `.knowledge.md` fájlokból indexel.
Ezt nem kell megváltoztatni, de a szinkronizációt explicitten kezelni kell:

- A seeder (`seed-agent-db.ts`) futtatásakor a ChromaDB is újraindexelődik.
- Hozzáadandó: `seed-agent-db.ts` végén `indexKnowledgeBase()` meghívása.
- Eredmény: egy `npm run seed` parancs mindkét tárolót szinkronizálja.

---

## Feladatok

| Típus | ID | Feladat | Becslés | Állapot |
|:-:|:---|:--------|:--------|:--------|
| `Arch` | T13-01 | Azon tool-ok listázása `mcpServer.ts`-ben, amelyek fájlrendszert hívnak; refaktor-terv dokumentálása | 0.5 nap | ✅ DONE |
| `Dev`  | T13-02 | `get_role`, `list_roles`, `list_domains` → `AgentDb` DB-first + file fallback | 0.5 nap | ✅ DONE |
| `Dev`  | T13-03 | `get_workflow`, `get_template`, `list_templates` → `AgentDb` DB-first + file fallback | 0.5 nap | ✅ DONE |
| `Dev`  | T13-04 | `get_core` (runbook + standards) → `AgentDb.getRunbook()` / `AgentDb.getStandard()` DB-first | 0.5 nap | ✅ DONE |
| `Dev`  | T13-05 | `seed-agent-db.ts` kiterjesztése: ChromaDB szinkronizáció (deferred, in-scope for EPIC-18) | 0.5 nap | ⏭️ Deferred |
| `QA`   | T13-06 | Unit tesztek: 19 DB-first teszt (DocumentServer.test.ts) — all GREEN | 0.5 nap | ✅ DONE |
| `QA`   | T13-07 | Regression teszt: DocumentServer without agentDb → file-system fallback intact | 0.5 nap | ✅ DONE |

**Összesített becslés:** ~4 nap

---

## Technológiai döntések

| Döntés | Választás | Indok |
|:-------|:----------|:------|
| Wrapper vs. új tool | Wrapper (azonos tool name) | Backward-compatible — meglévő agent prompt-ok nem törnek |
| Hiba kezelés | `throw McpError(NOT_FOUND)` ha DB-ben nincs | Konzisztens az MCP protokoll hibakezeléssel |
| Seeder + ChromaDB szinkron | Egyetlen `npm run seed` szkript | Nem kell két lépés az admin frissítéshez |

---

## Definition of Done

- [x] `DocumentServer` DB-first wrapperrel fut: `getRole`, `getWorkflow`, `getTemplate`, `getCore`, `listTemplates`, `listRoles`, `listDomains`.
- [x] `AgentDb` rendelkezik minden szükséges metódussal + új: `listDomains()`, `listRoleNames(domain)`.
- [x] File-system fallback minden metódusban megmarad — graceful degradation.
- [x] `index.ts`: `DocumentServer` megkapja az `agentDb` instance-t 4. paraméterként.
- [x] 19 unit teszt zöld: DB-first viselkedés + file fallback + no-agentDb regression (24/24 összesen).
- [ ] `npm run seed` lefuttatása után ChromaDB és SQLite egyaránt friss (EPIC-18 scope).

---

## Implementáció Összefoglaló

**Módosított fájlok:**

| Fájl | Változás |
|:-----|:---------|
| `src/mcp/AgentDb.ts` | +`listDomains()`, +`listRoleNames(domain)` |
| `src/mcp/DocumentServer.ts` | DB-first refactor: `AgentDb` 4. ctor param; `getRole`, `getWorkflow`, `getTemplate`, `listTemplates`, `getCore`, `listRoles`, `listDomains` |
| `src/index.ts` | `new DocumentServer(..., agentDb)` — wiring |
| `src/tests/unit/DocumentServer.test.ts` | +19 EPIC-16 DB-first unit tests |

---

## Blokkolók / Kockázatok

| Kockázat | Valószínűség | Mitigáció |
|:---------|:-------------|:----------|
| Meglévő tesztek fájl-fixture-öket használnak | Közepes | T13-06 feltárja; fixture-ök DB-alapúra cserélhetők |
| `standards` tábla hiányos a seederből | Alacsony | EPIC-09 már kezeli — seeder tölti a `standards` táblát |
