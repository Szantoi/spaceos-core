---
id: project-mcp-maintenance
title: "Project: MCP System Maintenance"
type: project
status: active
track: delivery
parent_program: agent-system-v2
created: 2026-02-27
last_updated: 2026-02-27
---

# 🔧 Project: MCP System Maintenance

Ez a projekt az Agent System MCP szerverének karbantartási feladatait fogja össze: RBAC jogosultságok konzisztenciája, szerver higiénia, és teszt lefedettség.

## Kontextus

Az MCP szerver (`src/agent-system/server/`) a rendszer központi kiszolgálója, amely a következő modulokat tartalmazza:

- **MCP Tools** (`src/mcp/`): DocumentServer, RbacFilter, mcpServer, mcpRouter
- **RAG** (`src/rag/`): VectorStore, indexKnowledgeBase
- **Metadata** (`src/metadata/`): WorkflowStateTracker, ResourceTracker
- **Roles** (`src/roles/`): RoleLoader, GuardrailService

## Audit Eredmények (2026-02-27)

### 🔴 P1: RBAC `mcp_tool_permissions` Hiány

15 schema YAML fájlból csak 1 (`backend_developer`) tartalmazza az `mcp_tool_permissions` mezőt.
A többi 14 role visszaesik public-only szintre → korlátozottan érheti el az MCP eszközöket.

**Érintett domének**: discovery (8 role), engineering (3 role), management (2 role), agentops (1 role)

**Hatás**: A meglévő RBAC e2e teszt (`mcp-rbac.test.ts`) az Explorer role-tól elvárja a `get_role`, `get_workflow`, `list_templates` eszközöket, de a schema nem definiálja → az e2e teszt elbukik.

### 🟡 P2: Szerver Gyökér — Maradvány Fájlok

Az alábbi fájlok mind **teszt és debug maradványok** igazoltan:

| Fájl | Git-tracked? | Eredet | Commit |
|:-----|:-------------|:-------|:-------|
| `test.ts` | ✅ Igen | Ad-hoc smoke teszt (POST /api/execute) | `5022b48` (2026-02-25) |
| `mock-test-out.txt` | ✅ Igen | LLM Evaluator teszt kimenet | korábbi |
| `mock-test-output.txt` | ✅ Igen | LLM Evaluator teszt kimenet | korábbi |
| `tsc_output.txt` | ✅ Igen | TypeScript fordítási napló | korábbi |
| `playwright-results.json` | ✅ Igen | Playwright e2e teszt eredmények | korábbi |
| `response_epic03.json` | ✅ Igen | LLM válasz debug napló (EPIC-03) | korábbi |
| `index_error.log` | ❌ Nem | RAG indexelési napló | lokális |
| `start_err.log` | ❌ Nem | Szerver indítási hiba napló | lokális |
| `start_error.log` | ❌ Nem | Szerver indítási hiba napló | lokális |
| `metadata.db` | ❌ Nem | SQLite workflow state DB (runtime) | lokális |

**Javasolt intézkedés**:

1. Git-tracked fájlok eltávolítása a git történetből (`git rm --cached`)
2. Untracked fájlok törlése
3. `.gitignore` bővítése ezekre a mintákra

### 🟢 P3: TypeScript Kompilációs Warning

A `tsc_output.txt` tartalmából és a `start_err.log` tartalmából kiderül, hogy a `zod` v4 csomag `esModuleInterop` flag hiányából fakadó warning-okat generál. Ez nem blokkoló, de a `tsconfig.json`-ban érdemes engedélyezni.

## Milestone Terv

### M01: RBAC és Higiénia

| Epic | Fókusz | Státusz |
|:-----|:-------|:--------|
| EPIC-01 | RBAC `mcp_tool_permissions` hozzáadása mind a 14 hiányzó schema-hoz | planned |
| EPIC-02 | Szerver gyökér takarítás és `.gitignore` frissítés | planned |

## Kapcsolódó Projektek

- [mcp-rbac](file:///c:/Users/szant/Documents/Development/JoineryTech.Flow/docs/joinerytech-flow/agent-system-v2/mcp-rbac/) — Az RBAC rendszer implementációjának dokumentációja
- [workflow-state](file:///c:/Users/szant/Documents/Development/JoineryTech.Flow/docs/joinerytech-flow/agent-system-v2/workflow-state/) — Workflow state tracker projekt
