# ADR-045: McpServer Standard Tools & RPC Interface

> **Státusz:** PROPOSED
> **Dátum:** 2026-06-17
> **Forrás:** MSG-ARCH-010 (Root konzultáció)
> **Tervdokumentum:** `docs/tasks/new/MCP_Integration_Plan_v1.md`

---

## Kontextus

A SpaceOS terminálok és Marvin agent-ek McpServer tool-okkal kommunikálnak. Jelenleg:
- `discovery_search` — Knowledge Service keresés (Phase 1 COMPLETE)

**Hiányzó toolok:**
- Artifact submission (idea/consensus fájl regisztráció)
- Workflow state tracking (FSM lifecycle)
- RBAC-aware tool visibility

A CLAUDE.md fájlok emberi szabályokat tartalmaznak ("ne hívd ezt a tool-t"), de nincs gépi enforcement.

---

## Döntés

**McpServer toolkit kiterjesztése 4 új tool-lal + RbacFilter implementálás.**

### Tool katalógus

| Tool | Leírás | Fázis |
|---|---|---|
| `discovery_search` | Knowledge base keresés | ✅ COMPLETE |
| `submitArtifact` | Idea/consensus fájl regisztráció | Fázis 2 |
| `getWorkflowState` | Terminál FSM state lekérdezés | Fázis 3 |
| `updateWorkflowState` | Terminál FSM state frissítés | Fázis 3 |
| **RbacFilter** | Tool visibility per role | Fázis 3 |

---

## Tool specifikációk

### 1. submitArtifact

```typescript
{
  name: "submitArtifact",
  description: "Idea vagy consensus fájl regisztrálása a planning pipeline-ban.",
  inputSchema: {
    filePath: string,           // relatív path: 'docs/planning/ideas/...'
    artifactType: "idea" | "selected" | "consensus",
    metadata: {
      domain?: string,          // 'manufacturing', 'sales', stb.
      priority?: "high" | "medium" | "low",
      segment?: string          // scanner segment neve
    }
  },
  output: {
    success: boolean,
    artifactId: string,         // UUID
    registeredAt: string        // ISO timestamp
  }
}
```

**Használat:** Scanner/Selector agent regisztrálja a létrehozott fájlokat.

### 2. getWorkflowState

```typescript
{
  name: "getWorkflowState",
  description: "Terminál aktuális workflow állapot lekérdezése.",
  inputSchema: {
    terminal: string            // 'kernel', 'joinery', 'cutting', stb.
  },
  output: {
    terminal: string,
    state: "initialized" | "briefed" | "in_progress" | "awaiting_input" |
           "ready_to_submit" | "submitted" | "failed",
    currentTask?: string,       // MSG-ID ha van
    lastStateChange: string,    // ISO timestamp
    blockedReason?: string      // ha awaiting_input
  }
}
```

**Használat:** Nightwatch/Conductor terminál állapot monitoring.

### 3. updateWorkflowState

```typescript
{
  name: "updateWorkflowState",
  description: "Terminál workflow állapot frissítése.",
  inputSchema: {
    terminal: string,
    newState: "initialized" | "briefed" | "in_progress" | "awaiting_input" |
              "ready_to_submit" | "submitted" | "failed",
    taskId?: string,            // MSG-ID
    reason?: string             // state change indoklás
  },
  output: {
    success: boolean,
    previousState: string,
    newState: string,
    changedAt: string
  }
}
```

**Használat:** Terminál session lifecycle tracking.

### 4. RbacFilter

```typescript
// Nem tool, hanem middleware
class RbacFilter {
  private roleToolMap: Record<string, string[]> = {
    'scanner': ['discovery_search'],
    'selector': ['discovery_search', 'submitArtifact'],
    'debater': ['discovery_search'],
    'reviewer': ['discovery_search', 'getWorkflowState'],
    'conductor': ['discovery_search', 'getWorkflowState', 'updateWorkflowState', 'submitArtifact'],
    'root': ['*']  // all tools
  };

  filterTools(role: string, availableTools: Tool[]): Tool[] {
    const allowed = this.roleToolMap[role] || [];
    if (allowed.includes('*')) return availableTools;
    return availableTools.filter(t => allowed.includes(t.name));
  }
}
```

**Használat:** MCP server tool listing — role alapján szűrt tool lista.

---

## Alternatívák értékelése

| Alternatíva | Értékelés | Miért nem |
|---|---|---|
| **CLAUDE.md emberi szabályok** | ❌ ELÉGTELEN | Nincs enforcement. Terminál "elfelejthet" szabályt. |
| **Tool-ban if/else role check** | ❌ ELVETETT | Szétszórt, nehéz maintenance. |
| **RbacFilter middleware** | ✅ VÁLASZTOTT | Centralizált, tool listing szinten szűr. |
| **Keycloak role integration** | ❌ OVERKILL | Terminálok nem user session-ök. Lokális role mapping elég. |

---

## Indoklás

### 1. Gépi enforcement > emberi szabályok

```
CLAUDE.md (ma):
  "Backend terminál NEM hívhat frontend tool-okat"
  → Nincs enforcement, terminál "elfelejthet"

RbacFilter (jövő):
  tools = rbacFilter.filterTools('kernel', allTools)
  → 'kernel' role nem kapja a 'deployFrontend' tool-t a listában
  → Nem tudja meghívni, mert nem is látja
```

### 2. Workflow state tracking

```
Bash (ma):
  tmux capture-pane -t spaceos-kernel -p | grep "DONE"
  → Törékeny regex, nincs state history

WorkflowStateTracker (jövő):
  getWorkflowState('kernel') → { state: 'in_progress', task: 'MSG-KERNEL-057' }
  → Strukturált, query-elhető, audit trail
```

### 3. Artifact registration

```
Bash (ma):
  echo "$IDEA" > docs/planning/ideas/2026-06-17_001_new-idea.md
  → Nincs tracking, nincs metadata

submitArtifact (jövő):
  submitArtifact({ filePath: '...', artifactType: 'idea', metadata: {...} })
  → Registered, UUID assigned, queryable
```

---

## Golden Rule ellenőrzés

| Szabály | Ellenőrzés | Státusz |
|---|---|---|
| **Data → Rules → Geometry** | Tools paramétereket kezelnek, nem számítanak | ✅ OK |
| **Modular Monolith** | MCP interface decoupled, tool-onként izolált logika | ✅ OK |
| **Immutability & Trust** | WorkflowState append-only log, artifact registration immutable | ✅ OK |
| **Need-to-Know RBAC** | **RbacFilter** — ez az ADR fő témája | ✅ ADDRESSED |
| **Walking Skeleton First** | Fázisonként bővülő toolkit, nem big bang | ✅ OK |

---

## Kritikus függőségek

| Függőség | Blokkoló? | Megoldás |
|---|---|---|
| Knowledge Service operational | **HIGH** | ✅ Phase 1 COMPLETE |
| Marvin agent integration | MEDIUM | ADR-043 — parallel track |
| SQLite state storage | LOW | Lightweight, no new dependency |

---

## Fázis lebontás

### Fázis 1: Knowledge Search (COMPLETE ✅)
- [x] `discovery_search` tool

### Fázis 2: Artifact Management (~2-3 nap)
- [ ] `submitArtifact` tool implementation
- [ ] Artifact registry (SQLite or JSON file)
- [ ] Scanner/Selector integration

### Fázis 3: Workflow Tracking + RBAC (~4-5 nap)
- [ ] `getWorkflowState` tool
- [ ] `updateWorkflowState` tool
- [ ] WorkflowStateTracker class (SQLite backend)
- [ ] RbacFilter middleware
- [ ] Role → tool mapping configuration
- [ ] Nightwatch/Conductor integration

---

## Technikai specifikáció

### MCP Server config

```json
// ~/.claude/settings.json
{
  "mcpServers": {
    "spaceos-nexus": {
      "command": "node",
      "args": ["/opt/spaceos/spaceos-nexus/mcp-server/dist/index.js"],
      "env": {
        "ROLE": "${TERMINAL_ROLE}",
        "VOYAGE_API_KEY": "${VOYAGE_API_KEY}"
      }
    }
  }
}
```

### Role mapping

| Terminál | Role | Available tools |
|---|---|---|
| plan-scan.sh (Haiku) | scanner | discovery_search |
| plan-select.sh (Sonnet) | selector | discovery_search, submitArtifact |
| plan-debate.sh (Sonnet) | debater | discovery_search |
| reviewer.sh (Haiku) | reviewer | discovery_search, getWorkflowState |
| nightwatch.sh | conductor | ALL |
| Root session | root | ALL |
| Architect session | architect | discovery_search |
| Kernel/Joinery/stb. | developer | discovery_search |

### State storage

```sql
-- SQLite: spaceos-nexus/data/workflow.db
CREATE TABLE workflow_states (
    id INTEGER PRIMARY KEY,
    terminal TEXT NOT NULL,
    state TEXT NOT NULL,
    task_id TEXT,
    reason TEXT,
    changed_at TEXT NOT NULL,
    UNIQUE(terminal, changed_at)
);

CREATE TABLE artifacts (
    id TEXT PRIMARY KEY,  -- UUID
    file_path TEXT NOT NULL UNIQUE,
    artifact_type TEXT NOT NULL,
    domain TEXT,
    priority TEXT,
    segment TEXT,
    registered_at TEXT NOT NULL
);
```

---

## Rollback terv

```bash
# Új toolok kikapcsolása
# MCP server config: tool registration comment out
# Terminálok: fallback bash működik

# RbacFilter kikapcsolása
# All tools visible to all roles (current behavior)
```

Backward compatible — új toolok optional, meglévő működés érintetlen.

---

## Megvalósító terminál

**NEXUS** (spaceos-nexus/) — MCP toolkit fejlesztés
- Tool implementations
- RbacFilter middleware
- SQLite state storage

**INFRA** — MCP server deployment
- Settings registration
- Role environment variables

---

## Referenciák

- Tervdokumentum: `docs/tasks/new/MCP_Integration_Plan_v1.md`
- JoineryTech.McpServer: `spaceos-nexus/mcp-server/src/`
- WorkflowStateTracker reference: JoineryTech.McpServer `src/metadata/`
- RbacFilter reference: JoineryTech.McpServer `src/mcp/`
